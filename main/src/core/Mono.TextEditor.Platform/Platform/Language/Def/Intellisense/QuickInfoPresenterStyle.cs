////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System.Collections.Generic;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    ///<summary>
    /// Defines a set of properties that will be used to style the default QuickInfo presenter.
    ///</summary>
    /// <remarks>
    /// This is a MEF component part, and should be exported with the following attributes:
    /// [Export(typeof(QuickInfoPresenterStyle))]
    /// [ContentType]
    /// [Name]
    /// [Order]
    /// All exports of this component part should be ordered after the "default" QuickInfo presenter style.  At a minimum, this
    /// means adding [Order(After="default")] to the export metadata.
    /// </remarks>
    public class QuickInfoPresenterStyle
    {
        /// <summary>
        /// Gets a <see cref="Brush"/> that will be used to paint the borders in the completion presenter.
        /// </summary>
        public virtual Brush BorderBrush { get; protected set; }

        /// <summary>
        /// Gets a <see cref="Brush"/> that will be used to paint the background of the completion presenter.
        /// </summary>
        public virtual Brush BackgroundBrush { get; protected set; }

        /// <summary>
        /// Gets a string that identifies the appearance category for the <see cref="ITextView"/>s displayed in the default
        /// QuickInfo presenter.
        /// </summary>
        /// <remarks>
        /// Manipulating this value will change the classification format map used in the translation of classification types to
        /// classification formats in the QuickInfo <see cref="ITextView"/>.
        /// </remarks>
        public virtual string QuickInfoAppearanceCategory { get; protected set; }

        /// <summary>
        /// Gets a value determining whether or not gradients should be used in the presentation of a
        /// <see cref="IQuickInfoSession"/>.
        /// </summary>
        public virtual bool? AreGradientsAllowed { get; protected set; }
    }
}
