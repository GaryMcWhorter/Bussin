using R3;
using System.Collections.Concurrent;

namespace Bussin;

public class Bus : IBus
{
    private readonly ConcurrentDictionary<Type, object> subjects = new();
    private bool disposed = false;

    public Observable<TEvent> GetEvent<TEvent>()
    {
        return (Observable<TEvent>)subjects.GetOrAdd(typeof(TEvent), _ => new Subject<TEvent>());
    }

    public void Publish<TEvent>(TEvent tevent)
    {
        var subject = (Subject<TEvent>)subjects.GetOrAdd(typeof(TEvent), _ => new Subject<TEvent>());
        subject.OnNext(tevent);
    }

    public IPublisher<TEvent> GetPublisher<TEvent>()
    {
        var subject = (Subject<TEvent>)subjects.GetOrAdd(typeof(TEvent), _ => new Subject<TEvent>());
        return new Publisher<TEvent>(subject);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposed) return;

        if (disposing)
        {
            foreach (var subject in subjects.Values)
            {
                (subject as IDisposable)?.Dispose();
            }
            subjects.Clear();
        }

        disposed = true;
    }
}
