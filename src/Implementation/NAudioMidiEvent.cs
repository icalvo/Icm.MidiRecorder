using System.Text.RegularExpressions;
using NAudio.Midi;

namespace MidiRecorder.Application.Implementation;

public class NAudioMidiEvent : IMidiEvent
{
    public NAudioMidiEvent(MidiEvent midiEvent, int port)
    {
        MidiEvent = midiEvent;
        Port = port;
    }

    public bool IsNoteOn => MidiEvent.IsNoteOn(MidiEvent);
    public bool IsNoteOff => MidiEvent.IsNoteOff(MidiEvent);
    public bool IsPedalOn => MidiEvent is ControlChangeEvent { Controller: MidiController.Sustain, ControllerValue: 127 };

    public bool IsPedalOff =>
        MidiEvent is ControlChangeEvent { Controller: MidiController.Sustain, ControllerValue: 0 };

    public int NoteNumber => MidiEvent is NoteEvent n ? n.NoteNumber : 0;
    public string NotePedalKey => IsNote ? $"{NoteNumber}{MidiEvent.Channel}{Port}" : "P";

    public IMidiEvent OffComplement =>
        MidiEvent switch
        {
            NoteEvent n => new NAudioMidiEvent(new NoteEvent(n.AbsoluteTime, n.Channel, MidiCommandCode.NoteOff, n.NoteNumber, n.Velocity), Port),
            ControlChangeEvent cc => new NAudioMidiEvent(new ControlChangeEvent(cc.AbsoluteTime, cc.Channel, cc.Controller, 0), Port),
            _ => this
        };

    public bool IsNote => MidiEvent is NoteEvent;
    public bool IsPedal => MidiEvent is ControlChangeEvent { Controller: MidiController.Sustain };
    public MidiEvent MidiEvent { get; }
    public int Port { get; init; }

    // [GeneratedRegex(@" Len: \d+")]
    // private static partial Regex Regex();

    private static readonly Regex Regex = new(@" Len: \d+");
    public override string ToString()
    {
        return $"P{Port} {Regex.Replace(MidiEvent.ToString(), "")}";
    }

    public bool Equals(NAudioMidiEvent? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return MidiEvent.ToString().Equals(other.MidiEvent.ToString()) && Port == other.Port;
    }

    public override bool Equals(object? obj) =>
        !ReferenceEquals(null, obj) &&
        (ReferenceEquals(this, obj) || obj.GetType() == GetType() && Equals((NAudioMidiEvent)obj));

    public override int GetHashCode() => HashCode.Combine(MidiEvent.ToString(), Port);
}
