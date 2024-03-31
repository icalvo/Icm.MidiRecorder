namespace MidiRecorder.Application;

public interface IMidiEvent
{
    bool IsNoteOn { get; }
    bool IsNoteOff { get; }
    int NoteNumber { get; }
    int Port { get; init; }
}
