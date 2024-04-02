namespace MidiRecorder.Application;

public interface IMidiSource<TMidiEvent> : IDisposable
{
    void StartReceiving();
    IObservable<TMidiEvent> AllEvents { get; }
}
