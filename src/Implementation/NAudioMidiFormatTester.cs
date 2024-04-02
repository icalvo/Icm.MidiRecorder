using LanguageExt;
using NAudio.Midi;

namespace MidiRecorder.Application.Implementation;

public static class NAudioMidiFormatTester
{
    public static Validation<string, Unit> TestFormat(string pathFormatString)
    {
        var eventList = new[] { new NAudioMidiEvent(new NoteOnEvent(11, 1, 78, 34, 333), 0) };
        
        try
        {
            _ = MidiFileContext.BuildFilePath(pathFormatString, eventList, DateTime.Now, Guid.NewGuid());
            return Prelude.unit;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }
}
