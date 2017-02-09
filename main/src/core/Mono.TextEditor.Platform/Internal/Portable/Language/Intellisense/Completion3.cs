// Copyright (c) Microsoft Corporation
// All rights reserved

#if TARGET_VS
using Microsoft.VisualStudio.Imaging.Interop;
#endif
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
#if TARGET_VS
using System.Windows.Data;
using System.Windows.Media;
#endif

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Completion3 uses <see cref="ImageMoniker"/>s instead of <see cref="ImageSource"/>s to reference icons.
    /// </summary>
    [CLSCompliant(false)]
    public class Completion3 : Completion2
    {
        /// <summary>
        /// Initializes a new instance of <see cref="Completion3"/>.
        /// </summary>
        public Completion3()
            : base()
        {

        }

        /// <summary>
        /// Initializes a new instance of <see cref="Completion3"/> with the specified text and description.
        /// </summary>
        /// <param name="displayText">The text that is to be displayed by an IntelliSense presenter.</param>
        /// <param name="insertionText">The text that is to be inserted into the buffer if this completion is committed.</param>
        /// <param name="description">A description that could be displayed with the display text of the completion.</param>
        /// <param name="iconMoniker">The icon to describe the completion item.</param>
        /// <param name="iconAutomationText">The automation name for the icon.</param>
#if TARGET_VS
        public Completion3(string displayText,
                          string insertionText,
                          string description,
                          ImageMoniker iconMoniker,
                          string iconAutomationText)
            : this(displayText, insertionText, description,
                   iconMoniker, iconAutomationText: iconAutomationText, attributeIcons: null)
        {
        }
#else
        public Completion3(string displayText,
                          string insertionText,
                          string description,
                          string iconAutomationText)
            : base(displayText, insertionText, description,
                   iconAutomationText: iconAutomationText, attributeIcons: null)
        {
        }
#endif

        /// <summary>
        /// Initializes a new instance of <see cref="Completion3"/> with the specified text, description, and icon.
        /// </summary>
        /// <param name="displayText">The text that is to be displayed by an IntelliSense presenter.</param>
        /// <param name="insertionText">The text that is to be inserted into the buffer if this completion is committed.</param>
        /// <param name="description">A description that could be displayed with the display text of the completion.</param>
        /// <param name="iconMoniker">The icon to describe the completion item.</param>
        /// <param name="iconAutomationText">The automation name for the icon.</param>
        /// <param name="attributeIcons">Additional icons shown to the right of the DisplayText.</param>
#if TARGET_VS
        public Completion3(string displayText,
                          string insertionText,
                          string description,
                          ImageMoniker iconMoniker,
                          string iconAutomationText,
                          IEnumerable<CompletionIcon2> attributeIcons)
            : base(displayText, insertionText, description,
                   iconSource: null, iconAutomationText: iconAutomationText,
                   attributeIcons: attributeIcons)
        {
            this.IconMoniker = iconMoniker;
        }
#endif

#if TARGET_VS
        /// <summary>
        /// Gets or sets the moniker used to define a multi-resolution image.
        /// </summary>
        public virtual ImageMoniker IconMoniker
        {
            get;
            private set;
        }

        /// <summary>
        /// This property is not supported by <see cref="Completion3"/> and will always return <value>null</value>.
        /// To get the current icon use <see cref="IconMoniker"/>.
        /// </summary>
        public override ImageSource IconSource
        {
            get
            {
                Debug.Fail("IconSource should not be used on Completion3");
                return null;
            }
            set
            {
                Debug.Assert(value == null, "IconSource should never be set on Completion3");
            }
        }
#endif
    }
}
