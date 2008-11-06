// EditActions.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
using Mono.TextEditor;

namespace MonoDevelop.SourceEditor
{
	public class TabAction
	{
		ExtensibleTextEditor editor;
		
		public TabAction (ExtensibleTextEditor editor)
		{
			this.editor = editor;
		}
		
		public void Action (TextEditorData data)
		{/*
			MonoDevelop.Projects.Dom.Parser.ProjectDom dom = MonoDevelop.Projects.Dom.Parser.ProjectDomService.GetProjectDom (MonoDevelop.Ide.Gui.IdeApp.Workbench.ActiveDocument.Project);
			System.DateTime now = DateTime.Now;
			long members = 0;
			foreach (object member in dom.GetNamespaceContents (new string[] {"System", "Gtk", "System.Collections", "System.IO", "System.Xml"}, true, true)) {
				members++;
			}
			System.Console.WriteLine(members + " -- " + (DateTime.Now - now).TotalMilliseconds);
			*/
			/*
			MonoDevelop.Projects.Dom.Parser.ProjectDom dom = MonoDevelop.Projects.Dom.Parser.ProjectDomService.GetProjectDom (MonoDevelop.Ide.Gui.IdeApp.Workbench.ActiveDocument.Project);
			MonoDevelop.Projects.Dom.IType type = dom.GetType ("System.Collections.ICollection", -1, true, true);
			foreach (MonoDevelop.Projects.Dom.IReturnType retType in dom.GetSubclasses (type)) {
				System.Console.WriteLine(retType);
			}*/
			
			if (!editor.DoInsertTemplate ())
				MiscActions.InsertTab (data);
		}
	}
	
	public static class EditActions
	{
		
		public static void AdvancedBackspace (TextEditorData data)
		{
			RemoveCharBeforCaret (data);
			//DeleteActions.Backspace (data, RemoveCharBeforCaret);
		}
		
		const string open    = "'\"([{<";
		const string closing = "'\")]}>";
		
		static int GetNextNonWsCharOffset (TextEditorData data, int offset)
		{
			int result = offset;
			if (result >= data.Document.Length)
				return -1;
			while (Char.IsWhiteSpace (data.Document.GetCharAt (result))) {
				result++;
				if (result >= data.Document.Length)
					return -1;
			}
			return result;
		}
		
		static void RemoveCharBeforCaret (TextEditorData data)
		{
			if (SourceEditorOptions.Options.AutoInsertMatchingBracket) {
				char ch = data.Document.GetCharAt (data.Caret.Offset - 1);
				int idx = open.IndexOf (ch);
				System.Console.WriteLine(idx);
				if (idx >= 0) {
					int nextCharOffset = GetNextNonWsCharOffset (data, data.Caret.Offset);
					if (nextCharOffset >= 0 && closing[idx] == data.Document.GetCharAt (nextCharOffset)) {
						bool updateToEnd = data.Document.OffsetToLineNumber (nextCharOffset) != data.Caret.Line;
						data.Document.Remove (data.Caret.Offset, nextCharOffset - data.Caret.Offset + 1);
						if (updateToEnd)
							data.Document.CommitLineToEndUpdate (data.Caret.Line);
					}
				}
			}
			DeleteActions.Backspace (data);
		}
	}
}
