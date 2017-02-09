// ****************************************************************************
// Copyright (C) Microsoft Corporation.  All Rights Reserved.
// ****************************************************************************
using System;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.Text.Classification
{
    /// <summary>
    /// Determining if an export should be visible to the user.
    /// </summary>
    public sealed class UserVisibleAttribute : SingletonBaseMetadataAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserVisibleAttribute"/>.
        /// </summary>
        /// <param name="userVisible"><c>true</c> if the extension is visible to the user, otherwise <c>false</c>.</param>
        public UserVisibleAttribute(bool userVisible)
        {
            this.UserVisible = userVisible;
        }

        /// <summary>
        /// Determines whether the extension is visible to the user.
        /// </summary>
        public bool UserVisible { get; private set; }
    }
}
