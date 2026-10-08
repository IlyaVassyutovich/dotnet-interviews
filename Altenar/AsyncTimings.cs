using System.Diagnostics;

namespace Altenar;

public class AsyncTimings
{
    private static readonly TimeSpan Unit = TimeSpan.FromSeconds(1);

    [Fact]
    public async Task SleepBeforeAwait__WhenAll_Over_Lazy_Select__Calls_Run_Sequentially()
    {
        var measure = await MeasureConcurrentCalls(5, SleepBeforeAwait);

        Assert.True(Strict(6) <= measure && measure < Strict(7));
    }

    [Fact]
    public async Task SleepAfterAwait__WhenAll_Over_Lazy_Select__Calls_Overlap()
    {
        var measure = await MeasureConcurrentCalls(5, SleepAfterAwait);

        Assert.True(Strict(2) <= measure && measure < Strict(3));
    }

    private static async Task<TimeSpan> MeasureConcurrentCalls(int count, Func<Task> process)
    {
        // Select is lazy: no call has been made yet, this is only a recipe for making them.
        var calls = Enumerable.Range(0, count).Select(_ => process());

        // WhenAll needs the complete set of tasks before it can return its own, so it enumerates the sequence
        // synchronously. The calls therefore happen here, one after another on the caller's thread, and whatever
        // each of them does before its first incomplete await is serialized rather than overlapped.
        var start = Stopwatch.GetTimestamp();
        var all = Task.WhenAll(calls);

        // Only the work that the calls left behind as pending continuations is still running at this point.
        await all;
        var measure = Stopwatch.GetElapsedTime(start);

        return measure;
    }

    private static TimeSpan Strict(int count) => count * Unit;

    // Blocks the caller: an async method runs synchronously until its first incomplete await, so the sleep
    // executes inside the call itself. Each call returns its task only after a full unit has passed,
    // which makes the next call start a unit later.
    private static async Task SleepBeforeAwait()
    {
        Thread.Sleep(Unit);

        await Task.Delay(Unit);
    }

    // Returns at once: the method yields at the very first statement, so all calls are started back to back
    // and their delays overlap. The sleep is part of the continuation and runs on whichever thread resumes it,
    // not on the caller's.
    private static async Task SleepAfterAwait()
    {
        await Task.Delay(Unit);
        Thread.Sleep(Unit);
    }
}
