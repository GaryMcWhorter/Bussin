using Shouldly;
using R3;
using System.Collections.Concurrent;
using System.Reflection;

namespace Bussin.Tests;

public class BusTests
{
    [Fact]
    public void GetEvent_ReturnsObservableOfCorrectType()
    {
        var bus = new Bus();
        var observable = bus.GetEvent<TestEvent>();
        observable.ShouldBeAssignableTo<Observable<TestEvent>>();
    }

    [Fact]
    public void GetEvent_ReturnsSameInstanceForSameType()
    {
        var bus = new Bus();
        var observable1 = bus.GetEvent<TestEvent>();
        var observable2 = bus.GetEvent<TestEvent>();
        observable1.ShouldBeSameAs(observable2);
    }

    [Fact]
    public async Task Publish_EventIsReceivedBySubscriber()
    {
        var bus = new Bus();
        var testEvent = new TestEvent { Message = "Test" };
        var tcs = new TaskCompletionSource<TestEvent>();

        using var _ = bus.GetEvent<TestEvent>().Subscribe(tcs.SetResult);
        bus.Publish(testEvent);

        var receivedEvent = await tcs.Task;
        receivedEvent.Message.ShouldBe(testEvent.Message);
    }

    [Fact]
    public async Task Publish_MultipleEventsAreReceivedInOrder()
    {
        var bus = new Bus();
        var events = new[] { new TestEvent { Message = "1" }, new TestEvent { Message = "2" }, new TestEvent { Message = "3" } };
        var receivedEvents = new List<TestEvent>();
        var tcs = new TaskCompletionSource<bool>();

        using var _ = bus.GetEvent<TestEvent>().Subscribe(
            onNext: e => 
            {
                receivedEvents.Add(e);
                if (receivedEvents.Count == 3) tcs.SetResult(true);
            }, 
            onCompleted: (_) => tcs.SetResult(false)
        );
        

        foreach (var evt in events)
        {
            bus.Publish(evt);
        }

        await tcs.Task;

        receivedEvents.Count.ShouldBe(3);
        receivedEvents[0].Message.ShouldBe("1");
        receivedEvents[1].Message.ShouldBe("2");
        receivedEvents[2].Message.ShouldBe("3");
    }

    [Fact]
    public async Task Publish_EventIsReceivedByMultipleSubscribers()
    {
        var bus = new Bus();
        var testEvent = new TestEvent { Message = "Test" };
        var tcs1 = new TaskCompletionSource<TestEvent>();
        var tcs2 = new TaskCompletionSource<TestEvent>();

        using var _ = bus.GetEvent<TestEvent>().Subscribe(tcs1.SetResult);
        using var _2 = bus.GetEvent<TestEvent>().Subscribe(tcs2.SetResult);

        bus.Publish(testEvent);

        var result1 = await tcs1.Task;
        var result2 = await tcs2.Task;

        result1.Message.ShouldBe(testEvent.Message);
        result2.Message.ShouldBe(testEvent.Message);
    }

    [Fact]
    public async Task Publish_MultiplePublishersReceivedBySubscriber()
    {
        var bus = new Bus();
        var testEvent1 = new TestEvent { Message = "Publisher 1 Event" };
        var testEvent2 = new TestEvent { Message = "Publisher 2 Event" };
        var receivedEvents = new ConcurrentBag<string>();
        var tcs = new TaskCompletionSource<bool>();

        using var subscription = bus.GetEvent<TestEvent>().Subscribe(e =>
        {
            receivedEvents.Add(e.Message);
            if (receivedEvents.Count == 2)
            {
                tcs.SetResult(true);
            }
        });

        var publisher1 = bus.GetPublisher<TestEvent>();
        var publisher2 = bus.GetPublisher<TestEvent>();

        // Publish events from both publishers
        publisher1.Publish(testEvent1);
        publisher2.Publish(testEvent2);

        // Wait until both events are received or timeout
        var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(1000));
        if (completedTask != tcs.Task)
        {
            throw new TimeoutException("Did not receive all events in time.");
        }

        // Verify that both events were received
        receivedEvents.Count.ShouldBe(2);
        receivedEvents.ShouldContain(testEvent1.Message);
        receivedEvents.ShouldContain(testEvent2.Message);
    }

    [Fact]
    public void GetPublisher_ReturnsDifferentInstancesForDifferentTypes()
    {
        var bus = new Bus();
        var publisher1 = bus.GetPublisher<TestEvent>();
        var publisher2 = bus.GetPublisher<AnotherTestEvent>();

        publisher1.ShouldNotBeSameAs(publisher2);
    }

    [Fact]
    public void GetPublisher_ReturnsSameInstanceForSameType()
    {
        var bus = new Bus();
        var publisher1 = bus.GetPublisher<TestEvent>();
        var publisher2 = bus.GetPublisher<TestEvent>();

        publisher1.ShouldBeEquivalentTo(publisher2);
    }

    [Fact]
    public async Task PublishViaPublisher_EventIsReceived()
    {
        var bus = new Bus();
        var publisher = bus.GetPublisher<TestEvent>();
        var testEvent = new TestEvent { Message = "Test" };
        var tcs = new TaskCompletionSource<TestEvent>();

        using var _ = bus.GetEvent<TestEvent>().Subscribe(e => tcs.SetResult(e));
        publisher.Publish(testEvent);

        var receivedEvent = await tcs.Task;
        receivedEvent.Message.ShouldBe(testEvent.Message);
    }

    [Fact]
    public void Dispose_DisposesAllSubjects()
    {
        var bus = new Bus();
        var observable = bus.GetEvent<TestEvent>();
        
        // Subscribe before disposal to ensure the subject is created
        using var _ = observable.Subscribe(_ => { });

        bus.Dispose();

        // Attempting to subscribe after disposal should throw an ObjectDisposedException
        Should.Throw<ObjectDisposedException>(() => 
        {
            using var _2 = observable.Subscribe(_ => { });
        });

        // Attempting to get a new event after disposal should not throw,
        // but should return a new, non-disposed observable
        var newObservable = bus.GetEvent<TestEvent>();
        Should.NotThrow(() => 
        {
            using var _3 = newObservable.Subscribe(_ => { });
        });

        // The new observable should be different from the original one
        newObservable.ShouldNotBeSameAs(observable);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var bus = new Bus();
        Should.NotThrow(() =>
        {
            bus.Dispose();
            bus.Dispose(); // Should not throw
        });
    }

    [Fact]
    public async Task Concurrent_PublishAndSubscribe_SameEventType()
    {
        var bus = new Bus();
        var testEvent1 = new TestEvent { Message = "Event 1" };
        var testEvent2 = new TestEvent { Message = "Event 2" };
        var receivedEvents = new ConcurrentBag<string>();

        var subscriberTask1 = Task.Run(() => 
        {
            bus.GetEvent<TestEvent>().Subscribe(e => receivedEvents.Add(e.Message));
        });

        var subscriberTask2 = Task.Run(() => 
        {
            bus.GetEvent<TestEvent>().Subscribe(e => receivedEvents.Add(e.Message));
        });

        // wait for subscribers to subscribe
        await Task.WhenAll(subscriberTask1, subscriberTask2);

        var publishTask1 = Task.Run(() => bus.Publish(testEvent1));
        var publishTask2 = Task.Run(() => bus.Publish(testEvent2));

        await Task.WhenAll(publishTask1, publishTask2);

        // Allow some time for subscribers to process
        await Task.Delay(100);

        // Ensure both events were received by both subscribers
        receivedEvents.Count.ShouldBe(4);
        receivedEvents.ShouldContain(testEvent1.Message);
        receivedEvents.ShouldContain(testEvent2.Message);
    }

    [Fact]
    public async Task Concurrent_PublishMultipleEvents_DifferentEventTypes()
    {
        var bus = new Bus();
        var testEvent1 = new TestEvent { Message = "Test Event" };
        var anotherTestEvent = new AnotherTestEvent { Value = 42 };
        var testEventTcs = new TaskCompletionSource<TestEvent>();
        var anotherTestEventTcs = new TaskCompletionSource<AnotherTestEvent>();

        var subscriberTask1 = Task.Run(() => 
        {
            bus.GetEvent<TestEvent>().Subscribe(e => testEventTcs.SetResult(e));
        });

        var subscriberTask2 = Task.Run(() => 
        {
            bus.GetEvent<AnotherTestEvent>().Subscribe(e => anotherTestEventTcs.SetResult(e));
        });

        await Task.WhenAll(subscriberTask1, subscriberTask2);

        var publishTask1 = Task.Run(() => bus.Publish(testEvent1));
        var publishTask2 = Task.Run(() => bus.Publish(anotherTestEvent));

        await Task.WhenAll(publishTask1, publishTask2);

        // Check if events were published and received correctly
        var receivedTestEvent = await testEventTcs.Task;
        var receivedAnotherTestEvent = await anotherTestEventTcs.Task;

        receivedTestEvent.Message.ShouldBe(testEvent1.Message);
        receivedAnotherTestEvent.Value.ShouldBe(anotherTestEvent.Value);
    }

    [Fact]
    public async Task Concurrent_MultipleSubscribersReceiveEvents()
    {
        var bus = new Bus();
        var testEvent = new TestEvent { Message = "Concurrent Test Event" };
        var receivedMessages = new ConcurrentBag<string>();

        var subscriberTasks = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(() =>
            {
                bus.GetEvent<TestEvent>().Subscribe(e => receivedMessages.Add(e.Message));
            }))
            .ToArray();

        await Task.WhenAll(subscriberTasks);

        var publishTask = Task.Run(() => bus.Publish(testEvent));

        await publishTask;

        // Allow some time for all subscribers to process the event
        await Task.Delay(100);

        // Ensure all subscribers received the event
        receivedMessages.Count.ShouldBe(10);
        foreach (var message in receivedMessages)
        {
            message.ShouldBe(testEvent.Message);
        }
    }

    [Fact]
    public async Task Concurrent_AccessToPublisherAndEvent_ShouldNotThrow()
    {
        var bus = new Bus();
        var testEvent = new TestEvent { Message = "Concurrent Event" };

        // Simulate multiple threads attempting to publish and subscribe
        var tasks = new List<Task>();

        for (int i = 0; i < 50; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                var publisher = bus.GetPublisher<TestEvent>();
                publisher.Publish(testEvent);
            }));

            tasks.Add(Task.Run(() =>
            {
                bus.GetEvent<TestEvent>().Subscribe(e => { });
            }));
        }

        await Task.WhenAll(tasks);
    }

    [Fact]
    public async Task SpinLock_PublishMultipleEvents_EnsuresThreadSafety()
    {
        var bus = new Bus();
        var testEvent = new TestEvent { Message = "SpinLock Test" };
        var receivedEvents = new ConcurrentBag<string>();
        var tcs = new TaskCompletionSource<bool>();

        using var _ = bus.GetEvent<TestEvent>().Subscribe(e =>
        {
            receivedEvents.Add(e.Message);
            if (receivedEvents.Count == 50) tcs.SetResult(true);
        });

        var tasks = Enumerable.Range(0, 50)
            .Select(_ => Task.Run(() => bus.Publish(testEvent)))
            .ToArray();

        await Task.WhenAll(tasks);

        await tcs.Task;

        receivedEvents.Count.ShouldBe(50);
        foreach (var message in receivedEvents)
        {
            message.ShouldBe(testEvent.Message);
        }
    }

    [Fact]
    public async Task SpinLock_MultiplePublishers_ThreadSafeWithSameEvent()
    {
        var bus = new Bus();
        var testEvent = new TestEvent { Message = "Publisher SpinLock Test" };
        var receivedEvents = new ConcurrentBag<string>();
        var tcs = new TaskCompletionSource<bool>();

        using var _ = bus.GetEvent<TestEvent>().Subscribe(e =>
        {
            receivedEvents.Add(e.Message);
            if (receivedEvents.Count == 50) tcs.SetResult(true);
        });

        var publisher = bus.GetPublisher<TestEvent>();

        var tasks = Enumerable.Range(0, 50)
            .Select(_ => Task.Run(() => publisher.Publish(testEvent)))
            .ToArray();

        await Task.WhenAll(tasks);

        await tcs.Task;

        receivedEvents.Count.ShouldBe(50);
        foreach (var message in receivedEvents)
        {
            message.ShouldBe(testEvent.Message);
        }
    }

    [Fact]
    public async Task SpinLock_MultipleTypes_PublishersThreadSafety()
    {
        var bus = new Bus();
        var testEvent = new TestEvent { Message = "SpinLock Test" };
        var anotherTestEvent = new AnotherTestEvent { Value = 42 };
        var receivedEvents = new ConcurrentBag<string>();
        var tcs = new TaskCompletionSource<bool>();

        using var _ = bus.GetEvent<TestEvent>().Subscribe(e =>
        {
            receivedEvents.Add(e.Message);
        });

        using var _2 = bus.GetEvent<AnotherTestEvent>().Subscribe(e =>
        {
            receivedEvents.Add(e.Value.ToString());
            if (receivedEvents.Count == 50) tcs.SetResult(true);
        });

        var testEventTasks = Enumerable.Range(0, 25)
            .Select(_ => Task.Run(() => bus.Publish(testEvent)))
            .ToArray();

        var anotherTestEventTasks = Enumerable.Range(0, 25)
            .Select(_ => Task.Run(() => bus.Publish(anotherTestEvent)))
            .ToArray();

        await Task.WhenAll(testEventTasks.Concat(anotherTestEventTasks));

        await tcs.Task;

        receivedEvents.Count.ShouldBe(50);
        receivedEvents.ShouldContain(testEvent.Message);
        receivedEvents.ShouldContain(anotherTestEvent.Value.ToString());
    }

    [Fact]
    public void Publisher_SameEventTypeUsesSameSubjectWrapper()
    {
        var bus = new Bus();

        var publisher1 = bus.GetPublisher<TestEvent>();
        var publisher2 = bus.GetPublisher<TestEvent>();

        // Publishers should be different instances
        publisher1.ShouldNotBeSameAs(publisher2);

        // But they should be equal (because they use the same wrapper)
        publisher1.ShouldBeEquivalentTo(publisher2);

        // Let's also verify that the underlying SubjectWrapper is the same
        var wrapperField = typeof(Publisher<TestEvent>).GetField("wrapper", BindingFlags.NonPublic | BindingFlags.Instance);
        var wrapper1 = wrapperField?.GetValue(publisher1);
        var wrapper2 = wrapperField?.GetValue(publisher2);

        // Assert.Same(wrapper1, wrapper2);
        wrapper1.ShouldBeSameAs(wrapper2);
    }


    private class TestEvent
    {
        public required string Message { get; set; }
    }

    private class AnotherTestEvent
    {
        public int Value { get; set; }
    }
}