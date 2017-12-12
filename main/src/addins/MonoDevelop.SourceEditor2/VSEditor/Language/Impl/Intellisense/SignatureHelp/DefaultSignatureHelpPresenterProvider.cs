////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Utilities;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Language.Intellisense.Implementation
{
    [Export(typeof(IIntellisensePresenterProvider))]
    [Name("Default Signature Help Presenter")]
    [ContentType("text")]
    [Order]
    internal sealed class DefaultSignatureHelpPresenterProvider :
        MultiSessionIntellisensePresenterProvider<ISignatureHelpSession, DefaultSignatureHelpPresenter>,
        IIntellisensePresenterProvider
    {
        [Export]
        [Name("intellisense")]
        [BaseDefinition("text")]
        internal ContentTypeDefinition intellisenseContentType;

        [Export]
        [Name("sighelp-doc")]
        [BaseDefinition("intellisense")]
        internal ContentTypeDefinition sigHelpDocumentationContentType;

        [Export(typeof(IClassifierProvider))]
        [ContentType("sighelp-doc")]
        internal sealed class SigHelpDocClassifierProvider : IClassifierProvider
        {
            [Import]
            internal IClassificationTypeRegistryService ClassificationTypeRegistryService { get; set; }

            public IClassifier GetClassifier(ITextBuffer buffer)
            {
                TotalClassifier classifier = 
                    new TotalClassifier
                            (buffer,
                             this.ClassificationTypeRegistryService.GetClassificationType("sighelp-documentation"));

                return classifier;
            }
        }

        [Export]
        [Name("sighelp-documentation")]
        [BaseDefinition("text")]
        internal ClassificationTypeDefinition sigHelpDocClassificationType;

        [Export(typeof(EditorFormatDefinition))]
        [Name("SigHelpDocumentationFormat")]
        [UserVisible(false)]
        [Order(Before = Priority.High, After = Priority.Default)]
        [ClassificationType(ClassificationTypeNames = "sighelp-documentation")]
        internal class SigHelpDocClassificationFormatDefinition : ClassificationFormatDefinition
        {
            public SigHelpDocClassificationFormatDefinition()
                : base()
            {
                this.IsBold = true;
            }
        }

        [Export]
        [Name("sighelp")]
        [BaseDefinition("intellisense")]
        internal ContentTypeDefinition sigHelpContentType;

        [Export(typeof(IClassifierProvider))]
        [ContentType("sighelp")]
        internal sealed class SignatureHelpParameterBoldingClassifierProvider : IClassifierProvider
        {
            [Import]
            internal IClassificationTypeRegistryService ClassificationTypeRegistryService { get; set; }

            public IClassifier GetClassifier(ITextBuffer buffer)
            {
                SignatureHelpParameterBoldingClassfier classifier = 
                    new SignatureHelpParameterBoldingClassfier(buffer, this.ClassificationTypeRegistryService);

                return classifier;
            }
        }

        [Export]
        [Name("currentParam")]
        [BaseDefinition("text")]
        internal ClassificationTypeDefinition currentParamClassificationType;

        [Export(typeof(EditorFormatDefinition))]
        [Name("CurrentParameterFormat")]
        [UserVisible(false)]
        [Order(Before = Priority.High, After = Priority.Default)]
        [ClassificationType(ClassificationTypeNames = "currentParam")]
        internal class CurrentParamClassificationFormatDefinition : ClassificationFormatDefinition
        {
            public CurrentParamClassificationFormatDefinition()
                : base()
            {
                this.IsBold = true;
            }
        }

        [Import]
        internal IContentTypeRegistryService ContentTypeRegistryService { get; set; }

        [Import]
        internal ITextBufferFactoryService TextBufferFactoryService { get; set; }

        [Import]
        internal ITextEditorFactoryService TextEditorFactoryService { get; set; }

        [Import]
        internal IEditorOptionsFactoryService EditorOptionsFactoryService { get; set; }

        [ImportMany]
        internal List<Lazy<SignatureHelpPresenterStyle, IOrderableContentTypeMetadata>> UnOrderedPresenterStyles
        { get; set; }

        [Import]
        internal GuardedOperations GuardedOperations { get; set; }

#if DEBUG
        [ImportMany]
        internal List<Lazy<IObjectTracker>> ObjectTrackers { get; set; }
#endif

        public IIntellisensePresenter TryCreateIntellisensePresenter(IIntellisenseSession session)
        {
            DefaultSignatureHelpPresenter presenter = null;

            // We only support signature help sessions with this presenter, and we only support sessions that actually have
            // signatures
            ISignatureHelpSession signatureHelpSession = session as ISignatureHelpSession;
            if ((signatureHelpSession != null) &&
                (signatureHelpSession.Signatures != null) &&
                (signatureHelpSession.Signatures.Count > 0))
            {
                if (!this.GetOrCreatePresenter(signatureHelpSession, () => new DefaultSignatureHelpPresenter(this), out presenter))
                {
#if DEBUG
                    Helpers.TrackObject(this.ObjectTrackers, "Default Signature Help Presenters", presenter);
#endif
                }
            }

            return presenter;
        }

        internal SignatureHelpPresenterStyle GetMergedPresenterStyle(ISignatureHelpSession session)
        {
            return new MergedSignatureHelpPresenterStyle
                (Helpers.GetMatchingPresenterStyles<ISignatureHelpSession, SignatureHelpPresenterStyle>
                    (session, this.OrderedPresenterStyles, this.GuardedOperations));
        }

        private IList<Lazy<SignatureHelpPresenterStyle, IOrderableContentTypeMetadata>> _orderedPresenterStyles;
        private IList<Lazy<SignatureHelpPresenterStyle, IOrderableContentTypeMetadata>> OrderedPresenterStyles
        {
            get
            {
                if (_orderedPresenterStyles == null)
                {
                    _orderedPresenterStyles = Orderer.Order(this.UnOrderedPresenterStyles);
                }

                return _orderedPresenterStyles;
            }
        }
    }
}
