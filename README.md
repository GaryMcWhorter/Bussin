# Bussin

A bussin' event bus implementation built on top of [R3 Subjects](https://github.com/Cysharp/R3)

## Getting Started <a name = "getting_started"></a>

Bussin isn't on Nuget, so you'll have to integrate the source directly in your project or add it as a submodule.

### Prerequisites

Bussin was written with .net 8.0 in mind, but may be backwards compatible with earlier versions.

## Usage <a name = "usage"></a>

```cs
// Make a bus
var bus = new Bus();

// Subscribe to something...
var subscription = bus.GetEvent<string>().Subscribe(e => Console.WriteLine(e));

// Publish something...
bus.Publish("Hello");

// Maybe you want to cache a publisher...
var publisher = bus.GetPublisher<string>();
publisher.Publish("World!");

// Don't forget to dispose subscriptions!
subscription.Dispose();
```