// 
// AspDocumentBuilder.cs
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

using System;
using System.Linq;
using MonoDevelop.AspNet.Gui;
using System.Text;
using System.Collections.Generic;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Gui;
using Mono.TextEditor;
using MonoDevelop.Ide.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.CSharp.Parser;
using System.IO;
using ICSharpCode.NRefactory.Completion;
using MonoDevelop.AspNet.StateEngine;
using MonoDevelop.Xml.StateEngine;



namespace MonoDevelop.CSharp.Completion
{
	class AspLanguageBuilder : ILanguageCompletionBuilder
	{
		public bool SupportsLanguage (string language)
		{
			return language == "C#";
		}
		
		public static ParsedDocument Parse (string fileName, string text)
		{
			using (var content = new StringReader (text)) {
				return new MonoDevelop.CSharp.Parser.TypeSystemParser ().Parse (true, fileName, content);
			}
		}
		
		static void WriteUsings (IEnumerable<string> usings, StringBuilder builder)
		{
			foreach (var u in usings) {
				builder.Append ("using ");
				builder.Append (u);
				builder.AppendLine (";");
			}
		}
		
		static void WriteClassDeclaration (DocumentInfo info, StringBuilder builder)
		{
			builder.Append ("partial class ");
			builder.Append (info.ClassName);
			builder.Append (" : ");
			builder.AppendLine (info.BaseType);
		}
		
		public LocalDocumentInfo BuildLocalDocument (DocumentInfo info, TextEditorData data,
		                                             string expressionText, string textAfterCaret, bool isExpression)
		{
			var sb = new StringBuilder ();
			
			WriteUsings (info.Imports, sb);
			WriteClassDeclaration (info, sb);
			sb.AppendLine ("{");
			var result = new LocalDocumentInfo ();
			if (isExpression) {
				sb.AppendLine ("void Generated ()");
				sb.AppendLine ("{");
				//Console.WriteLine ("start:" + location.BeginLine  +"/" +location.BeginColumn);
				foreach (var node in info.XExpressions) {
					bool isBlock = node is AspNetRenderBlock;

					if (node.Region.Begin.Line > data.Caret.Line || node.Region.Begin.Line == data.Caret.Line && node.Region.Begin.Column > data.Caret.Column - 5) 
						continue;
					//Console.WriteLine ("take xprt:" + expressions.Key.BeginLine  +"/" +expressions.Key.BeginColumn);

					var start = data.Document.LocationToOffset (node.Region.Begin.Line, node.Region.Begin.Column) + 2;
					var end = data.Document.LocationToOffset (node.Region.End.Line, node.Region.End.Column) - 2;

					if (!isBlock) {
						sb.Append ("WriteLine (");
						start += 1;
					}

					string expr = data.GetTextBetween (start, end);
					result.AddTextPosition (start, end, expr.Length);
					sb.Append (expr);

					if (!isBlock)
						sb.Append (");");
				}
			}
			sb.Append (expressionText);
			int caretPosition = sb.Length;
			sb.Append (textAfterCaret);
			
			sb.AppendLine ();
			sb.AppendLine ("}");
			sb.AppendLine ("}");
			
			result.LocalDocument = sb.ToString ();
			result.CaretPosition = caretPosition;
			result.OriginalCaretPosition = data.Caret.Offset;
			result.ParsedLocalDocument = Parse (info.AspNetDocument.FileName, sb.ToString ());
			return result;
		}
		
		public ICompletionDataList HandlePopupCompletion (MonoDevelop.Ide.Gui.Document realDocument, DocumentInfo info, LocalDocumentInfo localInfo)
		{
			CodeCompletionContext codeCompletionContext;
			using (var completion = CreateCompletion (realDocument, info, localInfo, out codeCompletionContext)) {
				return completion.CodeCompletionCommand (codeCompletionContext);
			}
		}
		
		public ICompletionDataList HandleCompletion (MonoDevelop.Ide.Gui.Document realDocument, CodeCompletionContext completionContext, DocumentInfo info, LocalDocumentInfo localInfo, char currentChar, ref int triggerWordLength)
		{
			CodeCompletionContext ccc;
			using (var completion = CreateCompletion (realDocument, info, localInfo, out ccc)) {
				return completion.HandleCodeCompletion (completionContext, currentChar, ref triggerWordLength);
			}
		}
		
		public ParameterDataProvider HandleParameterCompletion (MonoDevelop.Ide.Gui.Document realDocument, CodeCompletionContext completionContext, DocumentInfo info, LocalDocumentInfo localInfo, char completionChar)
		{
			CodeCompletionContext ccc;
			using (var completion = CreateCompletion (realDocument, info, localInfo, out ccc)) {
				return completion.HandleParameterCompletion (completionContext, completionChar);
			}
		}
		
		public bool GetParameterCompletionCommandOffset (MonoDevelop.Ide.Gui.Document realDocument, DocumentInfo info, LocalDocumentInfo localInfo, out int cpos)
		{
			CodeCompletionContext codeCompletionContext;
			using (var completion = CreateCompletion (realDocument, info, localInfo, out codeCompletionContext)) {
				return completion.GetParameterCompletionCommandOffset (out cpos);
			}
		}

		public ICompletionWidget CreateCompletionWidget (MonoDevelop.Ide.Gui.Document realDocument, LocalDocumentInfo localInfo)
		{
			return new AspCompletionWidget (realDocument, localInfo);
		}
		
		CSharpCompletionTextEditorExtension CreateCompletion (MonoDevelop.Ide.Gui.Document realDocument, DocumentInfo info, LocalDocumentInfo localInfo, out CodeCompletionContext codeCompletionContext)
		{
			var doc = new Mono.TextEditor.TextDocument () {
				Text = localInfo.LocalDocument,
			};
			var documentLocation = doc.OffsetToLocation (localInfo.CaretPosition);
			
			codeCompletionContext = new CodeCompletionContext () {
				TriggerOffset = localInfo.CaretPosition,
				TriggerLine = documentLocation.Line,
				TriggerLineOffset = documentLocation.Column - 1
			};
			
			return new CSharpCompletionTextEditorExtension (localInfo.HiddenDocument) {
				CompletionWidget = CreateCompletionWidget (realDocument, localInfo)
			};
		}
		
		class AspCompletionWidget : ICompletionWidget
		{
			MonoDevelop.Ide.Gui.Document realDocument;
			LocalDocumentInfo localInfo;
			
			public AspCompletionWidget (MonoDevelop.Ide.Gui.Document realDocument, LocalDocumentInfo localInfo)
			{
				this.realDocument = realDocument;
				this.localInfo = localInfo;
			}

			#region ICompletionWidget implementation
			public CodeCompletionContext CurrentCodeCompletionContext {
				get {
					int delta = realDocument.Editor.Caret.Offset - localInfo.OriginalCaretPosition;
					return CreateCodeCompletionContext (localInfo.CaretPosition + delta);
				}
			}

			public event EventHandler CompletionContextChanged;

			public string GetText (int startOffset, int endOffset)
			{
				endOffset = Math.Min (endOffset, localInfo.LocalDocument.Length); 
				if (endOffset <= startOffset)
					return "";
				return localInfo.LocalDocument.Substring (startOffset, endOffset - startOffset);
			}

			public char GetChar (int offset)
			{
				if (offset < 0 || offset >= localInfo.LocalDocument.Length)
					return '\0';
				return localInfo.LocalDocument[offset];
			}

			public void Replace (int offset, int count, string text)
			{
				throw new NotImplementedException ();
			}

			public CodeCompletionContext CreateCodeCompletionContext (int triggerOffset)
			{
				var savedCtx = realDocument.GetContent<ICompletionWidget> ().CreateCodeCompletionContext (realDocument.Editor.Caret.Offset + triggerOffset - localInfo.CaretPosition);
				CodeCompletionContext result = new CodeCompletionContext ();
				result.TriggerOffset = triggerOffset;
				DocumentLocation loc = localInfo.HiddenDocument.Editor.Document.OffsetToLocation (triggerOffset);
				result.TriggerLine   = loc.Line;
				result.TriggerLineOffset = loc.Column - 1;
				
				result.TriggerXCoord = savedCtx.TriggerXCoord;
				result.TriggerYCoord = savedCtx.TriggerYCoord;
				result.TriggerTextHeight = savedCtx.TriggerTextHeight;
				return result;
			}

			public string GetCompletionText (CodeCompletionContext ctx)
			{
				if (ctx == null)
					return null;
				int min = Math.Min (ctx.TriggerOffset, localInfo.HiddenDocument.Editor.Caret.Offset);
				int max = Math.Max (ctx.TriggerOffset, localInfo.HiddenDocument.Editor.Caret.Offset);
				return localInfo.HiddenDocument.Editor.Document.GetTextBetween (min, max);
			}
			
			public void SetCompletionText (CodeCompletionContext ctx, string partial_word, string complete_word)
			{
				 SetCompletionText (ctx, partial_word, complete_word, complete_word.Length);
			}

			public void SetCompletionText (CodeCompletionContext ctx, string partial_word, string complete_word, int wordOffset)
			{
				CodeCompletionContext translatedCtx = new CodeCompletionContext ();
				int offset = localInfo.OriginalCaretPosition + ctx.TriggerOffset - localInfo.CaretPosition;
				translatedCtx.TriggerOffset = offset;
				DocumentLocation loc = localInfo.HiddenDocument.Editor.Document.OffsetToLocation (offset);
				translatedCtx.TriggerLine   = loc.Line;
				translatedCtx.TriggerLineOffset = loc.Column - 1;
				translatedCtx.TriggerWordLength = ctx.TriggerWordLength;
				realDocument.GetContent <ICompletionWidget> ().SetCompletionText (translatedCtx, partial_word, complete_word, wordOffset);
			}
			
			public int CaretOffset {
				get {
					return localInfo.HiddenDocument.Editor.Caret.Offset;
				}
			}
			
			public int TextLength {
				get {
					return localInfo.HiddenDocument.Editor.Document.TextLength;
				}
			}

			public int SelectedLength {
				get {
					return 0;
				}
			}

			public Gtk.Style GtkStyle {
				get {
					return Gtk.Widget.DefaultStyle;
				}
			}
			#endregion
		}
		
		public ParsedDocument BuildDocument (DocumentInfo info, TextEditorData data)
		{
			var docStr = BuildDocumentString (info, data);
			return Parse (info.AspNetDocument.FileName, docStr);
		}
		 
		public string BuildDocumentString (DocumentInfo info, TextEditorData data, List<LocalDocumentInfo.OffsetInfo> offsetInfos = null, bool buildExpressions = false)
		{
			var document = new StringBuilder ();
			
			WriteUsings (info.Imports, document);

			foreach (var node in info.XScriptBlocks) {
				var start = data.Document.LocationToOffset (node.Region.Begin.Line, node.Region.Begin.Column) + 2;
				var end = data.Document.LocationToOffset (node.Region.End.Line, node.Region.End.Column) - 2;
				if (offsetInfos != null)
					offsetInfos.Add (new LocalDocumentInfo.OffsetInfo (start, document.Length, end - start));
				
				document.AppendLine (data.Document.GetTextBetween (start, end));
			}
			if (buildExpressions) {
				WriteClassDeclaration (info, document);
				document.AppendLine ("{");
				document.AppendLine ("void Generated ()");
				document.AppendLine ("{");
				//Console.WriteLine ("start:" + location.BeginLine  +"/" +location.BeginColumn);

				foreach (var node in info.XExpressions) {
					bool isBlock = node is AspNetRenderBlock;

					var start = data.Document.LocationToOffset (node.Region.Begin.Line, node.Region.Begin.Column) + 2;
					var end = data.Document.LocationToOffset (node.Region.End.Line, node.Region.End.Column) - 2;
					
					if (!isBlock) {
						document.Append ("WriteLine (");
						start += 1;
					}
					
					string expr = data.GetTextBetween (start, end);
					if (offsetInfos != null) {
						offsetInfos.Add (new LocalDocumentInfo.OffsetInfo (start, document.Length, expr.Length));
					}
					document.Append (expr);
					if (!isBlock)
						document.Append (");");
				}
				document.AppendLine ("}");
				document.AppendLine ("}");
			}
			return document.ToString ();
		}
	}
}
