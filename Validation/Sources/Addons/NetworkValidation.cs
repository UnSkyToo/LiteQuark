using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using LiteQuark.Runtime;
using UnityEngine;

namespace LiteQuark.Editor
{
    internal sealed partial class AddonsValidationRunner : BundleValidation
    {
        private sealed class NetworkPayload { public int Value; }
        private static readonly byte[] NetworkBody = System.Text.Encoding.UTF8.GetBytes("{\"Value\":7}");

        private static async UniTask<NetworkSystem> Network()
        {
            var system = new NetworkSystem { Setting = new NetworkSystemSetting { MaxConcurrentRequests = 1, DefaultTimeout = 30 } };
            await system.Initialize();
            return system;
        }

        private static bool Cancelled<T>(HttpResponse<T> response)
            => !response.IsSuccess && response.StatusCode == 0 && !string.IsNullOrEmpty(response.Error);

        private static async UniTask NetworkQueue()
        {
            var system = await Network();
            var servers = Enumerable.Range(0, 4).Select(_ => new BundleHttpServer(NetworkBody)).ToArray();
            try
            {
                servers[0].ReleaseBody();
                var batch = system.Http.SendBatch<NetworkPayload>(servers.Take(3).Select(s => new HttpRequest(s.Url)).ToArray(), 1).Preserve();
                await Wait(() => servers[1].BodyStarted, "Second batch group in flight");
                var queued = system.Http.Send<NetworkPayload>(new HttpRequest(servers[3].Url));
                await system.DisposeAsync();
                var results = await batch;
                Check(results.Length == 3 && results[0].Data.Value == 7 && Cancelled(results[1]) && Cancelled(results[2]),
                    "Accepted batch preserves completed results and cancels the remaining groups in input order.");
                Check(Cancelled(await queued) && !servers[2].Accepted && !servers[3].Accepted, "Queued requests never reach the server.");
                await Wait(() => servers[1].ClientClosed && system.ActiveRequests == 0, "Active transport closed");
            }
            finally { await system.DisposeAsync(); await StopNetworkServers(servers); }
        }

        private sealed class Interceptor : IHttpInterceptor
        {
            internal bool Response;
            internal bool Entered;
            internal readonly UniTaskCompletionSource Gate = new();
            public async UniTask<HttpRequest> OnRequest(HttpRequest request)
            {
                if (!Response) { Entered = true; await Gate.Task; }
                return request;
            }
            public async UniTask<HttpResponse> OnResponse(HttpResponse response)
            {
                if (Response) { Entered = true; await Gate.Task; }
                return response;
            }
        }

        private static async UniTask NetworkInterceptor()
        {
            foreach (var response in new[] { false, true })
            {
                var system = await Network();
                var gate = new Interceptor { Response = response };
                var later = new Interceptor { Response = response };
                later.Gate.TrySetResult();
                var server = new BundleHttpServer(NetworkBody);
                try
                {
                    system.Http.AddInterceptor(gate);
                    system.Http.AddInterceptor(later);
                    server.ReleaseBody();
                    var request = system.Http.SendRaw(new HttpRequest(server.Url)).Preserve();
                    await Wait(() => gate.Entered, "Interceptor entered");
                    await system.DisposeAsync();
                    Check(request.Status == UniTaskStatus.Pending, "Shutdown returns before user interceptor completes.");
                    gate.Gate.TrySetResult();
                    var result = await request;
                    Check(!result.IsSuccess && result.StatusCode == 0 && !later.Entered && server.Accepted == response,
                        "A resumed interceptor cannot advance the closed pipeline.");
                }
                finally { gate.Gate.TrySetResult(); await system.DisposeAsync(); await StopNetworkServers(server); }
            }
        }

        private static async UniTask NetworkFiles()
        {
            foreach (var download in new[] { false, true })
            {
                var system = await Network();
                var server = new BundleHttpServer(NetworkBody);
                var progress = new NetworkProgress();
                try
                {
                    UniTask<byte[]> read = default;
                    UniTask<HttpResponse<NetworkPayload>> write = default;
                    if (download) read = system.Http.DownloadFile(server.Url, progress);
                    else write = system.Http.UploadFile<NetworkPayload>(server.Url, NetworkBody, "probe.bin", progress: progress);
                    await Wait(() => server.BodyStarted, "Native file transfer");
                    await system.DisposeAsync();
                    if (download)
                    {
                        var cancelled = false;
                        try { await read; } catch (OperationCanceledException) { cancelled = true; }
                        Check(cancelled, "Download throws cancellation.");
                    }
                    else Check(Cancelled(await write), "Upload returns cancellation response.");
                    await Wait(() => server.ClientClosed, "Transfer socket closed");
                    var count = progress.Reports;
                    await UniTask.NextFrame();
                    Check(count > 0 && count == progress.Reports, "Progress notifications end with the transfer.");
                }
                finally { await system.DisposeAsync(); await StopNetworkServers(server); }
            }
        }

        private static async UniTask NetworkRetry()
        {
            foreach (var closeSystem in new[] { false, true })
            {
                var system = await Network();
                var server = new BundleHttpServer(NetworkBody, 503);
                using var cancellation = new CancellationTokenSource();
                var originalScale = Time.timeScale;
                try
                {
                    server.ReleaseBody();
                    var policy = new HttpRetryPolicy(3, 60f, false);
                    var retry = system.Http.SendWithRetry<NetworkPayload>(new HttpRequest(server.Url) { CancellationToken = cancellation.Token }, policy).Preserve();
                    await Wait(() => server.Work.IsCompleted && system.ActiveRequests == 0 && retry.Status == UniTaskStatus.Pending, "HTTP retry enters delay without a sending slot");
                    Check(!server.Work.IsFaulted && retry.Status == UniTaskStatus.Pending, "A real 503 response enters retry delay after releasing its native request and slot.");
                    Time.timeScale = 0f;
                    if (closeSystem)
                    {
                        var shutdown = system.DisposeAsync().Preserve();
                        Check(shutdown.Status == UniTaskStatus.Succeeded && retry.Status == UniTaskStatus.Pending,
                            "Shutdown returns before the retry delay observes cancellation on the main thread.");
                        await Wait(() => retry.Status.IsCompleted(), "Retry observes shutdown while timeScale is zero");
                        await shutdown;
                    }
                    else
                    {
                        var mainThread = Thread.CurrentThread.ManagedThreadId;
                        var cancelThread = 0;
                        var resumedThread = 0;
                        async UniTask ObserveRetry() { await retry; resumedThread = Thread.CurrentThread.ManagedThreadId; }
                        var observer = ObserveRetry().Preserve();
                        using var registration = cancellation.Token.Register(() => cancelThread = Thread.CurrentThread.ManagedThreadId);
                        cancellation.CancelAfter(1);
                        await Wait(() => observer.Status.IsCompleted() && Volatile.Read(ref cancelThread) != 0, "Timer-driven retry cancellation");
                        Check(observer.Status == UniTaskStatus.Succeeded && resumedThread == mainThread && cancelThread != 0 && cancelThread != mainThread,
                            "CancelAfter fires on a worker thread, but the retry consumer resumes on the Unity main thread without another native send.");
                        await observer;
                    }
                    Check(retry.Status == UniTaskStatus.Succeeded && Cancelled(await retry) && system.ActiveRequests == 0,
                        "Shutdown and caller cancellation both stop retry delay while timeScale is zero and return a cancelled response instead of another attempt.");
                }
                finally
                {
                    Time.timeScale = originalScale;
                    await system.DisposeAsync();
                    await StopNetworkServers(server);
                }
            }
        }

        private sealed class NetworkProgress : IProgress<float>
        {
            internal int Reports;
            internal bool Completed;
            public void Report(float value) { Reports++; Completed |= value >= 1f; }
        }

        private static async UniTask StopNetworkServers(params BundleHttpServer[] servers)
        {
            foreach (var server in servers) server.Dispose();
            await Wait(() => servers.All(server => server.Work.IsCompleted), "Network server teardown");
            foreach (var server in servers) if (server.Work.IsFaulted) throw server.Work.Exception;
        }
    }
}
