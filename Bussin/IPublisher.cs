using R3;

namespace Bussin;

public interface IPublisher<TEvent>
{
    void Publish(TEvent tevent);
}