////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Defines a provider of a smart tag source. 
    /// </summary>
    /// <remarks>
    /// This is a MEF component part, and should be exported with the following attribute:
    /// [Export(typeof(ISmartTagSourceProvider))]
    /// [Order]
    /// [Name]
    /// [ContentType]
    /// You must specify the ContentType so that the source provider creates sources for buffers of content types that it
    /// recognizes, and Order to specify the order in which the sources are called.
    /// </remarks>
    [Obsolete(SmartTag.DeprecationMessage)]
    public interface ISmartTagSourceProvider
    {
        /// <summary>
        /// Attempts to create a smart tag source for the specified buffer.
        /// </summary>
        /// <param name="textBuffer">The text buffer for which to create a smart tag source.</param>
        /// <returns>The <see cref="ISmartTagSource"/>, or null if no smart tag source could be created.</returns>
        ISmartTagSource TryCreateSmartTagSource(ITextBuffer textBuffer);
    }
}
