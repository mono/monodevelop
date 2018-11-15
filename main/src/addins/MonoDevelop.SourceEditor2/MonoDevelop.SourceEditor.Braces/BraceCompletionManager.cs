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
	using Microsoft.VisualStudio.Text.Utilities;
	using MonoDevelop.Ide.Editor;
	using System;
	using System.Diagnostics;

	/// <summary>
	/// A per view manager for brace completion. This is called by the command filter in the 
	/// editor pkg.
	/// </summary>
	internal sealed class BraceCompletionManager : IBraceCompletionManager
	{
		#region Private Members

		private readonly IBraceCompletionStack _stack;
		private readonly IBraceCompletionAggregatorFactory _sessionFactory;
		private readonly IBraceCompletionAggregator _sessionAggregator;
		private readonly ITextView _textView;
		private readonly GuardedOperations _guardedOperations;

		private IBraceCompletionSession _postSession;
		private IBraceCompletionSession _waitingSession;
		private SnapshotPoint? _waitingSessionOpeningPoint;

		#endregion

		#region Constructors

		internal BraceCompletionManager (ITextView textView, IBraceCompletionStack stack, IBraceCompletionAggregatorFactory sessionFactory, GuardedOperations guardedOperations)
		{
			_textView = textView;
			_stack = stack;
			_sessionFactory = sessionFactory;
			_guardedOperations = guardedOperations;
			_sessionAggregator = sessionFactory.CreateAggregator ();

			GetOptions ();
			RegisterEvents ();
		}

		#endregion

		#region IBraceCompletionManager

		public bool Enabled => DefaultSourceEditorOptions.Instance.AutoInsertMatchingBracket;

		public string ClosingBraces {
			get {
				return _sessionAggregator.ClosingBraces;
			}
		}

		public bool HasActiveSessions {
			get { return _stack.TopSession != null; }
		}

		public string OpeningBraces {
			get {
				return _sessionAggregator.OpeningBraces;
			}
		}

		public void PreTypeChar (char character, out bool handledCommand)
		{
			bool handled = false;

			bool hasSelection = HasSelection;

			Debug.Assert (_postSession == null, "_postSession should have been cleared");

			// give the existing session a chance to handle the character first

			if (_stack.TopSession != null && !hasSelection) {
				IBraceCompletionSession session = _stack.TopSession;

				// check for an existing session first
				_guardedOperations.CallExtensionPoint (() => {
					if (session.ClosingBrace.Equals (character) && IsCaretOnBuffer (session.SubjectBuffer)) {
						session.PreOverType (out handled);

						if (!handled) {
							_postSession = session;
						}
					}
				});
			}

			handledCommand = handled;

			// otherwise check if this starts a new session
			if (_postSession == null && !handled && Enabled && !hasSelection
			    && _sessionAggregator.OpeningBraces.IndexOf (character) > -1 && !HasForwardTypingOnLine) {
				SnapshotPoint? openingPoint = _textView.Caret.Position.Point.GetInsertionPoint ((b => _sessionAggregator.IsSupportedContentType (b.ContentType, character)));
				if (openingPoint.HasValue) {
					IBraceCompletionSession session = null;
					if (_sessionAggregator.TryCreateSession (_textView, openingPoint.Value, character, out session)) {
						// add the session after the current keystroke completes
						_waitingSession = session;
						_waitingSessionOpeningPoint = openingPoint;
					}
				}
			}
		}

		public void PostTypeChar (char character)
		{
			// check for any waiting sessions
			if (_waitingSession != null) {
				// Verify the session is still valid before starting it
				// and inserting the closing brace.
				if (ValidateStart (_waitingSessionOpeningPoint, character)) {
					_stack.PushSession (_waitingSession);
				}

				_waitingSession = null;
				_waitingSessionOpeningPoint = null;
			} else if (_postSession != null) {
				_guardedOperations.CallExtensionPoint (() => {
					if (_postSession.ClosingBrace.Equals (character)) {
						_postSession.PostOverType ();
					}
				});

				_postSession = null;
			}
		}

		public void PreTab (out bool handledCommand)
		{
			bool handled = false;

			Debug.Assert (_postSession == null, "_postSession should have been cleared");

			// tab should only be handled by brace completion if there is no selection and both braces on the same line still
			if (_stack.TopSession != null && !HasSelection) {
				IBraceCompletionSession session = _stack.TopSession;

				_guardedOperations.CallExtensionPoint (() => {
					if (IsSingleLine (session.OpeningPoint, session.ClosingPoint)) {
						session.PreTab (out handled);

						if (!handled) {
							_postSession = session;
						}
					}
				});
			}

			handledCommand = handled;
		}

		public void PostTab ()
		{
			if (_postSession != null) {
				_guardedOperations.CallExtensionPoint (() => {
					_postSession.PostTab ();
				});

				_postSession = null;
			}
		}

		public void PreBackspace (out bool handledCommand)
		{
			bool handled = false;

			Debug.Assert (_postSession == null, "_postSession should have been cleared");

			if (_stack.TopSession != null && !HasSelection) {
				IBraceCompletionSession session = _stack.TopSession;

				_guardedOperations.CallExtensionPoint (() => {
					if (session.OpeningPoint != null && session.ClosingPoint != null) {
						session.PreBackspace (out handled);

						if (!handled) {
							_postSession = session;
						}
					}
				});
			}

			handledCommand = handled;
		}

		public void PostBackspace ()
		{
			if (_postSession != null) {
				_guardedOperations.CallExtensionPoint (() => {
					_postSession.PostBackspace ();
				});

				_postSession = null;
			}
		}

		public void PreDelete (out bool handledCommand)
		{
			bool handled = false;

			Debug.Assert (_postSession == null, "_postSession should have been cleared");

			if (_stack.TopSession != null && !HasSelection) {
				IBraceCompletionSession session = _stack.TopSession;

				_guardedOperations.CallExtensionPoint (() => {
					if (session.OpeningPoint != null && session.ClosingPoint != null) {
						session.PreDelete (out handled);

						if (!handled) {
							_postSession = session;
						}
					}
				});
			}

			handledCommand = handled;
		}

		public void PostDelete ()
		{
			if (_postSession != null) {
				_guardedOperations.CallExtensionPoint (() => {
					_postSession.PostDelete ();
				});

				_postSession = null;
			}
		}

		public void PreReturn (out bool handledCommand)
		{
			bool handled = false;

			Debug.Assert (_postSession == null, "_postSession should have been cleared");

			if (_stack.TopSession != null && !HasSelection) {
				IBraceCompletionSession session = _stack.TopSession;

				_guardedOperations.CallExtensionPoint (() => {
					if (IsSingleLine (session.OpeningPoint, session.ClosingPoint)) {
						session.PreReturn (out handled);

						if (!handled) {
							_postSession = session;
						}
					}
				});
			}

			handledCommand = handled;
		}

		public void PostReturn ()
		{
			if (_postSession != null) {
				_guardedOperations.CallExtensionPoint (() => {
					_postSession.PostReturn ();
				});

				_postSession = null;
			}
		}

		#endregion

		#region Events/Options

		private void RegisterEvents ()
		{
			_textView.Closed += textView_Closed;
			_textView.Options.OptionChanged += Options_OptionChanged;
			DefaultSourceEditorOptions.Instance.Changed += EditorOptions_OptionChanged;
		}

		private void textView_Closed (object sender, EventArgs e)
		{
			UnregisterEvents ();
		}

		private void UnregisterEvents ()
		{
			_textView.Closed -= textView_Closed;
			_textView.Options.OptionChanged -= Options_OptionChanged;
			DefaultSourceEditorOptions.Instance.Changed -= EditorOptions_OptionChanged;
		}

		private void EditorOptions_OptionChanged (object sender, EventArgs args)
		{
			GetOptions ();
		}

		private void Options_OptionChanged (object sender, EditorOptionChangedEventArgs e)
		{
			GetOptions ();
		}

		private void GetOptions ()
		{
			// if completion was disabled, clear out the stack
			if (!Enabled) {
				_waitingSession = null;
				_postSession = null;
				_stack.Clear ();
			}
		}

		#endregion

		#region Private Helpers

		private bool IsCaretOnBuffer (ITextBuffer buffer)
		{
			return _textView.Caret.Position.Point.GetPoint (buffer, PositionAffinity.Successor).HasValue;
		}

		private bool HasSelection {
			get {
				return !_textView.Selection.IsEmpty;
			}
		}

		// Determine if the line has text on it apart from any active braces in this view
		private bool HasForwardTypingOnLine {
			get {
				SnapshotPoint start = _textView.Caret.Position.BufferPosition;

				// TODO: MONODEVELOP: The original line is commented out below because ContainingTextViewLine
				//   occasionally returns null in MD. Revert this back to the original once that is addressed.
				SnapshotPoint end = start.Snapshot.GetLineFromPosition (start).End;
				//SnapshotPoint end = _textView.Caret.ContainingTextViewLine.End;

				if (start != end) {
					// if we have an active session use that brace as the end
					if (_stack.TopSession != null) {
						ITrackingPoint closingPoint = null;
						IBraceCompletionSession session = _stack.TopSession;

						_guardedOperations.CallExtensionPoint (() => {
							// only set these if they are on the same buffer
							if (session.OpeningPoint != null && session.ClosingPoint != null
								&& session.OpeningPoint.TextBuffer == session.ClosingPoint.TextBuffer) {
								closingPoint = session.ClosingPoint;
							}
						});

						if (closingPoint != null) {
							SnapshotPoint? innerBraceEnd = closingPoint.GetPoint (closingPoint.TextBuffer.CurrentSnapshot);

							if (innerBraceEnd.HasValue && _stack.TopSession.SubjectBuffer != _textView.TextBuffer) {
								// map the closing point to the text buffer for the check
								innerBraceEnd = _textView.BufferGraph.MapUpToBuffer (innerBraceEnd.Value,
									closingPoint.TrackingMode, PositionAffinity.Predecessor, _textView.TextBuffer);
							}

							if (innerBraceEnd.HasValue && innerBraceEnd.Value.Position <= end && innerBraceEnd.Value.Position > 0) {
								end = innerBraceEnd.Value.Subtract (1);
							}
						}
					}

					// check if we aren't the last closing brace
					if (start == end) {
						return false;
					} else if (start < end) {
						SnapshotSpan span = new SnapshotSpan (start, end);

						if (!span.IsEmpty) {
							return !string.IsNullOrWhiteSpace (span.GetText ());
						}
					} else {
						Debug.Fail ("unable to check for forward typing");
						// shouldn't happen, but if it does count it as forward typing to avoid
						// further action
						return true;
					}
				}

				return false;
			}
		}

		private bool IsSingleLine (ITrackingPoint openingPoint, ITrackingPoint closingPoint)
		{
			if (openingPoint != null && closingPoint != null) {
				ITextSnapshot snapshot = openingPoint.TextBuffer.CurrentSnapshot;

				return openingPoint.GetPoint (snapshot).GetContainingLine ().End.Position >= closingPoint.GetPoint (snapshot).Position;
			}

			return false;
		}

		// Verify that we are about to insert the closing brace right after the opening one
		private bool ValidateStart (SnapshotPoint? openingPoint, char openingChar)
		{
			if (openingPoint.HasValue) {
				ITextBuffer subjectBuffer = openingPoint.Value.Snapshot.TextBuffer;

				// Get the position based on the predecessor which should be the opening brace
				SnapshotPoint? caretPosition = _textView.Caret.Position.Point.GetPoint (subjectBuffer, PositionAffinity.Predecessor);

				// verify that the opening brace is right behind the caret
				if (caretPosition.HasValue && caretPosition.Value.Position > 0) {
					SnapshotPoint openingBrace = caretPosition.Value.Subtract (1);
					return (openingBrace.GetChar () == openingChar);
				}
			}

			return false;
		}

		#endregion
	}
}
