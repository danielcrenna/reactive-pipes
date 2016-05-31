reactive.pipes
======

```powershell
> Install-Package reactive.pipes
```

Reactive Pipes is a lightweight eventing middleware library for distributed applications.
 
It gives you high performing, non-blocking and parallelized eventing. You build other components out of it.

### Why would I use this?
- You're building a real-time system and want to notify your hub as things occur
- You want to synchronize activity between various application components at the event level
- You want to use the "[shared nothing](http://en.wikipedia.org/wiki/Shared_nothing_architecture)" integration pattern
- You want to denormalize events into multiple views for queries (event sourcing)

### Usage
--------
This library provides a foundation for distributed middleware on three levels: pipes, protocols,
and event dispatch. It provides most of what you need to handle concurrency and synchronization
of arbitrary producers and consumers. Here is a simple example using two built-in components.

```csharp
var block = new ManualResetEvent(false);

var producer = new ObservingProducer<int>();
var consumer = new DelegatingConsumer<int>(i => Console.WriteLine(i));

producer.Produces(Observable.Range(1, 10000), onCompleted: () => block.Set());
producer.Attach(consumer);
producer.Start();

block.WaitOne();
```

#### Addendum

This is a streamlined version of [copper](https://github.com/danielcrenna/vault/tree/master/copper), a library I wrote in 2012.
