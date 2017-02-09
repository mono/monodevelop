// Copyright (c) Microsoft Corporation
// All rights reserved

using System;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Provides exception data for an event.
    /// </summary>
    public class ExceptionEventArgs : EventArgs
    {
        /// <summary>
        /// Creates a new instance of <see cref="ExceptionEventArgs"/>.
        /// </summary>
        /// <param name="e">The exception that details the cause of the failure.</param>
        public ExceptionEventArgs(Exception e)
        {
            Exception = e;
        }

        /// <summary>
        /// Gets the exception that details the cause of the failure.
        /// </summary>
        public Exception Exception { get; private set; }
    }
}
