#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using SephiriaEnhancements.Integration;

namespace SephiriaEnhancements.Diagnostics
{
    internal static class SupportFailureDetails
    {
        internal static string Format(Exception? exception)
        {
            var chain = new List<string>();
            var methods = new List<string>();
            var bindings = new List<string>();
            // A loader failure can have no inner stack. Keep wrapper frames too,
            // using metadata only: never copy messages, arguments or file paths.
            for (Exception? current = exception; current != null; current = current.InnerException)
            {
                chain.Add(current.GetType().FullName ?? current.GetType().Name);
                if (current is NativeBindingException binding)
                    bindings.Add(binding.Owner.FullName + "." + binding.MemberName);
                var frames = new StackTrace(current, false).GetFrames();
                if (frames == null) continue;
                foreach (var frame in frames)
                {
                    var method = frame.GetMethod();
                    if (method != null) methods.Add(method.DeclaringType?.FullName + "." + method.Name);
                }
            }
            return "exception=" + (exception?.GetType().FullName ?? "unknown") +
                " cause=" + (exception?.GetBaseException().GetType().FullName ?? "unknown") +
                " chain=" + string.Join(" > ", chain) + " methods=" + string.Join(" > ", methods) +
                (bindings.Count == 0 ? "" : " binding=" + string.Join(" > ", bindings));
        }
    }
}
