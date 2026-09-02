using SephiriaEnhancements.Runtime.Execution;
using System.Collections;

namespace SephiriaEnhancements.ModelChecks.Runtime.Execution;

internal static class AmbientExecutionContextChecks
{
    internal static void Run()
    {
        object outerExecutionContext = new();
        object innerExecutionContext = new();
        using (AmbientExecutionContext<object>.Enter(outerExecutionContext))
        {
            if (!ReferenceEquals(AmbientExecutionContext<object>.Current,
                    outerExecutionContext))
                throw new InvalidOperationException(
                    "ambient execution context must expose the current scope");
            using (AmbientExecutionContext<object>.Enter(innerExecutionContext))
            {
                if (!ReferenceEquals(AmbientExecutionContext<object>.Current,
                        innerExecutionContext))
                    throw new InvalidOperationException(
                        "nested ambient execution context must expose the inner scope");
            }
            if (!ReferenceEquals(AmbientExecutionContext<object>.Current,
                    outerExecutionContext))
                throw new InvalidOperationException(
                    "ambient execution context must restore the outer scope");
        }
        if (AmbientExecutionContext<object>.Current != null)
            throw new InvalidOperationException(
                "ambient execution context must clear after the outer scope ends");

        object coroutineExecutionContext = new();
        bool coroutineStepScopeDisposed = false;
        int coroutineCompletionCount = 0;
        IEnumerator ContextAwareRoutine()
        {
            if (!ReferenceEquals(AmbientExecutionContext<object>.Current,
                    coroutineExecutionContext))
                throw new InvalidOperationException(
                    "coroutine execution context must be active while advancing");
            yield return "context-step";
        }
        IEnumerator contextualCoroutine =
            AmbientExecutionContext<object>.WrapCoroutine(
                ContextAwareRoutine(), coroutineExecutionContext,
                _ => new CallbackScope(() => coroutineStepScopeDisposed = true),
                _ => coroutineCompletionCount++);
        if (!contextualCoroutine.MoveNext() ||
            !Equals(contextualCoroutine.Current, "context-step") ||
            !coroutineStepScopeDisposed ||
            AmbientExecutionContext<object>.Current != null ||
            contextualCoroutine.MoveNext() ||
            coroutineCompletionCount != 1 ||
            contextualCoroutine.MoveNext() ||
            coroutineCompletionCount != 1)
            throw new InvalidOperationException(
                "coroutine execution context must restore each step and complete once");
        bool failedStepScopeDisposed = false;
        IEnumerator FailingContextAwareRoutine()
        {
            yield return null;
            throw new InvalidOperationException("expected contextual coroutine failure");
        }
        IEnumerator failingContextualCoroutine =
            AmbientExecutionContext<object>.WrapCoroutine(
                FailingContextAwareRoutine(), coroutineExecutionContext,
                _ => new CallbackScope(() => failedStepScopeDisposed = true));
        failingContextualCoroutine.MoveNext();
        failedStepScopeDisposed = false;
        try
        {
            failingContextualCoroutine.MoveNext();
            throw new InvalidOperationException(
                "failing contextual coroutine must propagate its exception");
        }
        catch (InvalidOperationException exception) when (
            exception.Message == "expected contextual coroutine failure")
        {
        }
        if (!failedStepScopeDisposed ||
            AmbientExecutionContext<object>.Current != null)
            throw new InvalidOperationException(
                "failing coroutine steps must dispose and restore their context");
        IEnumerator disposalFailureCoroutine =
            AmbientExecutionContext<object>.WrapCoroutine(
                ContextAwareRoutine(), coroutineExecutionContext,
                _ => new CallbackScope(() => throw new InvalidOperationException(
                    "expected step-scope disposal failure")));
        try
        {
            disposalFailureCoroutine.MoveNext();
            throw new InvalidOperationException(
                "step-scope disposal failure must propagate");
        }
        catch (InvalidOperationException exception) when (
            exception.Message == "expected step-scope disposal failure")
        {
        }
        if (AmbientExecutionContext<object>.Current != null)
            throw new InvalidOperationException(
                "ambient context must restore when step-scope disposal fails");
        Console.WriteLine("AmbientExecutionContext: nesting, coroutine completion and failure cleanup passed");
    }

    private sealed class CallbackScope : IDisposable
    {
        private readonly Action callback;
        private bool disposed;

        internal CallbackScope(Action callback)
        {
            this.callback = callback;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            callback();
        }
    }
}
