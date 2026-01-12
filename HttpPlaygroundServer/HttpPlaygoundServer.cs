////////////////////////////////////////////////////////
// Copyright (c) 2025 Sameer Khandekar                //
// License: MIT License.                              //
////////////////////////////////////////////////////////
using HttpPlaygroundServer.Model;
using System;
using System.Collections.Generic;
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
        // listener for http requests
        private HttpListener _listener;

        // Request processor
        HttpRequestProcessor _rp = null;

        /// <summary>
        /// Detetemines if requests should be logged to files or not.
        /// By default they are logged.
        /// </summary>
        public bool IsRequestLoggingEnabled 
        { 
            get
            {
                return _rp.LogRequests;
            } 
            set
            {
                _rp.LogRequests = value;
            }
        }

        /// <summary>
        /// This collects Request Responses. They are stored in the order of sending.
        /// This is intended for running single functional test at a time. 
        /// If multiple tests are are run in parallel, this may mix results of multiple tests.
        /// Tip: Clear it before starting a funcional test and use it after completing the test.
        /// </summary>
        public IReadOnlyList<Tuple<RequestModel, ResponseModel>> RequestResponses => _rp.RequestResponses;

        /// <summary>
        /// This will clear request responses.
        /// </summary>
        public void ClearRequestResponses() => _rp.ClearRequestResponses();

        /// <summary>
        /// The constructor will wither assign the request processor in the parameter or create the default one.
        /// Custom can be used to simulate serer processing.
        /// </summary>
        /// <param name="rp">Request Processor</param>
        public HttpPlaygoundServer(HttpRequestProcessor rp = null)
        {
            _rp = rp ?? new HttpRequestProcessor();
        }

        /// <summary>
        /// Starts HTTP listener. After starting, it will signal the manual reset event. So the caller can start making the requests.
        /// It will coninuously process incoming HTTP requests in async manner and stop when cancellation is requested.. 
        /// </summary>
        /// <remarks>This method initializes an <see cref="HttpListener"/> to listen on the configured URL
        /// and port specified in  <c>ServerConfig.URL</c> and <c>ServerConfig.Port</c>. It processes incoming HTTP
        /// requests asynchronously using a request processor. The method runs until the provided <paramrer
        /// name="token"/> signals  cancellation. <para> The caller is notified that the server has started by setting
        /// the provided <paramref name="mre"/>.  Ensure that the <paramref name="token"/> is monitored to stop the
        /// listener when no longer needed. </para></remarks>
        /// <param name="mre">A <see cref="ManualResetEventSlim"/> used to signal the caller when the HTTP listener has started
        /// successfully.</param>
        /// <param name="token">A <see cref="CancellationToken"/> used to stop the HTTP listener gracefully when cancellation is requested.</param>
        public async Task StartServer(ManualResetEventSlim mre, CancellationToken token)
        {
            _listener = new HttpListener();

            // Listen on all prefixes
            UriBuilder uriBuilder = new UriBuilder("http", ServerConfig.HostName, ServerConfig.Port);
            _listener.Prefixes.Add(uriBuilder.Uri.AbsoluteUri);

            // start the server
            _listener.Start();

            // signal the caller that server has started
            mre.Set();

            // listen to the rquests and process them asynchronously
            while (!token.IsCancellationRequested)
            {
                // this check is in effect if stop has been called; but not the cancellation
                if(_listener.IsListening)
                await _listener.GetContextAsync().ContinueWith(async (task) =>
                {
                    await Task.Run(async () =>
                    {
                        if (_listener.IsListening) // chekcing again. This may still fail sometime; but I am not taking trouble to have a lock and check
                            await _rp.ProcessRequests(task.Result).ConfigureAwait(false);
                    }).ConfigureAwait(false);
                }).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Stops and closes the listner
        /// </summary>
        public void StopServer()
        {
            _listener.Stop();
            _listener.Close();
        }
    }
}
