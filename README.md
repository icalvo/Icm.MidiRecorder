[![Build status](https://github.com/icalvo/Icm.MidiRecorder/actions/workflows/ci.yml/badge.svg)](https://github.com/icalvo/Icm.MidiRecorder/actions/workflows/ci.yml)

![GitHub release (latest by date)](https://img.shields.io/github/v/release/MidiRecorder/MidiRecorder)

![GitHub release (latest by date)](https://img.shields.io/github/downloads/MidiRecorder/MidiRecorder/latest/total)

# MIDI Recorder

## Introduction

Every musician has improvised something that then was unable to remember precisely. With the current technology you can solve that problem: just fire your favorite DAW and click record on a MIDI track. But this system is a bit cumbersome: first of all you have to remember to open your DAW, or have it permanently opened. You need a separate project because probably you are working on other things. So maybe two DAW instances? And you need to open this secondary instance every time you restart your system! Maybe your DAW offers some feature for background MIDI recording, but, does it preserve the data if you close the DAW (or it is closed accidentally)? How easy is to navigate the recorded MIDI data after, say, 3 days, in which you have maybe played at several points of the day?

If you think that there must be an alternative way, you are right!

**MIDI Recorder** is a little tool that allows you to record into MIDI files everything you play on your MIDI controllers. You can easily set MIDI Recorder to run when your system starts. MIDI Recorder splits what you play based on silences and date/time. You can set up a custom folder and filename structure based on date/time, some characteristics of the the MIDI Input, and other available properties. You can have it running while you work with your favorite DAW.

## Usage

MIDI Recorder will give you results without parameters:
```
midirecord.exe
```

This way, it will record from all your MIDI inputs at once. Every time it detects a pause of 5 seconds in all the devices, it saves a file with the format `yyyyMMddHHmmss.mid` in your working directory. You can stop recording by pressing any key.

You can further customize the behavior by specifying the MIDI inputs to be recorded, the delay and the format of the saved filenames.

### MIDI Inputs (`-i` or `--input`)

You refer to your MIDI inputs by its name or by its index. To know the indexes and names of the MIDI inputs in your system, use the `list` verb:

```
midirecord.exe list
```

You can specify several MIDI inputs separating them by commas:

```
midirecord.exe -i M1,Triton
```

Finally, as said earlier, you can record all available MIDI inputs if you omit the `-i` option.

### Path Formatting (`-f` or `--format`)

There are three available variables you can use in your path format string:


Variable | Type | Description
---------|------|------------
`Now` | `System.DateTime` | Date and time when the file is saved.
`NumberOfEvents` | `System.Int32`  | Number of MIDI events to be saved (including controllers, note-off, after touch, etc.)
`NumberOfNoteEvents` | `System.Int32` | Number of Note ON MIDI events
`Guid`  | `System.Guid` | A randomly generated unique identifier.

These variables can be used in your format string using the [.NET rules for standard and custom formatting](https://docs.microsoft.com/es-es/dotnet/standard/base-types/formatting-types). Take into account that, instead of index numbers, you have to use the full name of the variable. So, for example, you typically use the format strings like:

```csharp
	String.Format("{0:yyyy}/{0:HHmmss}{1}_{2}.mid", Now, NumberOfEvents, Guid);
```

But here you would use instead:
```
	{Now:yyyyMMdd}/{Now:HHmmss}{NumberOfEvents}_{Guid}.mid
```