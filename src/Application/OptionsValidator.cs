using LanguageExt;

namespace MidiRecorder.Application;

internal static class OptionsValidator
{
    public static Validation<string, TypedRecordOptions> Validate(
        this IRecordOptions options,
        Func<string, IEnumerable<(int, string)>> midiInputSearch,
        Func<string, Validation<string, Unit>> testFormat) =>
        options.MidiInputs
            .ToSeq()
            .SelectMany(midiInputSearch)
            .Distinct()
            .Match(
                () => $"No MIDI inputs for '{string.Join(", ", options.MidiInputs)}' could be located",
                inputIds =>
                    testFormat(options.PathFormatString)
                        .Map(_ =>
                            new TypedRecordOptions(
                                TimeSpan.FromMilliseconds(options.DelayToSave),
                                TimeSpan.FromMilliseconds(30000),
                                options.PathFormatString,
                                options.MidiResolution,
                                inputIds)));
}
