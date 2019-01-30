//
// Copyright (c) Microsoft Corp. (https://www.microsoft.com)
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
using System.Windows.Input;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.Text.Operations;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui;
using MonoDevelop.TextEditor;

namespace MonoDevelop.Ide.Text
{
	internal sealed class DefaultKeyProcessor : KeyProcessor
	{
		private readonly IWpfTextView _textView;
		private readonly IEditorOperations _editorOperations;
		private readonly ITextUndoHistoryRegistry _undoHistoryRegistry;
		private readonly Microsoft.VisualStudio.Text.Editor.Commanding.CommandDispatcher commandDispatcher;

		internal DefaultKeyProcessor (
			IWpfTextView textView,
			IEditorOperations editorOperations,
			ITextUndoHistoryRegistry undoHistoryRegistry,
			Microsoft.VisualStudio.Text.Editor.Commanding.IEditorCommandHandlerService editorCommandHandlerService)
		{
			this._textView = textView;
			this._editorOperations = editorOperations;
			this._undoHistoryRegistry = undoHistoryRegistry;
			this.commandDispatcher = new Microsoft.VisualStudio.Text.Editor.Commanding.CommandDispatcher (editorCommandHandlerService, editorOperations);
		}

		ICommandHandler commandHandler;
		ICommandHandler CommandHandler {
			get {
				return commandHandler ?? (commandHandler = _textView.Properties.GetProperty<ViewContent> (typeof (ViewContent)) as ICommandHandler);
			}
		}

		public override void KeyDown (KeyEventArgs args)
		{
			if (args.Handled) {
				return;
			}

			args.Handled = true;
			switch (args.KeyboardDevice.Modifiers) {
			case ModifierKeys.None:
				HandleKey (args);
				break;
			case ModifierKeys.Control:
				HandleControlKey (args);
				break;
			case ModifierKeys.Alt:
				HandleAltKey (args);
				break;
			case ModifierKeys.Shift | ModifierKeys.Alt:
				HandleAltShiftKey (args);
				break;
			case ModifierKeys.Control | ModifierKeys.Shift:
				HandleControlShiftKey (args);
				break;
			case ModifierKeys.Shift:
				HandleShiftKey (args);
				break;
			case ModifierKeys.Control | ModifierKeys.Alt:
				HandleAltControlKey (args);
				break;
			default:
				args.Handled = false;
				break;
			}
		}

		private void HandleAltControlKey (KeyEventArgs args)
		{
			switch (args.Key) {
			case Key.Home:
				_editorOperations.MoveCurrentLineToTop ();
				break;
			case Key.End:
				_editorOperations.MoveCurrentLineToBottom ();
				break;
			default:
				args.Handled = false;
				break;
			}
		}

		private void HandleShiftKey (KeyEventArgs args)
		{
			switch (args.Key) {
			case Key.Back:
				args.Handled = this.PerformEditAction (() => _editorOperations.Backspace ());
				break;
			case Key.Right:
				_editorOperations.MoveToNextCharacter (true);
				break;
			case Key.Left:
				_editorOperations.MoveToPreviousCharacter (true);
				break;
			case Key.Up:
				_editorOperations.MoveLineUp (true);
				break;
			case Key.Down:
				_editorOperations.MoveLineDown (true);
				break;
			case Key.Home:
				_editorOperations.MoveToHome (true);
				break;
			case Key.End:
				_editorOperations.MoveToEndOfLine (true);
				break;
			case Key.PageUp:
				_editorOperations.PageUp (true);
				break;
			case Key.PageDown:
				_editorOperations.PageDown (true);
				break;
			case Key.Tab:
				args.Handled = this.PerformEditAction (() => _editorOperations.Unindent ());
				break;
			case Key.Enter:
				args.Handled = this.PerformEditAction (() => _editorOperations.InsertNewLine ());
				break;
			default:
				args.Handled = false;
				break;
			}
		}

		private void HandleControlShiftKey (KeyEventArgs args)
		{
			switch (args.Key) {
			case Key.Right:
				_editorOperations.MoveToNextWord (true);
				break;
			case Key.Left:
				_editorOperations.MoveToPreviousWord (true);
				break;
			case Key.Home:
				_editorOperations.MoveToStartOfDocument (true);
				break;
			case Key.End:
				_editorOperations.MoveToEndOfDocument (true);
				break;
			case Key.U:
				args.Handled = this.PerformEditAction (() => _editorOperations.MakeUppercase ());
				break;
			default:
				args.Handled = false;
				break;
			}
		}

		private void HandleAltShiftKey (KeyEventArgs args)
		{
			if (args.Key == Key.T) {
				args.Handled = this.PerformEditAction (() => _editorOperations.TransposeLine ());
				return;
			}

			// If this is starting a new selection, put the selection in
			// box selection mode.
			if ((args.Key == Key.Down ||
				args.Key == Key.Up ||
				args.Key == Key.Left ||
				args.Key == Key.Right) &&
				_textView.Selection.IsEmpty) {
				_textView.Selection.Mode = TextSelectionMode.Box;
			}

			// Treat it as a regular Shift + keypress
			HandleShiftKey (args);
		}

		private void HandleAltKey (KeyEventArgs args)
		{
			switch (args.Key) {
			case Key.Left:
				_editorOperations.SelectEnclosing ();
				break;
			case Key.Right:
				_editorOperations.SelectFirstChild ();
				break;
			case Key.Down:
				_editorOperations.SelectNextSibling (false);
				break;
			case Key.Up:
				_editorOperations.SelectPreviousSibling (false);
				break;
			default:
				args.Handled = false;
				break;
			}
		}

		private void HandleControlKey (KeyEventArgs args)
		{
			switch (args.Key) {
			case Key.Back:
				args.Handled = this.PerformEditAction (() => _editorOperations.DeleteWordToLeft ());
				break;
			case Key.Delete:
				args.Handled = this.PerformEditAction (() => _editorOperations.DeleteWordToRight ());
				break;
			case Key.A:
				_editorOperations.SelectAll ();
				break;
			case Key.W:
				_editorOperations.SelectCurrentWord ();
				break;
			case Key.Right:
				_editorOperations.MoveToNextWord (false);
				break;
			case Key.Left:
				_editorOperations.MoveToPreviousWord (false);
				break;
			case Key.Home:
				_editorOperations.MoveToStartOfDocument (false);
				break;
			case Key.End:
				_editorOperations.MoveToEndOfDocument (false);
				break;
			case Key.Up:
				_editorOperations.ScrollUpAndMoveCaretIfNecessary ();
				break;
			case Key.Down:
				_editorOperations.ScrollDownAndMoveCaretIfNecessary ();
				break;
			case Key.T:
				args.Handled = this.PerformEditAction (() => _editorOperations.TransposeCharacter ());
				break;
			case Key.U:
				args.Handled = this.PerformEditAction (() => _editorOperations.MakeLowercase ());
				break;
			case Key.C:
				_editorOperations.CopySelection ();
				break;
			case Key.X:
				args.Handled = this.PerformEditAction (() => _editorOperations.CutSelection ());
				break;
			case Key.V:
				args.Handled = this.PerformEditAction (() => _editorOperations.Paste ());
				break;
			case Key.Z:
				if (UndoHistory.CanUndo)
					UndoHistory.Undo (1);
				else
					args.Handled = false;
				break;
			case Key.Y:
				if (UndoHistory.CanRedo)
					UndoHistory.Redo (1);
				else
					args.Handled = false;
				break;
			default:
				args.Handled = false;
				break;
			}
		}

		private void ExecuteCommand(string command)
		{
			CommandHandler.Run (null, new ActionCommand (command, null));
		}

		private void HandleKey (KeyEventArgs args)
		{
			switch (args.Key) {
			case Key.Right:
				_editorOperations.MoveToNextCharacter (false);
				break;
			case Key.Left:
				//_editorOperations.MoveToPreviousCharacter (false);
				ExecuteCommand ("MonoDevelop.Ide.Commands.TextEditorCommands.CharLeft");
				break;
			case Key.Up:
				_editorOperations.MoveLineUp (false);
				break;
			case Key.Down:
				_editorOperations.MoveLineDown (false);
				break;
			case Key.PageUp:
				_editorOperations.PageUp (false);
				break;
			case Key.PageDown:
				_editorOperations.PageDown (false);
				break;
			case Key.Home:
				_editorOperations.MoveToHome (false);
				break;
			case Key.End:
				_editorOperations.MoveToEndOfLine (false);
				break;
			case Key.Escape:
				_editorOperations.ResetSelection ();
				break;
			case Key.Delete:
				args.Handled = this.PerformEditAction (() => _editorOperations.Delete ());
				break;
			case Key.Back:
				args.Handled = this.PerformEditAction (() => _editorOperations.Backspace ());
				break;
			case Key.Insert:
				_editorOperations.Options.SetOptionValue (DefaultTextViewOptions.OverwriteModeId,
					!_editorOperations.Options.IsOverwriteModeEnabled ());
				break;
			case Key.Enter:
				args.Handled = this.PerformEditAction (() => _editorOperations.InsertNewLine ());
				break;
			case Key.Tab:
				args.Handled = this.PerformEditAction (() => _editorOperations.Indent ());
				break;
			default:
				args.Handled = false;
				break;
			}
		}

		public override void TextInput (TextCompositionEventArgs args)
		{
			if (args.Text.Length == 1) {
				commandDispatcher.InsertChar (args.Text[0]);
				args.Handled = true;
			}
			// The view will generate an text input event of length zero to flush the current provisional composition span.
			// No one else should be doing that, so ignore zero length inputs unless there is provisional text to flush.
			//if ((args.Text.Length > 0) || (_editorOperations.ProvisionalCompositionSpan != null)) {
			//	args.Handled = this.PerformEditAction (() => _editorOperations.InsertText (args.Text));

			//	if (args.Handled)
			//		_textView.Caret.EnsureVisible ();
			//}
		}

		public override void TextInputStart (TextCompositionEventArgs args)
		{
			if (args.TextComposition is ImeTextComposition) {
				// This TextInputStart message is part of an IME event and needs to be treated like
				// provisional text input (if the cast failed, then an IME is not the source of the
				// text input and we can rely on getting an identical TextInput event as soon as we
				// exit).
				this.HandleProvisionalImeInput (args);
			}
		}

		public override void TextInputUpdate (TextCompositionEventArgs args)
		{
			if (args.TextComposition is ImeTextComposition) {
				this.HandleProvisionalImeInput (args);
			} else {
				args.Handled = false;
			}
		}

		private void HandleProvisionalImeInput (TextCompositionEventArgs args)
		{
			if (args.Text.Length > 0) {
				args.Handled = this.PerformEditAction (() => _editorOperations.InsertProvisionalText (args.Text));

				if (args.Handled) {
					_textView.Caret.EnsureVisible ();
				}
			}
		}

		/// <summary>
		/// Performs the passed editAction if the view does not prohibit user input.
		/// </summary>
		/// <returns>True if the editAction was performed.</returns>
		private bool PerformEditAction (Action editAction)
		{
			if (!_textView.Options.GetOptionValue<bool> (DefaultTextViewOptions.ViewProhibitUserInputId)) {
				editAction.Invoke ();
				return true;
			}

			return false;
		}

		private ITextUndoHistory UndoHistory {
			get {
				return _undoHistoryRegistry.GetHistory (_textView.TextBuffer);
			}
		}
	}
}
