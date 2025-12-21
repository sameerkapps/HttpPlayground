////////////////////////////////////////////////////////
// Copyright (c) 2025 Sameer Khandekar                //
// License: MIT License.                              //
////////////////////////////////////////////////////////
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace HttpPlaygroundServer
{
    /// <summary>
    /// Provides functionality to start and manage an HTTP server that listens for and processes incoming HTTP requests
    /// asynchronously.
    /// </summary>
    public class HttpPlaygoundServer
    {
        HttpRequestProcessor _rp = null;
        public HttpPlaygoundServer(HttpRequestProcessor rp = null)
        {
            _rp = rp ?? new HttpRequestProcessor();
        }

        /// <summary>
        /// Starts an HTTP listener that processes incoming HTTP requests asynchronously.
        /// </summary>
        /// <remarks>This method initializes an <see cref="HttpListener"/> to listen on the configured URL
        /// and port specified in  <c>ServerConfig.URL</c> and <c>ServerConfig.Port</c>. It processes incoming HTTP
        /// requests asynchronously  using a separate request processor. The method runs until the provided <paramrer
        /// name="token"/> signals  cancellation. <para> The caller is notified that the server has started by setting
        /// the provided <paramref name="mre"/>.  Ensure that the <paramref name="token"/> is monitored to stop the
        /// listener when no longer needed. </para></remarks>
        /// <param name="mre">A <see cref="ManualResetEventSlim"/> used to signal the caller when the HTTP listener has started
        /// successfully.</param>
        /// <param name="token">A <see cref="CancellationToken"/> used to stop the HTTP listener gracefully when cancellation is requested.</param>
        public void StartHttpListner(ManualResetEventSlim mre, CancellationToken token)
        {
            HttpListener listener = new HttpListener();

            // Listen on all prefixes
            UriBuilder uriBuilder = new UriBuilder("http", ServerConfig.HostName, ServerConfig.Port);
            listener.Prefixes.Add(uriBuilder.Uri.AbsoluteUri);
            // start the server
            listener.Start();
            Console.WriteLine($"HTTP Server started. Listening on {uriBuilder.Uri.AbsoluteUri}");

            // signal the caller that server has started
            mre.Set();

            // listen to the rquests and process them asynchronously
            while (!token.IsCancellationRequested)
            {
                listener.GetContextAsync().ContinueWith(async (task) =>
                {
                    await Task.Run(async () =>
                    {                        
                        await _rp.ProcessRequests(task.Result).ConfigureAwait(false);
                    }).ConfigureAwait(false);
                });
            }
        }
    }
}
