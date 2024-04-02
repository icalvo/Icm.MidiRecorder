namespace MidiRecorder.Application;

public interface IRecordOptions
{
    IEnumerable<string> MidiInputs { get; }
    long DelayToSave { get; }
    string PathFormatString { get; }
    int MidiResolution { get; }
}
