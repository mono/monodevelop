////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Utilities;

namespace Microsoft.VisualStudio.Language.Intellisense.Implementation
{
    internal class SignatureHelpParameterBoldingClassfier : IClassifier
    {
        private ITextBuffer _textBuffer;
        private event EventHandler<ClassificationChangedEventArgs> _classificationChanged;
        private ISignatureHelpSession _session;
        private IClassificationTypeRegistryService _classificationTypeRegistry;

        public const string UsePrettyPrintedContentKey = "UsePrettyPrintedContent";

        public SignatureHelpParameterBoldingClassfier(ITextBuffer textBuffer,
            IClassificationTypeRegistryService classificationTypeRegistry)
        {
            _textBuffer = textBuffer;
            _classificationTypeRegistry = classificationTypeRegistry;

            // Find the signature help session that created this text buffer.

            if ((!_textBuffer.Properties.TryGetProperty<ISignatureHelpSession>(typeof(ISignatureHelpSession), out _session)) ||
                (_session == null))
            {
                throw new ArgumentException
                    ("Invalid text buffer.  The specified buffer wasn't created by the default signature help presenter",
                     "textBuffer");
            }

            _session.Dismissed += this.OnSession_Dismissed;
            ((INotifyCollectionChanged)_session.Signatures).CollectionChanged += this.OnSignaturesChanged;
            _session.SelectedSignatureChanged += this.OnSession_SelectedSignatureChanged;

            if (_session.SelectedSignature != null)
            {
                _session.SelectedSignature.CurrentParameterChanged += this.OnSelectedSignature_CurrentParameterChanged;
            }
        }

        public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged
        {
            add { _classificationChanged += value; }
            remove { _classificationChanged -= value; }
        }

        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan trackingSpan)
        {
            IClassificationType currentParamType = _classificationTypeRegistry.GetClassificationType("currentParam");
            if (currentParamType == null)
            {
                throw (new InvalidOperationException("Unable to retrieve 'current parameter' classification type"));
            }

            bool usePrettyPrintedContent;
            if (!_textBuffer.Properties.TryGetProperty<bool>(SignatureHelpParameterBoldingClassfier.UsePrettyPrintedContentKey, out usePrettyPrintedContent))
            {
                usePrettyPrintedContent = false;
            }

            FrugalList<ClassificationSpan> classSpans = new FrugalList<ClassificationSpan>();
            if ((_session != null) &&
                (_session.SelectedSignature != null) &&
                (_session.SelectedSignature.CurrentParameter != null))
            {
                Span span = usePrettyPrintedContent ?
                    _session.SelectedSignature.CurrentParameter.PrettyPrintedLocus :
                    _session.SelectedSignature.CurrentParameter.Locus;

                if ((trackingSpan.IntersectsWith(span)) &&
                    (span.End <= _textBuffer.CurrentSnapshot.Length))
                {
                    // We only have one classification span to add, and that's the classification span for the current parameter.

                    classSpans.Add
                        (new ClassificationSpan
                            (new SnapshotSpan(_textBuffer.CurrentSnapshot, span),
                             currentParamType));
                }
            }

            return classSpans;
        }

        private void OnSignaturesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            this.FireClassificationChanged
                (new SnapshotSpan(_textBuffer.CurrentSnapshot, 0, _textBuffer.CurrentSnapshot.Length));
        }

        private void OnSession_SelectedSignatureChanged(object sender, SelectedSignatureChangedEventArgs e)
        {
            // If the selected signature changed, we should subscribe to current parameter change notification on this new
            // selected signature.

            if (e.PreviousSelectedSignature != null)
            {
                e.PreviousSelectedSignature.CurrentParameterChanged -= this.OnSelectedSignature_CurrentParameterChanged;
            }

            if (e.NewSelectedSignature != null)
            {
                e.NewSelectedSignature.CurrentParameterChanged += this.OnSelectedSignature_CurrentParameterChanged;
            }

            this.FireClassificationChanged
                (new SnapshotSpan(_textBuffer.CurrentSnapshot, 0, _textBuffer.CurrentSnapshot.Length));
        }

        private void OnSelectedSignature_CurrentParameterChanged(object sender, CurrentParameterChangedEventArgs e)
        {
            this.FireClassificationChanged
                (new SnapshotSpan(_textBuffer.CurrentSnapshot, 0, _textBuffer.CurrentSnapshot.Length));
        }

        private void OnSession_Dismissed(object sender, EventArgs e)
        {
            _session.Dismissed -= this.OnSession_Dismissed;
            ((INotifyCollectionChanged)_session.Signatures).CollectionChanged -= this.OnSignaturesChanged;
            _session.SelectedSignatureChanged -= this.OnSession_SelectedSignatureChanged;

            if (_session.SelectedSignature != null)
            {
                _session.SelectedSignature.CurrentParameterChanged -= this.OnSelectedSignature_CurrentParameterChanged;
            }
            _session = null;
        }

        private void FireClassificationChanged(SnapshotSpan changeSpan)
        {
            EventHandler<ClassificationChangedEventArgs> tempHandler = _classificationChanged;
            if (tempHandler != null)
            {
                tempHandler(this, new ClassificationChangedEventArgs(changeSpan));
            }
        }
    }
}
