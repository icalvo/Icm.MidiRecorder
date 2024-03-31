using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Microsoft.Reactive.Testing;
using MidiRecorder.Application;

namespace MidiRecorder.Tests;

public static class Recording
{
    public static Recorded<string>[][] RecordTwoLevels<T>(this IObservable<IObservable<T>> sut, TestScheduler scheduler)
    {
        List<List<Recorded<T>>> splitGroups = new ();
        IObservable<IObservable<T>> sutSplitGroups = sut;
        sutSplitGroups.Subscribe(
            o =>
            {
                var groupList = new List<Recorded<T>>();
                splitGroups.Add(groupList);
                o.Timestamp(scheduler).Select(it => Recorded.Create(it.Timestamp.Ticks, it.Value)).Subscribe(groupList.Add);
            });
        scheduler.Start();

        return splitGroups.Select(g => g.Select(o => Recorded.Create(o.Time, o.Value?.ToString() ?? "")).ToArray()).ToArray();
    }


    public static Recorded<T>[] Record<T>(this IObservable<T> obs, TestScheduler scheduler)
    {
        var recordedList = obs.Sub(scheduler);
        scheduler.Start();
        return recordedList();
    }
}
