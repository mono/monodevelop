//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Platform
{
    using System;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.Text;
    using Microsoft.VisualStudio.Text.Editor;
    using MonoDevelop.Ide.Editor.Highlighting;

    /// <summary>
    /// Creates a syntax highlighter for VS ITextBuffer suitable for use in MD.
    /// </summary>
    /// <remarks>This is a MEF component part, and should be imported as follows:
    /// [Import]
    /// ITagBasedSyntaxHighlightingFactory factory = null;
    /// </remarks>
    public interface ITagBasedSyntaxHighlightingFactory
    {
        ISyntaxHighlighting CreateSyntaxHighlighting (ITextView textView);
    }
}
