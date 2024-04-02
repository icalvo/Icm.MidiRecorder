using LanguageExt;

namespace MidiRecorder.Application;

public class MidiSplit<TMidiEvent>
{
    public MidiSplit(
        IObservable<int> heldNotesAndPedals,
        IObservable<TMidiEvent> notesAndPedalsWithoutHeld,
        IObservable<Unit> adjustedReleaseMarkers,
        IObservable<Unit> savingPoints,
        IObservable<IObservable<TMidiEvent>> splitGroups,
        IObservable<TMidiEvent> extraOffEvents)
    {
        HeldNotesAndPedals = heldNotesAndPedals;
        NotesAndPedalsWithoutHeld = notesAndPedalsWithoutHeld;
        AdjustedReleaseMarkers = adjustedReleaseMarkers;
        SavingPoints = savingPoints;
        SplitGroups = splitGroups;
        ExtraOffEvents = extraOffEvents;
    }

    public IObservable<int> HeldNotesAndPedals { get; }
    public IObservable<TMidiEvent> NotesAndPedalsWithoutHeld { get; }
    public IObservable<Unit> AdjustedReleaseMarkers { get; }
    public IObservable<Unit> SavingPoints { get; }
    public IObservable<IObservable<TMidiEvent>> SplitGroups { get; }
    public IObservable<TMidiEvent> ExtraOffEvents { get; }
}
