using NAudio.Midi;

namespace MidiRecorder.Application.Implementation;

public record NAudioMidiEvent(MidiEvent MidiEvent, int Port) : IMidiEvent
{
    public bool IsNoteOn => MidiEvent is NoteEvent { CommandCode: MidiCommandCode.NoteOn };
    public bool IsNoteOff => MidiEvent is NoteEvent { CommandCode: MidiCommandCode.NoteOff };
    public int NoteNumber => MidiEvent is NoteEvent n ? n.NoteNumber : 0;   
    public override string ToString()
    {
        return $"P{Port} {MidiEvent}";
    }
}
