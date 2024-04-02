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

        var tracks = from midiEvent in events
            group midiEvent by (midiEvent.Port, midiEvent.MidiEvent.Channel)
            into trackGroup
            orderby trackGroup.Key
            let metaEvents = new[] { EndOfTrackMarker(trackGroup), TrackName(trackGroup.Key.Channel, trackGroup.Key.Port) }
            select (NAudioMidiEvent[])[
                TrackName(trackGroup.Key.Channel, trackGroup.Key.Port),
                ..trackGroup,
                EndOfTrackMarker(trackGroup)];

        NAudioMidiEvent[] metaTrack = [
            new NAudioMidiEvent(new TextEvent("Arranger Track", MetaEventType.SequenceTrackName, 0), 0),
            new NAudioMidiEvent(new MetaEvent(MetaEventType.EndTrack, 0, 0), 0)];
        return [metaTrack, ..tracks];
    }

    private static NAudioMidiEvent EndOfTrackMarker(IEnumerable<NAudioMidiEvent> track)
    {
        return new NAudioMidiEvent(
            new MetaEvent(MetaEventType.EndTrack, 0, track.Last().MidiEvent.AbsoluteTime + 1),
            0);
    }

    private static NAudioMidiEvent TrackName(int channel, int port)
    {
        return new NAudioMidiEvent(
            new TextEvent($"Channel {channel}, Port {port}", MetaEventType.SequenceTrackName, 0),
            0);
    }
}
