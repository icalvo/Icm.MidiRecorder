using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MidiRecorder.Tests;

[TestClass]
public class RxTests
{
    [TestInitialize]
    public void TestInit()
    {
        AssertionOptions.AssertEquivalencyUsing(o => o.WithStrictOrdering());
    }

    [TestMethod]
    public void Split_SingleGroupTest()
    {
        var marbleInput = "----A----B------------C----D----";
        var expectation = "----------------B-----------------D";
        
        var events = Parse(marbleInput);

        TimeSpan timeToSaveAfterHeldEvents = TimeSpan.FromTicks(6);
        var scheduler = new TestScheduler();
        var sut = scheduler.CreateColdObservable(events);

        var result = sut.Throttle(timeToSaveAfterHeldEvents, scheduler).Record(scheduler);
        result.Should().BeEquivalentTo(Parse(expectation));
    }

    private static Recorded<Notification<char>>[] Parse(string marbles)
    {
        return marbles.Aggregate(
                (elapsed: 0, events: ImmutableList.Create<Recorded<Notification<char>>>()),
                (a, c) => c switch
                {
                    '-' => (a.elapsed+1, a.events),
                    '|' => (a.elapsed+1, a.events.Add(Recorded.Complete<char>(a.elapsed))),
                    'X' => (a.elapsed+1, a.events.Add(Recorded.OnError<char>(a.elapsed))),
                    _ => (a.elapsed+1, a.events.Add(Recorded.OnNext(a.elapsed, c)))
                })
            .events.ToArray();
    }
    
    private static string Render(Recorded<Notification<string>>[] record)
    {
        var sb = new StringBuilder();
        foreach (var item in record)
        {
            sb.Append('-', (int)item.Time);
            sb.Append(
                item.Value.Kind switch
                {
                    NotificationKind.OnError => "X",
                    NotificationKind.OnNext => item.Value.Value,
                    NotificationKind.OnCompleted => "|",
                    _ => throw new ArgumentOutOfRangeException()
                });
        }

        return sb.ToString();
    }
}
