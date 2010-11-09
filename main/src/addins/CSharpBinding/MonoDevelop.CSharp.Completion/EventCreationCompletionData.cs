// EventCreationCompletionData.cs
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
using System.Linq;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Output;

using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.CSharp.Formatting;
using MonoDevelop.CSharp.Parser;
using Mono.TextEditor;
using System.Collections.Generic;

namespace MonoDevelop.CSharp.Completion
{
	public class EventCreationCompletionData : CompletionData
	{
		string parameterList;
		IMember callingMember;
		TextEditorData editor;
		int initialOffset;
		
		public EventCreationCompletionData (TextEditorData editor, string varName, IType delegateType, IEvent evt, string parameterList, IMember callingMember, IType declaringType) : base (null)
		{
			if (string.IsNullOrEmpty (varName)) {
				this.DisplayText   = "Handle" + (evt != null ? evt.Name : "");
			} else {
				this.DisplayText   = "Handle" + Char.ToUpper (varName[0]) + varName.Substring (1) + (evt != null ? evt.Name : "");
			}
			
			if (declaringType != null && declaringType.SearchMember (this.DisplayText, true).Count > 0) {
				for (int i = 1; i < 10000; i++) {
					if (declaringType.SearchMember (this.DisplayText + i.ToString (), true).Count == 0) {
						this.DisplayText = this.DisplayText + i.ToString ();
						break;
					}
				}
			}
			this.editor        = editor;
			this.parameterList = parameterList;
			this.callingMember = callingMember;
			this.Icon          = "md-newmethod";
			this.initialOffset = editor.Caret.Offset;
		}
		
		public override void InsertCompletionText (CompletionListWindow window)
		{
			// insert add/remove event handler code after +=/-=
			editor.Replace (initialOffset, editor.Caret.Offset - initialOffset, this.DisplayText + ";");
			
			// Search opening bracket of member
			int pos = callingMember != null ? editor.Document.LocationToOffset (callingMember.BodyRegion.Start.Line, callingMember.BodyRegion.Start.Column) : initialOffset;
			while (pos < editor.Document.Length && editor.Document.GetCharAt (pos) != '{') {
				pos++;
			}
			
			// Search closing bracket of member
			pos = editor.Document.GetMatchingBracketOffset (pos) + 1;
			
			pos = Math.Max (0, Math.Min (pos, editor.Document.Length - 1));
			
			// Insert new event handler after closing bracket
			string indent = editor.Document.GetLine (callingMember.Location.Line).GetIndentation (editor.Document);
			
			StringBuilder sb = new StringBuilder ();
			sb.Append (editor.EolMarker);
			sb.Append (editor.EolMarker);
			sb.Append (indent);
			if (callingMember.IsStatic)
				sb.Append ("static ");
			sb.Append ("void ");
			int pos2 = sb.Length;
			sb.Append (this.DisplayText);sb.Append (' ');sb.Append (this.parameterList);sb.Append (editor.EolMarker);
			sb.Append (indent);sb.Append ("{");sb.Append (editor.EolMarker);
			sb.Append (indent);sb.Append (TextEditorProperties.IndentString);
			int cursorPos = pos + sb.Length;
			sb.Append (editor.EolMarker);
			sb.Append (indent);sb.Append ("}");
			editor.Insert (pos, sb.ToString ());
			editor.Caret.Offset = cursorPos;
			
			// start text link mode after insert
			List<TextLink> links = new List<TextLink> ();
			TextLink link = new TextLink ("name");
			
			link.AddLink (new Segment (0, this.DisplayText.Length));
			link.AddLink (new Segment (pos - initialOffset + pos2, this.DisplayText.Length));
			links.Add (link);
			
			CompletionTextLinkMode tle = new CompletionTextLinkMode (editor.Parent, initialOffset, links);
			tle.TriggerCodeCompletion = false;
			tle.SetCaretPosition = true;
			tle.SelectPrimaryLink = true;
			tle.OldMode = editor.CurrentMode;
			tle.StartMode ();
			editor.CurrentMode = tle;
		}
	}
	

}
