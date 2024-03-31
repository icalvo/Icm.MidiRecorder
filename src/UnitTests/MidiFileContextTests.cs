using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MidiRecorder.Application;

namespace MidiRecorder.Tests;

[TestClass]
public class MidiFileContextTests
{
    [TestMethod]
    public void BuildFilePath_AllData()
    {
        var eventList = new[]
        {
            "NOTE",
            "Other"
        };
        
        var guid = new Guid("64ea2c65-12b9-44c7-8d0b-fcf9a298f156");
        var result = MidiFileContext.BuildFilePath(
            "{Guid}/{NumberOfNoteEvents}/{NumberOfEvents}/{Now:yyyy}/{Now:MM}/{Now:dd_HH_mm_ss}.mid",
            eventList,
            new DateTime(2024, 3, 17, 14, 34, 22),
            guid,
            x => x == "NOTE");
        result.Should().Be("64ea2c65-12b9-44c7-8d0b-fcf9a298f156/1/2/2024/03/17_14_34_22.mid");
    }
}
