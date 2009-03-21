//
// CodeTemplate.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using MonoDevelop.Core;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using Mono.TextEditor;

namespace MonoDevelop.Ide.CodeTemplates
{
	[Flags]
	public enum CodeTemplateType {
		Unknown       = 0,
		Expansion     = 1,
		SurroundsWith = 2
	}
	
	public class CodeTemplate
	{
		public string Group { 
			get; 
			set; 
		}
		
		public string Shortcut {
			get;
			set;
		}
		
		public CodeTemplateType CodeTemplateType {
			get;
			set;
		}
		
		public string MimeType {
			get;
			set;
		}
		
		public string Description {
			get;
			set;
		}
		
		public string Code {
			get;
			set;
		}
		
		Dictionary<string, CodeTemplateVariable> variableDecarations = new Dictionary<string, CodeTemplateVariable> ();
		
		public IEnumerable<CodeTemplateVariable> Variables {
			get {
				return variableDecarations.Values;
			}
		}
		
		public CodeTemplate()
		{
		}
		
		public override string ToString ()
		{
			return string.Format("[CodeTemplate: Group={0}, Shortcut={1}, CodeTemplateType={2}, MimeType={3}, Description={4}, Code={5}]", Group, Shortcut, CodeTemplateType, MimeType, Description, Code);
		}
		
		static int FindPrevWordStart (MonoDevelop.Ide.Gui.TextEditor editor, int offset)
		{
			while (--offset >= 0 && !Char.IsWhiteSpace (editor.GetCharAt (offset))) 
				;
			return ++offset;
		}
		
		static string GetWordBeforeCaret (MonoDevelop.Ide.Gui.TextEditor editor)
		{
			int offset = editor.CursorPosition;
			int start  = FindPrevWordStart (editor, offset);
			return editor.GetText (start, offset);
		}
		
		static int DeleteWordBeforeCaret (MonoDevelop.Ide.Gui.TextEditor editor)
		{
			int offset = editor.CursorPosition;
			int start  = FindPrevWordStart (editor, offset);
			editor.DeleteText (start, offset - start);
			return start;
		}
		
		static string GetLeadingWhiteSpace (MonoDevelop.Ide.Gui.TextEditor editor, int lineNr)
		{
			string lineText = editor.GetLineText (lineNr);
			int index = 0;
			while (index < lineText.Length && Char.IsWhiteSpace (lineText[index]))
				index++;
			return index > 0 ? lineText.Substring (0, index) : "";
		}
		
		static Regex variableRegEx = new Regex ("\\$([^$]*)\\$", RegexOptions.Compiled);
		
		public List<string> ParseVariables (string code)
		{
			List<string> result = new List<string> ();
			foreach (Match match in variableRegEx.Matches (code)) {
				string name = match.Groups[1].Value;
				if (name == "end" || name == "selected" || string.IsNullOrEmpty (name) || name.Trim ().Length == 0)
					continue;
				if (!result.Contains (name))
					result.Add (name);
			}
			return result;
		}
		
		public void AddVariable (CodeTemplateVariable var)
		{
			this.variableDecarations.Add (var.Name, var);
		}
		
		public class TemplateResult
		{
			public string Code {
				get;
				set;
			}
			
			public int CaretEndOffset {
				get;
				set;
			}
			
			public int InsertPosition {
				get;
				set;
			}
			
			public List<TextLink> TextLinks {
				get;
				set;
			}
			
			public TemplateResult ()
			{
				Code = null;
				CaretEndOffset = -1;
			}
		}
		
		public TemplateResult FillVariables (TemplateContext context)
		{
			ExpansionObject expansion = CodeTemplateService.GetExpansionObject (this);
			TemplateResult result = new TemplateResult ();
			StringBuilder sb = new StringBuilder ();
			int lastOffset = 0;
			string code = context.TemplateCode;
			result.TextLinks = new List<TextLink> ();
			foreach (Match match in variableRegEx.Matches (code)) {
				string name = match.Groups[1].Value;
				sb.Append (code.Substring (lastOffset, match.Index - lastOffset));
				lastOffset = match.Index + match.Length;
				if (name == "end") {
					result.CaretEndOffset = sb.Length;
				} else if (name == "selected") {
					if (!string.IsNullOrEmpty (context.SelectedText))
						sb.Append (context.SelectedText);
				}
				if (!variableDecarations.ContainsKey (name))
					continue;
				TextLink link = result.TextLinks.Find (l => l.Name == name);
				bool isNew = link == null;
				if (isNew) {
					link         = new TextLink (name);
					link.Tooltip = variableDecarations[name].ToolTip;
					link.Values  = variableDecarations[name].Values.ToArray ();
					result.TextLinks.Add (link);
				}
				link.IsEditable = variableDecarations[name].IsEditable;
				if (!string.IsNullOrEmpty (variableDecarations[name].Function)) {
					string functionResult = expansion.RunFunction (context, null, variableDecarations[name].Function);
					string s = functionResult ?? variableDecarations[name].Default;
					link.AddLink (new Segment (sb.Length, s.Length));
					if (isNew) {
						link.GetStringFunc = delegate (Func<string, string> callback) {
							return expansion.RunFunction (context, callback, variableDecarations[name].Function);
						};
					}
					sb.Append (s);
				} else {
					link.AddLink (new Segment (sb.Length, variableDecarations[name].Default.Length));
					if (isNew)
						link.AddString (variableDecarations[name].Default);
					sb.Append (variableDecarations[name].Default);
				}
			}
			sb.Append (code.Substring (lastOffset, code.Length - lastOffset));
			result.Code = sb.ToString ();
			return result;
		}
		
		public string IndentCode (string code, string indent)
		{
			StringBuilder result = new StringBuilder ();
			for (int i = 0; i < code.Length; i++) {
				result.Append (code[i]);
				if (code[i] == '\n')
					result.Append (indent);
			}
			return result.ToString ();
		}
		
		// todo: better code formatting !!!
		string GetIndent (MonoDevelop.Ide.Gui.TextEditor d, int lineNumber, int terminateIndex)
		{
			string lineText = d.GetLineText (lineNumber);
			if(terminateIndex > 0)
				lineText = lineText.Substring(0, terminateIndex);
			
			StringBuilder whitespaces = new StringBuilder ();
			
			foreach (char ch in lineText) {
				if (!char.IsWhiteSpace (ch))
					break;
				whitespaces.Append (ch);
			}
			
			return whitespaces.ToString ();
		}
		
		public TemplateResult InsertTemplate (MonoDevelop.Ide.Gui.Document document)
		{
			ProjectDom dom = ProjectDomService.GetProjectDom (document.Project);
			ParsedDocument doc = document.ParsedDocument;
			MonoDevelop.Ide.Gui.TextEditor editor = document.TextEditor;
				
			int offset = editor.CursorPosition;
			int line, col;
			editor.GetLineColumnFromPosition (offset, out line, out col);
			string leadingWhiteSpace = GetLeadingWhiteSpace (editor, editor.CursorLine);
			
			TemplateContext context = new TemplateContext {
				Template       = this,
				Document       = document,
				ProjectDom     = dom,
				ParsedDocument = doc,
				InsertPosition = new DomLocation (line, col),
				SelectedText   = editor.SelectedText,
				TemplateCode   = IndentCode (Code, GetIndent (editor, line, 0))
			};
			
			if (!string.IsNullOrEmpty (editor.SelectedText)) {
				int selectionLength = editor.SelectionEndPosition - editor.SelectionStartPosition;
				if (editor.SelectionStartPosition < offset) {
					offset -= selectionLength;
				}
				editor.DeleteText (editor.SelectionStartPosition, selectionLength);
			} else {
				string word = GetWordBeforeCaret (editor).Trim ();
				if (word.Length > 0)
					offset = DeleteWordBeforeCaret (editor);
			}
			
			TemplateResult template = FillVariables (context);
			template.InsertPosition = offset;
			editor.InsertText (offset, template.Code);
			
			if (template.CaretEndOffset >= 0) {
				editor.CursorPosition = offset + template.CaretEndOffset; 
			} else {
				editor.CursorPosition = offset + template.Code.Length; 
			}
			return template;
		}

#region I/O
		public const string Node        = "CodeTemplate";
		const string HeaderNode          = "Header";
		const string GroupNode           = "_Group";
		const string MimeNode            = "MimeType";
		const string ShortcutNode        = "Shortcut";
		const string DescriptionNode     = "_Description";
		const string TemplateTypeNode    = "TemplateType";
		
		const string VariablesNode       = "Variables";
		
		
		const string CodeNode            = "Code";
			
		const string versionAttribute    = "version";
		const string version             = "2.0";
		
		const string descriptionAttribute = "description";
		
		public void Write (XmlWriter writer)
		{
			writer.WriteStartElement (Node);
			writer.WriteAttributeString (versionAttribute, version);
			
			writer.WriteStartElement (HeaderNode);
			
			writer.WriteStartElement (GroupNode);
			writer.WriteString (Group);
			writer.WriteEndElement ();
			
			writer.WriteStartElement (MimeNode);
			writer.WriteString (MimeType);
			writer.WriteEndElement ();
			
			writer.WriteStartElement (ShortcutNode);
			writer.WriteString (Shortcut);
			writer.WriteEndElement ();
			
			writer.WriteStartElement (DescriptionNode);
			writer.WriteString (Description);
			writer.WriteEndElement ();
			
			writer.WriteStartElement (TemplateTypeNode);
			writer.WriteString (CodeTemplateType.ToString ());
			writer.WriteEndElement ();
			
			writer.WriteEndElement (); // HeaderNode
			
			writer.WriteStartElement (VariablesNode);
			foreach (CodeTemplateVariable var in variableDecarations.Values) {
				var.Write (writer);
			}
			
			writer.WriteEndElement (); // VariablesNode
			
			writer.WriteStartElement (CodeNode);
			writer.WriteCData (Code);
			writer.WriteEndElement (); // CodeNode
			
			writer.WriteEndElement (); // Node
		}
		
		public static CodeTemplate Read (XmlReader reader)
		{
			Debug.Assert (reader.LocalName == Node);
			
			CodeTemplate result = new CodeTemplate ();
			
			XmlReadHelper.ReadList (reader, Node, delegate () {
				//Console.WriteLine (reader.LocalName);
				switch (reader.LocalName) {
				case HeaderNode:
					XmlReadHelper.ReadList (reader, HeaderNode, delegate () {
						switch (reader.LocalName) {
						case GroupNode:
							result.Group = reader.ReadElementContentAsString ();
							return true;
						case MimeNode:
							result.MimeType = reader.ReadElementContentAsString ();
							return true;
						case ShortcutNode:
							result.Shortcut = reader.ReadElementContentAsString ();
							return true;
						case DescriptionNode:
							result.Description = reader.ReadElementContentAsString ();
							return true;
						case TemplateTypeNode:
							result.CodeTemplateType = (CodeTemplateType)Enum.Parse (typeof (CodeTemplateType), reader.ReadElementContentAsString ());
							return true;
						}
						return false;
					});
					return true;
				case VariablesNode:
					XmlReadHelper.ReadList (reader, VariablesNode, delegate () {
						//Console.WriteLine ("var:" + reader.LocalName);
						switch (reader.LocalName) {
						case CodeTemplateVariable.Node:
							CodeTemplateVariable var = CodeTemplateVariable.Read (reader);
							result.variableDecarations [var.Name] = var;
							return true;
						}
						return false;
					});
					return true;
				case CodeNode:
					result.Code = reader.ReadElementContentAsString ();
					return true;
				}
				return false;
			});
			//Console.WriteLine ("result:" + result);
			return result;
		}
#endregion
	}
}
