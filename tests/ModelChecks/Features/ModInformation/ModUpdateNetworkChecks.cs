using System.Net;
using System.Net.Http;
using SephiriaEnhancements.ModInformation;

namespace SephiriaEnhancements.ModelChecks.Features.ModInformation;

internal static class ModUpdateNetworkChecks
{
    internal static async Task Run()
    {
        await Check(_ => Reply(200, "[]"), ModUpdateStatus.NoPublishedVersion);
        await Check(_ => Reply(200, "not-json"), ModUpdateStatus.InvalidResponse);
        await Check(_ => Reply(200, "null"), ModUpdateStatus.InvalidResponse);
        await Check(_ => Reply(200, "[{\"tag_name\":\"v1.0.0\"}]"), ModUpdateStatus.InvalidResponse);
        await Check(_ => Reply(429, ""), ModUpdateStatus.RateLimited, 429);
        await Check(_ => { var r = Reply(403, ""); r.Headers.Add("X-RateLimit-Remaining", "0"); return r; }, ModUpdateStatus.RateLimited, 403);
        await Check(_ => { var r = Reply(403, ""); r.Headers.Add("Retry-After", "60"); return r; }, ModUpdateStatus.RateLimited, 403);
        await Check(_ => Reply(403, ""), ModUpdateStatus.ServiceError, 403);
        await Check(_ => Reply(503, ""), ModUpdateStatus.ServiceError, 503);
        await Check(_ => throw new HttpRequestException(), ModUpdateStatus.ConnectionFailed);
        await Check(_ => throw new IOException(), ModUpdateStatus.ConnectionFailed);
        await Check(_ => throw new TaskCanceledException(), ModUpdateStatus.TimedOut);
        using (var timeoutClient = new HttpClient(new WaitingHandler()) { Timeout = TimeSpan.FromMilliseconds(25) })
        {
            var timeout = await ModUpdateCheck.FetchAsync(timeoutClient, "0.5.0-beta.9", CancellationToken.None);
            if (timeout.Status != ModUpdateStatus.TimedOut) throw new InvalidOperationException("HTTP timeout was misclassified");
        }
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        using var client = new HttpClient(new Handler(_ => throw new TaskCanceledException()));
        try
        {
            await ModUpdateCheck.FetchAsync(client, "0.5.0-beta.9", cancelled.Token);
            throw new InvalidOperationException("Explicit cancellation must not become a timeout result.");
        }
        catch (OperationCanceledException) { }
        Console.WriteLine("Update network checks: HTTP classification, invalid responses, connection errors, timeout and cancellation passed");
    }

    private static HttpResponseMessage Reply(int status, string body) =>
        new((HttpStatusCode)status) { Content = new StringContent(body) };

    private static async Task Check(Func<CancellationToken, HttpResponseMessage> reply, ModUpdateStatus expected, int httpStatus = 0)
    {
        using var client = new HttpClient(new Handler(reply));
        var result = await ModUpdateCheck.FetchAsync(client, "0.5.0-beta.9", CancellationToken.None);
        if (result.Status != expected || result.HttpStatus != httpStatus)
            throw new InvalidOperationException($"Expected {expected}/{httpStatus}, got {result.Status}/{result.HttpStatus}");
    }

    private sealed class WaitingHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            throw new InvalidOperationException("Unreachable after timeout");
        }
    }

    private sealed class Handler(Func<CancellationToken, HttpResponseMessage> reply) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri?.AbsoluteUri != ModOfficialLinks.ReleasesApi || request.Method != HttpMethod.Get)
                throw new InvalidOperationException("Unexpected update endpoint or method");
            return Task.FromResult(reply(cancellationToken));
        }
    }
}
