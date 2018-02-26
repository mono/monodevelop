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
using MonoDevelop.Ide.CodeFormatting;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Core.Text;
using System.Linq;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using System.IO;
using MonoDevelop.Projects;

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
		
		string shortcut;
		public string Shortcut {
			get {
				return shortcut;
			}
			set {
				if (value != null) {
					var trimmedValue = value.Trim ();
					if (trimmedValue != value)
						LoggingService.LogWarning ("Trimmed code template shortcut for:" + trimmedValue);
					shortcut = trimmedValue;
				} else {
					shortcut = null;
				}
			}
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
		
		static int FindPrevWordStart (TextEditor editor, int offset)
		{
			while (--offset >= 0) {
				var c = editor.GetCharAt (offset);
				//Only legal characters in template Shortcut
				//LetterOrDigit make sense
				//in theory we should probably just support LetterOrDigit and _
				if (!char.IsLetterOrDigit (c)) {
					//_ to allow underscore naming convention
					//# is because there are #if templates
					//~ because disctructor template
					//@ some Razor templates start with @
					if (c == '_' || c == '#' || c == '~' || c == '@')
						continue;

					// '-' because CSS property names templates include them
					if (c == '-' && DesktopService.GetMimeTypeIsSubtype(editor.MimeType, "text/x-css"))
						continue;

					break;
				}
			}
			return ++offset;
		}
		
		public static string GetTemplateShortcutBeforeCaret (TextEditor editor)
		{
			int offset = editor.CaretOffset;
			if (offset == 0)
				return "";
			int start  = FindPrevWordStart (editor, offset - 1);
			return editor.GetTextBetween (start, offset);
		}
		
		static int DeleteTemplateShortcutBeforeCaret (TextEditor editor)
		{
			int offset = editor.CaretOffset;
			int start  = FindPrevWordStart (editor, offset);

			// HTML snippets include the opening '<', so ensure that we remove the old one if present
			if (start > 0 && '<' == editor.GetCharAt(start - 1) && DesktopService.GetMimeTypeIsSubtype(editor.MimeType, "text/x-html"))
				start -= 1;

			editor.RemoveText (start, offset - start);
			return start;
		}
		
		static System.Text.RegularExpressions.Regex variableRegEx = new System.Text.RegularExpressions.Regex ("\\$([^$]*)\\$", RegexOptions.Compiled);
		
		public List<string> ParseVariables (string code)
		{
			var result = new List<string> ();
			foreach (System.Text.RegularExpressions.Match match in variableRegEx.Matches (code)) {
				string name = match.Groups[1].Value;
				if (name == "end" || name == "selected" || string.IsNullOrEmpty (name) || name.Trim ().Length == 0)
					continue;
				if (!result.Contains (name))
					result.Add (name);
			}
			return result;
		}

		static HashSet<string> reportedVariables = new HashSet<string> ();
		public void AddVariable (CodeTemplateVariable var)
		{
			if (variableDecarations.ContainsKey (var.Name)) {
				if (reportedVariables.Add (var.Name))
					LoggingService.LogWarning ("code template duplicate : " + var.Name);
			}
			variableDecarations [var.Name] = var;
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
			var expansion = CodeTemplateService.GetExpansionObject (this);
			var result = new TemplateResult ();
			var sb = StringBuilderCache.Allocate ();
			int lastOffset = 0;
			string code = context.Editor.FormatString (context.InsertPosition, context.TemplateCode);
			result.TextLinks = new List<TextLink> ();
			foreach (System.Text.RegularExpressions.Match match in variableRegEx.Matches (code)) {
				string name = match.Groups [1].Value;
				sb.Append (code, lastOffset, match.Index - lastOffset);
				lastOffset = match.Index + match.Length;
				if (string.IsNullOrEmpty (name)) { // $$ is interpreted as $
					sb.Append ("$");
				} else {
					switch (name) {
					case "end":
						result.CaretEndOffset = sb.Length;
						break;
					case "selected":
						if (!string.IsNullOrEmpty (context.SelectedText)) {
							string indent = GetIndent (sb);
							string selection = Reindent (context.SelectedText, indent);
							sb.Append (selection);
						}
						break;
					case "TM_CURRENT_LINE":
						sb.Append (context.Editor.CaretLine);
						break;
					case "TM_CURRENT_WORD":
						sb.Append ("");
						break;
					case "TM_FILENAME":
						sb.Append (context.Editor.FileName);
						break;
					case "TM_FILEPATH":
						sb.Append (Path.GetDirectoryName (context.Editor.FileName));
						break;
					case "TM_FULLNAME":
						sb.Append (AuthorInformation.Default.Name);
						break;
					case "TM_LINE_INDEX":
						sb.Append (context.Editor.CaretColumn - 1);
						break;
					case "TM_LINE_NUMBER":
						sb.Append (context.Editor.CaretLine);
						break;
					case "TM_SOFT_TABS":
						sb.Append (context.Editor.Options.TabsToSpaces ? "YES" : "NO"); // Note: these strings need no translation.
						break;
					case "TM_TAB_SIZE":
						sb.Append (context.Editor.Options.TabSize);
						break;
					}
				}
				if (!variableDecarations.ContainsKey (name))
					continue;
				var link = result.TextLinks.Find (l => l.Name == name);
				bool isNew = link == null;
				if (isNew) {
					link = new TextLink (name);
					if (!string.IsNullOrEmpty (variableDecarations [name].ToolTip))
						link.Tooltip = GettextCatalog.GetString (variableDecarations [name].ToolTip);
					link.Values = new CodeTemplateListDataProvider (variableDecarations [name].Values);
					if (!string.IsNullOrEmpty (variableDecarations [name].Function)) {
						link.Values = expansion.RunFunction (context, null, variableDecarations [name].Function);
					}
					result.TextLinks.Add (link);
				}
				link.IsEditable = variableDecarations [name].IsEditable;
				link.IsIdentifier = variableDecarations [name].IsIdentifier;
				if (!string.IsNullOrEmpty (variableDecarations [name].Function)) {
					var functionResult = expansion.RunFunction (context, null, variableDecarations [name].Function);
					if (functionResult != null && functionResult.Count > 0) {
						string s = (string)functionResult [functionResult.Count - 1];
						if (s == null) {
							if (variableDecarations.ContainsKey (name)) 
								s = variableDecarations [name].Default;
						}
						if (s != null) {
							link.AddLink (new TextSegment (sb.Length, s.Length));
							if (isNew) {
								link.GetStringFunc = delegate (Func<string, string> callback) {
									return expansion.RunFunction (context, callback, variableDecarations [name].Function);
								};
							}
							sb.Append (s);
						}
					} else {
						AddDefaultValue (sb, link, name);
					}
				} else {
					AddDefaultValue (sb, link, name);
				}
			}
			sb.Append (code, lastOffset, code.Length - lastOffset);
			
			// format & indent template code
			var data = TextEditorFactory.CreateNewDocument ();
			data.Text = StringBuilderCache.ReturnAndFree (sb);
			data.TextChanged += delegate(object sender, MonoDevelop.Core.Text.TextChangeEventArgs e) {
				for (int i = 0; i < e.TextChanges.Count; ++i) {
					var change = e.TextChanges[i];
					int delta = change.InsertionLength - change.RemovalLength;

					foreach (var link in result.TextLinks) {
						link.Links = link.Links.AdjustSegments (e).ToList ();
					}
					if (result.CaretEndOffset > change.Offset)
						result.CaretEndOffset += delta;
				}
			};

			IndentCode (data, context.LineIndent);
			result.Code = data.Text;
			return result;
		}

		void AddDefaultValue (StringBuilder sb, TextLink link, string name)
		{
			if (string.IsNullOrEmpty (variableDecarations [name].Default))
				return;
			link.AddLink (new TextSegment (sb.Length, variableDecarations[name].Default.Length));
			sb.Append (variableDecarations[name].Default);
		}

		
		public string IndentCode (string code, string eol, string indent)
		{
			var result = StringBuilderCache.Allocate ();
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
			return StringBuilderCache.ReturnAndFree (result);
		}

		static void IndentCode (ITextDocument data, string lineIndent)
		{
			for (int i = 1; i < data.LineCount; i++) {
				var line = data.GetLine (i + 1);
				if (line.Length > 0)
					data.InsertText (line.Offset, lineIndent);
			}
		}
		
		string GetIndent (StringBuilder sb)
		{
			string str = sb.ToString ();
			int i = str.Length - 1;
			while (i >= 0 && !Char.IsWhiteSpace (str[i])) {
				i--;
			}
			var indent = StringBuilderCache.Allocate ();
			while (i >= 0 && (str[i] == ' ' || str[i] == '\t')) {
				indent.Append (str[i]);
				i--;
			}
			return StringBuilderCache.ReturnAndFree (indent);
		}
		
		string RemoveIndent (string text, string indent)
		{
			var doc = TextEditorFactory.CreateNewDocument ();
			doc.Text = text;
			var result = StringBuilderCache.Allocate ();
			foreach (var line in doc.GetLines ()) {
				string curLineIndent = line.GetIndentation (doc);
				int offset = Math.Min (curLineIndent.Length, indent.Length);
				result.Append (doc.GetTextBetween (line.Offset + offset, line.EndOffsetIncludingDelimiter));
			}
			return StringBuilderCache.ReturnAndFree (result);
		}
		
		string Reindent (string text, string indent)
		{
			var doc = TextEditorFactory.CreateNewDocument ();
			doc.Text = text;
			var result = StringBuilderCache.Allocate ();
			foreach (var line in doc.GetLines ()) {
				if (result.Length > 0)
					result.Append (indent);
				result.Append (doc.GetTextAt (line.SegmentIncludingDelimiter));
			}
			return StringBuilderCache.ReturnAndFree (result);
		}

		public void Insert (MonoDevelop.Ide.Gui.Document document)
		{
			Insert (document.Editor, document);
		}

		public void Insert (TextEditor editor, DocumentContext context)
		{
			var handler = context.GetContent<ICodeTemplateHandler> ();
			if (handler != null) {
				handler.InsertTemplate (this, editor, context);
			} else {
				InsertTemplateContents (editor, context);
			}	
		}
		
		/// <summary>
		/// Don't use this unless you're implementing ICodeTemplateWidget. Use Insert instead.
		/// </summary>
		public TemplateResult InsertTemplateContents (TextEditor editor, DocumentContext context)
		{
			var data = editor;
			
			int offset = data.CaretOffset;
//			string leadingWhiteSpace = GetLeadingWhiteSpace (editor, editor.CursorLine);
			
			var templateCtx = new TemplateContext {
				Template = this,
				DocumentContext = context,
				Editor = editor,
				//ParsedDocument = context.ParsedDocument != null ? context.ParsedDocument.ParsedFile : null,
				InsertPosition = data.CaretLocation,
				InsertOffset = data.CaretOffset,
				LineIndent = data.GetLineIndent (data.CaretLocation.Line),
				TemplateCode = Code
			};

			if (data.IsSomethingSelected) {
				int start = data.SelectionRange.Offset;
				while (Char.IsWhiteSpace (data.GetCharAt (start))) {
					start++;
				}
				int end = data.SelectionRange.EndOffset;
				while (Char.IsWhiteSpace (data.GetCharAt (end - 1))) {
					end--;
				}
				templateCtx.LineIndent = data.GetLineIndent (data.OffsetToLineNumber (start));
				templateCtx.SelectedText = RemoveIndent (data.GetTextBetween (start, end), templateCtx.LineIndent);
				data.RemoveText (start, end - start);
				offset = start;
			} else {
				string word = GetTemplateShortcutBeforeCaret (data).Trim ();
				if (word.Length > 0)
					offset = DeleteTemplateShortcutBeforeCaret (data);
			}
			
			TemplateResult template = FillVariables (templateCtx);
			template.InsertPosition = offset;
			editor.InsertText (offset, template.Code);
			
			int newoffset;
			if (template.CaretEndOffset >= 0) {
				newoffset = offset + template.CaretEndOffset; 
			} else {
				newoffset = offset + template.Code.Length; 
			}

			editor.CaretLocation = editor.OffsetToLocation (newoffset) ;

			var prettyPrinter = CodeFormatterService.GetFormatter (data.MimeType);
			if (prettyPrinter != null && prettyPrinter.SupportsOnTheFlyFormatting) {
				int endOffset = template.InsertPosition + template.Code.Length;
				var oldVersion = data.Version;
				prettyPrinter.OnTheFlyFormat (editor, context, TextSegment.FromBounds (template.InsertPosition, editor.CaretOffset));
				if (editor.CaretOffset < endOffset)
					prettyPrinter.OnTheFlyFormat (editor, context, TextSegment.FromBounds (editor.CaretOffset, endOffset));
				
				foreach (var textLink in template.TextLinks) {
					for (int i = 0; i < textLink.Links.Count; i++) {
						var segment = textLink.Links [i];
						var translatedOffset = oldVersion.MoveOffsetTo (data.Version, template.InsertPosition + segment.Offset) - template.InsertPosition;
						textLink.Links [i] = new TextSegment (translatedOffset, segment.Length);
					}
				}
			}
			return template;
		}

		public TemplateResult InsertTemplateContents (Document document)
		{
			if (document == null)
				throw new ArgumentNullException ("document");
			return InsertTemplateContents (document.Editor, document);
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
			
		const string VersionAttribute    = "version";
		const string CurrentVersion      = "2.0";
		
		const string DescriptionAttribute = "description";
		
		public void Write (XmlWriter writer)
		{
			writer.WriteStartElement (Node);
			writer.WriteAttributeString (VersionAttribute, CurrentVersion);
			
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
			
			var result = new CodeTemplate ();
			
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
							var var = CodeTemplateVariable.Read (reader);
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
