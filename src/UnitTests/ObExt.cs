using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Microsoft.Reactive.Testing;

namespace MidiRecorder.Tests;

public static class ObExt
{
    public static Func<Recorded<T>[]> Sub<T>(this IObservable<T> observable, IScheduler scheduler)
    {
        var list = new List<Recorded<T>>();
        observable
            .Timestamp(scheduler)
            .Select(it => Recorded.Create(it.Timestamp.Ticks, it.Value))
            .Subscribe(x => list.Add(x));

        return list.ToArray;
    }
}
