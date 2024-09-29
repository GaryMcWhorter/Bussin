using R3;

namespace Bussin;

public interface IBus : IDisposable
{
    Observable<TEvent> GetEvent<TEvent>();
    void Publish<TEvent>(TEvent sampleEvent);
}