using NAudio.Midi;

namespace MidiRecorder.Application.Implementation;

public static class NAudioMidiEventAnalyzer
{
    public static int NoteAndSustainPedalCount(NAudioMidiEvent midiEvent)
    {
        return midiEvent.MidiEvent switch
        {
            NoteEvent { CommandCode: MidiCommandCode.NoteOn } => 1,
            NoteEvent { CommandCode: MidiCommandCode.NoteOff } => -1,
            ControlChangeEvent { Controller: MidiController.Sustain, ControllerValue: 127 } => 1,
            ControlChangeEvent { Controller: MidiController.Sustain, ControllerValue: 0 } => -1,
            _ => 0
        };
    }

    public static bool IsNote(NAudioMidiEvent midiEvent)
    {
        return midiEvent.MidiEvent is NoteEvent;
    }
}
