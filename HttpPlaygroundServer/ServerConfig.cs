////////////////////////////////////////////////////////
// Copyright (c) 2025 Sameer Khandekar                //
// License: MIT License.                              //
////////////////////////////////////////////////////////
using System.IO;

namespace HttpPlaygroundServer
{
    /// <summary>
    /// Represents the configuration settings for the HTTP Playground Server.
    /// </summary>
    public class ServerConfig
    {
        /// <summary>
        /// Gets or sets the base URL for the server.
        /// Default value is "localhost".
        /// </summary>
        public static string HostName { get; set; } = "localhost";

        /// <summary>
        /// Gets or sets the port number on which the server listens.
        /// Default value is 8080.
        /// </summary>
        public static int Port { get; set; } = 8080;

        /// <summary>
        /// Gets or sets the path to the storage folder used by the server.
        /// Default value is the system's temporary folder.
        /// </summary>
        public static string StorageFolder { get; set; } = Path.GetTempPath();
    }
}
