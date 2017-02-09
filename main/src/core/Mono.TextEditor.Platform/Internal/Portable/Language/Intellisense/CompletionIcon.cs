////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using Microsoft.VisualStudio.Utilities;
using System;
using System.ComponentModel;
#if TARGET_VS
using System.Windows.Media;
#endif

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Represents an icon used in the completion.
    /// </summary>
    public class CompletionIcon : IComparable<CompletionIcon>
    {
#if TARGET_VS
        public virtual ImageSource IconSource { get; set; }
#endif
        public virtual string AutomationName { get; set; }
        public virtual string AutomationId { get; set; }
        public virtual int Position { get; set; }

        /// <summary>
        /// Initializes a new instance of <see cref="CompletionIcon"/>.
        /// </summary>
        public CompletionIcon()
        {

        }

        /// <summary>
        /// Initializes a new instance of <see cref="CompletionIcon"/> with the given image, automation values, and position.
        /// </summary>
        /// <param name="imageSource">The icon to describe the completion item.</param>
        /// <param name="automationName">The automation name for the icon.</param>
        /// <param name="automationId">The automation id for the icon.</param>
        /// <param name="position">The display position of the icon. If no value is provided this will be zero.</param>
#if TARGET_VS
        public CompletionIcon(ImageSource imageSource, string automationName, string automationId, int position=0)
        {
            if (imageSource == null)
                throw new ArgumentNullException("imageSource");

            this.IconSource = imageSource;
#else
        public CompletionIcon(string automationName, string automationId, int position = 0)
        {
#endif
            this.AutomationName = automationName;
            this.AutomationId = automationId;
            this.Position = position;
        }

        public int CompareTo(CompletionIcon obj)
        {
            // Sort CompletionIcons by position.
            int x = Position.CompareTo(obj.Position);

            if (x == 0 && AutomationName != null && obj.AutomationName != null)
            {
                x = AutomationName.CompareTo(obj.AutomationName);
            }

            return x;
        }
    }
}
