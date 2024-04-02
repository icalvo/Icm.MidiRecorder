using NAudio.Midi;

namespace MidiRecorder.Application.Implementation;

public static class NAudioMidiInputs
{
    public static IEnumerable<MidiInput> GetMidiInputs()
    {
        for (var device = 0; device < MidiIn.NumberOfDevices; device++)
        {
            MidiInCapabilities midiInCapabilities = MidiIn.DeviceInfo(device);
            yield return CreateMidiInput(midiInCapabilities);
        }
    }

    public static IEnumerable<(int, string)> SearchMidiInputId(string midiInputName)
    {
        var midiInCapabilities = GetMidiInputs().ToArray();

        if (midiInCapabilities.Length == 0)
        {
            yield break;
        }

        if (midiInputName == "*")
        {
            foreach (var i in Enumerable.Range(0, midiInCapabilities.Length))
            {
                yield return (i, midiInCapabilities[i].Name);
            }

            yield break;
        }

        int? selectedIdx = null;
        if (int.TryParse(midiInputName, out var s))
        {
            selectedIdx = s >= 0 && s < midiInCapabilities.Length ? s : null;
        }

        selectedIdx ??= midiInCapabilities.Select((port, idx) => new { port, idx })
            .FirstOrDefault(x => string.Equals(x.port.Name, midiInputName, StringComparison.OrdinalIgnoreCase))
            ?.idx;

        if (selectedIdx == null)
        {
            yield break;
        }

        yield return (selectedIdx.Value, midiInCapabilities[selectedIdx.Value].Name);
    }

    private static MidiInput CreateMidiInput(MidiInCapabilities capabilities)
    {
        return new MidiInput(capabilities.ProductName);
    }
}
