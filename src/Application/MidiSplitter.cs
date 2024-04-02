using System.Reactive.Concurrency;
using System.Reactive.Linq;
using static LanguageExt.Prelude;

namespace MidiRecorder.Application;

public static class MidiSplitter
{
    public static MidiSplit<TMidiEvent> Split<TMidiEvent>(
        IObservable<TMidiEvent> allEvents,
        TimeSpan timeToSaveAfterHeldEvents,
        TimeSpan timeToSaveAfterAllOff,
        IScheduler? scheduler = null)
        where TMidiEvent: IMidiEvent
    {
        scheduler ??= Scheduler.Default;

        var notesAndPedals = allEvents.Where(e => e.IsNoteOrPedal);
        var extraOffEvents = notesAndPedals.GroupBy(n => n.NotePedalKey)
            .SelectMany(x => x
                    .Throttle(timeToSaveAfterHeldEvents, scheduler)
                    .Where(x1 => x1.IsNoteOn || x1.IsPedalOn)
                    .Select(x2 => (TMidiEvent)x2.OffComplement)
                .DistinctUntilChanged());
        
        var notesAndPedalWithoutHeld = Observable.Merge(notesAndPedals, extraOffEvents);
        
        // How many notes + sustain pedal are held?
        var heldNotesAndPedals = notesAndPedalWithoutHeld.Select(e => e.IsNoteOn || e.IsPedalOn ? 1 : e.IsNoteOff || e.IsPedalOff ? -1 : 0)
            .Where(x => x != 0)
            .Scan(0, (accum, n) => Math.Max(0, accum + n));

        // Release markers (after removing notes and pedals held for too long)
        var adjustedReleaseMarkers =
            heldNotesAndPedals
                .Where(x => x == 0)
                .Select(_ => unit);

        // Remove releases that are too close to each other
        var savingPoints = heldNotesAndPedals.Throttle(timeToSaveAfterAllOff, scheduler).Where(x => x == 0).Select(_ => unit);

        var splitGroups = allEvents.Window(savingPoints);

        return new MidiSplit<TMidiEvent>(
            heldNotesAndPedals,
            notesAndPedalWithoutHeld,
            adjustedReleaseMarkers,
            savingPoints,
            splitGroups,
            extraOffEvents);
    }
}
