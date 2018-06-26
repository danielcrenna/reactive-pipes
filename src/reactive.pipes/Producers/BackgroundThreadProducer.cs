using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using reactive.pipes.Consumers;
using reactive.pipes.Extensions;
using reactive.pipes.Helpers;

namespace reactive.pipes.Producers
{
    /// <summary>
    /// A default producer implementation that produces on a background thread
    /// <remarks>
    /// - The production queue is seeded explicitly by callers, or by subscribing to an observable
    /// - The producer-consumer problem requires a shared buffer; you must give full control of the buffer to the producer, and pass it to any consumers
    /// - Backlogged and undeliverable messages are managed by other consumers you can attach; by default, all special case message handling is sent into the abyss
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
	    private IConsume<Exception> _errorConsumer;

		private readonly bool _internal;
		private BlockingCollection<T> _buffer;
	    private readonly IDictionary<ulong, int> _attempts = new ConcurrentDictionary<ulong, int>();

	    public int MaxDegreeOfParallelism { get; set; }

        public TimeSpan Uptime => _uptime.Elapsed;

        public int Sent => _sent;

        public double Rate => _sent / _uptime.Elapsed.TotalSeconds;

	    public int Queued => _buffer.Count;

        public int Undeliverable => _undelivered;

        public bool Running { get; private set; }

        public Action OnStarted { get; set; }

        public Action OnStopped { get; set; }

        public RetryPolicy RetryPolicy { get; }

        public RateLimitPolicy RateLimitPolicy { get; }

	    public BackgroundThreadProducer(IObservable<T> source) : this()
        {
            Produce(source);

            OnStarted = () => { };
            OnStopped = () => { };
        }

        public BackgroundThreadProducer() : this(new BlockingCollection<T>(), true) { }

        public BackgroundThreadProducer(int capacity) : this(new BlockingCollection<T>(capacity), true) { }

        public BackgroundThreadProducer(IProducerConsumerCollection<T> source) : this(new BlockingCollection<T>(source), true) { }

        public BackgroundThreadProducer(IProducerConsumerCollection<T> source, int capacity) : this(new BlockingCollection<T>(source, capacity), true) { }

	    public BackgroundThreadProducer(BlockingCollection<T> source) : this(source, false) { }

	    BackgroundThreadProducer(BlockingCollection<T> source, bool @internal = true)
        {
            _buffer = source;
	        _internal = @internal;

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

        public async Task Produce(T message)
        {
            if (_buffer.IsAddingCompleted)
            {
                // If we added to the backlog queue while stopping,
                // then observables could fill it after a flush 
                await HandleBacklog(message);
            }
            else
            {
                _buffer.Add(message);
            }
        }

	    public async Task Produce(IList<T> messages)
        {
            if (messages.Count == 0)
                return;

            foreach (var @event in messages)
                await Produce(@event);
        }

        public void Produce(IEnumerable<T> stream, TimeSpan? interval = null)
        {
            var projection = new Func<IEnumerable<T>>(() => stream).AsContinuousObservable();

            if (interval.HasValue)
                Produce(projection.Buffer(interval.Value));
            else
                Produce(projection);
        }

        public void Produce(Func<T> func, TimeSpan? interval = null)
        {
            if (_buffer.IsAddingCompleted)
                throw new InvalidOperationException("You cannot subscribe the buffer while stopping");

            func.AsContinuousObservable(interval).Subscribe(
	            onNext: async x => { await Produce(x); },
	            onError: async e => { if(_errorConsumer != null) await _errorConsumer?.HandleAsync(e); },
				onCompleted: () => { },
	            token: _cancel.Token);
        }

        public void Produce(IObservable<T> observable)
        {
            if (_buffer.IsAddingCompleted)
            {
                throw new InvalidOperationException("You cannot subscribe the buffer while stopping");
            }

	        observable.Subscribe(
				onNext: async x => { await Produce(x); },
				onError: async e => { if (_errorConsumer != null) await _errorConsumer?.HandleAsync(e); },
				onCompleted: () => { },
				token: _cancel.Token);
		}

        public void Produce(IObservable<IList<T>> observable)
        {
            if (_buffer.IsAddingCompleted)
            {
                throw new InvalidOperationException("You cannot subscribe the buffer while stopping");
            }

            observable.Subscribe(
				onNext: async x => { await Produce(x); },
	            onError: async e => { if (_errorConsumer != null) await _errorConsumer?.HandleAsync(e); },
	            onCompleted: () => { },
	            token: _cancel.Token);
		}

        public void Produce(IObservable<IObservable<T>> observable)
        {
            if (_buffer.IsAddingCompleted)
            {
                throw new InvalidOperationException("You cannot subscribe the buffer while stopping");
            }

            observable.Subscribe(
				onNext: Produce,
	            onError: async e => { if (_errorConsumer != null) await _errorConsumer?.HandleAsync(e); },
	            onCompleted: () => { },
	            token: _cancel.Token);
		}

        public void Attach(IConsume<T> consumer)
        {
            _consumer = consumer;
        }

        public void Attach(Action<T> @delegate)
        {
            _consumer = new ActionConsumer<T>(@delegate);
        }

		/// <summary>
		/// This consumer is invoked when the producer is stopped immediately or otherwise interrupted, as such on disposal.
		/// Any messages still waiting to be delivered are flushed to this consumer. If the consumer reports a failure, then the
		/// messages are swept to the undeliverable consumer. 
		/// </summary>
        public void AttachBacklog(IConsume<T> consumer)
        {
            _backlogConsumer = consumer;
        }

	    /// <summary>
	    /// This consumer is invoked when the producer is stopped immediately or otherwise interrupted, as such on disposal.
	    /// Any messages still waiting to be delivered are flushed to this consumer. If the consumer reports a failure, then the
	    /// messages are swept to the undeliverable consumer. 
	    /// </summary>
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

	    public void AttachError(Action<Exception> onError)
	    {
			_errorConsumer = new ActionConsumer<Exception>(onError);
	    }

        public async Task Start(bool immediate = false)
        {
            if (Running)
            {
                return;
            }

            if (_background != null)
            {
                await Stop(immediate);
                _background = null;
            }

            RequisitionBackgroundTasks();

            _uptime.Start();
            Running = true;
        }

		/// <summary>Stops accepting new messages for immediate delivery. </summary>
		/// <param name="immediate">If <code>true</code>, the service immediately redirects all messages in the queue to the backlog; emails that are queued after a stop call are always sent to the backlog. Otherwise, all queued messages are sent before closing the producer to additional messages.</param>
		public async Task Stop(bool immediate = false)
        {
            if (!Running)
                return;

            _buffer.CompleteAdding();

            if (!immediate)
                await WaitForEmptyBuffer();
            else
                await FlushBacklog();

			Running = false;
	        _uptime.Stop();
	        if (_internal)
		        _buffer = new BlockingCollection<T>();
		}

	    private async Task WaitForEmptyBuffer()
        {
			_empty.Wait();
	        while (!_buffer.IsCompleted)
		        await Task.Delay(10);
	        _empty.Release();

			_cancel.Cancel();
            _cancel.Token.WaitHandle.WaitOne();
        }

	    private async Task HandleBacklog(T message)
	    {
		    if (_backlogConsumer != null)
		    {
			    if (_errorConsumer != null)
			    {
				    try
				    {
					    if (!await _backlogConsumer.HandleAsync(message))
					    {
						    await HandleUndeliverable(message);
					    }
					}
				    catch (Exception e)
				    {
						await _errorConsumer.HandleAsync(e);
					    await HandleUndeliverable(message);
					}
				}
			    else
			    {
				    if (!await _backlogConsumer.HandleAsync(message))
				    {
					    await HandleUndeliverable(message);
				    }
			    }
		    }
	    }

		private async Task HandleUndeliverable(T message)
        {
			if(_undeliverableConsumer != null)
			{
				if (_errorConsumer != null)
				{
					try
					{
						if (await _undeliverableConsumer.HandleAsync(message))
						{
							Interlocked.Increment(ref _undelivered);
						}
					}
					catch (Exception e)
					{
						await _errorConsumer.HandleAsync(e);
					}
				}
				else
				{
					if (await _undeliverableConsumer.HandleAsync(message))
					{
						Interlocked.Increment(ref _undelivered);
					}
				}
			}
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
                Stop().RunSynchronously();
            }

	        _background?.Dispose();
            _background = null;
	        _cancel?.Dispose();
	        _cancel = null;
		}

        public async Task Restart(bool immediate = false)
        {
            await Stop(immediate);

            await Start(immediate);
        }

        private void RequisitionBackgroundTasks()
        {
            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = MaxDegreeOfParallelism,
                CancellationToken = _cancel.Token
            };

            _background = Task.Run(async () =>
            {
                try
                {
                    ProduceOn(GetProductionSource(), options);
                }
                catch (OperationCanceledException)
                {
                    await FlushBacklog();
                }
            });
		}

        private BlockingCollection<T> GetProductionSource()
        {
            BlockingCollection<T> source;
            if (RateLimitPolicy.Enabled)
            {
                // Convert the outgoing blocking collection into a rate limited observable, then feed a new blocking queue with it
                var sequence = _buffer.AsRateLimitedObservable(RateLimitPolicy.Occurrences, RateLimitPolicy.TimeUnit, _cancel.Token);
                source = new BlockingCollection<T>();
                sequence.Subscribe(source.Add, exception => { }, () => { });
            }
            else
            {
                source = _buffer;
            }
            return source;
        }

        private async Task FlushBacklog()
        {
            _empty.Wait();
            while (!_buffer.IsCompleted)
            {
				if(_buffer.TryTake(out var message, -1, _cancel.Token))
					await HandleBacklog(message);
            }
            _empty.Release();
        }

        private void ProduceOn(BlockingCollection<T> source, ParallelOptions options)
        {
            var partitioner = source.GetConsumingPartitioner();

            Parallel.ForEach(partitioner, options, async (@event, state) => await ProductionCycle(options, @event, state));
        }

        private async Task ProductionCycle(ParallelOptions options, T message, ParallelLoopState state)
        {
            if (state.ShouldExitCurrentIteration)
            {
                await HandleBacklog(message);
                return;
            }
			
	        if (_errorConsumer != null)
	        {
		        try
		        {
			        if (!await _consumer.HandleAsync(message))
			        {
				        await HandleUnsuccessfulDelivery(options, message, state);
				        return;
			        }
				}
		        catch (Exception e)
		        {
			        await _errorConsumer.HandleAsync(e);
					await HandleUnsuccessfulDelivery(options, message, state);
			        return;
		        }
			}
	        else
	        {
		        if (!await _consumer.HandleAsync(message))
		        {
			        await HandleUnsuccessfulDelivery(options, message, state);
			        return;
		        }
	        }

			Interlocked.Increment(ref _sent);
			options.CancellationToken.ThrowIfCancellationRequested();
        }

		private async Task HandleUnsuccessfulDelivery(ParallelOptions options, T message, ParallelLoopState state)
		{
			var hash = HashMessage(message);
			var attempts = IncrementAttempts(hash);
			var decision = RetryPolicy?.DecideOn(message, attempts) ?? RetryDecision.Undeliverable;

			switch (decision)
			{
				case RetryDecision.RetryImmediately:
					await ProductionCycle(options, message, state);
					break;
				case RetryDecision.Requeue:
					if (!_buffer.IsAddingCompleted && RetryPolicy?.RequeueInterval != null)
						Produce(Observable.Return(message).Delay(RetryPolicy.RequeueInterval(attempts)));
					else
						await Produce(message);
					break;
				case RetryDecision.Backlog:
					_attempts.Remove(hash);
					await HandleBacklog(message);
					break;
				case RetryDecision.Undeliverable:
					_attempts.Remove(hash);
					await HandleUndeliverable(message);
					break;
				case RetryDecision.Destroy:
					_attempts.Remove(hash);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public Func<T, ulong> HashFunction => x => XXHash.XXH64(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(x)));
			
	    private ulong HashMessage(T message)
	    {
		    return HashFunction?.Invoke(message) ?? (ulong) message.GetHashCode();
	    }
		
	    private int IncrementAttempts(ulong hash)
        {
	        if (!_attempts.TryGetValue(hash, out var attempts))
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
    }
}