using System;
using System.Windows;
using System.Windows.Markup;

namespace VoiceMeeterVolumeLink.Markup;

public class ViewModelProvider : MarkupExtension
{
    public Type ViewModelType { get; set; }
    
    public ViewModelProvider(Type viewModelType)
    {
        this.ViewModelType = viewModelType;
    }

    public override object? ProvideValue(IServiceProvider serviceProvider)
    {
        return ((App)Application.Current).ServiceProvider.GetService(this.ViewModelType);
    }
}