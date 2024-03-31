using NAudio.Midi;

namespace MidiRecorder.Application.Implementation;

public static class NAudioMidiFileSaver
{
    public static void Save(IEnumerable<IEnumerable<NAudioMidiEvent>> tracks, string filePath, int timeDivision)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var midiEventCollection = new MidiEventCollection(1, timeDivision);
        foreach (var track in tracks)
        {
            midiEventCollection.AddTrack(track.Select(mp => mp.MidiEvent).ToList());
        }

        MidiFile.Export(filePath, midiEventCollection);
    }
}
