using System.ComponentModel;
using System.Reactive;

namespace VoiceMeeter.NET.Configuration;

public interface IVoiceMeeterResource: INotifyPropertyChanged
{
    public int Index { get; }
    public string? Name { get; set; }
    public bool IsVirtual { get; }
    IObservable<EventPattern<PropertyChangedEventArgs>> PropertyChangedObservable { get; }
}