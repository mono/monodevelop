//
// VsCompletionTextEditorExtension.cs
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
using Microsoft.VisualStudio.Language.Intellisense;
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
	[Export (typeof (IExperimentationServiceInternal))]
	class ExperimentationServiceInternal : IExperimentationServiceInternal
	{
		public bool IsCachedFlightEnabled (string flightName)
		{
			return "CompletionAPI" == flightName;
		}
	}

	public class VsCompletionTextEditorExtension : TextEditorExtension
	{
		private ITextView view;
		IEditorCommandHandlerService editorCommandHandlerService;

		protected override void Initialize ()
		{
			base.Initialize ();
			view = Editor.TextView;
			editorCommandHandlerService = CompositionManager.GetExportedValue<IEditorCommandHandlerServiceFactory> ().GetService (view, view.TextBuffer);
		}

		public override bool IsValidInContext (DocumentContext context)
		{
			ITextEditorImpl textEditorImpl = context.GetContent<ITextEditorImpl> ();
			ITextView textView = textEditorImpl?.TextView;
			bool isValidInContext;

			if (textView == null) {
				isValidInContext = false;
			} else if (textView.TextBuffer is IProjectionBuffer) {
				isValidInContext = true;
			} else {
				isValidInContext = CompositionManager.GetExportedValue<IAsyncCompletionBroker> ().IsCompletionSupported (textView);
			}

			return isValidInContext;
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
