using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
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
    public static Recorded<Notification<T>>[] Record<T>(this IObservable<T> observable, TestScheduler scheduler)
    {
        return scheduler.Start(() => observable, 0, 0, 1000).Messages.ToArray();
    }

    public static Notification<T2> Select<T1, T2>(this Notification<T1> notification, Func<T1, T2> transform) =>
        notification.Kind switch
        {
            NotificationKind.OnNext => Notification.CreateOnNext(transform(notification.Value)),
            NotificationKind.OnError => Notification.CreateOnError<T2>(new Exception()),
            NotificationKind.OnCompleted => Notification.CreateOnCompleted<T2>(),
            _ => throw new ArgumentOutOfRangeException()
        };

    public static Recorded<Notification<T>>[][] RecordTwoLevels<T>(this IObservable<IObservable<T>> observable, TestScheduler scheduler)
    {

        IObservable<(T, int)> selectMany = observable.SelectMany<IObservable<T>, (T, int)>((o, i) => o.Select(y => (y, i)));
        return scheduler
            .Start(() => selectMany, 0, 0, 1000)
            .Messages
            .GroupBy(x => x.Value.Value.Item2)
            .Select(y => y.Select(z => Recorded.Create(z.Time, z.Value.Select(t => t.Item1))).ToArray()).ToArray();
    }
}
