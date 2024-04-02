# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

- Service mode to have MIDI Recorder permanently on, or "Autostart" feature.
- Notification icon.
- Customize MIDI track names.

## [1.2.0] - 2024-03-11

### Added
- MIDI files now have track names that show channel and port.

### Fixed
- Issue #17: problem with empty saves.
- Issue #17: wrong detection of note on and note off.
- `NumberOfNoteEvents` was not correctly calculated.
- Removed weird negative lengths on NoteOn.

## [1.1.2] - 2023-02-19

### Fixed
- Sustain Pedal On was not correctly detected.

## [1.1.1] - 2023-02-16

### Added
- Check output path format string (option `-f`) before start recording.

## [1.1.0] - 2023-02-16

### Added
- Split files after all notes and sustain pedal are released.
- Create tracks by channel/port

## [1.0.3] - 2021-03-20

### Added

- Option `-r` to specify MIDI file resolution.

## [1.0.2] - 2021-03-20

### Fixed

- The default format didn't work correctly.

## [1.0.1] - 2021-03-20

### Added

- Shortened executable name to `midirec`.
- Binaries for Windows, Linux and Mac.

## [1.0.0] - 2021-03-20

### Added

- Automatic MIDI recording and saving to files
- Configurable list of MIDI inputs
- Configurable delay to trigger file save
- Configurable path for saved files
- `list` verb to list the MIDI inputs on your system
