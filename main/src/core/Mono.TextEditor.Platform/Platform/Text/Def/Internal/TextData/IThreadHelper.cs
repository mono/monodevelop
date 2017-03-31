//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain internal APIs that are subject to change without notice.
// Use at your own risk.
//
using System;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Utilities
{
    /// <summary>
    /// A helper exposing a methods to delegate work to the UI thread.
    /// </summary>
    /// <remarks>
    /// [Import] IThreadHelper
    /// </remarks>
    public interface IThreadHelper
    {
        Task RunOnUIThread(Action action);
        Task RunOnUIThread(UIThreadPriority priority, Action action);

        Task<T> RunOnUIThread<T>(Func<T> function);
        Task<T> RunOnUIThread<T>(UIThreadPriority priority, Func<T> function);
    }

    public enum UIThreadPriority
    {
        Normal = 1,
        Background = 2,
        Idle = 3
    }
}

