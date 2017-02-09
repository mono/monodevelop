////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    ///<summary>
    /// Defines a set of properties that will be used to style the default signature help presenter.
    ///</summary>
    /// <remarks>
    /// This is a MEF component part, and should be exported with the following attributes:
    /// [Export(typeof(SignatureHelpPresenterStyle))]
    /// [ContentType]
    /// [Name]
    /// [Order]
    /// All exports of this component part should be ordered after the "default" signature help presenter style.  At a minimum,
    /// this means adding [Order(After="default")] to the export metadata.
    /// </remarks>
    public class SignatureHelpPresenterStyle
    {
        /// <summary>
        /// Gets a <see cref="Brush"/> that will be used to paint the foreground of the signature help presenter.
        /// </summary>
        public virtual Brush ForegroundBrush { get; protected set; }

        /// <summary>
        /// Gets a <see cref="Brush"/> that will be used to paint the borders in the signature help presenter.
        /// </summary>
        public virtual Brush BorderBrush { get; protected set; }

        /// <summary>
        /// Gets a <see cref="Brush"/> that will be used to paint the background of the signature help presenter.
        /// </summary>
        public virtual Brush BackgroundBrush { get; protected set; }

        /// <summary>
        /// Gets a string that identifies the appearance category for the <see cref="ITextView"/>s displayed in the default
        /// signature help presenter.
        /// </summary>
        /// <remarks>
        /// Manipulating this value will change the classification format map used in the translation of classification types to
        /// classification formats in the signature <see cref="ITextView"/>.
        /// </remarks>
        public virtual string SignatureAppearanceCategory { get; protected set; }

        /// <summary>
        /// Gets a <see cref="TextRunProperties"/> that will be used to format the text of up/down signature spinner.
        /// </summary>
        public virtual TextRunProperties UpDownSignatureTextRunProperties { get; protected set; }

        /// <summary>
        /// Gets a <see cref="TextRunProperties"/> that will be used to format the text of the signature documentation.
        /// </summary>
        public virtual TextRunProperties SignatureDocumentationTextRunProperties { get; protected set; }

        /// <summary>
        /// Gets a <see cref="TextRunProperties"/> that will be used to format the text of the current parameter name.
        /// </summary>
        public virtual TextRunProperties CurrentParameterNameTextRunProperties { get; protected set; }

        /// <summary>
        /// Gets a <see cref="TextRunProperties"/> that will be used to format the text of the current parameter documentation.
        /// </summary>
        public virtual TextRunProperties CurrentParameterDocumentationTextRunProperties { get; protected set; }

        /// <summary>
        /// Gets a value determining whether or not gradients should be used in the presentation of a
        /// <see cref="ISignatureHelpSession"/>.
        /// </summary>
        public virtual bool? AreGradientsAllowed { get; protected set; }
    }
}
