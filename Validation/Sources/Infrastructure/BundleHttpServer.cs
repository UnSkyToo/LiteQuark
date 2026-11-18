using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace LiteQuark.Editor
{
    internal sealed class BundleHttpServer : IDisposable
    {
        private readonly TcpListener _listener;
        private readonly System.Threading.Tasks.TaskCompletionSource<bool> _bodyGate = new(System.Threading.Tasks.TaskCreationOptions.RunContinuationsAsynchronously);
        private volatile TcpClient _client;
        private volatile bool _disposed;
        internal volatile bool BodyStarted;
        internal string RequestLine;
        internal bool Accepted => _client != null;
        internal bool ClientClosed
        {
            get
            {
                var client = _client;
                if (client == null) return false;
                try { return client.Client == null || (client.Client.Poll(0, SelectMode.SelectRead) && client.Available == 0); }
                catch (ObjectDisposedException) { return true; }
                catch (SocketException) { return true; }
            }
        }
        internal readonly string Url;
        internal readonly System.Threading.Tasks.Task Work;

        internal BundleHttpServer(byte[] body, int statusCode = 200)
        {
            _listener = new TcpListener(IPAddress.Loopback, 0);
            _listener.Start();
            Url = $"http://127.0.0.1:{((IPEndPoint)_listener.LocalEndpoint).Port}/bundle.ab";
            Work = System.Threading.Tasks.Task.Run(async () =>
            {
                try
                {
                    using var client = await _listener.AcceptTcpClientAsync().ConfigureAwait(false);
                    _client = client;
                    if (_disposed) return;
                    using var stream = client.GetStream();
                    var header = new List<byte>();
                    var buffer = new byte[1];
                    while (header.Count < 8192)
                    {
                        if (await stream.ReadAsync(buffer, 0, 1).ConfigureAwait(false) == 0) throw new IOException("Missing HTTP request header.");
                        header.Add(buffer[0]);
                        var n = header.Count;
                        if (n >= 4 && header[n - 4] == 13 && header[n - 3] == 10 && header[n - 2] == 13 && header[n - 1] == 10) break;
                    }
                    RequestLine = System.Text.Encoding.ASCII.GetString(header.ToArray()).Split('\r')[0];
                    var contentLength = System.Text.Encoding.ASCII.GetString(header.ToArray()).Split('\n')
                        .FirstOrDefault(line => line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase));
                    var remaining = contentLength == null ? 0 : int.Parse(contentLength.Substring(contentLength.IndexOf(':') + 1).Trim());
                    var requestBody = new byte[4096];
                    while (remaining > 0)
                    {
                        var read = await stream.ReadAsync(requestBody, 0, Math.Min(remaining, requestBody.Length)).ConfigureAwait(false);
                        if (read == 0) throw new IOException("Incomplete HTTP request body.");
                        remaining -= read;
                    }
                    var response = System.Text.Encoding.ASCII.GetBytes($"HTTP/1.1 {statusCode} Validation\r\nContent-Length: {body.Length}\r\nConnection: close\r\n\r\n");
                    await stream.WriteAsync(response, 0, response.Length).ConfigureAwait(false);
                    var split = Math.Max(1, body.Length / 2);
                    await stream.WriteAsync(body, 0, split).ConfigureAwait(false);
                    await stream.FlushAsync().ConfigureAwait(false);
                    BodyStarted = true;
                    await _bodyGate.Task.ConfigureAwait(false);
                    if (_disposed) return;
                    await stream.WriteAsync(body, split, body.Length - split).ConfigureAwait(false);
                    await stream.FlushAsync().ConfigureAwait(false);
                }
                catch (Exception) when (_disposed)
                {
                    // Closing the fixture interrupts its pending socket operation.
                }
            });
        }

        internal void ReleaseBody() => _bodyGate.TrySetResult(true);

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _bodyGate.TrySetResult(true);
            _client?.Dispose();
            _listener.Stop();
        }
    }
}
