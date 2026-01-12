////////////////////////////////////////////////////////
// Copyright (c) 2025 Sameer Khandekar                //
// License: MIT License.                              //
////////////////////////////////////////////////////////

namespace PlaygorundTestApp.FunctionalTesting.Models
{
    /// <summary>
    /// Cat profile model. Adds extra attribute of PhotoURI.
    /// </summary>
    internal class CatProfileModel : CatModel
    {
        public string? PhotoUrl { get; set; }
    }
}
