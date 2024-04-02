using System;
using System.Reactive;
using Microsoft.Reactive.Testing;

namespace MidiRecorder.Tests;

public static class Recorded
{
    public static Recorded<T> Create<T>(long time, T value)
    {
        return new Recorded<T>(time, value);
    }

    public static Recorded<Notification<T>> OnNext<T>(long time, T value)
    {
        return Create(time, Notification.CreateOnNext(value));
    }

    public static Recorded<Notification<T>> Complete<T>(long time)
    {
        return Create(time, Notification.CreateOnCompleted<T>());
    }

    public static Recorded<Notification<T>> OnError<T>(long time)
    {
        return Create(time, Notification.CreateOnError<T>(new Exception()));
    }
}
