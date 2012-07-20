// 
// SearchCommands.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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


using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Commands;

namespace MonoDevelop.Ide.Commands
{
	public enum SearchCommands
	{
		Find,
		FindNext,
		FindPrevious,
		EmacsFindNext,
		EmacsFindPrevious,
		Replace,
		FindInFiles,
		FindNextSelection,
		FindPreviousSelection,
		FindBox,
		ReplaceInFiles,
		
		UseSelectionForFind,
		UseSelectionForReplace,
		
		GotoType,
		GotoFile,
		GotoLineNumber,
		
		ToggleBookmark,
		PrevBookmark,
		NextBookmark,
		ClearBookmarks,

		CreateBookmark1,
		CreateBookmark2,
		CreateBookmark3,
		CreateBookmark4,
		CreateBookmark5,
		CreateBookmark6,
		CreateBookmark7,
		CreateBookmark8,
		CreateBookmark9,
		CreateBookmark0,

		GoToBookmark1,
		GoToBookmark2,
		GoToBookmark3,
		GoToBookmark4,
		GoToBookmark5,
		GoToBookmark6,
		GoToBookmark7,
		GoToBookmark8,
		GoToBookmark9,
		GoToBookmark0,
	}
}
