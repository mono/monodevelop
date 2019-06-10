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
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.Text.Operations;

namespace MonoDevelop.Ide.Text
{
	sealed class DefaultKeyProcessor : KeyProcessor
	{
		readonly IWpfTextView _textView;
		readonly IEditorOperations _editorOperations;
		readonly ITextUndoHistoryRegistry _undoHistoryRegistry;
		readonly IEditorCommandHandlerService _editorCommandHandlerService;

		static Func<CommandState> Unspecified { get; } = () => CommandState.Unspecified;
		static Action Noop { get; } = () => { };

		internal DefaultKeyProcessor (
			IWpfTextView textView,
			IEditorOperations editorOperations,
			ITextUndoHistoryRegistry undoHistoryRegistry,
			IEditorCommandHandlerService editorCommandHandlerService)
		{
			this._textView = textView;
			this._editorOperations = editorOperations;
			this._undoHistoryRegistry = undoHistoryRegistry;
			this._editorCommandHandlerService = editorCommandHandlerService;
		}

		public void QueryAndExecute<T> (Func<ITextView, ITextBuffer, T> argsFactory) where T : EditorCommandArgs
		{
			var state = _editorCommandHandlerService.GetCommandState (argsFactory, Unspecified);
			if (state.IsAvailable)
				_editorCommandHandlerService.Execute (argsFactory, Noop);
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
			case Key.Space:
				QueryAndExecute ((v, b) => new ToggleCompletionModeCommandArgs (v, b));
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
				QueryAndExecute ((v, b) => new BackspaceKeyCommandArgs (v, b));
				break;
			case Key.Right:
				_editorOperations.MoveToNextCharacter (extendSelection: true);
				break;
			case Key.Left:
				_editorOperations.MoveToPreviousCharacter (extendSelection: true);
				break;
			case Key.Up:
				_editorOperations.MoveLineUp (extendSelection: true);
				break;
			case Key.Down:
				_editorOperations.MoveLineDown (extendSelection: true);
				break;
			case Key.Home:
				QueryAndExecute ((v, b) => new LineStartExtendCommandArgs (v, b));
				break;
			case Key.End:
				QueryAndExecute ((v, b) => new LineEndExtendCommandArgs (v, b));
				break;
			case Key.PageUp:
				_editorOperations.PageUp (extendSelection: true);
				break;
			case Key.PageDown:
				_editorOperations.PageDown (extendSelection: true);
				break;
			case Key.Tab:
				QueryAndExecute ((v, b) => new BackTabKeyCommandArgs (v, b));
				break;
			case Key.Enter:
				QueryAndExecute ((v, b) => new ReturnKeyCommandArgs (v, b));
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
				_editorOperations.MoveToNextWord (extendSelection: true);
				break;
			case Key.Left:
				_editorOperations.MoveToPreviousWord (extendSelection: true);
				break;
			case Key.Home:
				_editorOperations.MoveToStartOfDocument (extendSelection: true);
				break;
			case Key.End:
				_editorOperations.MoveToEndOfDocument (extendSelection: true);
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
				_editorOperations.SelectNextSibling (extendSelection: false);
				break;
			case Key.Up:
				_editorOperations.SelectPreviousSibling (extendSelection: false);
				break;
			default:
				args.Handled = false;
				break;
			}
		}

		private void HandleControlKey (KeyEventArgs args)
		{
			switch (args.Key) {
			case Key.Space:
				QueryAndExecute ((v, b) => new CommitUniqueCompletionListItemCommandArgs (v, b));
				break;
			case Key.Back:
				QueryAndExecute ((v, b) => new WordDeleteToStartCommandArgs (v, b));
				break;
			case Key.Delete:
				QueryAndExecute ((v, b) => new WordDeleteToEndCommandArgs (v, b));
				break;
			case Key.A:
				QueryAndExecute ((v, b) => new SelectAllCommandArgs (v, b));
				break;
			case Key.F:
				QueryAndExecute ((v, b) => new FindCommandArgs (v, b));
				break;
			case Key.H:
				QueryAndExecute ((v, b) => new ReplaceCommandArgs (v, b));
				break;
			case Key.W:
				_editorOperations.SelectCurrentWord ();
				break;
			case Key.Right:
				_editorOperations.MoveToNextWord (extendSelection: false);
				break;
			case Key.Left:
				_editorOperations.MoveToPreviousWord (extendSelection: false);
				break;
			case Key.Home:
				QueryAndExecute ((v, b) => new DocumentStartCommandArgs (v, b));
				break;
			case Key.End:
				QueryAndExecute ((v, b) => new DocumentEndCommandArgs (v, b));
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
				QueryAndExecute ((v, b) => new CopyCommandArgs (v, b));
				break;
			case Key.X:
				QueryAndExecute ((v, b) => new CutCommandArgs (v, b));
				break;
			case Key.V:
				QueryAndExecute ((v, b) => new PasteCommandArgs (v, b));
				break;
			case Key.Z:
				if (UndoHistory.CanUndo)
					QueryAndExecute ((v, b) => new UndoCommandArgs (v, b));
				else
					args.Handled = false;
				break;
			case Key.Y:
				if (UndoHistory.CanRedo)
					QueryAndExecute ((v, b) => new RedoCommandArgs (v, b));
				else
					args.Handled = false;
				break;
			default:
				args.Handled = false;
				break;
			}
		}

		private void HandleKey (KeyEventArgs args)
		{
			switch (args.Key) {
			case Key.Right:
				QueryAndExecute ((v, b) => new RightKeyCommandArgs (v, b));
				break;
			case Key.Left:
				QueryAndExecute ((v, b) => new LeftKeyCommandArgs (v, b));
				break;
			case Key.Up:
				QueryAndExecute ((v, b) => new UpKeyCommandArgs (v, b));
				break;
			case Key.Down:
				QueryAndExecute ((v, b) => new DownKeyCommandArgs (v, b));
				break;
			case Key.PageUp:
				QueryAndExecute ((v, b) => new PageUpKeyCommandArgs (v, b));
				break;
			case Key.PageDown:
				QueryAndExecute ((v, b) => new PageDownKeyCommandArgs (v, b));
				break;
			case Key.Home:
				QueryAndExecute ((v, b) => new LineStartCommandArgs (v, b));
				break;
			case Key.End:
				QueryAndExecute ((v, b) => new LineEndCommandArgs (v, b));
				break;
			case Key.Escape:
				QueryAndExecute ((v, b) => new EscapeKeyCommandArgs (v, b));
				break;
			case Key.Delete:
				QueryAndExecute ((v, b) => new DeleteKeyCommandArgs (v, b));
				break;
			case Key.Back:
				QueryAndExecute ((v, b) => new BackspaceKeyCommandArgs (v, b));
				break;
			case Key.Insert:
				_editorOperations.Options.SetOptionValue (DefaultTextViewOptions.OverwriteModeId,
					!_editorOperations.Options.IsOverwriteModeEnabled ());
				break;
			case Key.Enter:
				QueryAndExecute ((v, b) => new ReturnKeyCommandArgs (v, b));
				break;
			case Key.Tab:
				QueryAndExecute ((v, b) => new TabKeyCommandArgs (v, b));
				break;
			case Key.F2:
				QueryAndExecute ((v, b) => new RenameCommandArgs (v, b));
				break;
			default:
				args.Handled = false;
				break;
			}
		}

		public override void TextInput (TextCompositionEventArgs args)
		{
			if (args.Text.Length == 1) {
				QueryAndExecute ((v, b) => new TypeCharCommandArgs (v, b, args.Text[0]));
				args.Handled = true;
			}
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

// TODO:
// InvokeCompletionList
// InvokeSignatureHelp
// InsertSnippet
// MoveSelectedLinesUp
// MoveSelectedLinesDown
// Rename
// SurroundWith