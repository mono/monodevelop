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
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.CSharp.Formatting;
using MonoDevelop.CSharp.Parser;
using System.Collections.Generic;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Core.Text;

namespace MonoDevelop.CSharp.Completion
{
	class EventCreationCompletionData : CompletionData
	{
		string parameterList;
		IUnresolvedMember callingMember;
//		CSharpCompletionTextEditorExtension ext;
		int initialOffset;
		public bool AddSemicolon = true;
		TextEditor editor;
		 
		public override TooltipInformation CreateTooltipInformation (bool smartWrap)
		{
			var tooltipInfo = new TooltipInformation ();
			return tooltipInfo;
		}

		public EventCreationCompletionData (CSharpCompletionTextEditorExtension ext, string methodName, IType delegateType, IEvent evt, string parameterList, IUnresolvedMember callingMember, IUnresolvedTypeDefinition declaringType) : base (null)
		{
			if (string.IsNullOrEmpty (methodName)) {
				this.DisplayText   = (evt != null ? evt.Name : "");
			} else {
				this.DisplayText   = Char.ToUpper (methodName[0]) + methodName.Substring (1) + (evt != null ? evt.Name : "");
			}
			
			if (declaringType != null && declaringType.Members.Any (m => m.Name == this.DisplayText)) {
				for (int i = 1; i < 10000; i++) {
					if (!declaringType.Members.Any (m => m.Name == this.DisplayText + i)) {
						this.DisplayText = this.DisplayText + i.ToString ();
						break;
					}
				}
			}
			this.editor        = ext.TextEditorData;
			this.parameterList = parameterList;
			this.callingMember = callingMember;
			this.Icon          = "md-newmethod";
			this.initialOffset = editor.CaretOffset;
		}

		public override void InsertCompletionText (CompletionListWindow window, ref KeyActions ka, Gdk.Key closeChar, char keyChar, Gdk.ModifierType modifier)
		{
			// insert add/remove event handler code after +=/-=
			editor.Replace (initialOffset, editor.CaretOffset - initialOffset, this.DisplayText + (AddSemicolon ? ";" : ""));
			
			// Search opening bracket of member
			int pos = callingMember != null && !callingMember.BodyRegion.Begin.IsEmpty ? editor.LocationToOffset (callingMember.BodyRegion.BeginLine, callingMember.BodyRegion.BeginColumn) : initialOffset;
			while (pos < editor.Length && editor.GetCharAt (pos) != '{') {
				pos++;
			}
			
			// Search closing bracket of member
			pos = SimpleBracketMatcher.GetMatchingBracketOffset (editor, pos) + 1;
			
			pos = Math.Max (0, Math.Min (pos, editor.Length - 1));
			
			// Insert new event handler after closing bracket
			var line = callingMember != null ? editor.GetLine (callingMember.Region.BeginLine) : editor.GetLineByOffset (initialOffset);
			string indent = line.GetIndentation (editor);
			
			StringBuilder sb = new StringBuilder ();
			sb.Append (editor.EolMarker);
			sb.Append (editor.EolMarker);
			sb.Append (indent);
			if (callingMember != null && callingMember.IsStatic)
				sb.Append ("static ");
			sb.Append ("void ");
			int pos2 = sb.Length;
			sb.Append (this.DisplayText);
			sb.Append (' ');
			sb.Append (this.parameterList);
			sb.Append (editor.EolMarker);
			sb.Append (indent);
			sb.Append ("{");
			sb.Append (editor.EolMarker);
			sb.Append (indent);
			sb.Append (editor.Options.IndentationString);
			int cursorPos = pos + sb.Length;
			sb.Append (editor.EolMarker);
			sb.Append (indent);
			sb.Append ("}");
			editor.Insert (pos, sb.ToString ());
			editor.CaretOffset = cursorPos;
			
			// start text link mode after insert
			var links = new List<TextLink> ();
			var link = new TextLink ("name");
			
			link.AddLink (new TextSegment (0, this.DisplayText.Length));
			link.AddLink (new TextSegment (pos - initialOffset + pos2, this.DisplayText.Length));
			links.Add (link);
			editor.StartTextLinkMode (links);
			
/*			var tle = new TextLinkEditMode (editor.Parent, initialOffset, links);
			tle.TextLinkMode = TextLinkMode.EditIdentifier;
			tle.SetCaretPosition = true;
			tle.SelectPrimaryLink = true;
			tle.OldMode = editor.CurrentMode;
			tle.StartMode ();
			editor.CurrentMode = tle;*/
		}
	}
	

}
