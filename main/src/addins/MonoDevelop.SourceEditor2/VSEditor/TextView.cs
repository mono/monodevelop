//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Implementation;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Text.Utilities;
using Microsoft.VisualStudio.Utilities;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Editor;
using Microsoft.VisualStudio.Platform;

namespace Microsoft.VisualStudio.Text.Editor.Implementation
{
    internal class TextView : ITextView
    {
        #region Private Members
        private TextEditor _textEditor;

        ITextBuffer _textBuffer;
        //		ITextSnapshot _textSnapshot;

        //		ITextBuffer _visualBuffer;
        //		ITextSnapshot _visualSnapshot;

        IBufferGraph _bufferGraph;
        ITextViewRoleSet _roles;

        ConnectionManager _connectionManager;

        TextEditorFactoryService _factoryService;

        //		IEditorFormatMap _editorFormatMap;

        private bool _hasInitializeBeenCalled = false;

        private ITextSelection _selection;

        private bool _hasAggregateFocus;

        private IEditorOptions _editorOptions;

        private List<Lazy<ITextViewCreationListener, IDeferrableContentTypeAndTextViewRoleMetadata>> _deferredTextViewListeners;

        private ITextCaret _caret;

        bool _isClosed = false;

        private PropertyCollection _properties = new PropertyCollection();

        //Only one view at a time will have aggregate focus, so keep track of it so that (when sending aggregate focus changed events)
        //we give a view that had focus the chance to send its lost focus message before we claim aggregate focus.
        [ThreadStatic]
        static TextView ViewWithAggregateFocus = null;
#if DEBUG
        [ThreadStatic]
        static bool SettingAggregateFocus = false;
#endif

#endregion // Private Members

        /// <summary>
        /// Text View constructor.
        /// </summary>
        /// <param name="textViewModel">The text view model that provides the text to visualize.</param>
        /// <param name="roles">Roles for this view.</param>
        /// <param name="parentOptions">Parent options for this view.</param>
        /// <param name="factoryService">Our handy text editor factory service.</param>
        internal TextView(TextEditor textEditor, ITextViewModel textViewModel, ITextViewRoleSet roles, IEditorOptions parentOptions, TextEditorFactoryService factoryService, bool initialize = true)
        {
            _textEditor = textEditor;

            _roles = roles;

            _factoryService = factoryService;

            this.TextDataModel = textViewModel.DataModel;
            this.TextViewModel = textViewModel;

            _textBuffer = textViewModel.EditBuffer;
            //			_visualBuffer = textViewModel.VisualBuffer;

            //			_textSnapshot = _textBuffer.CurrentSnapshot;
            //			_visualSnapshot = _visualBuffer.CurrentSnapshot;

            _editorOptions = _factoryService.EditorOptionsFactoryService.GetOptions(this);
            _editorOptions.Parent = parentOptions;

            if (initialize)
                this.Initialize();
        }

        internal bool IsTextViewInitialized { get { return _hasInitializeBeenCalled; } }

        // This method should only be called once (it is normally called from the ctor unless we're using
        // ITextEditorFactoryService2.CreateTextViewWithoutInitialization on the factory to delay initialization).
        internal void Initialize()
        {
            if (_hasInitializeBeenCalled)
                throw new InvalidOperationException("Attempted to Initialize a WpfTextView twice");

            _bufferGraph = _factoryService.BufferGraphFactoryService.CreateBufferGraph(this.TextViewModel.VisualBuffer);

            //_editorFormatMap = _factoryService.EditorFormatMapService.GetEditorFormatMap(this);

            _selection = new TextSelection(_textEditor, this);

            // Create caret
            _caret = new TextCaret(_textEditor, this);

            //			this.Loaded += OnLoaded;

            // TODO: *Someone* needs to call this to execute UndoHistoryRegistry.RegisterHistory -- VS does this via the ShimCompletionControllerFactory.
            _factoryService.EditorOperationsProvider.GetEditorOperations (this);

            _connectionManager = new ConnectionManager(this, _factoryService.TextViewConnectionListeners, _factoryService.GuardedOperations);

            SubscribeToEvents();

            // Binding content type specific assets includes calling out to content-type
            // specific view creation listeners.  We need to do this as late as possible.
            this.BindContentTypeSpecificAssets(null, TextViewModel.DataModel.ContentType);

            //Subscribe now so that there is no chance that a layout could be forced by a text change.
            //_visualBuffer.ChangedLowPriority += OnVisualBufferChanged;
            //_visualBuffer.ContentTypeChanged += OnVisualBufferContentTypeChanged;

            _hasInitializeBeenCalled = true;
        }

        public ITextCaret Caret
        {
            get
            {
                return _caret;
            }
        }

        public bool HasAggregateFocus
        {
            get
            {
                return _hasAggregateFocus;
            }
        }

        public bool InLayout
        {
            get
            {
                return false;
            }
        }

        public bool IsClosed { get { return _isClosed; } }

        public bool IsMouseOverViewOrAdornments
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public double LineHeight
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public double MaxTextRightCoordinate
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public IEditorOptions Options
        {
            get { return _editorOptions; }
        }

        public PropertyCollection Properties
        {
            get
            {
                return _properties;
            }
        }

        public ITrackingSpan ProvisionalTextHighlight
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public ITextSelection Selection
        {
            get
            {
                return _selection;
            }
        }

        public ITextViewRoleSet Roles
        {
            get
            {
                return _roles;
            }
        }

        /// <summary>
        /// Gets the text buffer whose text, this text editor renders
        /// </summary>
        public ITextBuffer TextBuffer
        {
            get
            {
                return _textBuffer;
            }
        }

        public IBufferGraph BufferGraph
        {
            get
            {
                return _bufferGraph;
            }
        }

        public ITextSnapshot TextSnapshot
        {
            get
            {
                // TODO: MONO: WpfTextView has a much more complex calculation of this
                return TextBuffer.CurrentSnapshot;
                //				return _textSnapshot;
            }
        }

        public ITextSnapshot VisualSnapshot
        {
            get
            {
                return TextBuffer.CurrentSnapshot;
                //                return _visualSnapshot;
            }
        }

        public ITextDataModel TextDataModel { get; private set; }
        public ITextViewModel TextViewModel { get; private set; }

#if TARGET_VS
        public ITextViewLineCollection TextViewLines
        {
            get
            {
                return _textViewLinesCollection;
            }
        }
#endif

        public double ViewportBottom
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public double ViewportHeight
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public double ViewportLeft
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public double ViewportRight
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public double ViewportTop
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public double ViewportWidth
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public IViewScroller ViewScroller
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public ITextViewLineCollection TextViewLines
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public event EventHandler Closed;
        public event EventHandler GotAggregateFocus;
        public event EventHandler LostAggregateFocus;
#pragma warning disable CS0067
        public event EventHandler<TextViewLayoutChangedEventArgs> LayoutChanged;
        public event EventHandler ViewportLeftChanged;
        public event EventHandler ViewportHeightChanged;
        public event EventHandler ViewportWidthChanged;
        public event EventHandler<MouseHoverEventArgs> MouseHover;
#pragma warning restore CS0067

        public void Close()
        {
            if (_isClosed)
                throw new InvalidOperationException();//Strings.TextViewClosed);

            if (_hasAggregateFocus)
            {
                //Silently lose aggregate focus (to preserve Dev11 compatibility which did not raise a focus changed event when the view was closed).
                Debug.Assert(TextView.ViewWithAggregateFocus == this);
                TextView.ViewWithAggregateFocus = null;
                _hasAggregateFocus = false;
            }

            UnsubscribeFromEvents();

            _connectionManager.Close();

            TextViewModel.Dispose();
            TextViewModel = null;

            _isClosed = true;

            _factoryService.GuardedOperations.RaiseEvent(this, this.Closed);
        }

        public void DisplayTextLineContainingBufferPosition(SnapshotPoint bufferPosition, double verticalDistance, ViewRelativePosition relativeTo)
        {
            throw new NotImplementedException();
        }

        public void DisplayTextLineContainingBufferPosition(SnapshotPoint bufferPosition, double verticalDistance, ViewRelativePosition relativeTo, double? viewportWidthOverride, double? viewportHeightOverride)
        {
            throw new NotImplementedException();
        }

        public SnapshotSpan GetTextElementSpan(SnapshotPoint point)
        {
            throw new NotImplementedException();
        }

        public ITextViewLine GetTextViewLineContainingBufferPosition(SnapshotPoint bufferPosition)
        {
            throw new NotImplementedException();
        }

        public void QueueSpaceReservationStackRefresh()
        {
            throw new NotImplementedException();
        }

        /// <remarks>
        /// If you add an event subscription to this method, be sure to add the corresponding unsubscription to
        /// UnsubscribeFromEvents()
        /// </remarks>
        private void SubscribeToEvents()
        {
                if (IdeApp.IsInitialized)
                        IdeApp.Workbench.ActiveDocumentChanged += Workbench_ActiveDocumentChanged;
        }

        void Workbench_ActiveDocumentChanged(object sender, EventArgs e)
        {
            QueueAggregateFocusCheck();
        }

        private void UnsubscribeFromEvents()
        {
                if (IdeApp.IsInitialized)
                        IdeApp.Workbench.ActiveDocumentChanged -= Workbench_ActiveDocumentChanged;
        }

        private void BindContentTypeSpecificAssets(IContentType beforeContentType, IContentType afterContentType)
        {
            // Notify the Text view creation listeners
            var extensions = UIExtensionSelector.SelectMatchingExtensions(_factoryService.TextViewCreationListeners, afterContentType, beforeContentType, _roles);
            foreach (var extension in extensions)
            {
                string deferOptionName = extension.Metadata.OptionName;
                if (!string.IsNullOrEmpty(deferOptionName) && Options.IsOptionDefined(deferOptionName, false))
                {
                    object value = Options.GetOptionValue(deferOptionName);
                    if (value is bool)
                    {
                        if (!(bool)value)
                        {
                            if (_deferredTextViewListeners == null)
                            {
                                _deferredTextViewListeners = new List<Lazy<ITextViewCreationListener, IDeferrableContentTypeAndTextViewRoleMetadata>>();
                            }
                            _deferredTextViewListeners.Add(extension);
                            continue;
                        }
                    }
                }

                var instantiatedExtension = _factoryService.GuardedOperations.InstantiateExtension(extension, extension);
                if (instantiatedExtension != null)
                {
                    _factoryService.GuardedOperations.CallExtensionPoint(instantiatedExtension,
                        () => instantiatedExtension.TextViewCreated(this));
                }
            }
        }

        /// <summary>
        /// Handles the Classification changed event that comes from the Classifier aggregator
        /// </summary>
        void OnClassificationChanged(object sender, ClassificationChangedEventArgs e)
        {
            if (!_isClosed)
            {
                // When classifications change, we just invalidate the lines. That invalidation will
                // create new lines based on the new classifications.

                // Map the classification change (from the edit buffer) to the visual buffer
                Span span = Span.FromBounds(
                        TextViewModel.GetNearestPointInVisualSnapshot(e.ChangeSpan.Start, VisualSnapshot, PointTrackingMode.Negative),
                        TextViewModel.GetNearestPointInVisualSnapshot(e.ChangeSpan.End, VisualSnapshot, PointTrackingMode.Positive));

                //Classifications changes invalidate only the characters contained in the span so a zero length change
                //will have no effect.
                if (span.Length > 0)
                {
                    //IsLineInvalid will invalidate a line if it intersects the end. The result is that any call to InvalidateSpan() implicitly
                    //invalidates any line that starts at the end of the invalidated span, which we do not want here. Reduce the length of the classification
                    //change span one so -- if someone invalidated an entire line including the line break -- the next line will not be invalidated.
                    span = new Span(span.Start, span.Length - 1);

// MONO: TODO: this

                    //lock (_invalidatedSpans)
                    //{
                    //	if ((_attachedLineCache.Count > 0) || (_unattachedLineCache.Count > 0))
                    //	{
                    //		_reclassifiedSpans.Add(span);
                    //		this.QueueLayout();
                    //	}
                    //}
                }
            }
        }

        internal void QueueAggregateFocusCheck(bool checkForFocus = true)
        {
#if DEBUG
            if (TextView.SettingAggregateFocus)
            {
                Debug.Fail("WpfTextView.SettingAggregateFocus");
            }
#endif

            if (!_isClosed)
            {
                bool newHasAggregateFocus = (IdeApp.Workbench.ActiveDocument?.Editor == _textEditor);
                if (newHasAggregateFocus != _hasAggregateFocus)
                {
                    _hasAggregateFocus = newHasAggregateFocus;

                    if (_hasAggregateFocus)
                    {
                        //Got focus so make sure that the view that had focus (which wasn't us since we didn't have focus before) raises its
                        //lost focus event before we raise our got focus event. This will potentially do bad things if someone changes focus 
                        //if the lost aggregate focus handler.
                        Debug.Assert(TextView.ViewWithAggregateFocus != this);
                        if (TextView.ViewWithAggregateFocus != null)
                        {
                            TextView.ViewWithAggregateFocus.QueueAggregateFocusCheck(checkForFocus: false);
                        }
                        Debug.Assert(TextView.ViewWithAggregateFocus == null);
                        TextView.ViewWithAggregateFocus = this;
                    }
                    else
                    {
                        //Lost focus (which means we were the view with focus).
                        Debug.Assert(TextView.ViewWithAggregateFocus == this);
                        TextView.ViewWithAggregateFocus = null;
                    }

                    EventHandler handler = _hasAggregateFocus ? this.GotAggregateFocus : this.LostAggregateFocus;

#if DEBUG
                    try
                    {
                        TextView.SettingAggregateFocus = true;
#endif
                    _factoryService.GuardedOperations.RaiseEvent(this, handler);
#if DEBUG
                    }
                    finally
                    {
                        TextView.SettingAggregateFocus = false;
                    }
#endif
                }
            }
        }
    }
}

