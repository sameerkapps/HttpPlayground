////////////////////////////////////////////////////////
// Copyright (c) 2025 Sameer Khandekar                //
// License: MIT License.                              //
////////////////////////////////////////////////////////

namespace FunctionalTestingApp
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            await FunctionalTestingWithServerSimulation.Run().ConfigureAwait(false);
        }
    }
}
