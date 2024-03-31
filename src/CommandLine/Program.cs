using System.Diagnostics;
using System.Reflection;
using CommandLine;
using CommandLine.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MidiRecorder.Application;
using MidiRecorder.Application.Implementation;
using MidiRecorder.CommandLine;
using MidiRecorder.CommandLine.Logging;

const string environmentVarPrefix = "MidiRecorder_";
IConfigurationRoot config = new ConfigurationBuilder().AddJsonFile("appsettings.json", false, false)
    .AddEnvironmentVariables(environmentVarPrefix)
    .Build();

using ILoggerFactory loggerFactory = LoggerFactory.Create(
    builder =>
    {
        builder.ClearProviders();
        builder.AddConfiguration(config.GetSection("Logging"));

        builder.AddConsole();
        builder.AddConsoleFormatter<CustomConsoleFormatter, CustomConsoleFormatterOptions>();
    });
ILogger logger = loggerFactory.CreateLogger("MidiRecorder");

using var parser = new Parser(with => { with.HelpWriter = null; });

var parserResult = parser.ParseArguments<RecordOptions, ListMidiInputsOptions>(args);

try
{
    var appService = new ApplicationService<NAudioMidiEvent>(
        NAudioMidiInputs.GetMidiInputs,
        loggerFactory.CreateLogger<ApplicationService<NAudioMidiEvent>>(),
        NAudioMidiInputs.SearchMidiInputId,
        NAudioMidiEventAnalyzer.IsNote,
        NAudioMidiFormatTester.TestFormat,
        o => new NAudioMidiSource(o),
        NAudioMidiEventAnalyzer.NoteAndSustainPedalCount,
        NAudioMidiTrackBuilder.BuildTracks,
        NAudioMidiFileSaver.Save,
        errorMessage =>
        {
            logger.LogCritical("{Message}", errorMessage);
            DisplayHelp(parserResult, Enumerable.Empty<Error>());
            return 1;
        });
    return parserResult.MapResult<RecordOptions, ListMidiInputsOptions, int>(
        appService.Record,
        _ => appService.ListMidiInputs(),
        errors => DisplayHelp(parserResult, errors));
}
#pragma warning disable CA1031 Topmost catch to present exception
catch (Exception ex)
#pragma warning restore CA1031
{
    logger.LogCritical(ex.Demystify(), "{Message}", ex.Message);
    return 1;
}

static int DisplayHelp<T>(ParserResult<T> result, IEnumerable<Error> errors)
{
    var errs = errors.ToArray();

    if (errs.IsVersion())
    {
        var helpText = HelpText.AutoBuild(result);
        Console.WriteLine(helpText);
        return 0;
    }

    if (errs.IsHelp())
    {
        Console.WriteLine(GetHelpText(true));
        return 0;
    }

    Console.WriteLine(GetHelpText(false));
    return 1;

    string GetHelpText(bool verbs)
    {
        return HelpText.AutoBuild(
            result,
            h =>
            {
                h.AdditionalNewLineAfterOption = false;
                var assemblyDescription = Assembly.GetExecutingAssembly()
                    .GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false)
                    .OfType<AssemblyDescriptionAttribute>()
                    .FirstOrDefault()
                    ?.Description;
                if (errs.IsHelp())
                {
                    h.AddPreOptionsLine(assemblyDescription);
                }

                return HelpText.DefaultParsingErrorsHandler(result, h);
            },
            e => e,
            verbs);
    }
}
