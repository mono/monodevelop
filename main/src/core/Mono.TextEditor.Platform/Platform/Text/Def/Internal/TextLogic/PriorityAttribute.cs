//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain internal APIs that are subject to change without notice.
// Use at your own risk.
//
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.VisualStudio.Text.Utilities
{
    /// <summary>
    /// An attribute used to indicate the priority of a named MEF import. If two imports have
    /// the same name, then the one with the higher priority wins,
    /// </summary>
    /// <remarks><para>Currently used only by the EditorFormatMap imports.</para>
    /// <para>Default priority is 0 and negative priorities are allowed.</para></remarks>
    [global::System.AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class PriorityAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of <see cref="PriorityAttribute"/>.
        /// </summary>
        public PriorityAttribute(int priority)
        {
            this.Priority = priority;
        }

        /// <summary>
        /// Gets the priority of the export (where an export with the same name and a lower priority will be
        /// suppressed by the export with a higher priority).
        /// </summary>
        public int Priority { get; }
    }
}
