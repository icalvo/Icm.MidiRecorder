using System.Reactive.Concurrency;
using System.Reactive.Linq;
using static LanguageExt.Prelude;

namespace MidiRecorder.Application;

public static class MidiSplitter
{
    public static MidiSplit<TMidiEvent> Split<TMidiEvent>(
        IObservable<TMidiEvent> allEvents,
        Func<TMidiEvent, int> noteAndSustainPedalCount,
        TimeSpan timeToSaveAfterHeldEvents,
        TimeSpan timeToSaveAfterAllOff,
        IScheduler? scheduler = null)
    {
        scheduler ??= Scheduler.Default;
        // How many notes + sustain pedal are held?
        var heldNotesAndPedals = allEvents.Select(noteAndSustainPedalCount)
            .Where(x => x != 0)
            .Scan(0, (accum, n) => Math.Max(0, accum + n));

        // Finds points where a note or pedal is held for too long
        // (The marker is at the end of such a pause)
        var heldNotesAndPedalsTimeoutMarkers =
            heldNotesAndPedals
                .Throttle(timeToSaveAfterHeldEvents, scheduler)
                .Where(x => x > 0)
                .Select(_ => unit);

        // Introduce a zero in held notes and pedals where a note or pedal is held for too long;
        // remove repeated zeros.
        var adjustedHeldNotesAndPedals =
            heldNotesAndPedals
                .Merge(heldNotesAndPedalsTimeoutMarkers.Select(_ => 0))
                .DistinctUntilChanged();

        // Release markers (after removing notes and pedals held for too long)
        var adjustedReleaseMarkers =
            adjustedHeldNotesAndPedals
                .Where(x => x == 0)
                .Select(_ => unit);

        // Remove releases that are too close to each other
        var globalReleaseSavingPoints = adjustedReleaseMarkers.Throttle(timeToSaveAfterAllOff, scheduler).Select(_ => unit);

        var savingPoints = heldNotesAndPedalsTimeoutMarkers.Merge(globalReleaseSavingPoints);

        var splitGroups = allEvents.Window(savingPoints);

        return new MidiSplit<TMidiEvent>(
            heldNotesAndPedals,
            heldNotesAndPedalsTimeoutMarkers,
            adjustedHeldNotesAndPedals,
            adjustedReleaseMarkers,
            globalReleaseSavingPoints,
            savingPoints,
            splitGroups);
    }
}
