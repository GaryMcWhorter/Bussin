using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using R3;

namespace Bussin.Benchmarks;

[MemoryDiagnoser]
[IterationCount(10)]
[WarmupCount(5)]
public class BusBenchmark
{
    public struct BusEvent { public float Value; public int Id; }
    private readonly Bus bus = new();
    private readonly BusEvent testEvent = new() { Value = 1.0f, Id = 1 };

    [Params(1, 10, 100, 1000)]
    public int NumberOfThreads { get; set; }

    [Params(100, 1000, 10000)]
    public int NumberOfEvents { get; set; }

    [Benchmark]
    public void BenchmarkLock()
    {
        RunTest(bus);
    }

    private void RunTest(Bus bus)
    {
        var subscribers = new List<IDisposable>(NumberOfEvents / 4);
        for (int i = 0; i < NumberOfEvents / 4; i++)
        {
            subscribers.Add(bus.GetEvent<BusEvent>().Subscribe((e) =>
            {
                // Simulate some small workload to make it more realistic
                var temp = e.Value * 2.0f;
                temp = temp + e.Id;
            }));
        }

        Parallel.For(0, NumberOfThreads, (i) =>
        {
            for (int j = 0; j < NumberOfEvents / NumberOfThreads; j++)
            {
                bus.Publish(testEvent);
            }
        });

        foreach (var subscriber in subscribers)
        {
            subscriber.Dispose();
        }
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<BusBenchmark>();
    }
}