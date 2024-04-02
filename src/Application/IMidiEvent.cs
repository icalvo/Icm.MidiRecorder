namespace MidiRecorder.Application;

public interface IMidiEvent
{
    bool IsNoteOn { get; }
    bool IsNoteOff { get; }
    bool IsPedalOn { get; }
    bool IsPedalOff { get; }
    bool IsNote { get; }
    bool IsPedal { get; }
    bool IsNoteOrPedal => IsNote || IsPedal;
    int NoteNumber { get; }
    int Port { get; init; }
    string NotePedalKey { get; }
    
    IMidiEvent OffComplement { get; }
}
