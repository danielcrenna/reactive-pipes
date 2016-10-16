using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using reactive.pipes.Consumers;
using reactive.pipes.Extensions;

namespace reactive.pipes.Producers
{
    /// <summary>
    /// A default producer implementation that produces on a background thread
    /// <remarks>
    /// - The production queue is seeded explicitly by callers, or by subscribing to an observable
    /// - The producer-consumer problem requires a shared buffer; you must give full control of the buffer to the producer, and pass it to any consumers
    /// - Backlogged and undeliverable events are managed by other consumers you can attach; by default, all special case event handling is sent into the abyss
    /// </remarks> 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class BackgroundThreadProducer<T> : IProduce<T>, IDisposable
    {
        private int _sent;
        private int _undelivered;

        private Task _background;
        private CancellationTokenSource _cancel;
        private readonly SemaphoreSlim _empty;
        private readonly Stopwatch _uptime;

        private IConsume<T> _consumer;
        private IConsume<T> _backlogConsumer;
        private IConsume<T> _undeliverableConsumer;

        public int MaxDegreeOfParallelism { get; set; }

        public TimeSpan Uptime => _uptime.Elapsed;

        public int Sent => _sent;

        public double Rate => _sent / _uptime.Elapsed.TotalSeconds;

        public int Queued => Buffer.Count;

        public int Undeliverable => _undelivered;

        public bool Running { get; private set; }

        public Action OnStarted { get; set; }

        public Action OnStopped { get; set; }

        public RetryPolicy RetryPolicy { get; }

        public RateLimitPolicy RateLimitPolicy { get; }

        public BlockingCollection<T> Buffer { get; private set; }

        public BackgroundThreadProducer(IObservable<T> source) : this()
        {
            Produce(source);

            OnStarted = () => { };
            OnStopped = () => { };
        }

        public BackgroundThreadProducer() : this(new BlockingCollection<T>()) { }

        public BackgroundThreadProducer(int capacity) : this(new BlockingCollection<T>(capacity)) { }

        public BackgroundThreadProducer(IProducerConsumerCollection<T> source) : this(new BlockingCollection<T>(source)) { }

        public BackgroundThreadProducer(IProducerConsumerCollection<T> source, int capacity) : this(new BlockingCollection<T>(source, capacity)) { }

        public BackgroundThreadProducer(BlockingCollection<T> source)
        {
            Buffer = source;
            MaxDegreeOfParallelism = 1;

            _uptime = new Stopwatch();
            _cancel = new CancellationTokenSource();
            _empty = new SemaphoreSlim(1);

            RetryPolicy = new RetryPolicy();
            RateLimitPolicy = new RateLimitPolicy();

            var devNull = new ActionConsumer<T>(@event => { });

            _consumer = devNull;
            _backlogConsumer = devNull;
            _undeliverableConsumer = devNull;
        }

        public async void Produce(T @event)
        {
            if (Buffer.IsAddingCompleted)
            {
                // If we added to the backlog queue while stopping, then observables could fill it after a flush 
                await HandleBacklog(@event);
            }
            else
            {
                Buffer.Add(@event);
            }
        }

        public void Produce(IList<T> events)
        {
            if (events.Count == 0)
                return;

            foreach (var @event in events)
                Produce(@event);
        }

        public void Produce(IEnumerable<T> stream, TimeSpan? interval = null)
        {
            IObservable<T> projection = new Func<IEnumerable<T>>(() => stream).AsContinuousObservable();

            if (interval.HasValue)
                Produce(projection.Buffer(interval.Value));
            else
                Produce(projection);
        }

        public void Produce(Func<T> func, TimeSpan? interval = null)
        {
            if (Buffer.IsAddingCompleted)
                throw new InvalidOperationException("You cannot subscribe the buffer while stopping");

            func.AsContinuousObservable(interval).Subscribe(Produce, exception => { }, () => { }, _cancel.Token);
        }

        public void Produce(IObservable<T> observable)
        {
            if (Buffer.IsAddingCompleted)
            {
                throw new InvalidOperationException("You cannot subscribe the buffer while stopping");
            }

            observable.Subscribe(Produce, exception => { }, () => { }, _cancel.Token);
        }

        public void Produce(IObservable<IList<T>> observable)
        {
            if (Buffer.IsAddingCompleted)
            {
                throw new InvalidOperationException("You cannot subscribe the buffer while stopping");
            }

            observable.Subscribe(Produce, exception => { }, () => { }, _cancel.Token);
        }

        public void Produce(IObservable<IObservable<T>> observable)
        {
            if (Buffer.IsAddingCompleted)
            {
                throw new InvalidOperationException("You cannot subscribe the buffer while stopping");
            }

            observable.Subscribe(Produce, exception => { }, () => { }, _cancel.Token);
        }

        public void Attach(IConsume<T> consumer)
        {
            _consumer = consumer;
        }

        public void Attach(Action<T> @delegate)
        {
            _consumer = new ActionConsumer<T>(@delegate);
        }

        public void AttachBacklog(IConsume<T> consumer)
        {
            _backlogConsumer = consumer;
        }

        public void AttachBacklog(Action<T> @delegate)
        {
            _backlogConsumer = new ActionConsumer<T>(@delegate);
        }

        public void AttachUndeliverable(IConsume<T> consumer)
        {
            _undeliverableConsumer = consumer;
        }

        public void AttachUndeliverable(Action<T> @delegate)
        {
            _undeliverableConsumer = new ActionConsumer<T>(@delegate);
        }

        public void Start(bool immediate = false)
        {
            if (Running)
            {
                return;
            }

            if (_background != null)
            {
                Stop(immediate);
                _background = null;
            }

            RequisitionBackgroundTask();

            _uptime.Start();
            Running = true;
        }

        public void Stop(bool immediate = false)
        {
            if (!Running)
                return;

            Buffer.CompleteAdding();

            if (!immediate)
                WaitForEmptyBuffer();
            else
                TransferBufferToBacklog();

            ResetToInitialState();
        }

        private void WaitForEmptyBuffer()
        {
            _cancel.Cancel();
            _cancel.Token.WaitHandle.WaitOne();
            WithEmptyWait(() => { /* Wait for cancellation to empty the buffer to the backlogging consumer */ });
        }

        private void ResetToInitialState()
        {
            Running = false;
            Buffer = new BlockingCollection<T>();
        }

        private async Task HandleBacklog(T @event)
        {
            if (!await _backlogConsumer.HandleAsync(@event))
            {
                await HandleUndeliverable(@event);
            }
        }

        private async Task HandleUndeliverable(T @event)
        {
            await _undeliverableConsumer.HandleAsync(@event);

            Interlocked.Increment(ref _undelivered);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            if (Running)
            {
                Stop();
            }

            _background = null;

            if (_cancel != null)
            {
                _cancel.Dispose();
                _cancel = null;
            }
        }

        public void Restart()
        {
            Stop();
            Start();
        }

        private void RequisitionBackgroundTask()
        {
            ParallelOptions options = new ParallelOptions
            {
                MaxDegreeOfParallelism = MaxDegreeOfParallelism,
                CancellationToken = _cancel.Token
            };

            _background = Task.Run(() =>
            {
                try
                {
                    ProduceOn(GetProductionSource(), options);
                }
                catch (OperationCanceledException)
                {
                    TransferBufferToBacklog();
                }
            });
        }

        private BlockingCollection<T> GetProductionSource()
        {
            BlockingCollection<T> source;
            if (RateLimitPolicy.Enabled)
            {
                // Convert the outgoing blocking collection into a rate limited observable, then feed a new blocking queue with it
                var sequence = Buffer.AsRateLimitedObservable(RateLimitPolicy.Occurrences, RateLimitPolicy.TimeUnit, _cancel.Token);
                source = new BlockingCollection<T>();
                sequence.Subscribe(source.Add, exception => { }, () => { });
            }
            else
            {
                source = Buffer;
            }
            return source;
        }

        private async void TransferBufferToBacklog()
        {
            _empty.Wait();
            while (!Buffer.IsCompleted)
            {
                await HandleBacklog(Buffer.Take());
            }
            _empty.Release();
        }

        private void ProduceOn(BlockingCollection<T> source, ParallelOptions options)
        {
            Partitioner<T> partitioner = source.GetConsumingPartitioner();

            Parallel.ForEach(partitioner, options, (@event, state) => ProductionCycle(options, @event, state));
        }

        private readonly IDictionary<int, int> _attempts = new ConcurrentDictionary<int, int>();

        private async void ProductionCycle(ParallelOptions options, T @event, ParallelLoopState state)
        {
            if (state.ShouldExitCurrentIteration)
            {
                await HandleBacklog(@event);
                return;
            }

            if (!await _consumer.HandleAsync(@event))
            {
                HandleUnsuccessfulDelivery(options, @event, state);
            }

            Interlocked.Increment(ref _sent);
            options.CancellationToken.ThrowIfCancellationRequested();
        }


        private async void HandleUnsuccessfulDelivery(ParallelOptions options, T @event, ParallelLoopState state)
        {
            if (RetryPolicy != null)
            {
                var decision = RetryPolicy.DecideOn(@event, GetAttempts(@event));

                switch (decision)
                {
                    case RetryDecision.RetryImmediately:
                        ProductionCycle(options, @event, state);
                        break;
                    case RetryDecision.Requeue:
                        Buffer.Add(@event);
                        break;
                    case RetryDecision.Backlog:
                        await HandleBacklog(@event);
                        break;
                    case RetryDecision.Undeliverable:
                        await HandleUndeliverable(@event);
                        break;
                    case RetryDecision.Destroy:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            else
            {
                Buffer.Add(@event);
            }
        }

        private int GetAttempts(T @event)
        {
            int attempts;
            var hash = @event.GetHashCode();
            if (!_attempts.TryGetValue(hash, out attempts))
            {
                _attempts.Add(hash, 1);
            }
            else
            {
                _attempts[hash] = _attempts[hash] + 1;
            }
            attempts = _attempts[hash];
            return attempts;
        }

        private void WithEmptyWait(Action closure)
        {
            _empty.Wait();
            closure();
            _empty.Release();
        }
    }
}