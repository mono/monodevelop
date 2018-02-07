// TextEditorCommands.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Commands
{
	public enum TextEditorCommands
	{
		ShowCompletionWindow,
		ShowCodeTemplateWindow,
		ShowCodeSurroundingsWindow,
		LineEnd,
		LineStart,
		DeleteLeftChar,
		DeleteRightChar,
		CharLeft,
		CharRight,
		LineUp,
		LineDown,
		DocumentStart,
		DocumentEnd,
		PageUp,
		PageDown,
		ScrollLineUp,
		ScrollLineDown,
		ScrollPageUp,
		ScrollPageDown,
		ScrollTop,
		ScrollBottom,
		DeleteLine,
		DeleteToLineStart,
		DeleteToLineEnd,
		MoveBlockUp,
		MoveBlockDown,
		ShowParameterCompletionWindow,
		GotoMatchingBrace,
		SelectionMoveLeft,
		SelectionMoveRight,
		MovePrevWord,
		MoveNextWord,
		SelectionMovePrevWord,
		SelectionMoveNextWord,
		SelectionMoveUp,
		SelectionMoveDown,
		SelectionMoveHome,
		SelectionMoveEnd,
		SelectionMoveToDocumentStart,
		SelectionMoveToDocumentEnd,
		ExpandSelectionToLine,
		ExpandSelection,
		ShrinkSelection,
		SwitchCaretMode,
		InsertTab,
		RemoveTab,
		InsertNewLine,
		InsertNewLinePreserveCaretPosition,
		InsertNewLineAtEnd,
		CompleteStatement,
		DeletePrevWord,
		DeleteNextWord,
		SelectionPageDownAction,
		SelectionPageUpAction,
		MovePrevSubword,
		MoveNextSubword,
		SelectionMovePrevSubword,
		SelectionMoveNextSubword,
		DeletePrevSubword,
		DeleteNextSubword,
		TransposeCharacters,
		RecenterEditor,
		DuplicateLine,
		
		ToggleCompletionSuggestionMode,
		ToggleBlockSelectionMode,

		DynamicAbbrev,

		PulseCaret,

		ShowQuickInfo
	}
	
	public class ToggleCompletionSuggestionModeHandler : CommandHandler
	{
		protected override void Run ()
		{
			IdeApp.Preferences.ForceSuggestionMode.Value = !IdeApp.Preferences.ForceSuggestionMode;
		}

		protected override void Update (CommandInfo info)
		{
			info.Enabled = IdeApp.Workbench.ActiveDocument?.Editor != null;
			if (IdeApp.Preferences.ForceSuggestionMode)
				info.Text = GettextCatalog.GetString ("Switch to Completion Mode");
			else
				info.Text = GettextCatalog.GetString ("Switch to Suggestion Mode");
		}
	}
}
