namespace Altenar;

public class Tasks
{
    [Fact]
    public async Task AsyncMethod__Throws_Before_First_Await__Surfaces_On_Await_And_Wait()
    {
        var (onCall, onAwait, onWait) = await ObserveExceptions(ThrowInAsyncMethod);

        Assert.Null(onCall);
        Assert.IsType<TestException>(onAwait);
        var aggregate = Assert.IsType<AggregateException>(onWait);
        Assert.Same(onAwait, Assert.Single(aggregate.InnerExceptions));
    }

    [Fact]
    public async Task TaskReturningMethod__Throws_Before_Returning_Task__Surfaces_On_Call()
    {
        var (onCall, onAwait, onWait) = await ObserveExceptions(ThrowInTaskReturningMethod);

        Assert.IsType<TestException>(onCall);
        Assert.Null(onAwait);
        Assert.Null(onWait);
    }

    private static async Task<Observed> ObserveExceptions(Func<bool, Task> call)
    {
        // Starts as a completed task so that the later stages stay observable (and silent)
        // when the call throws synchronously and the assignment below never happens.
        var task = Task.CompletedTask;
        Exception? onCall = null;
        Exception? onAwait = null;
        Exception? onWait = null;

        try
        {
            task = call(true);
        }
        catch (Exception caught)
        {
            onCall = caught;
        }

        // await unwraps: the awaiter rethrows the first stored exception as is,
        // so that async code can catch the same type that synchronous code would.
        try
        {
            await task;
        }
        catch (Exception caught)
        {
            onAwait = caught;
        }

        // Wait wraps: a task may hold several exceptions (e.g. from Task.WhenAll),
        // and the blocking API reports all of them through a single AggregateException.
        try
        {
            task.Wait();
        }
        catch (Exception caught)
        {
            onWait = caught;
        }

        return new Observed(onCall, onAwait, onWait);
    }

    private sealed record Observed(Exception? OnCall, Exception? OnAwait, Exception? OnWait);

    private sealed class TestException : Exception;

    // Nothing escapes the call: the compiler rewrites an async method into a state machine whose body runs
    // inside a try/catch, so even a throw before the first await is stored in the returned task (Faulted)
    // instead of propagating to the caller. It can only be observed by awaiting or blocking on that task.
    private static async Task ThrowInAsyncMethod(bool shouldThrow)
    {
        if (shouldThrow)
        {
            throw new TestException();
        }

        await Task.Delay(100);
    }

    // The call itself throws: without the async modifier this is an ordinary method that happens to return a Task,
    // so the throw runs on the caller's stack before any task exists. There is no task to carry the exception.
    private static Task ThrowInTaskReturningMethod(bool shouldThrow)
    {
        if (shouldThrow)
        {
            throw new TestException();
        }

        return Task.Delay(100);
    }
}
