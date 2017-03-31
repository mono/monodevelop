//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain internal APIs that are subject to change without notice.
// Use at your own risk.
//
using Microsoft.VisualStudio.Threading;

namespace Microsoft.VisualStudio.Utilities
{
    /// <summary>
    /// A helper exposing a factory for starting asynchronous tasks that can mitigate deadlocks when the tasks require the Main thread of an application and the Main thread may itself be blocking on the completion of a task.
    /// </summary>
    public interface IJoinableTaskFactoryHelper
    {
        /// <summary>
        /// Gets the singleton <see cref="JoinableTaskContext"/> instance for the hosting application.
        /// </summary>
        JoinableTaskContext JoinableTaskContext { get; }

        /// <summary>
        /// Gets the <see cref="JoinableTaskFactory"/> for the hosting application.
        /// </summary>
        JoinableTaskFactory JoinableTaskFactory { get; }
    }    
}

