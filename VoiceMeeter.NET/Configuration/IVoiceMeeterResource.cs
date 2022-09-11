using System.ComponentModel;
using System.Reactive;

namespace VoiceMeeter.NET.Configuration;

public interface IVoiceMeeterResource: INotifyPropertyChanged
{
    public int Index { get; }
    public string? Name { get; set; }
    IObservable<EventPattern<PropertyChangedEventArgs>> PropertyChangedObservable { get; }
}