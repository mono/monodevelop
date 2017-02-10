// Copyright (c) Microsoft Corporation
// All rights reserved

using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Completion4 adds the Suffix property, which is the text displayed to the right of the display text (with different text properties).
    /// </summary>
    [CLSCompliant(false)]
    public class Completion4 : Completion3
    {
        /// <summary>
        /// Initializes a new instance of <see cref="Completion4"/>.
        /// </summary>
        public Completion4()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="Completion4"/> with the specified text and description.
        /// </summary>
        /// <param name="displayText">The text that is to be displayed by an IntelliSense presenter.</param>
        /// <param name="insertionText">The text that is to be inserted into the buffer if this completion is committed.</param>
        /// <param name="description">A description that could be displayed with the display text of the completion.</param>
        /// <param name="iconMoniker">The icon to describe the completion item.</param>
        /// <param name="iconAutomationText">The automation name for the icon.</param>
        /// <param name="attributeIcons">Additional icons shown to the right of the DisplayText.</param>
        /// <param name="suffix">Additional text to be shown to the right of the DisplayText.</param>
        public Completion4(string displayText,
                           string insertionText,
                           string description,
                           ImageMoniker iconMoniker,
                           string iconAutomationText = null,
                           IEnumerable<CompletionIcon2> attributeIcons = null,
                           string suffix = null)
            : base(displayText, insertionText, description,
                   iconMoniker: iconMoniker, iconAutomationText: iconAutomationText,
                   attributeIcons: attributeIcons)
        {
            this.Suffix = suffix;
        }

        /// <summary>
        /// The text to be displayed to the right of the DisplayText (and before the attributeIcons).
        /// </summary>
        public string Suffix { get; }
    }
}
