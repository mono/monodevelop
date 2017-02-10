// Copyright (c) Microsoft Corporation
// All rights reserved

using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
#if TARGET_VS
using System.Windows.Data;
using System.Windows.Media;
#endif

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Completion2 adds an additional context icon to the completion.
    /// </summary>
    public class Completion2 : Completion
    {
        private List<CompletionIcon> _attributeIcons;

        /// <summary>
        /// Initializes a new instance of <see cref="Completion2"/>.
        /// </summary>
        public Completion2()
            : base()
        {

        }

        /// <summary>
        /// Initializes a new instance of <see cref="Completion2"/> with the specified text and description.
        /// </summary>
        /// <param name="displayText">The text that is to be displayed by an IntelliSense presenter.</param>
        /// <param name="insertionText">The text that is to be inserted into the buffer if this completion is committed.</param>
        /// <param name="description">A description that could be displayed with the display text of the completion.</param>
        /// <param name="iconSource">The icon to describe the completion item.</param>
        /// <param name="iconAutomationText">The automation name for the icon.</param>
#if TARGET_VS
        public Completion2(string displayText,
                          string insertionText,
                          string description,
                          ImageSource iconSource,
                          string iconAutomationText)
            : base(displayText, insertionText, description, iconSource, iconAutomationText)
        {

        }
#else
        public Completion2(string displayText,
                          string insertionText,
                          string description,
                          string iconAutomationText)
            : base(displayText, insertionText, description, iconAutomationText)
        {

        }
#endif

        /// <summary>
        /// Initializes a new instance of <see cref="Completion2"/> with the specified text and description.
        /// </summary>
        /// <param name="displayText">The text that is to be displayed by an IntelliSense presenter.</param>
        /// <param name="insertionText">The text that is to be inserted into the buffer if this completion is committed.</param>
        /// <param name="description">A description that could be displayed with the display text of the completion.</param>
        /// <param name="iconSource">The icon to describe the completion item.</param>
        /// <param name="iconAutomationText">The automation name for the icon.</param>
        /// <param name="attributeIcons">Additional icons shown to the right of the DisplayText.</param>
#if TARGET_VS
        public Completion2(string displayText,
                          string insertionText,
                          string description,
                          ImageSource iconSource,
                            string iconAutomationText,
                          IEnumerable<CompletionIcon> attributeIcons)
            : base(displayText, insertionText, description, iconSource, iconAutomationText)
#else
        public Completion2(string displayText,
                          string insertionText,
                          string description,
                          string iconAutomationText,
                          IEnumerable<CompletionIcon> attributeIcons)
            : base(displayText, insertionText, description, iconAutomationText)
#endif
        {
            if (attributeIcons != null)
            {
                AttributeIcons = attributeIcons;
            }
        }

        /// <summary>
        /// Gets or sets the additional icons displayed for this completion item.
        /// </summary>
        /// <remarks>Returns null if no attribute icons were provided.</remarks>
        public virtual IEnumerable<CompletionIcon> AttributeIcons
        {
            get
            {
                return _attributeIcons;
            }
            set
            {
                _attributeIcons = SortIcons(value);
            }
        }

        private static List<CompletionIcon> SortIcons(IEnumerable<CompletionIcon> icons)
        {
            // evaluate and sort the IEnumerable here
            List<CompletionIcon> sortedIcons = new List<CompletionIcon>(icons);
            sortedIcons.Sort();
            return sortedIcons;
        }
    }
}
