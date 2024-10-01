using R3;
using System.Collections.Concurrent;

namespace Bussin;

public class Bus : IBus
{
    private readonly ConcurrentDictionary<Type, SubjectWrapper> subjects = new();
    private bool disposed = false;

    public Observable<TEvent> GetEvent<TEvent>()
    {
        return ((SubjectWrapper<TEvent>)subjects.GetOrAdd(typeof(TEvent), _ => new SubjectWrapper<TEvent>())).GetSubject();
    }

    public void Publish<TEvent>(TEvent tevent)
    {
        var wrapper = (SubjectWrapper<TEvent>)subjects.GetOrAdd(typeof(TEvent), _ => new SubjectWrapper<TEvent>());
        wrapper.Publish(tevent);
    }

    public IPublisher<TEvent> GetPublisher<TEvent>()
    {
        var wrapper = (SubjectWrapper<TEvent>)subjects.GetOrAdd(typeof(TEvent), _ => new SubjectWrapper<TEvent>());
        return new Publisher<TEvent>(wrapper);
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
                subject.Dispose();
            }
            subjects.Clear();
        }

        disposed = true;
    }
}