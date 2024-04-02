namespace MidiRecorder.Application;

public record TypedRecordOptions(
    TimeSpan DelayToSave,
    TimeSpan TimeoutToSave,
    string PathFormatString,
    int MidiResolution,
    IEnumerable<(int id, string name)> MidiInputs)
{
    public override string ToString()
    {
        return
            $"{{ timeToSaveAfterAllOff = {DelayToSave}, timeToSaveAfterHeldEvents = {TimeoutToSave}, pathFormatString = {PathFormatString}, midiResolution = {MidiResolution} }}";
    }
}
