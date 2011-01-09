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
using Mono.TextEditor.PopupWindow;

namespace MonoDevelop.Ide.CodeTemplates
{
	[Flags]
	public enum CodeTemplateType {
		Unknown       = 0,
		Expansion     = 1,
		SurroundsWith = 2
	}
	
	public enum CodeTemplateContext {
		Standard,
		InExpression
	}
	
	public interface ICodeTemplateContextProvider
	{
		CodeTemplateContext GetCodeTemplateContext ();
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
		
		public CodeTemplateContext CodeTemplateContext {
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
		
		public string Version {
			get;
			set;
		}
		
		public IconId Icon {
			get {
				return Code.Contains ("$selected$") ? "md-template-surroundwith" : "md-template";
			}
		}
		
		Dictionary<string, CodeTemplateVariable> variableDecarations = new Dictionary<string, CodeTemplateVariable> ();
		
		public IEnumerable<CodeTemplateVariable> Variables {
			get {
				return variableDecarations.Values;
			}
		}
		
		public CodeTemplate()
		{
			CodeTemplateContext = CodeTemplateContext.Standard;
		}
		
		public override string ToString ()
		{
			return string.Format("[CodeTemplate: Group={0}, Shortcut={1}, CodeTemplateType={2}, MimeType={3}, Description={4}, Code={5}]", Group, Shortcut, CodeTemplateType, MimeType, Description, Code);
		}
		
		static int FindPrevWordStart (TextEditorData editor, int offset)
		{
			while (--offset >= 0 && !Char.IsWhiteSpace (editor.GetCharAt (offset))) 
				;
			return ++offset;
		}
		
		public static string GetWordBeforeCaret (TextEditorData editor)
		{
			int offset = editor.Caret.Offset;
			int start  = FindPrevWordStart (editor, offset);
			return editor.GetTextBetween (start, offset);
		}
		
		static int DeleteWordBeforeCaret (TextEditorData editor)
		{
			int offset = editor.Caret.Offset;
			int start  = FindPrevWordStart (editor, offset);
			editor.Remove (start, offset - start);
			return start;
		}
		
//		static string GetLeadingWhiteSpace (MonoDevelop.Ide.Gui.TextEditor editor, int lineNr)
//		{
//			string lineText = editor.GetLineText (lineNr);
//			int index = 0;
//			while (index < lineText.Length && Char.IsWhiteSpace (lineText[index]))
//				index++;
//			return index > 0 ? lineText.Substring (0, index) : "";
//		}
		
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
			string code = context.Document.Editor.FormatString (context.InsertPosition, context.TemplateCode);
			
			result.TextLinks = new List<TextLink> ();
			foreach (Match match in variableRegEx.Matches (code)) {
				string name = match.Groups[1].Value;
				sb.Append (code.Substring (lastOffset, match.Index - lastOffset));
				lastOffset = match.Index + match.Length;
				if (string.IsNullOrEmpty (name)) { // $$ is interpreted as $
					sb.Append ("$");
				} else if (name == "end") {
					result.CaretEndOffset = sb.Length;
				} else if (name == "selected") {
					if (!string.IsNullOrEmpty (context.SelectedText)) {
						string indent = GetIndent (sb);
						string selection = Reindent (context.SelectedText, indent);
						sb.Append (selection);
					}
				}
				if (!variableDecarations.ContainsKey (name))
					continue;
				TextLink link = result.TextLinks.Find (l => l.Name == name);
				bool isNew = link == null;
				if (isNew) {
					link         = new TextLink (name);
					if (!string.IsNullOrEmpty (variableDecarations[name].ToolTip))
						link.Tooltip = GettextCatalog.GetString (variableDecarations[name].ToolTip);
					link.Values  = new CodeTemplateListDataProvider (variableDecarations[name].Values);
					if (!string.IsNullOrEmpty (variableDecarations[name].Function)) {
						link.Values  = expansion.RunFunction (context, null, variableDecarations[name].Function);
					}
					result.TextLinks.Add (link);
				}
				link.IsEditable = variableDecarations[name].IsEditable;
				link.IsIdentifier = variableDecarations[name].IsIdentifier;
				if (!string.IsNullOrEmpty (variableDecarations[name].Function)) {
					IListDataProvider<string> functionResult = expansion.RunFunction (context, null, variableDecarations[name].Function);
					if (functionResult != null && functionResult.Count > 0) {
						string s = (string)functionResult[functionResult.Count - 1];
						if (s == null) {
							if (variableDecarations.ContainsKey (name)) 
								s = variableDecarations[name].Default;
						}
						if (s != null) {
							link.AddLink (new Segment (sb.Length, s.Length));
							if (isNew) {
								link.GetStringFunc = delegate (Func<string, string> callback) {
									return expansion.RunFunction (context, callback, variableDecarations[name].Function);
								};
							}
							sb.Append (s);
						}
					} else {
						AddDefaultValue (sb, link, name);
					}
				} else  {
					AddDefaultValue (sb, link, name);
				}
			}
			sb.Append (code.Substring (lastOffset, code.Length - lastOffset));
			result.Code = sb.ToString ();
			return result;
		}

		void AddDefaultValue (StringBuilder sb, TextLink link, string name)
		{
			if (string.IsNullOrEmpty (variableDecarations[name].Default))
				return;
			link.AddLink (new Segment (sb.Length, variableDecarations[name].Default.Length));
			sb.Append (variableDecarations[name].Default);
		}

		
		public string IndentCode (string code, string eol, string indent)
		{
			StringBuilder result = new StringBuilder ();
			for (int i = 0; i < code.Length; i++) {
				switch (code[i]) {
				case '\r':
					if (i + 1 < code.Length && code[i + 1] == '\n')
						i++;
					goto case '\n';
				case '\n':
					result.Append (eol);
					result.Append (indent);
					break;
				default:
					result.Append (code[i]);
					break;
				}
			}
			return result.ToString ();
		}
		
		
		string GetIndent (StringBuilder sb)
		{
			string str = sb.ToString ();
			int i = str.Length - 1;
			while (i >= 0 && !Char.IsWhiteSpace (str[i])) {
				i--;
			}
			StringBuilder indent = new StringBuilder ();
			while (i >= 0 && (str[i] == ' ' || str[i] == '\t')) {
				indent.Append (str[i]);
				i--;
			}
			return indent.ToString ();
		}
		
		string RemoveIndent (string text, string indent)
		{
			Document doc = new Document ();
			doc.Text = text;
			StringBuilder result = new StringBuilder ();
			foreach (LineSegment line in doc.Lines) {
				string curLineIndent = line.GetIndentation (doc);
				int offset = Math.Min (curLineIndent.Length, indent.Length);
				result.Append (doc.GetTextBetween (line.Offset + offset, line.EndOffset));
			}
			return result.ToString ();
		}
		
		string Reindent (string text, string indent)
		{
			Document doc = new Document ();
			doc.Text = text;
			StringBuilder result = new StringBuilder ();
			foreach (LineSegment line in doc.Lines) {
				if (result.Length > 0)
					result.Append (indent);
				result.Append (doc.GetTextAt (line));
			}
			return result.ToString ();
		}
		
		public void Insert (MonoDevelop.Ide.Gui.Document document)
		{
			var handler = document.GetContent<ICodeTemplateHandler> ();
			if (handler != null) {
				handler.InsertTemplate (this, document);
			} else {
				InsertTemplateContents (document);
			}	
		}
		
		/// <summary>
		/// Don't use this unless you're implementing ICodeTemplateWidget. Use Insert instead.
		/// </summary>
		public TemplateResult InsertTemplateContents (MonoDevelop.Ide.Gui.Document document)
		{
			ProjectDom dom = document.Dom;
			ParsedDocument doc = document.ParsedDocument ?? MonoDevelop.Projects.Dom.Parser.ProjectDomService.GetParsedDocument (dom, document.FileName);
			Mono.TextEditor.TextEditorData data = document.Editor;

			int offset = data.Caret.Offset;
//			string leadingWhiteSpace = GetLeadingWhiteSpace (editor, editor.CursorLine);
			
			TemplateContext context = new TemplateContext {
				Template = this,
				Document = document,
				ProjectDom = dom,
				ParsedDocument = doc,
				InsertPosition = data.Caret.Location,
				LineIndent = data.Document.GetLineIndent (data.Caret.Line),
				TemplateCode = IndentCode (Code, document.Editor.EolMarker, data.Document.GetLineIndent (data.Caret.Line))
			};

			if (data.IsSomethingSelected) {
				int start = data.SelectionRange.Offset;
				while (Char.IsWhiteSpace (data.Document.GetCharAt (start))) {
					start++;
				}
				int end = data.SelectionRange.EndOffset;
				while (Char.IsWhiteSpace (data.Document.GetCharAt (end - 1))) {
					end--;
				}
				context.LineIndent = data.Document.GetLineIndent (data.Document.OffsetToLineNumber (start));
				context.SelectedText = RemoveIndent (data.Document.GetTextBetween (start, end), context.LineIndent);
				data.Remove (start, end - start);
				offset = start;
			} else {
				string word = GetWordBeforeCaret (data).Trim ();
				if (word.Length > 0)
					offset = DeleteWordBeforeCaret (data);
			}
			
			TemplateResult template = FillVariables (context);
			template.InsertPosition = offset;
			int length = document.Editor.Insert (offset, template.Code);
			
			if (template.CaretEndOffset >= 0) {
				document.Editor.Caret.Offset = offset + template.CaretEndOffset; 
			} else {
				document.Editor.Caret.Offset = offset + template.Code.Length; 
			}
			
			if (PropertyService.Get ("OnTheFlyFormatting", false)) {
				string mt = DesktopService.GetMimeTypeForUri (document.FileName);
				var formatter = MonoDevelop.Ide.CodeFormatting.CodeFormatterService.GetFormatter (mt);
				if (formatter != null && formatter.SupportsOnTheFlyFormatting) {
					document.Editor.Document.BeginAtomicUndo ();
					formatter.OnTheFlyFormat (document.Project != null ? document.Project.Policies : null, 
						document.Editor, offset, offset + length);
					document.Editor.Document.EndAtomicUndo ();
				}
			}
			return template;
		}

#region I/O
		public const string Node        = "CodeTemplate";
		const string HeaderNode          = "Header";
		const string GroupNode           = "_Group";
		const string MimeNode            = "MimeType";
		const string ContextNode        = "Context";
		const string ShortcutNode        = "Shortcut";
		const string DescriptionNode     = "_Description";
		const string TemplateTypeNode    = "TemplateType";
		
		const string VariablesNode       = "Variables";
		
		const string VersionNode         = "Version";
		
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
			
			writer.WriteStartElement (VersionNode);
			writer.WriteString (Version);
			writer.WriteEndElement ();
			
			writer.WriteStartElement (MimeNode);
			writer.WriteString (MimeType);
			writer.WriteEndElement ();
			
			if (CodeTemplateContext != CodeTemplateContext.Standard) {
				writer.WriteStartElement (ContextNode);
				writer.WriteString (CodeTemplateContext.ToString ());
				writer.WriteEndElement ();
			}
			
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
						case VersionNode:
							result.Version = reader.ReadElementContentAsString ();
							return true;
						case MimeNode:
							result.MimeType = reader.ReadElementContentAsString ();
							return true;
						case ContextNode:
							result.CodeTemplateContext = (CodeTemplateContext)Enum.Parse (typeof (CodeTemplateContext), reader.ReadElementContentAsString ());
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
