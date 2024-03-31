using NAudio.Midi;

namespace MidiRecorder.Application.Implementation;

public static class NAudioMidiTrackBuilder
{
    public static IEnumerable<IEnumerable<NAudioMidiEvent>> BuildTracks(IEnumerable<NAudioMidiEvent> midiEvents)
    {
        var events = midiEvents.ToArray();
        if (events.Length == 0)
        {
            return Enumerable.Empty<IEnumerable<NAudioMidiEvent>>();
        }

        var firstTime = events[0].MidiEvent.AbsoluteTime;
        foreach (NAudioMidiEvent midiEvent in events)
        {
            midiEvent.MidiEvent.AbsoluteTime -= firstTime;
        }

        return from midiEvent in events
            group midiEvent by (midiEvent.Port, midiEvent.MidiEvent.Channel)
            into trackGroups
            orderby trackGroups.Key
            let x = trackGroups.Concat(new[] { EndOfTrackMarker(trackGroups) })
            select x;
    }

    private static NAudioMidiEvent EndOfTrackMarker(IEnumerable<NAudioMidiEvent> track)
    {
        return new NAudioMidiEvent(
            new MetaEvent(MetaEventType.EndTrack, 0, track.Last().MidiEvent.AbsoluteTime + 1),
            0);
    }
}
