using R3;

namespace Bussin;

public class Publisher<TEvent>(Subject<TEvent> subject) : IPublisher<TEvent>, IEquatable<Publisher<TEvent>>
{
    private readonly Subject<TEvent> subject = subject;

    public Subject<TEvent> GetSubject() => subject;

    public void Publish(TEvent tevent)
    {
        subject.OnNext(tevent);
    }

    public bool Equals(Publisher<TEvent>? other)
    {
        return other != null && subject == other.GetSubject();
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as Publisher<TEvent>);
    }

    public override int GetHashCode()
    {
        return subject.GetHashCode();
    }
}