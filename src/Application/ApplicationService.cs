using System.Reactive.Linq;
using LanguageExt;
using Microsoft.Extensions.Logging;

namespace MidiRecorder.Application;

public class ApplicationService<TMidiEvent> where TMidiEvent : IMidiEvent
{
    private readonly Func<IEnumerable<MidiInput>> _getMidiInputs;
    private readonly ILogger<ApplicationService<TMidiEvent>> _logger;
    private readonly Func<string, IEnumerable<(int, string)>> _searchMidiInputId;
    private readonly Func<TMidiEvent, bool> _isNote;
    private readonly Func<string, Func<TMidiEvent, bool>, Validation<string, Unit>> _testFormat;
    private readonly Func<TypedRecordOptions, IMidiSource<TMidiEvent>> _buildMidiSource;
    private readonly Func<TMidiEvent, int> _noteAndSustainPedalCount;
    private readonly Func<IEnumerable<TMidiEvent>, IEnumerable<IEnumerable<TMidiEvent>>> _buildTracks;
    private readonly Action<IEnumerable<IEnumerable<TMidiEvent>>, string, int> _save;
    private readonly Func<Seq<string>, int> _handleError;

    public ApplicationService(Func<IEnumerable<MidiInput>> getMidiInputs, ILogger<ApplicationService<TMidiEvent>> logger, Func<string, IEnumerable<(int, string)>> searchMidiInputId, Func<TMidiEvent, bool> isNote, Func<string, Func<TMidiEvent, bool>, Validation<string, Unit>> testFormat, Func<TypedRecordOptions, IMidiSource<TMidiEvent>> buildMidiSource, Func<TMidiEvent, int> noteAndSustainPedalCount, Func<IEnumerable<TMidiEvent>, IEnumerable<IEnumerable<TMidiEvent>>> buildTracks, Action<IEnumerable<IEnumerable<TMidiEvent>>, string, int> save, Func<Seq<string>, int> handleError)
    {
        _getMidiInputs = getMidiInputs;
        _logger = logger;
        _searchMidiInputId = searchMidiInputId;
        _isNote = isNote;
        _testFormat = testFormat;
        _buildMidiSource = buildMidiSource;
        _noteAndSustainPedalCount = noteAndSustainPedalCount;
        _buildTracks = buildTracks;
        _save = save;
        _handleError = handleError;
    }

    public int ListMidiInputs() =>
        _getMidiInputs().ToSeq()
            .Match(
                () =>
                {
                    _logger.LogError("{Message}", "No MIDI inputs");
                    return 1;
                },
                midiInCapabilities =>
                {
                    _ = midiInCapabilities.Iter((i, midiInput) => Console.WriteLine($"{i}. {midiInput.Name}"));
                    return 0;
                });

    public int Record(IRecordOptions options) =>
        options
            .Validate(
                _searchMidiInputId,
                x => _testFormat(x, _isNote))
            .Match(
                typedOptions =>
                {
                    _ = typedOptions.MidiInputs.Iter(input =>
                        _logger.LogInformation("Using MIDI input {MidiInputId} ({MidiInputName})", input.id, input.name));
                    var source = _buildMidiSource(typedOptions);
                    PrintOptions(typedOptions, _logger);
                    var allEvents = source.AllEvents;

                    NoteDuration.CalculateDurations(
                        allEvents,
                        x => x.IsNoteOn,
                        x => x.IsNoteOff,
                        x => x.NoteNumber)
                        .ForEachAsync(x =>
                        {
                            if (x.duration == TimeSpan.Zero)
                            {
                                _logger.LogTrace("{MidiEvent}", x.ev);
                            }
                            else
                            {
                                _logger.LogTrace(@"{MidiEvent} Dur: {Duration:s\.ff}s", x.ev, x.duration);
                            }
                        });
                    
                    var split = MidiSplitter.Split(
                        allEvents,
                        _noteAndSustainPedalCount,
                        typedOptions.TimeoutToSave,
                        typedOptions.DelayToSave);

                    _ = split.AdjustedReleaseMarkers.ForEachAsync(_ => _logger.LogTrace("All Notes/Pedals Off!"));
                    _ = split.SplitGroups
                        .SelectMany(x =>x
                            .ToArray()
                            .Where(midiEvents => midiEvents.Length > 0)
                            .Select(
                                midiEvents =>
                                {
                                    var filePath = MidiFileContext.BuildFilePath(
                                        typedOptions.PathFormatString,
                                        midiEvents,
                                        DateTime.Now,
                                        Guid.NewGuid(),
                                        _isNote);
                                    var tracks = _buildTracks(midiEvents);
                                    return (tracks, filePath);
                                }))
                        .ForEachAsync(x =>
                        {
                            _logger.LogInformation(
                                "Saving {EventCount} events (plus 1 EndOfTrack) to file {FilePath}...",
                                x.tracks.Sum(y => y.Count()) - 1,
                                x.filePath);
                            try
                            {
                                _save(x.tracks, x.filePath, typedOptions.MidiResolution);
                            }
    #pragma warning disable CA1031
                            catch (Exception ex)
    #pragma warning restore CA1031
                            {
                                _logger.LogError(ex, "There was an error when saving the file");
                            }
                        });

                    
                    source.StartReceiving();
                    
                    _logger.LogInformation("Recording started, Press any key to quit");
                    Console.ReadLine();
                    return 0;
                },
                _handleError);

    private static void PrintOptions(TypedRecordOptions options, ILogger logger)
    {
        (TimeSpan timeToSaveAfterAllOff, TimeSpan timeToSaveAfterHeldEvents, var pathFormatString, var midiResolution, _) = options;
    #pragma warning disable CA1848
        logger.LogInformation("Working dir: {CurrentDirectory}", Environment.CurrentDirectory);
        logger.LogInformation("Delay to save: {DelayToSave}", timeToSaveAfterAllOff);
        logger.LogInformation("Timeout to save: {TimeoutToSave}", timeToSaveAfterHeldEvents);
        logger.LogInformation("Output Path: {PathFormatString}", pathFormatString);
        logger.LogInformation("MIDI resolution: {MidiResolution}", midiResolution);
    }
}
