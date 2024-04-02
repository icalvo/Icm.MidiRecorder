using System;
using System.Linq;
using Microsoft.Reactive.Testing;

namespace MidiRecorder.Tests;

public static class TestSchedulerExtensions
{
    public static Recorded<T>[] WaitAndGetRecorded<T>(this IObservable<T> o, TestScheduler scheduler)
    {
        scheduler.Start();
        
        var testObserver = scheduler.Start(create:() => o, created: 0, subscribed: 0, disposed: 5000);

        return testObserver.Messages.Select(r => new Recorded<T>(r.Time, r.Value.Value)).ToArray();
    }
}
