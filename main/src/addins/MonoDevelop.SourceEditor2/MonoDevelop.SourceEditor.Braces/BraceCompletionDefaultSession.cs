//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace MonoDevelop.SourceEditor.Braces
{
	using Microsoft.VisualStudio.Text;
	using Microsoft.VisualStudio.Text.BraceCompletion;
	using Microsoft.VisualStudio.Text.Editor;
	using Microsoft.VisualStudio.Text.Operations;
	using System.Diagnostics;

	/// <summary>
	/// BraceCompletionDefaultSession is a language neutral brace completion session
	/// capable of handling the default behaviors. Language specific behaviors 
	/// and formatting are handled by the optional IBraceCompletionContext.
	/// </summary>
	internal class BraceCompletionDefaultSession : IBraceCompletionSession
	{
		#region Private Members

		private char _openingBrace;
		private char _closingBrace;
		private ITrackingPoint _openingPoint;
		private ITrackingPoint _closingPoint;
		private ITextBuffer _subjectBuffer;
		private ITextView _textView;
		private IBraceCompletionContext _context;
		private ITextBufferUndoManagerProvider _undoManager;
		private ITextUndoHistory _undoHistory;
		private IEditorOperations _editorOperations;

		#endregion

		#region Constructors

		/// <summary>
		/// Default session with no context
		/// </summary>
		public BraceCompletionDefaultSession (ITextView textView, ITextBuffer subjectBuffer,
				SnapshotPoint openingPoint, char openingBrace, char closingBrace,
				ITextBufferUndoManagerProvider undoManager, IEditorOperationsFactoryService editorOperationsFactoryService)
			: this (textView, subjectBuffer, openingPoint, openingBrace, closingBrace, undoManager, editorOperationsFactoryService, context: null)
		{

		}

		/// <summary>
		/// Default session with a language specific context
		/// </summary>
		public BraceCompletionDefaultSession (ITextView textView, ITextBuffer subjectBuffer,
			SnapshotPoint openingPoint, char openingBrace, char closingBrace, ITextBufferUndoManagerProvider undoManager,
			IEditorOperationsFactoryService editorOperationsFactoryService, IBraceCompletionContext context)
		{
			_textView = textView;
			_subjectBuffer = subjectBuffer;
			_openingBrace = openingBrace;
			_closingBrace = closingBrace;
			_closingPoint = SubjectBuffer.CurrentSnapshot.CreateTrackingPoint (openingPoint.Position, PointTrackingMode.Positive);
			_context = context;
			_undoManager = undoManager;
			_undoHistory = undoManager.GetTextBufferUndoManager (_textView.TextBuffer).TextBufferUndoHistory;
			_editorOperations = editorOperationsFactoryService.GetEditorOperations (_textView);
		}

		#endregion

		#region IBraceCompletionSession Methods

		public void Start ()
		{
			// this is where the caret should go after the change
			SnapshotPoint pos = _textView.Caret.Position.BufferPosition;
			ITrackingPoint beforeTrackingPoint = pos.Snapshot.CreateTrackingPoint (pos.Position, PointTrackingMode.Negative);

			ITextSnapshot snapshot = _subjectBuffer.CurrentSnapshot;
			SnapshotPoint closingSnapshotPoint = _closingPoint.GetPoint (snapshot);

			if (closingSnapshotPoint.Position < 1) {
				Debug.Fail ("The closing point was not found at the expected position.");
				EndSession ();
				return;
			}

			SnapshotPoint openingSnapshotPoint = closingSnapshotPoint.Subtract (1);

			if (openingSnapshotPoint.GetChar () != OpeningBrace) {
				Debug.Fail ("The opening brace was not found at the expected position.");
				EndSession ();
				return;
			}

			_openingPoint = SubjectBuffer.CurrentSnapshot.CreateTrackingPoint (openingSnapshotPoint, PointTrackingMode.Positive);

			using (ITextUndoTransaction undo = CreateUndoTransaction ()) {
				// insert the closing brace
				using (ITextEdit edit = _subjectBuffer.CreateEdit ()) {
					edit.Insert (closingSnapshotPoint, _closingBrace.ToString ());

					if (edit.HasFailedChanges) {
						Debug.Fail ("Unable to insert closing brace");

						// exit without setting the closing point which will take us off the stack
						edit.Cancel ();
						undo.Cancel ();
						return;
					} else {
						snapshot = edit.Apply ();
					}
				}

				SnapshotPoint beforePoint = beforeTrackingPoint.GetPoint (_textView.TextSnapshot);

				// switch from positive to negative tracking so it stays against the closing brace
				_closingPoint = SubjectBuffer.CurrentSnapshot.CreateTrackingPoint (_closingPoint.GetPoint (snapshot), PointTrackingMode.Negative);

				Debug.Assert (_closingPoint.GetPoint (snapshot).Position > 0 && (new SnapshotSpan (_closingPoint.GetPoint (snapshot).Subtract (1), 1))
							.GetText ().Equals (_closingBrace.ToString ()), "The closing point does not match the closing brace character");

				// move the caret back between the braces
				_textView.Caret.MoveTo (beforePoint);

				if (_context != null) {
					// allow the context to do extra formatting
					_context.Start (this);
				}

				undo.Complete ();
			}
		}

		public void PreBackspace (out bool handledCommand)
		{
			handledCommand = false;

			SnapshotPoint? caretPos = CaretPosition;
			ITextSnapshot snapshot = SubjectBuffer.CurrentSnapshot;

			if (caretPos.HasValue && caretPos.Value.Position > 0 && (caretPos.Value.Position - 1) == _openingPoint.GetPoint (snapshot).Position
				&& !HasForwardTyping) {
				using (ITextUndoTransaction undo = CreateUndoTransaction ()) {
					using (ITextEdit edit = SubjectBuffer.CreateEdit ()) {
						SnapshotSpan span = new SnapshotSpan (_openingPoint.GetPoint (snapshot), _closingPoint.GetPoint (snapshot));

						edit.Delete (span);

						if (edit.HasFailedChanges) {
							edit.Cancel ();
							undo.Cancel ();
							Debug.Fail ("Unable to clear braces");
							// just let this backspace proceed normally
						} else {
							// handle the command so the backspace does 
							// not go through since we've already cleared the braces
							handledCommand = true;
							edit.Apply ();
							undo.Complete ();
							EndSession ();
						}
					}
				}
			}
		}

		public void PostBackspace ()
		{

		}

		public void PreOverType (out bool handledCommand)
		{
			handledCommand = false;

			// AllowOverType may make changes to the buffer such as for completing intellisense
			if (!HasForwardTyping && (_context == null || _context.AllowOverType (this))) {
				SnapshotPoint? caretPos = CaretPosition;
				SnapshotPoint closingSnapshotPoint = _closingPoint.GetPoint (SubjectBuffer.CurrentSnapshot);

				Debug.Assert (caretPos.HasValue && caretPos.Value.Position < closingSnapshotPoint.Position);

				// ensure that we are within the session before clearing
				if (caretPos.HasValue && caretPos.Value.Position < closingSnapshotPoint.Position && closingSnapshotPoint.Position > 0) {
					using (ITextUndoTransaction undo = CreateUndoTransaction ()) {
						_editorOperations.AddBeforeTextBufferChangePrimitive ();

						SnapshotSpan span = new SnapshotSpan (caretPos.Value, closingSnapshotPoint.Subtract (1));

						using (ITextEdit edit = _subjectBuffer.CreateEdit ()) {
							edit.Delete (span);

							if (edit.HasFailedChanges) {
								Debug.Fail ("Unable to clear closing brace");
								edit.Cancel ();
								undo.Cancel ();
							} else {
								handledCommand = true;

								edit.Apply ();

								MoveCaretToClosingPoint ();

								_editorOperations.AddAfterTextBufferChangePrimitive ();

								undo.Complete ();
							}
						}
					}
				}
			}
		}

		public void PostOverType ()
		{

		}

		public void PreTab (out bool handledCommand)
		{
			handledCommand = false;

			if (!HasForwardTyping) {
				handledCommand = true;

				using (ITextUndoTransaction undo = CreateUndoTransaction ()) {
					_editorOperations.AddBeforeTextBufferChangePrimitive ();

					MoveCaretToClosingPoint ();

					_editorOperations.AddAfterTextBufferChangePrimitive ();

					undo.Complete ();
				}
			}
		}

		public void PreReturn (out bool handledCommand)
		{
			handledCommand = false;
		}

		public void PostReturn ()
		{
			if (_context != null && CaretPosition.HasValue) {
				SnapshotPoint closingSnapshotPoint = _closingPoint.GetPoint (SubjectBuffer.CurrentSnapshot);

				if (closingSnapshotPoint.Position > 0 && HasNoForwardTyping (CaretPosition.Value, closingSnapshotPoint.Subtract (1))) {
					_context.OnReturn (this);
				}
			}
		}

		public void Finish ()
		{
			if (_context != null) {
				_context.Finish (this);
			}
		}

		#endregion

		#region Unused IBraceCompletionSession Methods

		public void PostTab () { }

		public void PreDelete (out bool handledCommand)
		{
			handledCommand = false;
		}

		public void PostDelete () { }

		#endregion

		#region IBraceCompletionSession Properties

		public ITextBuffer SubjectBuffer {
			get { return _subjectBuffer; }
		}

		public ITextView TextView {
			get { return _textView; }
		}

		public char ClosingBrace {
			get { return _closingBrace; }
		}

		public ITrackingPoint ClosingPoint {
			get { return _closingPoint; }
		}

		public char OpeningBrace {
			get { return _openingBrace; }
		}

		public ITrackingPoint OpeningPoint {
			get { return _openingPoint; }
		}

		#endregion

		#region Private Helpers

		private void EndSession ()
		{
			// set the points to null to get off the stack
			// the stack will determine that the current point
			// is not contained within the session if either are null
			_openingPoint = null;
			_closingPoint = null;
		}

		private SnapshotPoint? CaretPosition {
			get {
				return GetCaretPoint (SubjectBuffer);
			}
		}

		// get the caret position within the given buffer
		private SnapshotPoint? GetCaretPoint (ITextBuffer buffer)
		{
			return _textView.Caret.Position.Point.GetPoint (buffer, PositionAffinity.Predecessor);
		}

		// check if there any typing between the caret the closing point
		private bool HasForwardTyping {
			get {
				SnapshotPoint closingSnapshotPoint = _closingPoint.GetPoint (SubjectBuffer.CurrentSnapshot);

				if (closingSnapshotPoint.Position > 0) {
					SnapshotPoint? caretPos = CaretPosition;

					if (caretPos.HasValue && !HasNoForwardTyping (caretPos.Value, closingSnapshotPoint.Subtract (1))) {
						return true;
					}
				}

				return false;
			}
		}

		// verify that there is only whitespace between the two given points
		private static bool HasNoForwardTyping (SnapshotPoint caretPoint, SnapshotPoint endPoint)
		{
			Debug.Assert (caretPoint.Snapshot == endPoint.Snapshot, "snapshots do not match");

			if (caretPoint.Snapshot == endPoint.Snapshot) {
				if (caretPoint == endPoint) {
					return true;
				}

				if (caretPoint.Position < endPoint.Position) {
					SnapshotSpan span = new SnapshotSpan (caretPoint, endPoint);

					return string.IsNullOrWhiteSpace (span.GetText ());
				}
			}

			return false;
		}

		private ITextUndoTransaction CreateUndoTransaction ()
		{
			// TODO: VS4MAC
			return _undoHistory.CreateTransaction ("Brace completion undo");

			//return _undoHistory.CreateTransaction(Strings.BraceCompletionUndo);
		}


		private void MoveCaretToClosingPoint ()
		{
			SnapshotPoint closingSnapshotPoint = _closingPoint.GetPoint (SubjectBuffer.CurrentSnapshot);

			// find the position just after the closing brace in the view's text buffer
			SnapshotPoint? afterBrace = _textView.BufferGraph.MapUpToBuffer (closingSnapshotPoint,
				PointTrackingMode.Negative, PositionAffinity.Predecessor, _textView.TextBuffer);

			Debug.Assert (afterBrace.HasValue, "Unable to move caret to closing point");

			if (afterBrace.HasValue) {
				_textView.Caret.MoveTo (afterBrace.Value);
			}
		}

		#endregion
	}
}
