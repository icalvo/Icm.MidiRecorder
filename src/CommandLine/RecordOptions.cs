using System.Diagnostics.CodeAnalysis;
using CommandLine;
using CommandLine.Text;
using MidiRecorder.Application;

namespace MidiRecorder.CommandLine;

[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
[Verb("record", true, HelpText = "Records MIDI to files")]
public class RecordOptions : IRecordOptions
{
    public RecordOptions(IEnumerable<string> midiInputs, long delayToSave, string pathFormatString, int midiResolution)
    {
        MidiInputs = midiInputs;
        PathFormatString = pathFormatString;
        DelayToSave = delayToSave;
        MidiResolution = midiResolution;
    }

    [Option('i', "input", HelpText = "MIDI Input name or index", Default = new[] { "*" }, Separator = ',')]
    public IEnumerable<string> MidiInputs { get; }

    [Option(
        'd',
        "delay",
        HelpText = "Delay (in milliseconds) in silence before saving the latest recorded MIDI events",
        Default = 5000)]
    public long DelayToSave { get; }

    [Option('f', "format", HelpText = "Format String for output MIDI path", Default = "{Now:yyyyMMddHHmmss}.mid")]
    public string PathFormatString { get; }

    [Option('r', "resolution", HelpText = "MIDI resolution in pulses per quarter note (PPQN)", Default = 480)]
    public int MidiResolution { get; }

    [Usage]
    public static IEnumerable<Example> Examples
    {
        get
        {
            yield return new Example(
                "normal scenario",
                new RecordOptions(new[] { "M1", "Triton" }, 5000, "{Now}.mid", 480));
            yield return new Example(
                "date-based folder structure",
                new RecordOptions(
                    new[] { "Impulse" },
                    7000,
                    @"{Now:yyyy}\{Now:MM}\{Now:dd}\{Now:yyyyMMddHHmmss}_{NumberOfNoteEvents}.mid",
                    960));
        }
    }
}
