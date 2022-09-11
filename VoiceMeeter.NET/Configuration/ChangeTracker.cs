using System.Globalization;
using System.Reactive.Linq;
using System.Text;
using JetBrains.Annotations;
using VoiceMeeter.NET.Enums;
using VoiceMeeter.NET.Exceptions;

namespace VoiceMeeter.NET.Configuration;

/// <summary>
/// Class responsible for starting polling and pushing changes to VoiceMeeter
/// </summary>
public class ChangeTracker
{
    /// <summary>
    /// A <see cref="Dictionary{TKey,TValue}"/> holding all changed to be applied
    /// </summary>
    private Dictionary<string, string> ChangeStore { get; } = new();
    internal VoiceMeeterClient Client { get; }
    internal IObservable<bool> RefreshEventObservable { get; }
    
    /// <summary>
    /// Returns the <see cref="VoiceMeeterConfiguration"/> associated with this <see cref="ChangeTracker"/>
    /// </summary>
    public VoiceMeeterConfiguration VoiceMeeterConfiguration { get; }

    /// <summary>
    /// Gets or Sets a value defining if configuration changes are applied immediately or only on <see cref="Apply"/>
    /// </summary>
    /// <remarks>If <c>false</c> only the last value for each parameter is saved</remarks>
    [UsedImplicitly]
    public bool AutoApply { get; set; } = true;

    internal ChangeTracker(VoiceMeeterClient client, VoiceMeeterConfiguration voiceMeeterConfiguration,
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

    /// <summary>
    /// Saves a <see cref="float"/> value, applies immediately unless <see cref="AutoApply"/> is set to <c>false</c>
    /// </summary>
    /// <param name="paramName">The name of the VoiceMeeter parameter to write to</param>
    /// <param name="value">The value to set</param>
    /// <exception cref="VoiceMeeterException">In case of a general / unknown error when applying value</exception>
    /// <exception cref="ArgumentOutOfRangeException">If the parameter name is not known</exception>
    /// <exception cref="VoiceMeeterNotLoggedException">If the client <see cref="IVoiceMeeterClient.Status"/> is not <see cref="LoginResponse.Ok"/></exception>
    public void SaveValue(string paramName, float value)
    {
        if (this.AutoApply)
        {
            this.Client.SetParameter(paramName, value);
            return;
        }

        this.ChangeStore[paramName] = value.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Saves a <see cref="string"/> value, applies immediately unless <see cref="AutoApply"/> is set to <c>false</c>
    /// </summary>
    /// <param name="paramName">The name of the VoiceMeeter parameter to write to</param>
    /// <param name="value">The value to set</param>
    /// <exception cref="VoiceMeeterException">In case of a general / unknown error when applying value</exception>
    /// <exception cref="ArgumentOutOfRangeException">If the parameter name is not known</exception>
    /// <exception cref="VoiceMeeterNotLoggedException">If the client <see cref="IVoiceMeeterClient.Status"/> is not <see cref="LoginResponse.Ok"/></exception>
    public void SaveValue(string paramName, string value)
    {
        if (this.AutoApply)
        {
            this.Client.SetParameter(paramName, value);
            return;
        }

        this.ChangeStore[paramName] = '"' + value + '"';
    }

    /// <summary>
    /// Saves a <see cref="bool"/> value, applies immediately unless <see cref="AutoApply"/> is set to <c>false</c>
    /// </summary>
    /// <param name="paramName">The name of the VoiceMeeter parameter to write to</param>
    /// <param name="value">The value to set</param>
    /// <exception cref="VoiceMeeterException">In case of a general / unknown error when applying value</exception>
    /// <exception cref="ArgumentOutOfRangeException">If the parameter name is not known</exception>
    /// <exception cref="VoiceMeeterNotLoggedException">If the client <see cref="IVoiceMeeterClient.Status"/> is not <see cref="LoginResponse.Ok"/></exception>
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
    /// <exception cref="VoiceMeeterScriptException">If the generated script has an error</exception>
    /// <exception cref="VoiceMeeterNotLoggedException">If the client <see cref="IVoiceMeeterClient.Status"/> is not <see cref="LoginResponse.Ok"/></exception>
    /// <exception cref="VoiceMeeterException">In case of a general / unknown error when applying value</exception>
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