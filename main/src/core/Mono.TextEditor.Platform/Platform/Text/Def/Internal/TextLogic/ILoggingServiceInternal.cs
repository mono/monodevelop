//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain internal APIs that are subject to change without notice.
// Use at your own risk.
//
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Text.Utilities
{
    /// <summary>
    /// Allows code in src/Platform to log events.
    /// </summary>
    /// <remarks>
    /// For example, the VS Provider of this inserts data points into the telemetry data stream.
    /// </remarks>
    public interface ILoggingServiceInternal
    {
        /// <summary>
        /// Post the event named <paramref name="key"/> to the telemetry stream. Additional properties can be appended as name/value pairs in <paramref name="namesAndProperties"/>.
        /// </summary>
        void PostEvent(string key, params object[] namesAndProperties);

        /// <summary>
        /// Post the event named <paramref name="key"/> to the telemetry stream. Additional properties can be appended as name/value pairs in <paramref name="namesAndProperties"/>.
        /// </summary>
        void PostEvent(string key, IReadOnlyList<object> namesAndProperties);

        /// <summary>
        /// Adjust the counter associated with <paramref name="key"/> and <paramref name="name"/> by <paramref name="delta"/>.
        /// </summary>
        /// <remarks>
        /// <para>Counters start at 0.</para>
        /// <para>No information is sent over the wire until the <see cref="PostCounters"/> is called.</para>
        /// </remarks>
        void AdjustCounter(string key, string name, int delta = 1);

        /// <summary>
        /// Post all of the counters.
        /// </summary>
        /// <remarks>
        /// <para>The counters are logged as if PostEvent had been called for each key with a list counter names and values.</para>
        /// <para>The counters are cleared as a side-effect of this call.</para>
        /// </remarks>
        void PostCounters();
    }
}