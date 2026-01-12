////////////////////////////////////////////////////////
// Copyright (c) 2025 Sameer Khandekar                //
// License: MIT License.                              //
////////////////////////////////////////////////////////
using HttpPlaygroundServer;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Text.Json;

namespace PlaygorundTestApp;

/// <summary>
/// This app is for testing the server. It tests various permutation, combinations.
/// It integrates directly with code.
/// </summary>
internal class Program
{
    static async Task Main(string[] args)
    {
        // Loads values from appsettings.json
        LoadConfig();

        await TestWithNoServerSimulation.Run().ConfigureAwait(false);
        await TestWithServerSimulation.Run().ConfigureAwait(false);
        await TestFunctionalWithServerSimulation.Run().ConfigureAwait(false);
    }

    public static void LoadConfig(string configFilePath = "appsettings.json")
    {
        if (File.Exists(configFilePath))
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(configFilePath, optional: false, reloadOnChange: true);

            IConfigurationRoot configuration = builder.Build();

            ServerConfig.HostName = configuration["AppConfig:HostName"] ?? string.Empty;
            ServerConfig.Port = int.TryParse(configuration["AppConfig:Port"], out int portValue) ? portValue : 0;
            ServerConfig.StorageFolder = configuration["AppConfig:StorageFolder"] ?? string.Empty;
        }
    }
}
