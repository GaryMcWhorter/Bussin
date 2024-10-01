using R3;

namespace Bussin;

public abstract class SubjectWrapper : IDisposable
{
    protected readonly SpinLock spinLock = new(enableThreadOwnerTracking: false);
    public abstract void Dispose();
    public abstract void PublishObject(object obj);
}

public class SubjectWrapper<T> : SubjectWrapper
{
    private readonly Subject<T> subject = new();

    public Subject<T> GetSubject() => subject;

    public void Publish(T tevent)
    {
        bool lockTaken = false;
        try
        {
            spinLock.Enter(ref lockTaken);
            subject.OnNext(tevent);
        }
        finally
        {
            if (lockTaken) spinLock.Exit();
        }
    }

    public override void PublishObject(object obj)
    {
        if (obj is T tevent)
        {
            Publish(tevent);
        }
        else
        {
            throw new ArgumentException($"Object is not of type {typeof(T)}");
        }
    }

    public override void Dispose()
    {
        subject.Dispose();
        GC.SuppressFinalize(this);
    }
}