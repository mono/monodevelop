////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using Gtk;
using Microsoft.VisualStudio.Platform;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using MonoDevelop.Components;
using MonoDevelop.Core;

namespace Microsoft.VisualStudio.Language.Intellisense.Implementation
{
    internal sealed class SignatureHelpSessionView : IDisposable, INotifyPropertyChanged
    {
        private DefaultSignatureHelpPresenterProvider _componentContext;
        private ISignatureHelpSession _session;
        private string _pagerText;
        private bool _pagerVisibility = false;
        private ITextBuffer _signatureTextBuffer;
        private MonoDevelop.Components.FixedWidthWrapLabel _signatureWpfTextView;
        private string _signatureDocumentation;
        private bool _signatureDocumentationVisibility;
        private string _currentParameterName;
        private string _currentParameterDocumentation;
        private bool _currentParameterVisibility;
        private IContentType _textContentType;
        private double _pagerWidth;
        private bool _isAutoSizePending = false;

        internal SignatureHelpSessionView(DefaultSignatureHelpPresenterProvider componentContext)
        {
            _componentContext = componentContext;
        }

        public ISignatureHelpSession Session
        {
            get { return _session; }
            set
            {
                this.UnBindFromSession();
                _session = value;
                this.BindToSession();
            }
        }

        public string PagerText
        {
            get { return _pagerText; }
            private set
            {
                _pagerText = value;
                this.RaisePropertyChanged("PagerText");
            }
        }

        public bool PagerVisibility
        {
            get { return _pagerVisibility; }
            private set
            {
                _pagerVisibility = value;
                this.RaisePropertyChanged("PagerVisibility");
            }
        }

        public FixedWidthWrapLabel SignatureWpfViewVisualElement { get { return _signatureWpfTextView; } }

        public string SignatureDocumentation
        {
            get { return _signatureDocumentation; }
            private set
            {
                _signatureDocumentation = value;
                this.RaisePropertyChanged("SignatureDocumentation");
            }
        }

        public bool SignatureDocumentationVisibility
        {
            get { return _signatureDocumentationVisibility; }
            private set
            {
                _signatureDocumentationVisibility = value;
                this.RaisePropertyChanged("SignatureDocumentationVisibility");
            }
        }

        public string CurrentParameterName
        {
            get { return _currentParameterName; }
            private set
            {
                _currentParameterName = value;
                this.RaisePropertyChanged("CurrentParameterName");
            }
        }

        public string CurrentParameterDocumentation
        {
            get { return _currentParameterDocumentation; }
            private set
            {
                _currentParameterDocumentation = value;
                this.RaisePropertyChanged("CurrentParameterDocumentation");
            }
        }

        public bool CurrentParameterVisibility
        {
            get { return _currentParameterVisibility; }
            private set
            {
                _currentParameterVisibility = value;
                this.RaisePropertyChanged("CurrentParameterVisibility");
            }
        }

        public double ContainerMaxWidth
        {
            get
            {
                return Helpers.GetScreenRect(this.Session).Width * 0.9;
            }
        }

        public double ContainerMaxHeight
        {
            get
            {
                return Helpers.GetScreenRect(this.Session).Height * 0.5;
            }
        }

        public double CurrentParameterNameMaxWidth
        {
            get
            {
                return Helpers.GetScreenRect(this.Session).Width * 0.2;
            }
        }

        public double PagerWidth
        {
            get
            {
                return _pagerWidth;
            }
            set
            {
                _pagerWidth = value;

                this.AutoSizeSignatureTextView();
            }
        }
		
        public void Dispose()
        {
            this.UnBindFromSession();
			_signatureWpfTextView.Dispose ();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnSession_SelectedSignatureChanged(object sender, SelectedSignatureChangedEventArgs e)
        {
            // Stop listening to CurrentParameterChanged on the old selected signature, and start listening on the new selected
            // signature.
            if (e.PreviousSelectedSignature != null)
            { 
                e.PreviousSelectedSignature.CurrentParameterChanged -= this.OnSelectedSignature_CurrentParameterChanged; 
            }
            if (e.NewSelectedSignature != null)
            { 
                e.NewSelectedSignature.CurrentParameterChanged += this.OnSelectedSignature_CurrentParameterChanged; 
            }

            // Since the selected signature changed, we need to update the signature info being displayed.
            this.UpdateSignatureInfo();
        }

        private void OnSelectedSignature_CurrentParameterChanged(object sender, CurrentParameterChangedEventArgs e)
        {
            this.UpdateParameterInfo();
        }

        private void OnSignatures_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            this.UpdateSignatureInfo();
        }

        private void CreateSignatureTextView()
        {
            IEditorOptions options = _componentContext.EditorOptionsFactoryService.CreateOptions();
            _signatureTextBuffer = _componentContext.TextBufferFactoryService.CreateTextBuffer();
            _signatureWpfTextView = new MonoDevelop.Components.FixedWidthWrapLabel();
			_signatureWpfTextView.Indent = -20;

			_signatureWpfTextView.Wrap = Pango.WrapMode.WordChar;
			_signatureWpfTextView.BreakOnCamelCasing = false;
			_signatureWpfTextView.BreakOnPunctuation = false;
		}

        private void BindToSession()
        {
            // No work to do if we don't have a session
            if ((_session == null) || (_session.IsDismissed))
            { 
                return; 
            }

            // Create the signature text view (if it hasn't already been created)
            if (_signatureWpfTextView == null)
            {
                this.CreateSignatureTextView();
            }

            // Subscribe to a few events
            _session.SelectedSignatureChanged += this.OnSession_SelectedSignatureChanged;
            if (_session.SelectedSignature != null)
            {
                _session.SelectedSignature.CurrentParameterChanged += this.OnSelectedSignature_CurrentParameterChanged;
            }
            ((INotifyCollectionChanged)_session.Signatures).CollectionChanged += this.OnSignatures_CollectionChanged;

            // Ensure we watch the view for layouts and resize it to ensure its contents fit
            _signatureWpfTextView.SizeAllocated += this.OnTextView_LayoutChanged;

            // Add the session as a property to the signature help text buffer.  This will be used by the current parameter bolding
            // classifier.
            _signatureTextBuffer.Properties.AddProperty(typeof(ISignatureHelpSession), _session);

            // Start the process of updating our view model to match the selected signature for this newly-bound session.
            this.UpdateSignatureInfo();
        }

		private void OnTextView_LayoutChanged (object o, SizeAllocatedArgs args)
		{
			AutoSizeSignatureTextView ();
		}

		private void UnBindFromSession()
        {
            // Don't do anything with an invalid session (dismissed session is ok).
            if (_session == null)
            { 
                return; 
            }

            // Unsubscribe from some events.
            _session.SelectedSignatureChanged -= this.OnSession_SelectedSignatureChanged;
            if (_session.SelectedSignature != null)
            {
                _session.SelectedSignature.CurrentParameterChanged -= this.OnSelectedSignature_CurrentParameterChanged;
            }
            ((INotifyCollectionChanged)_session.Signatures).CollectionChanged -= this.OnSignatures_CollectionChanged;

            _signatureWpfTextView.SizeAllocated -= this.OnTextView_LayoutChanged;

            this.PagerVisibility = false;
            this.SignatureDocumentationVisibility = false;
            this.CurrentParameterVisibility = false;

            // Make sure that our property added to the signature text buffer is released.  Also, the buffer should be empty.
            _signatureTextBuffer.Replace(new Span(0, _signatureTextBuffer.CurrentSnapshot.Length), string.Empty);
            _signatureTextBuffer.Properties.RemoveProperty(typeof(ISignatureHelpSession));

            // Change the content-type of our signature text buffer back to text.  This is how we'll ensure that our classifiers
            // always get run.  When we change the content type when binding to a session, our classifiers will be re-run.
            _signatureTextBuffer.ChangeContentType(this.TextContentType, null);

            // Null-out our ref to the session.
            _session = null;
        }

        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler tempHandler = this.PropertyChanged;
            if (tempHandler != null)
            {
                tempHandler(this, new System.ComponentModel.PropertyChangedEventArgs (propertyName));
            }
        }

        private void UpdateSignatureInfo()
        {
            // If the session is in a bad state, just don't update.
            Debug.Assert(_session != null, "How'd we get a null session?");
            if ((_session == null) || _session.IsDismissed || (_session.Signatures.Count < 1))
            {
                return;
            }

            // Determine which signature we should be rendering.  This is usually the selected signature, but if we run up against
            // a session with no selected signature, just use the first one.
            ISignature sigToRender = _session.SelectedSignature != null ? _session.SelectedSignature : _session.Signatures[0];

            // Make sure the content type of the signature text buffer is correct.
            IContentType desiredContentType = this.GetSignatureHelpContentType(sigToRender);
            if (_signatureTextBuffer.ContentType != desiredContentType)
            {
                _signatureTextBuffer.ChangeContentType(desiredContentType, null);
            }

            // Update the text in the signature text buffer to match the new signature being displayed. 
            this.DisplayContent(sigToRender);

            // If all the content didn't fit in one line, then try the pretty printed content unless it's null (see Dev11 #376399)
            if (_signatureWpfTextView.RealWidth > this.ContainerMaxWidth - this.PagerWidth - 4 &&
                sigToRender.PrettyPrintedContent != null)
            {
                this.DisplayPrettyPrintedContent(sigToRender);

                // Ensure that the pretty printed content fits, otherwise, switch back to the regular content as a last resort
                if (_signatureWpfTextView.RealWidth > this.ContainerMaxWidth - this.PagerWidth - 4)
                {
                    this.DisplayContent(sigToRender);
                }
            }

            // Update the documentation of the signature to match.
            if (!string.IsNullOrEmpty(sigToRender.Documentation))
            {
                this.SignatureDocumentation = sigToRender.Documentation;
                this.SignatureDocumentationVisibility = true;
            }
            else
            {
                this.SignatureDocumentationVisibility = false;
                this.SignatureDocumentation = string.Empty;
            }

            // See if the pager should be enabled.  It should show up only if there are at least 2 signatures.
            if (this.Session.Signatures.Count > 1)
            {
                // The pager should be enabled.
                int selectionIndex = this.Session.Signatures.IndexOf(sigToRender);
				this.PagerText = GettextCatalog.GetString ("{0} of {1}",
					 selectionIndex + 1,
					 this.Session.Signatures.Count);
                this.PagerVisibility = true;
            }
            else
            {
                // The pager should not be enabled.
                this.PagerVisibility = false;
                this.PagerText = string.Empty;
            }

            // Whenever the signature information is being updated, chances are we have to update the parameter info as well.
            this.UpdateParameterInfo();
        }

		private void DisplayContent (ISignature sigToRender)
		{
			// Ensure word-wrap is off initially. We only turn word-wrap on if we are forced to due to lack of
			// space on the screen
			_signatureTextBuffer.Properties[SignatureHelpParameterBoldingClassfier.UsePrettyPrintedContentKey] = false;
			_signatureTextBuffer.Replace (new Span (0, _signatureTextBuffer.CurrentSnapshot.Length), sigToRender.Content);
			var classifierSpans = PlatformCatalog.Instance.ClassifierAggregatorService.GetClassifier (_signatureTextBuffer)?.GetClassificationSpans (new SnapshotSpan (_signatureTextBuffer.CurrentSnapshot, 0, _signatureTextBuffer.CurrentSnapshot.Length));
			if (classifierSpans != null && classifierSpans.Count > 0)
				_signatureWpfTextView.Markup = MDUtils.ClassificationsToMarkup (_signatureTextBuffer.CurrentSnapshot, classifierSpans, sigToRender.CurrentParameter?.Locus);
			else {
				_signatureWpfTextView.Markup = MonoDevelop.Ide.TypeSystem.Ambience.EscapeText (_signatureTextBuffer.CurrentSnapshot.GetText ());
				LoggingService.LogWarning ("No classification spans found for signature helper:" + _signatureTextBuffer.ContentType);
			}
			_signatureWpfTextView.Visible = true;
		}

        private void DisplayPrettyPrintedContent(ISignature sigToRender)
		{
			Debug.Assert (sigToRender.PrettyPrintedContent != null, "We shouldn't try to display null PrettyPrintedContent.");
			// Ensure word-wrap is off initially. We only turn word-wrap on if we are forced to due to lack of
			// space on the screen
			_signatureTextBuffer.Properties[SignatureHelpParameterBoldingClassfier.UsePrettyPrintedContentKey] = true;
			_signatureTextBuffer.Replace (new Span (0, _signatureTextBuffer.CurrentSnapshot.Length), sigToRender.PrettyPrintedContent);
			var classifierSpans = PlatformCatalog.Instance.ClassifierAggregatorService.GetClassifier (_signatureTextBuffer)?.GetClassificationSpans (new SnapshotSpan (_signatureTextBuffer.CurrentSnapshot, 0, _signatureTextBuffer.CurrentSnapshot.Length));
			if (classifierSpans != null && classifierSpans.Count > 0)
				_signatureWpfTextView.Markup = MDUtils.ClassificationsToMarkup (_signatureTextBuffer.CurrentSnapshot, classifierSpans, sigToRender.CurrentParameter?.Locus);
			else {
				_signatureWpfTextView.Markup = MonoDevelop.Ide.TypeSystem.Ambience.EscapeText (_signatureTextBuffer.CurrentSnapshot.GetText ());
				LoggingService.LogWarning ("No classification spans found for signature helper:" + _signatureTextBuffer.ContentType);
			}
			_signatureWpfTextView.Visible = true;
		}

        private void UpdateParameterInfo()
        {
            // If the session is in a bad state, just don't update.
            Debug.Assert(_session != null, "How'd we get a null session?");
            if ((_session == null) || _session.IsDismissed || (_session.Signatures.Count < 1))
            {
                return;
            }

            // Determine which parameter is the current one.
            IParameter currentParam = _session.SelectedSignature != null
                ? _session.SelectedSignature.CurrentParameter
                : _session.Signatures[0].CurrentParameter;

            // Should the "current parameter" section even be enabled?  Not if either the name or documentation is empty.
            if ((currentParam == null) ||
                string.IsNullOrEmpty(currentParam.Name) ||
                string.IsNullOrEmpty(currentParam.Documentation))
            {
                this.CurrentParameterVisibility = false;
                this.CurrentParameterName = string.Empty;
                this.CurrentParameterDocumentation = string.Empty;
            }
            else
            {
				this.CurrentParameterName = GettextCatalog.GetString ("{0}:",
					 currentParam.Name);
                this.CurrentParameterDocumentation = currentParam.Documentation;
                this.CurrentParameterVisibility = true;
            }
        }

        private IContentType TextContentType
        {
            get
            {
                if (_textContentType == null)
                {
                    _textContentType = _componentContext.ContentTypeRegistryService.GetContentType("text");
                }

                return _textContentType;
            }
        }

        private IContentType GetSignatureHelpContentType(ISignature signature)
        {
            if ((_session == null) || (_session.IsDismissed))
            {
                throw new InvalidOperationException("We can't determine the signature help content type without a session");
            }

			IContentType baseContentType = signature.ApplicableToSpan.TextBuffer.ContentType;
            string newContentTypeName = string.Format(CultureInfo.InvariantCulture, "{0} Signature Help", baseContentType.TypeName);
			if (newContentTypeName == "HTMLXProjection Signature Help")
				newContentTypeName = "CSharp Signature Help";
			IContentType sigHelpContentType = _componentContext.ContentTypeRegistryService.GetContentType(newContentTypeName);

            if (sigHelpContentType == null)
            {
                string[] baseContentTypeNames = new string[] { "sighelp" };

                sigHelpContentType = _componentContext.ContentTypeRegistryService.AddContentType
                    (newContentTypeName,
                     baseContentTypeNames);
            }

            return sigHelpContentType;
        }

        private void AutoSizeSignatureTextView()
        {
			//TODO
            //Helpers.AutoSizeTextView(_signatureWpfTextView, this.ContainerMaxWidth - this.PagerWidth - 4 /* 4px of Margin */, this.ContainerMaxHeight);
            _isAutoSizePending = false;
        }
    }
}
