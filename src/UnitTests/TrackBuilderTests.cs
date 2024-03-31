using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MidiRecorder.Application.Implementation;

namespace MidiRecorder.Tests;

[TestClass]
public class TrackBuilderTests
{
    [TestMethod(displayName:"Regression for #17")]
    public void Test()
    {
        NAudioMidiTrackBuilder.BuildTracks(Array.Empty<NAudioMidiEvent>()).Should().BeEmpty();
    }    
}
