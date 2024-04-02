using System;
using System.Linq;
using System.Reactive;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MidiRecorder.Application;
using MidiRecorder.Application.Implementation;
using NAudio.Midi;
using static LanguageExt.Prelude;
using static MidiRecorder.Tests.Events;

namespace MidiRecorder.Tests;

[TestClass]
public class MidiSplitterTests
{
    [TestInitialize]
    public void TestInit()
    {
        AssertionOptions.AssertEquivalencyUsing(o => o.WithStrictOrdering().ComparingByMembers<Recorded<NAudioMidiEvent>>());
    }
    
    [TestMethod]
    public void Split_SingleGroupTest()
    {
        var events = new[]
        {
            Recorded.OnNext(100, NoteOn(1, 20, 64, 1)),
            Recorded.OnNext(105, NoteOff(1, 20, 64, 1)),
            Recorded.OnNext(116, NoteOn(1, 30, 64, 1)),
            Recorded.OnNext(120, NoteOff(1, 30, 64, 1))
        };
    
        TimeSpan timeToSaveAfterHeldEvents = TimeSpan.FromTicks(20);
        TimeSpan timeToSaveAfterAllOff = TimeSpan.FromTicks(30);
        var scheduler = new TestScheduler();
        var sut = CreateSplit(scheduler, events, timeToSaveAfterHeldEvents, timeToSaveAfterAllOff);
    
        var result = sut.SplitGroups.RecordTwoLevels(scheduler);
        var expected = new[]
            {
                new[]
                {
                    Recorded.OnNext(100+1, NoteOn(1, 20, 64, 1)),
                    Recorded.OnNext(105+1, NoteOff(1, 20, 64, 1)),
                    Recorded.OnNext(116+1, NoteOn(1, 30, 64, 1)),
                    Recorded.OnNext(120+1, NoteOff(1, 30, 64, 1))
                }
            };
        result.Should().BeEquivalentTo(expected);
    }
    
    [TestMethod]
    public void Split_SplitByRelease()
    {
        var events = new[]
        {
            Recorded.OnNext(100, NoteOn(1, 20, 64, 1)),
            Recorded.OnNext(105, NoteOff(1, 20, 64, 1)),
            Recorded.OnNext(200, NoteOn(1, 30, 64, 1)),
            Recorded.OnNext(205, NoteOff(1, 30, 64, 1))            
        };
    
        TimeSpan timeToSaveAfterHeldEvents = TimeSpan.FromTicks(20);
        TimeSpan timeToSaveAfterAllOff = TimeSpan.FromTicks(30);
    
        var scheduler = new TestScheduler();
        var sut = CreateSplit(scheduler, events, timeToSaveAfterHeldEvents, timeToSaveAfterAllOff);
    
        var result = sut.SplitGroups.RecordTwoLevels(scheduler);
        var expected = new[]
        {
            new[]
            {
                Recorded.OnNext(100+1, NoteOn(1, 20, 64, 1)),
                Recorded.OnNext(105+1, NoteOff(1, 20, 64, 1)),
            },
            new[]
            {
                Recorded.OnNext(200+1, NoteOn(1, 30, 64, 1)),
                Recorded.OnNext(205+1, NoteOff(1, 30, 64, 1))            
            }
        };
        result.Should().BeEquivalentTo(expected);
    }

    [TestMethod]
    public void Split_SplitByRelease2()
    {
        var events = new[]
        {
            Recorded.OnNext(100, NoteOn(1, 20, 64, 1)),
            Recorded.OnNext(105, NoteOff(1, 20, 64, 1)),
            Recorded.OnNext(200, NoteOn(1, 30, 64, 1)),
            Recorded.OnNext(205, NoteOff(1, 30, 64, 1))            
        };
    
        TimeSpan timeToSaveAfterHeldEvents = TimeSpan.FromTicks(20);
        TimeSpan timeToSaveAfterAllOff = TimeSpan.FromTicks(30);
    
        var scheduler = new TestScheduler();
        var sut = CreateSplit(scheduler, events, timeToSaveAfterHeldEvents, timeToSaveAfterAllOff);
    
        var result = sut.HeldNotesAndPedals.Record(scheduler);
        
        var expected = new[]
        {
            Recorded.OnNext(101, 1),
            Recorded.OnNext(106, 0),
            Recorded.OnNext(201, 1),
            Recorded.OnNext(206, 0)            
        };
        result.Should().BeEquivalentTo(expected, o => o.WithTracing());
    }

    [TestMethod]
    public void Split_SplitByRelease3()
    {
        var events = new[]
        {
            Recorded.OnNext(100, NoteOn(1, 20, 64, 1)),
            Recorded.OnNext(105, NoteOff(1, 20, 64, 1)),
            Recorded.OnNext(200, NoteOn(1, 30, 64, 1)),
            Recorded.OnNext(205, NoteOff(1, 30, 64, 1))            
        };
    
        TimeSpan timeToSaveAfterHeldEvents = TimeSpan.FromTicks(20);
        TimeSpan timeToSaveAfterAllOff = TimeSpan.FromTicks(30);
    
        var scheduler = new TestScheduler();
        var sut = CreateSplit(scheduler, events, timeToSaveAfterHeldEvents, timeToSaveAfterAllOff);
    
        var result = sut.NotesAndPedalsWithoutHeld.Record(scheduler);
        
        var expected = new[]
        {
            Recorded.OnNext(100+1, NoteOn(1, 20, 64, 1)),
            Recorded.OnNext(105+1, NoteOff(1, 20, 64, 1)),
            Recorded.OnNext(200+1, NoteOn(1, 30, 64, 1)),
            Recorded.OnNext(205+1, NoteOff(1, 30, 64, 1))            
         
        };
        result.Should().BeEquivalentTo(expected);
    }
    
    [TestMethod]
    public void Split_SplitByHeldNote()
    {
        var events = new[]
        {
            Recorded.OnNext(100, NoteOn(1, 20, 64, 1)),
            Recorded.OnNext(105, NoteOff(1, 20, 64, 1)),
            Recorded.OnNext(110, NoteOn(1, 30, 64, 1)),
            Recorded.OnNext(192, NoteOff(1, 30, 64, 1)),
            Recorded.OnNext(200, NoteOn(1, 40, 64, 1)),
            Recorded.OnNext(205, NoteOff(1, 40, 64, 1))
        };
    
        TimeSpan timeToSaveAfterHeldEvents = TimeSpan.FromTicks(20);
        TimeSpan timeToSaveAfterAllOff = TimeSpan.FromTicks(30);
    
        var scheduler = new TestScheduler();
        var sut = CreateSplit(scheduler, events, timeToSaveAfterHeldEvents, timeToSaveAfterAllOff);
    
        var result2 = sut.SplitGroups.RecordTwoLevels(scheduler);
        result2.Should()
            .BeEquivalentTo(
                new Recorded<Notification<NAudioMidiEvent>>[][]
                {
                    [
                        Recorded.OnNext(100+1, NoteOn(1, 20, 64, 1)),
                        Recorded.OnNext(105+1, NoteOff(1, 20, 64, 1)),
                        Recorded.OnNext(110+1, NoteOn(1, 30, 64, 1)),
                    ],
                    [
                        Recorded.OnNext(192+1, NoteOff(1, 30, 64, 1)),
                        Recorded.OnNext(200+1, NoteOn(1, 40, 64, 1)),
                        Recorded.OnNext(205+1, NoteOff(1, 40, 64, 1))
                    ]
                });
    }
    
    [TestMethod]
    public void Split_OtherEventsAreIgnored()
    {
        var events = new[]
        {
            Recorded.OnNext(100, NoteOn(1, 20, 64, 1)),
            Recorded.OnNext(103, CC(1, MidiController.Expression, 33, 1)),
            Recorded.OnNext(105, NoteOff(1, 20, 64, 1)),
            Recorded.OnNext(110, CC(1, MidiController.Expression, 33, 1)),
            Recorded.OnNext(120, CC(1, MidiController.Expression, 33, 1)),
            Recorded.OnNext(130, CC(1, MidiController.Expression, 33, 1)),
            Recorded.OnNext(140, CC(1, MidiController.Expression, 33, 1)),
            Recorded.OnNext(150, CC(1, MidiController.Expression, 33, 1)),
            Recorded.OnNext(160, CC(1, MidiController.Expression, 33, 1)),
            Recorded.OnNext(200, NoteOn(1, 40, 64, 1)),
            Recorded.OnNext(202, CC(1, MidiController.Expression, 33, 1)),
            Recorded.OnNext(205, NoteOn(1, 40, 64, 1))
        };
    
        TimeSpan timeToSaveAfterHeldEvents = TimeSpan.FromTicks(20);
        TimeSpan timeToSaveAfterAllOff = TimeSpan.FromTicks(30);
    
        var scheduler = new TestScheduler();
        var sut = CreateSplit(scheduler, events, timeToSaveAfterHeldEvents, timeToSaveAfterAllOff);
    
        var result = sut.SplitGroups.RecordTwoLevels(scheduler);
        result.Should()
            .BeEquivalentTo(
                new Recorded<Notification<NAudioMidiEvent>>[][]
                {
                    [
                        Recorded.OnNext(100+1, NoteOn(1, 20, 64, 1)),
                        Recorded.OnNext(103+1, CC(1, MidiController.Expression, 33, 1)),
                        Recorded.OnNext(105+1, NoteOff(1, 20, 64, 1)),
                        Recorded.OnNext(110+1, CC(1, MidiController.Expression, 33, 1)),
                        Recorded.OnNext(120+1, CC(1, MidiController.Expression, 33, 1)),
                        Recorded.OnNext(130+1, CC(1, MidiController.Expression, 33, 1)),
                    ],
                    [
                        Recorded.OnNext(140+1, CC(1, MidiController.Expression, 33, 1)),
                        Recorded.OnNext(150+1, CC(1, MidiController.Expression, 33, 1)),
                        Recorded.OnNext(160+1, CC(1, MidiController.Expression, 33, 1)),
                        Recorded.OnNext(200+1, NoteOn(1, 40, 64, 1)),
                        Recorded.OnNext(202+1, CC(1, MidiController.Expression, 33, 1)),
                        Recorded.OnNext(205+1, NoteOn(1, 40, 64, 1))
                    ]
                });
    }

    [TestMethod]
    public void GroupsToSave_SplitByHeldNote2()
    {
        var events = new[]
        {
            Recorded.OnNext(100, NoteOn(1, 20, 64, 1)),
            Recorded.OnNext(105, NoteOff(1, 20, 64, 1)),
            Recorded.OnNext(110, NoteOn(1, 30, 64, 1)),
            Recorded.OnNext(192, NoteOff(1, 30, 64, 1)),
            Recorded.OnNext(200, NoteOn(1, 40, 64, 1)),
            Recorded.OnNext(205, NoteOff(1, 40, 64, 1))
        };

        TimeSpan timeToSaveAfterHeldEvents = TimeSpan.FromTicks(20);
        TimeSpan timeToSaveAfterAllOff = TimeSpan.FromTicks(30);

        var scheduler = new TestScheduler();
        var sut = CreateSplit(scheduler, events, timeToSaveAfterHeldEvents, timeToSaveAfterAllOff);

        var result2 = sut.SplitGroups.RecordTwoLevels(scheduler);
        result2.Should()
            .BeEquivalentTo(
                new Recorded<Notification<NAudioMidiEvent>>[][]
                {
                    [
                        Recorded.OnNext(100+1, NoteOn(1, 20, 64, 1)),
                        Recorded.OnNext(105+1, NoteOff(1, 20, 64, 1)),
                        Recorded.OnNext(110+1, NoteOn(1, 30, 64, 1))
                    ],
                    [
                        Recorded.OnNext(192+1, NoteOff(1, 30, 64, 1)),
                        Recorded.OnNext(200+1, NoteOn(1, 40, 64, 1)),
                        Recorded.OnNext(205+1, NoteOff(1, 40, 64, 1))
                    ],
                });
    }

    [TestMethod]
    public void RegressionTest17()
    {
        var events = new[]
        {
            Recorded.OnNext(100, NoteOn(2, 96, 64, 3)),
            Recorded.OnNext(101, NoteOn(1, 96, 64, 3)),
            Recorded.OnNext(105, NoteOff(2, 96, 64, 3)),
            Recorded.OnNext(106, NoteOff(1, 96, 64, 3)),
            Recorded.OnNext(110, NoteOn(2, 95, 64, 3)),
            Recorded.OnNext(111, NoteOn(1, 95, 64, 3)),
            Recorded.OnNext(112, NoteOff(2, 95, 64, 3)),
            Recorded.OnNext(113, NoteOff(1, 95, 64, 3)),
        };

        TimeSpan timeToSaveAfterHeldEvents = TimeSpan.FromTicks(30);
        TimeSpan timeToSaveAfterAllOff = TimeSpan.FromTicks(15);

        var scheduler = new TestScheduler();
        var sut = CreateSplit(scheduler, events, timeToSaveAfterHeldEvents, timeToSaveAfterAllOff);

        var splitGroups = sut.SplitGroups.RecordTwoLevels(scheduler);

        splitGroups.Should()
            .BeEquivalentTo(
                new Recorded<Notification<NAudioMidiEvent>>[][]
                {
                    [
                        Recorded.OnNext(100+1, NoteOn(2, 96, 64, 3)),
                        Recorded.OnNext(101+1, NoteOn(1, 96, 64, 3)),
                        Recorded.OnNext(105+1, NoteOff(2, 96, 64, 3)),
                        Recorded.OnNext(106+1, NoteOff(1, 96, 64, 3)),
                        Recorded.OnNext(110+1, NoteOn(2, 95, 64, 3)),
                        Recorded.OnNext(111+1, NoteOn(1, 95, 64, 3)),
                        Recorded.OnNext(112+1, NoteOff(2, 95, 64, 3)),
                        Recorded.OnNext(113+1, NoteOff(1, 95, 64, 3)),

                    ]
                });
    }

    [TestMethod]
    public void RegressionTest17SavingPoints()
    {
        var events = new[]
        {
            Recorded.OnNext(100, NoteOn(2, 96, 64, 3)),
            Recorded.OnNext(101, NoteOn(1, 96, 64, 3)),
            Recorded.OnNext(105, NoteOff(2, 96, 64, 3)),
            Recorded.OnNext(106, NoteOff(1, 96, 64, 3)),
            Recorded.OnNext(110, NoteOn(2, 95, 64, 3)),
            Recorded.OnNext(111, NoteOn(1, 95, 64, 3)),
            Recorded.OnNext(112, NoteOff(2, 95, 64, 3)),
            Recorded.OnNext(113, NoteOff(1, 95, 64, 3)),
        };

        TimeSpan timeToSaveAfterHeldEvents = TimeSpan.FromTicks(30);
        TimeSpan timeToSaveAfterAllOff = TimeSpan.FromTicks(15);

        var scheduler = new TestScheduler();
        var sut = CreateSplit(scheduler, events, timeToSaveAfterHeldEvents, timeToSaveAfterAllOff);

        var result = sut.SavingPoints.Record(scheduler);
        result.Should()
            .BeEquivalentTo(
                new[]
                {
                    Recorded.OnNext(events.Last().Time + timeToSaveAfterAllOff.Ticks + 1, unit),
                });
    }

    [TestMethod]
    public void ExpectedHeldTimeoutAndAllOffSavingPoints()
    {
        var events = new[]
        {
            Recorded.OnNext(100, NoteOn(2, 96, 64, 3)),
            Recorded.OnNext(101, NoteOn(1, 96, 64, 3)),
            Recorded.OnNext(140, NoteOn(2, 95, 64, 3)),
            Recorded.OnNext(141, NoteOn(1, 95, 64, 3)),
            Recorded.OnNext(142, NoteOff(2, 95, 64, 3)),
            Recorded.OnNext(149, NoteOff(1, 95, 64, 3)),
        };

        TimeSpan timeToSaveAfterHeldEvents = TimeSpan.FromTicks(30);
        TimeSpan timeToSaveAfterAllOff = TimeSpan.FromTicks(16);

        var scheduler = new TestScheduler();
        var sut = CreateSplit(scheduler, events, timeToSaveAfterHeldEvents, timeToSaveAfterAllOff);

        var result = sut.SavingPoints.Record(scheduler);
        result.Should()
            .BeEquivalentTo(
                new[]
                {
                    Recorded.OnNext(149+16+1, unit),
                });
    }

    [TestMethod]
    public void ExpectedHeldTimeoutAndAllOffSavingPoints1()
    {
        var events = new[]
        {
            Recorded.OnNext(100, NoteOn(2, 96, 64, 3)),
            Recorded.OnNext(101, NoteOn(1, 96, 64, 3)),
            Recorded.OnNext(140, NoteOn(2, 95, 64, 3)),
            Recorded.OnNext(141, NoteOn(1, 95, 64, 3)),
            Recorded.OnNext(142, NoteOff(2, 95, 64, 3)),
            Recorded.OnNext(149, NoteOff(1, 95, 64, 3)),
        };

        TimeSpan timeToSaveAfterHeldEvents = TimeSpan.FromTicks(30);
        TimeSpan timeToSaveAfterAllOff = TimeSpan.FromTicks(16);

        var scheduler = new TestScheduler();
        var sut = CreateSplit(scheduler, events, timeToSaveAfterHeldEvents, timeToSaveAfterAllOff);

        var result = sut.NotesAndPedalsWithoutHeld.Record(scheduler);
        result.Should()
            .BeEquivalentTo(
                new[]
                {
                    Recorded.OnNext(100+1, NoteOn(2, 96, 64, 3)),
                    Recorded.OnNext(101+1, NoteOn(1, 96, 64, 3)),
                    Recorded.OnNext(101+30, NoteOff(2, 96, 64, 3)),
                    Recorded.OnNext(101+30+1, NoteOff(1, 96, 64, 3)),
                    Recorded.OnNext(140+1, NoteOn(2, 95, 64, 3)),
                    Recorded.OnNext(141+1, NoteOn(1, 95, 64, 3)),
                    Recorded.OnNext(142+1, NoteOff(2, 95, 64, 3)),
                    Recorded.OnNext(149+1, NoteOff(1, 95, 64, 3)),
                });
    }

    [TestMethod]
    public void SplitCase()
    {
        var events = new[]
        {
            Recorded.OnNext(100, NoteOn(2, 96, 64, 3)),
            Recorded.OnNext(101, NoteOn(1, 96, 64, 3)),
            Recorded.OnNext(140, NoteOn(2, 95, 64, 3)),
            Recorded.OnNext(141, NoteOn(1, 95, 64, 3)),
            Recorded.OnNext(142, NoteOff(2, 95, 64, 3)),
            Recorded.OnNext(149, NoteOff(1, 95, 64, 3)),
        };

        TimeSpan timeToSaveAfterHeldEvents = TimeSpan.FromTicks(30);
        TimeSpan timeToSaveAfterAllOff = TimeSpan.FromTicks(16);

        var scheduler = new TestScheduler();
        var sut = CreateSplit(scheduler, events, timeToSaveAfterHeldEvents, timeToSaveAfterAllOff);

        var result = sut.SplitGroups.RecordTwoLevels(scheduler);
        result.Should().HaveCount(1);
    }

    [TestMethod]
    public void RegressionTest()
    {
        var events = new[]
        {
            Recorded.OnNext(100, NoteOn(2, 96, 64, 3)),
            Recorded.OnNext(101, NoteOn(1, 96, 64, 3)),
            Recorded.OnNext(105, NoteOff(2, 96, 64, 3)),
            Recorded.OnNext(106, NoteOff(1, 96, 64, 3)),
            Recorded.OnNext(127, NoteOn(2, 95, 64, 3)),
            Recorded.OnNext(128, NoteOn(1, 95, 64, 3)),
            Recorded.OnNext(129, NoteOff(2, 95, 64, 3)),
            Recorded.OnNext(130, NoteOff(1, 95, 64, 3)),
        };

        TimeSpan timeToSaveAfterHeldEvents = TimeSpan.FromTicks(20);
        TimeSpan timeToSaveAfterAllOff = TimeSpan.FromTicks(30);

        var scheduler = new TestScheduler();
        var sut = CreateSplitAux(scheduler);

        var result = sut.SplitGroups.RecordTwoLevels(scheduler);
        result.Should()
            .BeEquivalentTo(
                new Recorded<Notification<NAudioMidiEvent>>[][]
                {
                    [
                        Recorded.OnNext(100+1, NoteOn(2, 96, 64, 3)),
                        Recorded.OnNext(101+1, NoteOn(1, 96, 64, 3)),
                        Recorded.OnNext(105+1, NoteOff(2, 96, 64, 3)),
                        Recorded.OnNext(106+1, NoteOff(1, 96, 64, 3)),
                        Recorded.OnNext(127+1, NoteOn(2, 95, 64, 3)),
                        Recorded.OnNext(128+1, NoteOn(1, 95, 64, 3)),
                        Recorded.OnNext(129+1, NoteOff(2, 95, 64, 3)),
                        Recorded.OnNext(130+1, NoteOff(1, 95, 64, 3)),
                    ]
                });

        MidiSplit<NAudioMidiEvent> CreateSplitAux(TestScheduler testScheduler) =>
            CreateSplit(testScheduler, events, timeToSaveAfterHeldEvents, timeToSaveAfterAllOff);
    }

    [TestMethod]
    public void Split_AdjustedReleaseMarkers()
    {
        var events = new[]
        {
            Recorded.OnNext(100, NoteOn(2, 96, 64, 3)),
            Recorded.OnNext(101, NoteOn(1, 96, 64, 3)),
            Recorded.OnNext(105, NoteOff(2, 96, 64, 3)),
            Recorded.OnNext(106, NoteOff(1, 96, 64, 3)),
            Recorded.OnNext(127, NoteOn(2, 95, 64, 3)),
            Recorded.OnNext(128, NoteOn(1, 95, 64, 3)),
            Recorded.OnNext(129, NoteOff(2, 95, 64, 3)),
            Recorded.OnNext(130, NoteOff(1, 95, 64, 3)),
        };

        TimeSpan timeToSaveAfterHeldEvents = TimeSpan.FromTicks(20);
        TimeSpan timeToSaveAfterAllOff = TimeSpan.FromTicks(30);

        var scheduler = new TestScheduler();
        var sut = CreateSplitAux(scheduler);

        var result2 = sut.AdjustedReleaseMarkers.Record(scheduler);
        result2.Should()
            .BeEquivalentTo(new[]
            {
                Recorded.OnNext(106+1, unit),
                Recorded.OnNext(130+1, unit),
            });
        return;

        MidiSplit<NAudioMidiEvent> CreateSplitAux(TestScheduler testScheduler) =>
            CreateSplit(testScheduler, events, timeToSaveAfterHeldEvents, timeToSaveAfterAllOff);
    }

    private static MidiSplit<T> CreateSplit<T>(
        TestScheduler scheduler,
        Recorded<Notification<T>>[] events,
        TimeSpan timeToSaveAfterHeldEvents,
        TimeSpan timeToSaveAfterAllOff) where T : IMidiEvent =>
        MidiSplitter.Split(
            scheduler.CreateColdObservable(events),
            timeToSaveAfterHeldEvents,
            timeToSaveAfterAllOff,
            scheduler);

    private static int NoteAndSustainPedalCount(string s)
    {
        return int.Parse(s.Trim().Split(' ')[0]);
    }
}

public static class Events
{
    public static NAudioMidiEvent NoteOn(int channel, int noteNumber, int velocity, int port) =>
        new(new NoteOnEvent(100, channel, noteNumber, velocity, 444), port);
    public static NAudioMidiEvent NoteOff(int channel, int noteNumber, int velocity, int port) =>
        new(new NoteEvent(100, channel, MidiCommandCode.NoteOff, noteNumber, velocity), port);
    public static NAudioMidiEvent CC(int channel, MidiController controller, int value, int port) =>
        new(new ControlChangeEvent(100, channel, controller, value), port);
}
