using System;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MidiRecorder.Application;
using MidiRecorder.CommandLine;

namespace MidiRecorder.Tests;

[TestClass]
public class NoteDurationTests
{

    [TestMethod]
    public void CalculateDurationsTest()
    {
        var events = new[]
        {
            Recorded.OnNext(100, "CC1"),
            Recorded.OnNext(103, "On 45"),
            Recorded.OnNext(105, "On 48"),
            Recorded.OnNext(110, "Off 45"),
            Recorded.OnNext(120, "CC2"),
            Recorded.OnNext(130, "Off 48"),
            Recorded.OnNext(140, "CC3"),
        };
        var scheduler = new TestScheduler();
        var allEvents = scheduler.CreateColdObservable(events);
        var result = NoteDuration.CalculateDurations(
            allEvents,
            x => x.StartsWith("On"),
            x => x.StartsWith("Off"),
            x => int.Parse(x.Split(' ')[1]),
            scheduler);
        var result2 = result.WaitAndGetRecorded(scheduler);
        
        result2.Should()
            .BeEquivalentTo(
                new []
                {
                    Recorded.Create(101, ("CC1", TimeSpan.Zero)),
                    Recorded.Create(104, ("On 45", TimeSpan.Zero)),
                    Recorded.Create(106, ("On 48", TimeSpan.Zero)),
                    Recorded.Create(111, ("Off 45", TimeSpan.FromTicks(111-104))),
                    Recorded.Create(121, ("CC2", TimeSpan.Zero)),
                    Recorded.Create(131, ("Off 48", TimeSpan.FromTicks(131L-106L))),
                    Recorded.Create(141, ("CC3", TimeSpan.Zero)),
                });
    }
}