// NewOverrideCompletionData.cs
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
//

using System;
using System.Text;
using MonoDevelop.Projects.Gui.Completion;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Output;

using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;

namespace MonoDevelop.CSharpBinding
{
	public class NewOverrideCompletionData : CodeCompletionData, IActionCompletionData
	{
		TextEditor editor;
		IMember member;
		
		public NewOverrideCompletionData (TextEditor editor, IMember member)
		{
			this.editor = editor;
			this.member = member;
			Image       = member.StockIcon;
			Text        = new string[] { AmbienceService.Default.GetString (member, OutputFlags.ClassBrowserEntries) };
			CompletionString = member.Name;
			
		}
		
		public void InsertAction (ICompletionWidget widget, ICodeCompletionContext context)
		{
			if (member is IMethod) {
				InsertMethod (member as IMethod);
				return;
			}
			editor.InsertText (editor.CursorPosition, member.Name);
		}
		
		void GenerateMethodBody (StringBuilder sb, IMethod method)
		{
			if (method.ReturnType != null && method.ReturnType.FullName != "System.Void") {
				sb.Append ("return base.");
				sb.Append (method.Name);
				sb.Append (" (");
				if (method.Parameters != null) {
					for (int i = 0; i < method.Parameters.Count; i++) {
						if (i > 0)
							sb.Append (", ");
							
						// add parameter modifier
						if (method.Parameters[i].IsOut) {
							sb.Append ("out ");
						} else if (method.Parameters[i].IsRef) {
							sb.Append ("ref ");
						}
						
						sb.Append (method.Parameters[i].Name);
					}
				}
				sb.AppendLine (");");
			}
		}
		
		void InsertMethod (IMethod method)
		{
			StringBuilder sb = new StringBuilder ();
			// todo: implement csharpambience.
			sb.Append (method.ReturnType.FullName);
			sb.Append (" ");
			sb.Append (method.Name);
			sb.Append (" (");
			
			sb.Append (")");
			
			sb.AppendLine ();
			sb.AppendLine ("{");
			GenerateMethodBody (sb, method);
			sb.Append ("}"); 
			
			sb.AppendLine ();
			
			editor.InsertText (editor.CursorPosition, sb.ToString ());
		}
		
	}
}
