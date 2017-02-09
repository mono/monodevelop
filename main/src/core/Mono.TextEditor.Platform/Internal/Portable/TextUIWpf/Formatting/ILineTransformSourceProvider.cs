// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Text.Formatting
{
    using System.ComponentModel.Composition;
    using Microsoft.VisualStudio.Text.Editor;

    /// <summary>
    /// Provides <see cref="ILineTransformSource"/> objects.  
    /// </summary>
    /// <remarks>This is a MEF component part, and should be exported with the following attribute:
    /// [Export(typeof(ILineTransformSourceProvider))]
    /// Exporters must supply a ContentTypeAttribute and TextViewRoleAttribute.
    /// </remarks>
    public interface ILineTransformSourceProvider
    {
        /// <summary>
        /// Creates an <see cref="ILineTransformSource"/> for the given <paramref name="textView"/>.
        /// </summary>
        /// <param name="textView">The <see cref="IWpfTextView"/> on which the <see cref="ILineTransformSource"/> will format.</param>
        /// <returns>The new <see cref="ILineTransformSource"/>.  
        /// The value may be null if this <see cref="ILineTransformSourceProvider"/> decides not to participate.</returns>
        ILineTransformSource Create(IWpfTextView textView);
    }
}
