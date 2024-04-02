using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MidiRecorder.Application;

namespace MidiRecorder.Tests;

[TestClass]
public class StringExtTests
{
    [TestMethod]
    public void TranslateToStandardFormatString_HappyPath()
    {
        var actual = StringExt.TranslateToStandardFormatString("{Number} {Date:yyyyMM}-{Number:x}");
        actual.format.Should().Be("{0} {1:yyyyMM}-{0:x}");
        actual.itemNames.Should().BeEquivalentTo("Number", "Date");
    }

    [TestMethod]
    public void Format_EmptyBraces_ThrowsFormatException()
    {
        Action action = () => StringExt.Format(
            "{Number} {Date:yyyyMM}-{}",
            new { Number = 234, Date = new DateTime(2021, 02, 14) });

        action.Should().Throw<FormatException>();
    }

    [TestMethod]
    public void Format_NoItemNameAndThenColon_ThrowsFormatException()
    {
        Action action = () => StringExt.Format(
            "{Number} {:yyyyMM}-{}",
            new { Number = 234, Date = new DateTime(2021, 02, 14) });

        action.Should().Throw<FormatException>();
    }

    [TestMethod]
    public void Format_NoItemNameAndThenComma_ThrowsFormatException()
    {
        Action action = () => StringExt.Format(
            "{Number} {,7}-{}",
            new { Number = 234, Date = new DateTime(2021, 02, 14) });

        action.Should().Throw<FormatException>();
    }

    [TestMethod]
    public void Format_NullFormatString_ThrowsArgumentException()
    {
        Action action = () => StringExt.Format(null!, new { Number = 234, Date = new DateTime(2021, 02, 14) });

        action.Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void Format_NullObject_ThrowsArgumentNullException()
    {
        Action action = () => StringExt.Format("{Number}", null!);

        action.Should().Throw<ArgumentNullException>();
    }

    [TestMethod]
    public void Format_NullObjectIfNotNeeded_Success()
    {
        Action action = () => StringExt.Format("No placeholders", null!);

        action.Should().NotThrow();
    }

    [TestMethod]
    public void Format_ValueTupleData_ThrowsArgumentException()
    {
        Action action = () => StringExt.Format("{Number}", (Number: 7, Text: "hello"));

        action.Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void Format_PropertyNotFound_ThrowsArgumentException()
    {
        Action action = () => StringExt.Format("{Other}", new { Number = 7, Text = "hello" });

        action.Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void Format_HappyPath()
    {
        StringExt.Format("{Number} {Date:yyyyMM}-{Number,4:x}", new { Number = 234, Date = new DateTime(2021, 02, 14) })
            .Should()
            .Be("234 202102-  ea");
    }
}
