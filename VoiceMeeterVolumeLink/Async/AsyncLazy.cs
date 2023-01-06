using System.Runtime.CompilerServices;

namespace VoiceMeeterVolumeLink.Async;

public class AsyncLazy<T> : Lazy<Task<T>>
{
    public AsyncLazy(Func<T> valueFactory) :
        base(() => Task.Factory.StartNew(valueFactory), LazyThreadSafetyMode.ExecutionAndPublication) { }

    public AsyncLazy(Func<Task<T>> taskFactory) :
        base(() => Task.Factory.StartNew(taskFactory).Unwrap(), LazyThreadSafetyMode.ExecutionAndPublication) { }

    public TaskAwaiter<T> GetAwaiter() { return this.Value.GetAwaiter(); }
}