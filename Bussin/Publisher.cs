using R3;

namespace Bussin;

public class Publisher<TEvent>(SubjectWrapper<TEvent> wrapper) : IPublisher<TEvent>, IEquatable<Publisher<TEvent>>
{
    private readonly SubjectWrapper<TEvent> wrapper = wrapper;

    public void Publish(TEvent tevent)
    {
        wrapper.Publish(tevent);
    }

    public bool Equals(Publisher<TEvent>? other)
    {
        return other != null && wrapper == other.wrapper;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as Publisher<TEvent>);
    }

    public override int GetHashCode()
    {
        return wrapper.GetHashCode();
    }
}