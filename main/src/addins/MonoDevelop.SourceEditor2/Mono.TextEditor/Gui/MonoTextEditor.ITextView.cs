//
// MonoTextEditor.ITextView.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2018 Microsoft Corporation. All rights reserved.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Text.Editor.Implementation;
using Microsoft.VisualStudio.Text.Utilities;
using System.Diagnostics;
using MonoDevelop.Ide;
using Microsoft.VisualStudio.Text.Classification;
using System.Threading;
using Microsoft.VisualStudio.Text.Operations.Implementation;
using Microsoft.VisualStudio.Text.Operations;

namespace Mono.TextEditor
{
	partial class MonoTextEditor : IMdTextView
	{
		#region Private Members

		public Gtk.Container VisualElement { get => textArea; }

		ITextBuffer textBuffer;

		IBufferGraph bufferGraph;
		ITextViewRoleSet roles;

		ConnectionManager connectionManager;

		TextEditorFactoryService factoryService;
		int queuedSpaceReservationStackRefresh = 0;    //int so that it can be set via Interlocked.CompareExchange()

		//		IEditorFormatMap _editorFormatMap;

		bool hasInitializeBeenCalled = false;

		ITextSelection selection;

		private IEditorOperations editorOperations;
		internal IEditorOperations EditorOperations 
		{
			get {
				if (editorOperations == null) {
					// *Someone* needs to call this to execute UndoHistoryRegistry.RegisterHistory -- VS does this via the ShimCompletionControllerFactory.
					// See https://devdiv.visualstudio.com/DevDiv/_workitems/edit/669018 for details.
					editorOperations = factoryService.EditorOperationsProvider.GetEditorOperations (this);
				}

				return editorOperations;
			}
		}

		bool hasAggregateFocus;

		IEditorOptions editorOptions;

		List<Lazy<ITextViewCreationListener, IDeferrableContentTypeAndTextViewRoleMetadata>> deferredTextViewListeners;

		bool isClosed = false;

		private PropertyCollection properties = new PropertyCollection ();

		//Only one view at a time will have aggregate focus, so keep track of it so that (when sending aggregate focus changed events)
		//we give a view that had focus the chance to send its lost focus message before we claim aggregate focus.
		[ThreadStatic]
		static MonoTextEditor ViewWithAggregateFocus = null;
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
		internal void Initialize (ITextViewModel textViewModel, ITextViewRoleSet roles, IEditorOptions parentOptions, TextEditorFactoryService factoryService, bool initialize = true)
		{
			this.roles = roles;
			this.factoryService = factoryService;
            GuardedOperations = this.factoryService.GuardedOperations;
            _spaceReservationStack = new SpaceReservationStack(this.factoryService.OrderedSpaceReservationManagerDefinitions, this);

			this.TextDataModel = textViewModel.DataModel;
			this.TextViewModel = textViewModel;

			this.textArea.TextViewLines = new MdTextViewLineCollection (this);
			textArea.LayoutChanged += TextAreaLayoutChanged;

			textBuffer = textViewModel.EditBuffer;
            //			_visualBuffer = textViewModel.VisualBuffer;

            //			_textSnapshot = _textBuffer.CurrentSnapshot;
            //			_visualSnapshot = _visualBuffer.CurrentSnapshot;

            editorOptions = this.factoryService.EditorOptionsFactoryService.GetOptions (this);
			editorOptions.Parent = parentOptions;

			if (initialize)
				this.Initialize ();
		}

		static List<ITextViewLine> emptyTextViewLineList = new List<ITextViewLine> (0);
		void TextAreaLayoutChanged(object sender, EventArgs args)
		{
			//TODO: Properly implement LayoutChanged with all data
			LayoutChanged?.Invoke (this, new TextViewLayoutChangedEventArgs (new ViewState (this), new ViewState (this), emptyTextViewLineList, emptyTextViewLineList));
		}

		internal bool IsTextViewInitialized { get { return hasInitializeBeenCalled; } }

		// This method should only be called once (it is normally called from the ctor unless we're using
		// ITextEditorFactoryService2.CreateTextViewWithoutInitialization on the factory to delay initialization).
		internal void Initialize ()
		{
			if (hasInitializeBeenCalled)
				throw new InvalidOperationException ("Attempted to Initialize a WpfTextView twice");

			bufferGraph = factoryService.BufferGraphFactoryService.CreateBufferGraph (this.TextViewModel.VisualBuffer);

			//_editorFormatMap = _factoryService.EditorFormatMapService.GetEditorFormatMap(this);

			selection = new TextSelection (this);

			//			this.Loaded += OnLoaded;

			// We need to instantiate EditorOperations, because it in turn will register the UndoHistory
			// for the buffer via:
			// https://github.com/KirillOsenkov/vs-editor-api/blob/d06adf1581eb8e16242c8b6eabc7ba13ceaf0d54/src/Text/Impl/EditorOperations/EditorOperations.cs#L108
			// Without Undo History Roslyn Completion bails via:
			// https://github.com/dotnet/roslyn/blob/a107b43dcad83cf79addd47a9919590c7366d130/src/EditorFeatures/Core/Implementation/IntelliSense/Completion/Controller_Commit.cs#L66
			// See https://devdiv.visualstudio.com/DevDiv/_workitems/edit/669018 for details.
			var instantiateEditorOperations = EditorOperations;

			connectionManager = new ConnectionManager (this, factoryService.TextViewConnectionListeners, factoryService.GuardedOperations);

			SubscribeToEvents ();

			// Binding content type specific assets includes calling out to content-type
			// specific view creation listeners.  We need to do this as late as possible.
			this.BindContentTypeSpecificAssets (null, TextViewModel.DataModel.ContentType);

			//Subscribe now so that there is no chance that a layout could be forced by a text change.
			//_visualBuffer.ChangedLowPriority += OnVisualBufferChanged;
			//_visualBuffer.ContentTypeChanged += OnVisualBufferContentTypeChanged;

			// instantiate the MultiSelectionBroker
			var broker = MultiSelectionBroker;

			hasInitializeBeenCalled = true;
		}

		ITextCaret ITextView.Caret {
			get {
				return Caret;
			}
		}

		public ITextCaret TextCaret => Caret;

		public bool HasAggregateFocus {
			get {
				return hasAggregateFocus;
			}
		}

		public bool InLayout {
			get {
				return false;
			}
		}

		public bool IsClosed { get { return isClosed; } }

		public bool IsMouseOverViewOrAdornments {
			get {
				return textArea.IsMouseTrapped;
			}
		}

		public double MaxTextRightCoordinate {
			get {
				return Allocation.Width - TextViewMargin.XOffset;
			}
		}

		IEditorOptions ITextView.Options {
			get { return editorOptions; }
		}

		public PropertyCollection Properties {
			get {
				return properties;
			}
		}

		public ITrackingSpan ProvisionalTextHighlight {
			get {
				throw new NotImplementedException ();
			}

			set {
				throw new NotImplementedException ();
			}
		}

		public ITextSelection Selection {
			get {
				return selection;
			}
		}

		public ITextViewRoleSet Roles {
			get {
				return roles;
			}
		}

		/// <summary>
		/// Gets the text buffer whose text, this text editor renders
		/// </summary>
		public ITextBuffer TextBuffer {
			get {
				return textBuffer;
			}
		}

		public IBufferGraph BufferGraph {
			get {
				return bufferGraph;
			}
		}

		public ITextSnapshot TextSnapshot {
			get {
				// TODO: MONO: WpfTextView has a much more complex calculation of this
				return TextBuffer.CurrentSnapshot;
				//				return _textSnapshot;
			}
		}

		public ITextSnapshot VisualSnapshot {
			get {
				return TextBuffer.CurrentSnapshot;
				//                return _visualSnapshot;
			}
		}

		public ITextDataModel TextDataModel { get; private set; }
		public ITextViewModel TextViewModel { get; private set; }

		public ITextViewLineCollection TextViewLines { get => textArea.TextViewLines; }

		public double ViewportBottom {
			get {
				return TextViewMargin.RectInParent.Bottom;
			}
		}

		public double ViewportHeight {
			get {
				return TextViewMargin.RectInParent.Height;
			}
		}

		public double ViewportLeft {
			get {
				return 0;
			}
			set {
				throw new NotImplementedException ();
			}
		}

		public double ViewportRight {
			get {
				return TextViewMargin.RectInParent.Right + HAdjustment.Value;
			}
		}

		public double ViewportTop {
			get {
				return TextViewMargin.RectInParent.Top;// + VAdjustment.Value;
			}
		}

		public double ViewportWidth {
			get {
				return TextViewMargin.RectInParent.Width;
			}
		}

		public IViewScroller ViewScroller {
			get {
				return this;
			}
		}

		public event EventHandler Closed;
		public event EventHandler GotAggregateFocus;
		public event EventHandler LostAggregateFocus;
		public event EventHandler<TextViewLayoutChangedEventArgs> LayoutChanged;
#pragma warning disable CS0067
		public event EventHandler ViewportLeftChanged;
		public event EventHandler ViewportHeightChanged;
		public event EventHandler ViewportWidthChanged;
		public event EventHandler MaxTextRightCoordinateChanged;
#pragma warning restore CS0067

		public void Close ()
		{
			if (isClosed)
				throw new InvalidOperationException ();//Strings.TextViewClosed);
			isClosed = true;

			factoryService.GuardedOperations.RaiseEvent (this, this.Closed);

			if (hasAggregateFocus) {
				//Silently lose aggregate focus (to preserve Dev11 compatibility which did not raise a focus changed event when the view was closed).
				Debug.Assert (ViewWithAggregateFocus == this);
				ViewWithAggregateFocus = null;
				hasAggregateFocus = false;
			}

			UnsubscribeFromEvents ();

			connectionManager.Close ();

			TextViewModel.Dispose ();
			TextViewModel = null;
		}

		public void DisplayTextLineContainingBufferPosition (SnapshotPoint bufferPosition, double verticalDistance, ViewRelativePosition relativeTo)
		{
			this.textArea.ScrollTo (bufferPosition.Position);
		}

		public void DisplayTextLineContainingBufferPosition (SnapshotPoint bufferPosition, double verticalDistance, ViewRelativePosition relativeTo, double? viewportWidthOverride, double? viewportHeightOverride)
		{
			this.textArea.ScrollTo (bufferPosition.Position);
		}

		public SnapshotSpan GetTextElementSpan (SnapshotPoint point)
		{
			var line = this.GetTextViewLineContainingBufferPosition (point);
			return line.GetTextElementSpan (point);
		}

		public ITextViewLine GetTextViewLineContainingBufferPosition (SnapshotPoint bufferPosition)
		{
			return TextViewLines.GetTextViewLineContainingBufferPosition (bufferPosition);
		}

		public void QueueSpaceReservationStackRefresh ()
		{
			if (Interlocked.CompareExchange (ref queuedSpaceReservationStackRefresh, 1, 0) == 0) {
				MonoDevelop.Core.Runtime.RunInMainThread (new Action (delegate {
					Interlocked.Exchange (ref queuedSpaceReservationStackRefresh, 0);

					if (!isClosed) {
						_spaceReservationStack.Refresh ();
					}
				}));
			}
		}

		/// <remarks>
		/// If you add an event subscription to this method, be sure to add the corresponding unsubscription to
		/// UnsubscribeFromEvents()
		/// </remarks>
		private void SubscribeToEvents ()
		{
			if (IdeApp.IsInitialized)
				IdeApp.Workbench.ActiveDocumentChanged += Workbench_ActiveDocumentChanged;
		}

		void Workbench_ActiveDocumentChanged (object sender, EventArgs e)
		{
			QueueAggregateFocusCheck ();
		}

		private void UnsubscribeFromEvents ()
		{
			if (IdeApp.IsInitialized)
				IdeApp.Workbench.ActiveDocumentChanged -= Workbench_ActiveDocumentChanged;
		}

		private void BindContentTypeSpecificAssets (IContentType beforeContentType, IContentType afterContentType)
		{
			// Notify the Text view creation listeners
			var extensions = UIExtensionSelector.SelectMatchingExtensions (factoryService.TextViewCreationListeners, afterContentType, beforeContentType, roles);
			foreach (var extension in extensions) {
				string deferOptionName = extension.Metadata.OptionName;
				if (!string.IsNullOrEmpty (deferOptionName) && ((ITextView)this).Options.IsOptionDefined (deferOptionName, false)) {
					object value = ((ITextView)this).Options.GetOptionValue (deferOptionName);
					if (value is bool) {
						if (!(bool)value) {
							if (deferredTextViewListeners == null) {
								deferredTextViewListeners = new List<Lazy<ITextViewCreationListener, IDeferrableContentTypeAndTextViewRoleMetadata>> ();
							}
							deferredTextViewListeners.Add (extension);
							continue;
						}
					}
				}

				var instantiatedExtension = factoryService.GuardedOperations.InstantiateExtension (extension, extension);
				if (instantiatedExtension != null) {
					factoryService.GuardedOperations.CallExtensionPoint (instantiatedExtension,
						() => instantiatedExtension.TextViewCreated (this));
				}
			}
		}

		/// <summary>
		/// Handles the Classification changed event that comes from the Classifier aggregator
		/// </summary>
		void OnClassificationChanged (object sender, ClassificationChangedEventArgs e)
		{
			if (!isClosed) {
				// When classifications change, we just invalidate the lines. That invalidation will
				// create new lines based on the new classifications.

				// Map the classification change (from the edit buffer) to the visual buffer
				Span span = Span.FromBounds (
						TextViewModel.GetNearestPointInVisualSnapshot (e.ChangeSpan.Start, VisualSnapshot, PointTrackingMode.Negative),
						TextViewModel.GetNearestPointInVisualSnapshot (e.ChangeSpan.End, VisualSnapshot, PointTrackingMode.Positive));

				//Classifications changes invalidate only the characters contained in the span so a zero length change
				//will have no effect.
				if (span.Length > 0) {
					//IsLineInvalid will invalidate a line if it intersects the end. The result is that any call to InvalidateSpan() implicitly
					//invalidates any line that starts at the end of the invalidated span, which we do not want here. Reduce the length of the classification
					//change span one so -- if someone invalidated an entire line including the line break -- the next line will not be invalidated.
					span = new Span (span.Start, span.Length - 1);

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

		internal void QueueAggregateFocusCheck (bool checkForFocus = true)
		{
#if DEBUG
			if (SettingAggregateFocus) {
				Debug.Fail ("WpfTextView.SettingAggregateFocus");
			}
#endif

			if (!isClosed) {
				bool newHasAggregateFocus = ((IdeApp.Workbench.ActiveDocument?.Editor?.Implementation as MonoDevelop.SourceEditor.SourceEditorView)?.TextEditor == this);
				if (newHasAggregateFocus != hasAggregateFocus) {
					hasAggregateFocus = newHasAggregateFocus;

					if (hasAggregateFocus) {
						//Got focus so make sure that the view that had focus (which wasn't us since we didn't have focus before) raises its
						//lost focus event before we raise our got focus event. This will potentially do bad things if someone changes focus 
						//if the lost aggregate focus handler.
						Debug.Assert (ViewWithAggregateFocus != this);
						if (ViewWithAggregateFocus != null) {
							ViewWithAggregateFocus.QueueAggregateFocusCheck (checkForFocus: false);
						}
						Debug.Assert (ViewWithAggregateFocus == null);
						ViewWithAggregateFocus = this;
					} else {
						//Lost focus (which means we were the view with focus).
						Debug.Assert (ViewWithAggregateFocus == this);
						ViewWithAggregateFocus = null;
					}

					EventHandler handler = hasAggregateFocus ? this.GotAggregateFocus : this.LostAggregateFocus;

#if DEBUG
					try {
						SettingAggregateFocus = true;
#endif
						factoryService.GuardedOperations.RaiseEvent (this, handler);
#if DEBUG
					} finally {
						SettingAggregateFocus = false;
					}
#endif
				}
			}
		}

		public IGuardedOperations GuardedOperations;
		internal SpaceReservationStack _spaceReservationStack;

		public ISpaceReservationManager GetSpaceReservationManager (string name)
		{
			if (name == null)
				throw new ArgumentNullException ("name");

			return _spaceReservationStack.GetOrCreateManager (name);
		}

		internal TextEditorFactoryService ComponentContext {
			get { return factoryService; }
		}

		public bool InOuterLayout => false;

		private IMultiSelectionBroker multiSelectionBroker;
		public IMultiSelectionBroker MultiSelectionBroker {
			get {
				if (multiSelectionBroker == null) {
					multiSelectionBroker = factoryService.MultiSelectionBrokerFactory.CreateBroker (this);
					multiSelectionBroker.MultiSelectionSessionChanged += OnMultiSelectionSessionChanged;
				}

				return multiSelectionBroker;
			}
		}

		private void OnMultiSelectionSessionChanged (object sender, EventArgs e)
		{
			// The MultiSelectionBroker API has been updated, but currently in VSMac we still have a separate Caret concept.
			// We need to manually synchronize our caret with what MultiSelectionBroker thinks the caret is.
			// The other direction happens when we move our caret.
			if (TextCaret.Position.VirtualBufferPosition != MultiSelectionBroker.PrimarySelection.InsertionPoint) {
				TextCaret.MoveTo (MultiSelectionBroker.PrimarySelection.InsertionPoint);
			}
		}
	}
}
