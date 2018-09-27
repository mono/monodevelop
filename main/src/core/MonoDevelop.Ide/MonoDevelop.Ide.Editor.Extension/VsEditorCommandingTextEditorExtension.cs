//
// VsEditorCommandingTextEditorExtension.cs
//
// Author:
//       David Karlaš <david.karlas@microsoft.com>
//
// Copyright (c) 2018 Microsoft Corp
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
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Text.Utilities;
using Microsoft.VisualStudio.Utilities;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Composition;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Extension;

namespace MonoDevelop.Ide.Editor.Extension
{
	public class VsEditorCommandingTextEditorExtension : TextEditorExtension
	{
		private ITextView view;
		IEditorCommandHandlerService editorCommandHandlerService;

		protected override void Initialize ()
		{
			base.Initialize ();
			view = Editor.TextView;
			editorCommandHandlerService = CompositionManager.GetExportedValue<IEditorCommandHandlerServiceFactory> ().GetService (view);
		}

		[CommandUpdateHandler (TextEditorCommands.ShowCompletionWindow)]
		void UpdateCompletionCommand (CommandInfo info)
		{
			info.Enabled = editorCommandHandlerService.GetCommandState ((textView, textBuffer) => new CommitUniqueCompletionListItemCommandArgs (textView, textBuffer), null).IsAvailable;
		}

		[CommandHandler (TextEditorCommands.ShowCompletionWindow)]
		void RunCompletionCommand ()
		{
			editorCommandHandlerService.Execute ((textView, textBuffer) => new CommitUniqueCompletionListItemCommandArgs (textView, textBuffer), null);
		}

		[CommandUpdateHandler (TextEditorCommands.ToggleCompletionSuggestionMode)]
		void UpdateToggleSuggestionMode (CommandInfo info)
		{
			info.Enabled = editorCommandHandlerService.GetCommandState ((textView, textBuffer) => new ToggleCompletionModeCommandArgs (textView, textBuffer), null).IsAvailable;
		}

		[CommandHandler (TextEditorCommands.ToggleCompletionSuggestionMode)]
		void RunToggleSuggestionMode ()
		{
			editorCommandHandlerService.Execute ((textView, textBuffer) => new ToggleCompletionModeCommandArgs (textView, textBuffer), null);
		}

		[CommandUpdateHandler (EditCommands.AddCodeComment)]
		void OnAddCodeComment (CommandInfo info)
		{
			var commandState = editorCommandHandlerService.GetCommandState (CommentSelectionCommandArgsFactory, null);
			info.Enabled = commandState.IsAvailable;
			info.Visible = !commandState.IsUnspecified;
		}

		[CommandHandler (EditCommands.AddCodeComment)]
		internal void AddCodeComment ()
		{
			editorCommandHandlerService.Execute (CommentSelectionCommandArgsFactory, null);
		}

		[CommandUpdateHandler (EditCommands.RemoveCodeComment)]
		void OnRemoveCodeComment (CommandInfo info)
		{
			var commandState = editorCommandHandlerService.GetCommandState (UncommentSelectionCommandArgsFactory, null);
			info.Enabled = commandState.IsAvailable;
			info.Visible = !commandState.IsUnspecified;
		}

		[CommandHandler (EditCommands.RemoveCodeComment)]
		internal void RemoveCodeComment ()
		{
			editorCommandHandlerService.Execute (UncommentSelectionCommandArgsFactory, null);
		}

		[CommandUpdateHandler (EditCommands.ToggleCodeComment)]
		void OnUpdateToggleComment (CommandInfo info)
		{
			var uncommentCommandState = editorCommandHandlerService.GetCommandState (UncommentSelectionCommandArgsFactory, null);
			if (uncommentCommandState.IsAvailable) {
				info.Enabled = true;
				info.Visible = true;
			}
			else {
				var commentCommandState = editorCommandHandlerService.GetCommandState (CommentSelectionCommandArgsFactory, null);
				if (commentCommandState.IsAvailable) {
					info.Enabled = true;
					info.Visible = true;
				}
				else if (!uncommentCommandState.IsUnspecified || !commentCommandState.IsUnspecified) {
					info.Visible = true;
				}
			}
		}

		[CommandHandler (EditCommands.ToggleCodeComment)]
		internal void ToggleCodeComment ()
		{
			if (editorCommandHandlerService.GetCommandState (UncommentSelectionCommandArgsFactory, null).IsAvailable)
				editorCommandHandlerService.Execute (UncommentSelectionCommandArgsFactory, null);
			else if (editorCommandHandlerService.GetCommandState (CommentSelectionCommandArgsFactory, null).IsAvailable)
				editorCommandHandlerService.Execute (CommentSelectionCommandArgsFactory, null);
		}

		private Func<ITextView, ITextBuffer, UncommentSelectionCommandArgs> UncommentSelectionCommandArgsFactory {
			get {
				return (textView, textBuffer) => new UncommentSelectionCommandArgs (textView, textBuffer); ;
			}
		}

		private Func<ITextView, ITextBuffer, CommentSelectionCommandArgs> CommentSelectionCommandArgsFactory {
			get {
				return (textView, textBuffer) => new CommentSelectionCommandArgs (textView, textBuffer);
			}
		}

		public override bool KeyPress (KeyDescriptor descriptor)
		{
			bool? nextCommandResult = null;
			void NextCommand ()
			{
				nextCommandResult = base.KeyPress (descriptor);
			}
			try {
				switch (descriptor.SpecialKey) {
				case SpecialKey.BackSpace:
					editorCommandHandlerService.Execute ((textView, textBuffer) => new BackspaceKeyCommandArgs (textView, textBuffer), NextCommand);
					break;
				case SpecialKey.Escape:
					editorCommandHandlerService.Execute ((textView, textBuffer) => new EscapeKeyCommandArgs (textView, textBuffer), NextCommand);
					break;
				case SpecialKey.Delete:
					editorCommandHandlerService.Execute ((textView, textBuffer) => new DeleteKeyCommandArgs (textView, textBuffer), NextCommand);
					break;
				case SpecialKey.Return:
					editorCommandHandlerService.Execute ((textView, textBuffer) => new ReturnKeyCommandArgs (textView, textBuffer), NextCommand);
					break;
				case SpecialKey.Tab:
					editorCommandHandlerService.Execute ((textView, textBuffer) => new TabKeyCommandArgs (textView, textBuffer), NextCommand);
					break;
				case SpecialKey.Down:
					editorCommandHandlerService.Execute ((textView, textBuffer) => new DownKeyCommandArgs (textView, textBuffer), NextCommand);
					break;
				case SpecialKey.PageDown:
					editorCommandHandlerService.Execute ((textView, textBuffer) => new PageDownKeyCommandArgs (textView, textBuffer), NextCommand);
					break;
				case SpecialKey.Up:
					editorCommandHandlerService.Execute ((textView, textBuffer) => new UpKeyCommandArgs (textView, textBuffer), NextCommand);
					break;
				case SpecialKey.PageUp:
					editorCommandHandlerService.Execute ((textView, textBuffer) => new PageUpKeyCommandArgs (textView, textBuffer), NextCommand);
					break;
				case SpecialKey.Left:
					editorCommandHandlerService.Execute ((textView, textBuffer) => new LeftKeyCommandArgs (textView, textBuffer), NextCommand);
					break;
				case SpecialKey.Right:
					editorCommandHandlerService.Execute ((textView, textBuffer) => new RightKeyCommandArgs (textView, textBuffer), NextCommand);
					break;
				case SpecialKey.Home:
					editorCommandHandlerService.Execute ((textView, textBuffer) => new LineStartCommandArgs (textView, textBuffer), NextCommand);
					break;
				case SpecialKey.End:
					editorCommandHandlerService.Execute ((textView, textBuffer) => new LineEndCommandArgs (textView, textBuffer), NextCommand);
					break;
				default:
					if (descriptor.KeyChar != '\0')
						editorCommandHandlerService.Execute ((textView, textBuffer) => new TypeCharCommandArgs (textView, textBuffer, descriptor.KeyChar), NextCommand);
					break;
				}
			} catch (Exception ex) {
				Debug.WriteLine (ex);
				Debugger.Break ();
			}
			if (nextCommandResult.HasValue)
				return nextCommandResult.Value;
			else
				return false;
		}
	}
}
