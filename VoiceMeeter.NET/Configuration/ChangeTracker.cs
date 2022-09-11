using System.Globalization;
using System.Reactive.Linq;
using System.Text;
using JetBrains.Annotations;

namespace VoiceMeeter.NET.Configuration;

public class ChangeTracker
{
    public VoiceMeeterClient Client { get; }
    public VoiceMeeterConfiguration VoiceMeeterConfiguration { get; }

    [UsedImplicitly]
    public bool AutoApply { get; set; } = true;

    internal IObservable<bool> RefreshEventObservable { get; }

    private Dictionary<string, string> ChangeStore { get; } = new();

    public ChangeTracker(VoiceMeeterClient client, VoiceMeeterConfiguration voiceMeeterConfiguration,
        TimeSpan? refreshFrequency)
    {
        this.Client = client;
        this.VoiceMeeterConfiguration = voiceMeeterConfiguration;
        
        this.RefreshEventObservable = (refreshFrequency.HasValue
                ? Observable.Interval(refreshFrequency.Value)
                : Observable.Return(DateTimeOffset.Now.UtcTicks))
            .Select(_ => this.Client.IsDirty())
            .Publish()
            .RefCount();;
    }
    
    public void SaveValue(string paramName, float value)
    {
        if (this.AutoApply)
        {
            this.Client.SetParameter(paramName, value);
            return;
        }

        this.ChangeStore[paramName] = value.ToString(CultureInfo.InvariantCulture);
    }

    public void SaveValue(string paramName, string value)
    {
        if (this.AutoApply)
        {
            this.Client.SetParameter(paramName, value);
            return;
        }

        this.ChangeStore[paramName] = '"' + value + '"';
    }

    public void SaveValue(string paramName, bool value)
    {
        float floatValue = value ? 1 : 0;

        if (this.AutoApply)
        {
            this.Client.SetParameter(paramName, floatValue);
            return;
        }

        this.ChangeStore[paramName] = floatValue.ToString("F0");
    }

    /// <summary>
    /// Applies all saved changes to the connected VoiceMeeter Instance
    /// </summary>
    /// <exception cref="InvalidOperationException">When called whilst  <see cref="AutoApply"/> is set to <code>true</code></exception>
    [UsedImplicitly]
    public void Apply()
    {
        if (this.AutoApply) throw new InvalidOperationException($"Cannot apply when {nameof(this.AutoApply)} is enabled");
        
        var scriptBuilder = new StringBuilder();

        foreach (KeyValuePair<string, string> change in this.ChangeStore)
        {
            scriptBuilder.Append($"{change.Key} = {change.Value}\n");
        }

        this.Client.SetParameters(scriptBuilder.ToString());
        
        this.ClearChanges();
    }

    /// <summary>
    /// Empties the <see cref="ChangeStore"/> of changes to apply
    /// </summary>
    /// <exception cref="InvalidOperationException">When called whilst  <see cref="AutoApply"/> is set to <code>true</code></exception>
    [UsedImplicitly]
    public void ClearChanges()
    {
        if (this.AutoApply) throw new InvalidOperationException($"Cannot clear when {nameof(this.AutoApply)} is enabled");
        
        this.ChangeStore.Clear();
    }
}