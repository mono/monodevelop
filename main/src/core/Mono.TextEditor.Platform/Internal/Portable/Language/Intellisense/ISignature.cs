////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System.Collections.ObjectModel;
using Microsoft.VisualStudio.Text;
using System;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Represents an individual signature displayed in a tool, such as the signature help tool.
    /// </summary>
    public interface ISignature
    {
        /// <summary>
        /// Gets the span of text in the buffer to which this signature help is applicable.
        /// </summary>
        ITrackingSpan ApplicableToSpan { get; }

        /// <summary>
        /// Gets the content of the signature, including all the characters to be displayed.  
        /// </summary>
        /// <remarks>
        /// This text may appear in a text view, and can be colored using a standard classifier mechanism.
        /// </remarks>
        string Content { get; }

        /// <summary>
        /// Gets the content of the signature, pretty-printed into a form suitable for display on-screen.
        /// </summary>
        /// <remarks>
        /// Pretty-printed signatures are usually displayed in width-constrained environments when the regular signature content
        /// cannot be displayed on one line.
        /// </remarks>
        string PrettyPrintedContent { get; }

        /// <summary>
        /// Gets the content of the documentation associated with this signature.  
        /// </summary>
        /// <remarks>
        /// This text may appear
        /// alongside the signature in an IntelliSense tool.
        /// </remarks>
        string Documentation { get; }

        /// <summary>
        /// Gets the list of parameters that this signature knows about.  
        /// </summary>
        /// <remarks>
        /// Each parameter has information relating to its text position
        /// within the signature string.
        /// </remarks>
        ReadOnlyCollection<IParameter> Parameters { get; }

        /// <summary>
        /// Gets the current parameter for this signature.  
        /// </summary>
        /// <remarks>
        /// When the caret is within the signature's applicability
        /// span, this value is the parameter over which the caret is positioned. When the caret is not within the signature's
        /// applicability span, this value is undefined.
        /// </remarks>
        IParameter CurrentParameter { get; }

        /// <summary>
        /// Occurs when the current parameter changes.
        /// </summary>
        event EventHandler<CurrentParameterChangedEventArgs> CurrentParameterChanged;
    }
}
