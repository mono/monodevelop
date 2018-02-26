// 
// Commands.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using System.Linq;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Core;

namespace MonoDevelop.DocFood
{
	enum Commands {
		DocumentThis,
		DocumentBuffer
	}
	
	class DocumentThisHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			info.Enabled = IdeApp.Workbench.ActiveDocument != null && 
				IdeApp.Workbench.ActiveDocument.Editor != null &&
				IdeApp.Workbench.ActiveDocument.Editor.MimeType == "text/x-csharp";
			base.Update (info);
		}
		
		protected override void Run ()
		{
			// TODO - currently handled by the text editor extension.
		}
	}
	
	class DocumentBufferHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			info.Enabled = IdeApp.Workbench.ActiveDocument != null && 
				IdeApp.Workbench.ActiveDocument.Editor != null &&
				IdeApp.Workbench.ActiveDocument.Editor.MimeType == "text/x-csharp";
			base.Update (info);
		}
		
		protected override void Run ()
		{
//			var document = IdeApp.Workbench.ActiveDocument;
//			if (document == null)
//				return;
//			var unit = document.ParsedDocument;
//			if (unit == null)
//				return;
//			TextEditorData data = IdeApp.Workbench.ActiveDocument.Editor;
//			var types = new Stack<IUnresolvedTypeDefinition> (unit.TopLevelTypeDefinitions);
//			var docs = new List<KeyValuePair<int, string>> ();
//			while (types.Count > 0) {
//				var curType = types.Pop ();
//				foreach (var member in curType.Members) {
//					if (member is IUnresolvedTypeDefinition) {
//						types.Push ((IUnresolvedTypeDefinition)member);
//						continue;
//					}
//					if (!member.IsPublic) {
//						if (member.DeclaringTypeDefinition != null && member.DeclaringTypeDefinition.Kind != TypeKind.Interface)
//							continue;
//					}
//					if (!NeedsDocumentation (data, member))
//						continue;
//					int offset;
//					var ctx = (unit.ParsedFile as CSharpUnresolvedFile).GetTypeResolveContext (document.Compilation, member.Region.Begin);
//					var resolvedMember = member.CreateResolved (ctx);
//					string indent = GetIndent (data, resolvedMember, out offset);
//					string documentation = GenerateDocumentation (data, resolvedMember, indent);
//					if (documentation.Trim ().Length == 0)
//						continue;
//					docs.Add (new KeyValuePair <int, string> (offset, documentation));
//				}
//			}
//			docs.Sort ((a, b) => b.Key.CompareTo (a.Key));
//			using (var undo = data.OpenUndoGroup ()) {
//				docs.ForEach (doc => data.Insert (doc.Key, doc.Value));
//			}
		}
		
		static bool NeedsDocumentation (IReadonlyTextDocument data, ISymbol member)
		{
			int lineNr = data.OffsetToLineNumber (member.Locations.First().SourceSpan.Start) - 1;
			IDocumentLine line;

			do {
				line = data.GetLine (lineNr--);
			} while (lineNr > 0 && data.GetLineIndent (line).Length == line.Length);
			return !data.GetTextAt (line).TrimStart ().StartsWith ("///", StringComparison.Ordinal);
		}
		
		static string GetIndent (IReadonlyTextDocument data, ISymbol member, out int offset)
		{
			var line = data.GetLineByOffset (member.Locations.First().SourceSpan.Start);
			offset = line.Offset;
			return data.GetLineIndent (line);
		}
		
		internal static string GenerateDocumentation (IReadonlyTextDocument data, ISymbol member, string indent)
		{
			return GenerateDocumentation (data, member, indent, "/// ");
		}
		
		internal static string GenerateDocumentation (IReadonlyTextDocument data, ISymbol member, string indent, string prefix)
		{
			StringBuilder result = StringBuilderCache.Allocate ();
			
			var generator = new DocGenerator (data);
			generator.GenerateDoc (member);
			
			bool first = true;
			foreach (Section section in generator.sections) {
				if (first) {
					result.Append (indent);
					result.Append (prefix);
					result.Append ("<");
					first = false;
				} else {
					result.AppendLine ();
					result.Append (indent);
					result.Append (prefix);
					result.Append ("<");
				}
				result.Append (section.Name);
				foreach (var attr in section.Attributes) {
					result.Append (" ");
					result.Append (attr.Key);
					result.Append ("=\"");
					result.Append (attr.Value);
					result.Append ("\"");
				}
				if (section.Name == "summary")
				{
					result.AppendLine (">");
					result.Append (indent);
					result.Append (prefix);
				}
				else
				{
					result.Append (">");
				}
				bool inTag = false;
				int column = indent.Length + prefix.Length;
				StringBuilder curWord = StringBuilderCache.Allocate ();
				foreach (char ch in section.Documentation) {
					if (ch == '<')
						inTag = true;
					if (ch == '>')
						inTag = false;

					if (ch =='\n') {
						result.Append (curWord.ToString ());
						curWord.Length = 0;

						result.AppendLine ();
						result.Append (indent);
						result.Append (prefix);
						column = indent.Length + prefix .Length;
					} else if (!inTag && char.IsWhiteSpace (ch)) {
						if (column + curWord.Length > 120) {
							result.Length--; // trunk last char white space.
							result.AppendLine ();
							result.Append (indent);
							result.Append (prefix);
							column = indent.Length + prefix .Length;
						}
						result.Append (curWord.ToString ());
						result.Append (ch);
						column += curWord.Length + 1;
						curWord.Length = 0;
					} else {
						curWord.Append (ch);
					}
				}
				if (section.Name == "summary")
				{
					result.AppendLine(curWord.ToString ());
					result.Append(indent);
					result.Append(prefix);
				}
				else
				{
					result.Append(curWord.ToString ());
				}

				result.Append ("</");
				result.Append (section.Name);
				result.Append (">");
				StringBuilderCache.ReturnAndFree (curWord);
			}
			result.AppendLine ();
			return StringBuilderCache.ReturnAndFree (result);
		}
		
		internal static string GenerateEmptyDocumentation (IReadonlyTextDocument data, ISymbol member, string indent)
		{
			StringBuilder result = StringBuilderCache.Allocate ();
			
			DocGenerator generator = new DocGenerator (data);
			generator.GenerateDoc (member);
			
			bool first = true;
			foreach (Section section in generator.sections) {
				if (first) {
					result.Append (indent);
					result.Append ("/// <");
					first = false;
				} else {
					result.AppendLine ();
					result.Append (indent);
					result.Append ("/// <");
				}
				result.Append (section.Name);
				foreach (var attr in section.Attributes) {
					result.Append (" ");
					result.Append (attr.Key);
					result.Append ("=\"");
					result.Append (attr.Value);
					result.Append ("\"");
				}
				if (section.Name == "summary")
				{
					result.AppendLine (">");
					result.Append (indent);
					result.Append ("/// ");
					result.AppendLine ();
					result.Append (indent);
					result.Append ("/// ");
				}
				else
				{
					result.Append (">");
				}

//				bool inTag = false;
//				int column = indent.Length + "/// ".Length;

				result.Append ("</");
				result.Append (section.Name);
				result.Append (">");
			}
			result.AppendLine ();
			return StringBuilderCache.ReturnAndFree (result);
		}
	}
	
}

