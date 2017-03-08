//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
using System.Diagnostics;
namespace Microsoft.VisualStudio.Text.Implementation
{
    /// <summary>
    /// Base class for a page that participates in an MRU list.
    /// </summary>
    internal abstract class Page
    {
        internal Page More { get; set; }
        internal Page Less { get; set; }

        public abstract bool UnloadWhileLocked();
        public abstract string Id { get; }
    }
}
