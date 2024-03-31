namespace MidiRecorder.Application;

public static class MidiFileContext
{
    public static string BuildFilePath<TMidiEvent>(
        string formatString,
        IEnumerable<TMidiEvent> eventList,
        DateTime now,
        Guid uniqueIdentifier,
        Func<TMidiEvent, bool> isNote) =>
        StringExt.Format(
            formatString,
            new FormatData<TMidiEvent>(now, eventList, uniqueIdentifier, isNote));
    
    private class FormatData<TMidiEvent>
    {
        private readonly IEnumerable<TMidiEvent> _eventList;
        private readonly Func<TMidiEvent, bool> _isNote;
        private readonly Dictionary<string, object?> _memoStore = new();

        public FormatData(
            DateTime now,
            IEnumerable<TMidiEvent> eventList,
            Guid guid,
            Func<TMidiEvent, bool> isNote)
        {
            _eventList = eventList;
            _isNote = isNote;
            Now = now;
            Guid = guid;
        }

        public DateTime Now { get; }
        public int NumberOfEvents => _eventList.Count();
        public int NumberOfNoteEvents => Memoize(nameof(NumberOfNoteEvents), () => _eventList.Count(_isNote));
        public Guid Guid { get; }

        private T? Memoize<T>(string key, Func<T> expression)
        {
            if (!_memoStore.ContainsKey(key))
            {
                _memoStore.Add(key, expression());
            }

            return (T?)_memoStore[key];
        }

        public override bool Equals(object? obj)
        {
            return obj is FormatData<TMidiEvent> other && Now == other.Now && NumberOfEvents == other.NumberOfEvents &&
                   Guid.Equals(other.Guid);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Now, NumberOfEvents, Guid);
        }
    }
}
