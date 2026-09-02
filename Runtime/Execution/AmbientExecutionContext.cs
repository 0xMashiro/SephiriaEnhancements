using SephiriaEnhancements.Runtime.Inventory;
#nullable enable
using System;
using System.Collections;

namespace SephiriaEnhancements.Runtime.Execution
{
    internal static class AmbientExecutionContext<TContext>
        where TContext : class
    {
        [ThreadStatic]
        private static TContext? current;

        internal static TContext? Current => current;

        internal static IDisposable Enter(TContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            TContext? previous = current;
            current = context;
            return new RestoreScope(previous);
        }

        internal static IEnumerator WrapCoroutine(IEnumerator routine,
            TContext context,
            Func<TContext, IDisposable?>? stepScopeFactory = null,
            Action<TContext>? completed = null)
        {
            if (routine == null) throw new ArgumentNullException(nameof(routine));
            if (context == null) throw new ArgumentNullException(nameof(context));
            return new ContextualEnumerator(routine, context, stepScopeFactory,
                completed);
        }

        private sealed class RestoreScope : IDisposable
        {
            private readonly TContext? previous;
            private bool disposed;

            internal RestoreScope(TContext? previous)
            {
                this.previous = previous;
            }

            public void Dispose()
            {
                if (disposed) return;
                disposed = true;
                current = previous;
            }
        }

        private sealed class ContextualEnumerator : IEnumerator, IDisposable
        {
            private readonly IEnumerator inner;
            private readonly TContext context;
            private readonly Func<TContext, IDisposable?>? stepScopeFactory;
            private readonly Action<TContext>? completed;
            private bool completionSignaled;

            internal ContextualEnumerator(IEnumerator inner, TContext context,
                Func<TContext, IDisposable?>? stepScopeFactory,
                Action<TContext>? completed)
            {
                this.inner = inner;
                this.context = context;
                this.stepScopeFactory = stepScopeFactory;
                this.completed = completed;
            }

            public object Current => inner.Current;

            public bool MoveNext()
            {
                TContext? previous = current;
                current = context;
                IDisposable? stepScope = null;
                try
                {
                    stepScope = stepScopeFactory?.Invoke(context);
                    bool moved = inner.MoveNext();
                    if (!moved && !completionSignaled)
                    {
                        completionSignaled = true;
                        completed?.Invoke(context);
                    }
                    return moved;
                }
                finally
                {
                    try
                    {
                        stepScope?.Dispose();
                    }
                    finally
                    {
                        current = previous;
                    }
                }
            }

            public void Reset()
            {
                TContext? previous = current;
                current = context;
                IDisposable? stepScope = null;
                try
                {
                    stepScope = stepScopeFactory?.Invoke(context);
                    inner.Reset();
                    completionSignaled = false;
                }
                finally
                {
                    try
                    {
                        stepScope?.Dispose();
                    }
                    finally
                    {
                        current = previous;
                    }
                }
            }

            public void Dispose()
            {
                if (inner is IDisposable disposable)
                    disposable.Dispose();
            }
        }
    }
}
