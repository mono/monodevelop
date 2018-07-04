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
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;

	/// <summary>
	/// Represents the stack of active brace completion sessions.
	/// The stack handles removing sessions no longer in focus as
	/// well as marking the inner most closing brace with the 
	/// adornment.
	/// </summary>
	internal class BraceCompletionStack : IBraceCompletionStack
	{
		#region Private Members
		private Stack<IBraceCompletionSession> _stack;

		private ITextView _textView;
		private ITextBuffer _currentSubjectBuffer;

		private IBraceCompletionAdornmentServiceFactory _adornmentServiceFactory;
		private IBraceCompletionAdornmentService _adornmentService;
		private GuardedOperations _guardedOperations;
		#endregion

		#region Constructors
		public BraceCompletionStack (ITextView textView, IBraceCompletionAdornmentServiceFactory adornmentFactory, GuardedOperations guardedOperations)
		{
			_adornmentServiceFactory = adornmentFactory;
			_stack = new Stack<IBraceCompletionSession> ();

			_textView = textView;
			_guardedOperations = guardedOperations;

			RegisterEvents ();
		}
		#endregion

		#region IBraceCompletionStack
		public IBraceCompletionSession TopSession {
			get {
				return (_stack.Count > 0 ? _stack.Peek () : null);
			}
		}

		public void PushSession (IBraceCompletionSession session)
		{
			ITextView view = null;
			ITextBuffer buffer = null;

			_guardedOperations.CallExtensionPoint (() => {
				view = session.TextView;
				buffer = session.SubjectBuffer;
			});

			if (view != null && buffer != null) {
				SetCurrentBuffer (buffer);
				bool validStart = false;

				// start the session to add the closing brace
				_guardedOperations.CallExtensionPoint (() => {
					session.Start ();

					// verify the session is valid before going on.
					// some sessions may want to leave the stack at this point
					validStart = (session.OpeningPoint != null && session.ClosingPoint != null);
				});

				if (validStart) {
					// highlight the brace
					ITrackingPoint closingPoint = null;
					_guardedOperations.CallExtensionPoint (() => {
						closingPoint = session.ClosingPoint;
					});

					HighlightSpan (closingPoint);

					// put it on the stack for tracking
					_stack.Push (session);
				}
			}
		}

		public ReadOnlyObservableCollection<IBraceCompletionSession> Sessions {
			get {
				return new ReadOnlyObservableCollection<IBraceCompletionSession> (new ObservableCollection<IBraceCompletionSession> (_stack));
			}
		}

		public void RemoveOutOfRangeSessions (SnapshotPoint point)
		{
			bool updateHighlightSpan = false;

			while (_stack.Count > 0 && !Contains (TopSession, point)) {
				updateHighlightSpan = true;

				// remove the session and call Finish
				PopSession ();
			}

			if (updateHighlightSpan) {
				HighlightSpan (TopSession != null ? TopSession.ClosingPoint : null);
			}
		}

		public void Clear ()
		{
			while (_stack.Count > 0) {
				PopSession ();
			}

			SetCurrentBuffer (null);
			HighlightSpan (null);
		}

		#endregion

		#region Events

		private void RegisterEvents ()
		{
			if (_adornmentServiceFactory != null) {
				_adornmentService = _adornmentServiceFactory.GetOrCreateService (_textView);
			}

			_textView.Caret.PositionChanged += Caret_PositionChanged;
			_textView.Closed += TextView_Closed;
		}

		private void UnregisterEvents ()
		{
			_textView.Caret.PositionChanged -= Caret_PositionChanged;
			_textView.Closed -= TextView_Closed;

			// unhook subject buffer
			SetCurrentBuffer (null);

			_textView = null;
		}

		public void ConnectSubjectBuffer (ITextBuffer subjectBuffer)
		{
			subjectBuffer.PostChanged += SubjectBuffer_PostChanged;
		}

		public void DisconnectSubjectBuffer (ITextBuffer subjectBuffer)
		{
			subjectBuffer.PostChanged -= SubjectBuffer_PostChanged;
		}

		private void TextView_Closed (object sender, EventArgs e)
		{
			UnregisterEvents ();
		}

		// Remove any sessions that no longer contain the caret
		private void Caret_PositionChanged (object sender, CaretPositionChangedEventArgs e)
		{
			if (_stack.Count > 0) {
				// use the new position if possible, otherwise map to the subject buffer
				if (_currentSubjectBuffer != null && e.TextView.TextBuffer != _currentSubjectBuffer) {
					SnapshotPoint? newPosition = e.NewPosition.Point.GetPoint (_currentSubjectBuffer, PositionAffinity.Successor);

					if (newPosition.HasValue) {
						RemoveOutOfRangeSessions (newPosition.Value);
					} else {
						// caret is no longer in the subject buffer. probably
						// moved to different buffer in the same view.
						// clear all tracks
						_stack.Clear ();
					}
				} else {
					RemoveOutOfRangeSessions (e.NewPosition.BufferPosition);
				}
			}
		}

		// Verify that the top most session is still valid after a buffer change
		// This handles any issues that could result from text being replaced
		// or multi view scenarios where the caret is not being moved in the 
		// current view.
		private void SubjectBuffer_PostChanged (object sender, EventArgs e)
		{
			bool updateHighlightSpan = false;

			// only check the top most session
			// outer sessions could become invalid while the inner most
			// sessions stay valid, but there is no reason to check them every time
			while (_stack.Count > 0 && !IsSessionValid (TopSession)) {
				updateHighlightSpan = true;
				_stack.Pop ().Finish ();
			}

			if (updateHighlightSpan) {
				ITrackingPoint closingPoint = null;

				if (TopSession != null) {
					_guardedOperations.CallExtensionPoint (() => closingPoint = TopSession.ClosingPoint);
				}

				HighlightSpan (closingPoint);
			}
		}

		private bool IsSessionValid (IBraceCompletionSession session)
		{
			bool isValid = false;

			_guardedOperations.CallExtensionPoint (() => {
				if (session.ClosingPoint != null && session.OpeningPoint != null && session.SubjectBuffer != null) {
					ITextSnapshot snapshot = session.SubjectBuffer.CurrentSnapshot;
					SnapshotPoint closingSnapshotPoint = session.ClosingPoint.GetPoint (snapshot);
					SnapshotPoint openingSnapshotPoint = session.OpeningPoint.GetPoint (snapshot);

					// Verify that the closing and opening points still match the expected braces
					isValid = closingSnapshotPoint.Position > 1
						&& openingSnapshotPoint.Position <= (closingSnapshotPoint.Position - 2)
						&& openingSnapshotPoint.GetChar () == session.OpeningBrace
						&& closingSnapshotPoint.Subtract (1).GetChar () == session.ClosingBrace;
				}
			});

			return isValid;
		}

		#endregion

		#region Private Helpers

		private void SetCurrentBuffer (ITextBuffer buffer)
		{
			// Connect to the subject buffer of the session
			if (_currentSubjectBuffer != buffer) {
				if (_currentSubjectBuffer != null) {
					DisconnectSubjectBuffer (_currentSubjectBuffer);
				}

				_currentSubjectBuffer = buffer;

				if (_currentSubjectBuffer != null) {
					ConnectSubjectBuffer (_currentSubjectBuffer);
				}
			}
		}

		private void PopSession ()
		{
			IBraceCompletionSession session = _stack.Pop ();
			ITextBuffer nextSubjectBuffer = null;

			_guardedOperations.CallExtensionPoint (() => {
				// call finish to allow the session to do any cleanup
				session.Finish ();

				if (TopSession != null) {
					nextSubjectBuffer = TopSession.SubjectBuffer;
				}
			});

			SetCurrentBuffer (nextSubjectBuffer);
		}

		private void HighlightSpan (ITrackingPoint point)
		{
			if (_adornmentService != null) {
				_adornmentService.Point = point;
			}
		}

		private bool Contains (IBraceCompletionSession session, SnapshotPoint point)
		{
			bool contains = false;

			_guardedOperations.CallExtensionPoint (() => {
				// remove any sessions with nulls, if they decide they need to get off the stack
				// they can do it this way.
				if (session.OpeningPoint != null && session.ClosingPoint != null
					&& session.OpeningPoint.TextBuffer == session.ClosingPoint.TextBuffer
					&& point.Snapshot.TextBuffer == session.OpeningPoint.TextBuffer) {
					ITextSnapshot snapshot = point.Snapshot;

					contains = session.OpeningPoint.GetPosition (snapshot) < point.Position
						&& session.ClosingPoint.GetPosition (snapshot) > point.Position;
				}
			});

			return contains;
		}
		#endregion
	}
}
