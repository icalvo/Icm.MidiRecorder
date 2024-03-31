using System.Globalization;
using System.Reflection;
using System.Text;

namespace MidiRecorder.Application;

internal static class StringExt
{
    public static string Format(string formatString, object data)
    {
        static object? GetPropertyValue(object target, string propertyName)
        {
            if (target == null)
            {
                throw new ArgumentNullException(
                    nameof(target),
                    "The format string has placeholders and you passed null data.");
            }

            PropertyInfo? propertyInfo = target.GetType().GetProperty(propertyName);
            if (propertyInfo != null)
            {
                return propertyInfo.GetValue(target, null);
            }

            if (target is ValueTuple)
            {
                throw new ArgumentException(
                    $"Property {propertyName} not found. Don't use ValueTuple like (PropA:ValA, PropB: ValB). Instead use anonymous object syntax: new {{ PropA = ValA, PropB = ValB }}.",
                    nameof(propertyName));
            }

            var availableProperties = string.Join(", ", target.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(x => x.Name));
            throw new ArgumentException($"Property {propertyName} not found. Should be one of: {availableProperties}", nameof(propertyName));
        }

        var (format, itemNames) = TranslateToStandardFormatString(formatString);
        return string.Format(
            CultureInfo.CurrentCulture,
            format,
            itemNames.Select(itemName => GetPropertyValue(data, itemName)).ToArray());
    }

    public static (string format, List<string> itemNames) TranslateToStandardFormatString(string formatString)
    {
        if (string.IsNullOrWhiteSpace(formatString))
        {
            throw new ArgumentException(
                $"'{nameof(formatString)}' cannot be null or whitespace.",
                nameof(formatString));
        }

        var stringFormatBuilder = new StringBuilder(formatString.Length);
        var state = ParseState.Outside;
        var tagNameBuilder = new StringBuilder();
        var itemNames = new List<string>();

        int GetIndexFor(string itemName)
        {
            var existingItem = itemNames.IndexOf(itemName);
            if (existingItem == -1)
            {
                itemNames.Add(itemName);

                return itemNames.Count - 1;
            }

            return existingItem;
        }

        foreach (var character in formatString)
        {
            switch ((state, character))
            {
                case (ParseState.Outside, '{'):
                    stringFormatBuilder.Append(character);
                    state = ParseState.OpenBracket;
                    break;
                case (ParseState.Outside, _):
                    stringFormatBuilder.Append(character);
                    break;
                case (ParseState.OpenBracket, '{'):
                    stringFormatBuilder.Append(character);
                    state = ParseState.Outside;
                    break;
                case (ParseState.OpenBracket, '}'):
                case (ParseState.OpenBracket, ','):
                case (ParseState.OpenBracket, ':'):
                    throw new FormatException("Cannot have an empty item name");
                case (ParseState.OpenBracket, _):
                    tagNameBuilder.Append(character);
                    state = ParseState.ItemName;
                    break;
                case (ParseState.ItemName, ','):
                case (ParseState.ItemName, ':'):
                    stringFormatBuilder.Append(GetIndexFor(tagNameBuilder.ToString()));
                    stringFormatBuilder.Append(character);
                    tagNameBuilder.Clear();
                    state = ParseState.ItemAlignmentOrFormat;
                    break;
                case (ParseState.ItemName, '}'):
                    stringFormatBuilder.Append(GetIndexFor(tagNameBuilder.ToString()));
                    stringFormatBuilder.Append(character);
                    tagNameBuilder.Clear();
                    state = ParseState.Outside;
                    break;
                case (ParseState.ItemName, _):
                    tagNameBuilder.Append(character);
                    break;
                case (ParseState.ItemAlignmentOrFormat, '}'):
                    stringFormatBuilder.Append(character);
                    state = ParseState.Outside;
                    break;
                case (ParseState.ItemAlignmentOrFormat, _):
                    stringFormatBuilder.Append(character);
                    break;
            }
        }

        return (stringFormatBuilder.ToString(), itemNames);
    }

    private enum ParseState
    {
        Outside,
        OpenBracket,
        ItemName,
        ItemAlignmentOrFormat
    }
}
