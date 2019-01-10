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
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Refactoring;

namespace MonoDevelop.Ide.Text
{
	partial class TextViewContent
	{
		[CommandUpdateHandler (EditCommands.AddCodeComment)]
		void AddCodeComment (CommandInfo info)
		{
			var commandState = _editorCommandHandlerService.GetCommandState ((textView, textBuffer) => new CommentSelectionCommandArgs (textView, textBuffer), null);
			info.Enabled = commandState.IsAvailable;
			info.Visible = !commandState.IsUnspecified;
		}

		[CommandHandler (EditCommands.AddCodeComment)]
		void AddCodeComment ()
		{
			_editorCommandHandlerService.Execute ((textView, textBuffer) => new CommentSelectionCommandArgs (textView, textBuffer), null);
		}

		[CommandUpdateHandler (EditCommands.RemoveCodeComment)]
		void RemoveCodeComment (CommandInfo info)
		{
			var commandState = _editorCommandHandlerService.GetCommandState ((textView, textBuffer) => new UncommentSelectionCommandArgs (textView, textBuffer), null);
			info.Enabled = commandState.IsAvailable;
			info.Visible = !commandState.IsUnspecified;
		}

		[CommandHandler (EditCommands.RemoveCodeComment)]
		void RemoveCodeComment ()
		{
			_editorCommandHandlerService.Execute ((textView, textBuffer) => new UncommentSelectionCommandArgs (textView, textBuffer), null);
		}

		[CommandUpdateHandler (RefactoryCommands.GotoDeclaration)]
		void GotoDeclaration (CommandInfo info)
		{
			var commandState = _editorCommandHandlerService.GetCommandState ((textView, textBuffer) => new GoToDefinitionCommandArgs (textView, textBuffer), null);
			info.Enabled = commandState.IsAvailable;
			info.Visible = !commandState.IsUnspecified;
		}

		[CommandHandler (RefactoryCommands.GotoDeclaration)]
		void GotoDeclaration ()
		{
			_editorCommandHandlerService.Execute ((textView, textBuffer) => new GoToDefinitionCommandArgs (textView, textBuffer), null);
		}
	}
}
