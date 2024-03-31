using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace MidiRecorder.Application;

public static class NoteDuration
{
    public static IObservable<(TEvent ev, TimeSpan duration)> CalculateDurations<TEvent>(
        IObservable<TEvent> events,
        Func<TEvent, bool> isNoteOn,
        Func<TEvent, bool> isNoteOff,
        Func<TEvent, int> noteNumber,
        IScheduler? scheduler = null)
    {
        scheduler ??= Scheduler.Default;
        return events
            .GroupBy(x => isNoteOn(x) || isNoteOff(x) ? noteNumber(x): 0)
            .SelectMany(
                g => g.Key == 0
                    ? g.Select(x => (x, TimeSpan.Zero))
                    : g.TimeInterval(scheduler)
                        .Select(x => isNoteOff(x.Value) ? (x.Value, x.Interval) : (x.Value, TimeSpan.Zero)));
    }
}
