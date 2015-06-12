// 
// MoveTypeToFile.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory6.CSharp.Refactoring;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MonoDevelop.Core;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide;
using System.Collections.Generic;
using MonoDevelop.Ide.StandardHeader;
using ICSharpCode.NRefactory6.CSharp;

namespace MonoDevelop.CSharp.CodeFixes.MoveTypeToFile
{
	[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = "Move type to file")]
	class MoveTypeToFile : CodeRefactoringProvider
	{
		public async override Task ComputeRefactoringsAsync (CodeRefactoringContext context)
		{
			var document = context.Document;
			var span = context.Span;
			var cancellationToken = context.CancellationToken;

			var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait (false);
			if (model.IsFromGeneratedCode (cancellationToken))
				return;
			var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait (false);
			var token = root.FindToken(span.Start);

			var type = token.Parent as BaseTypeDeclarationSyntax;
			if (type == null)
				return;
			
			if (Path.GetFileNameWithoutExtension (document.FilePath) == type.Identifier.ToString ())
				return;
					
			string title;
			if (IsSingleType (root)) {
				title = String.Format (GettextCatalog.GetString ("Rename file to '{0}'"), Path.GetFileName (GetCorrectFileName (document, type)));
			} else {
				title = String.Format (GettextCatalog.GetString ("Move type to file '{0}'"), Path.GetFileName (GetCorrectFileName (document, type)));
			}
			context.RegisterRefactoring (new MyCodeAction (document, title, root, type));
		}

		class MyCodeAction : CodeAction
		{
			readonly Document document;
			readonly BaseTypeDeclarationSyntax type;
			readonly SyntaxNode root;

			public MyCodeAction (Document document, string title, SyntaxNode root, BaseTypeDeclarationSyntax type)
			{
				this.root = root;
				this.title = title;
				this.type = type;
				this.document = document;

			}

			string title;
			public override string Title {
				get {
					return this.title;
				}
			}

			protected override Task<Document> GetChangedDocumentAsync (System.Threading.CancellationToken cancellationToken)
			{
				var correctFileName = GetCorrectFileName (document, type);
				if (IsSingleType (root)) {
					FileService.RenameFile (document.FilePath, correctFileName);
					var doc = IdeApp.Workbench.ActiveDocument;
					if (doc.HasProject) {
						IdeApp.ProjectOperations.SaveAsync (doc.Project);
					}
					return Task.FromResult (document);
				} 
				return Task.FromResult (CreateNewFile (type, correctFileName));
			}

			Document CreateNewFile (BaseTypeDeclarationSyntax type, string correctFileName)
			{
				var doc = IdeApp.Workbench.ActiveDocument;
				var content = doc.Editor.Text;

				var types = new List<BaseTypeDeclarationSyntax> (
					root
					.DescendantNodesAndSelf (n => !(n is BaseTypeDeclarationSyntax))
					.OfType<BaseTypeDeclarationSyntax> ()
					.Where (t => t.SpanStart != type.SpanStart)
				);
				types.Sort ((x, y) => y.SpanStart.CompareTo (x.SpanStart));

				foreach (var removeType in types) {
					var bounds = CalcTypeBounds (removeType);
					content = content.Remove (bounds.Offset, bounds.Length);
				}

				if (doc.HasProject) {
					string header = StandardHeaderService.GetHeader (doc.Project, correctFileName, true);
					if (!string.IsNullOrEmpty (header))
						content = header + doc.Editor.GetEolMarker () + StripHeader (content);
				}
				content = StripDoubleBlankLines (content);

				File.WriteAllText (correctFileName, content);
				if (doc.HasProject) {
					doc.Project.AddFile (correctFileName);
					IdeApp.ProjectOperations.SaveAsync (doc.Project);
				}

				doc.Editor.RemoveText (CalcTypeBounds (type));

				return document;
			}

			ISegment CalcTypeBounds (BaseTypeDeclarationSyntax type)
			{
				int start = type.Span.Start;
				int end = type.Span.End;
				foreach (var trivia in type.GetLeadingTrivia ()) {
					if (trivia.Kind () == SyntaxKind.SingleLineDocumentationCommentTrivia) {
						start = trivia.FullSpan.Start;
					}
				}

				return TextSegment.FromBounds (start, end);
			}
		}

		static bool IsBlankLine (IReadonlyTextDocument doc, int i)
		{
			var line = doc.GetLine (i);
			return line.Length == line.GetIndentation (doc).Length;
		}

		static string StripDoubleBlankLines (string content)
		{
			var doc = TextEditorFactory.CreateNewDocument (new StringTextSource (content), "a.cs");
			for (int i = 1; i + 1 <= doc.LineCount; i++) {
				if (IsBlankLine (doc, i) && IsBlankLine (doc, i + 1)) {
					doc.RemoveText (doc.GetLine (i).SegmentIncludingDelimiter);
					i--;
					continue;
				}
			}
			return doc.Text;
		}

		static string StripHeader (string content)
		{
			var doc = TextEditorFactory.CreateNewDocument (new StringTextSource (content), "");
			while (true) {
				string lineText = doc.GetLineText (1);
				if (lineText == null)
					break;
				if (lineText.StartsWith ("//", StringComparison.Ordinal)) {
					doc.RemoveText (doc.GetLine (1).SegmentIncludingDelimiter);
					continue;
				}
				break;
			}
			return doc.Text;
		}
		
		static bool IsSingleType (SyntaxNode root)
		{
			return root.DescendantNodesAndSelf (c => !(c is BaseTypeDeclarationSyntax)).OfType<BaseTypeDeclarationSyntax> ().Count () == 1;
		}

		internal static string GetCorrectFileName (Document document, BaseTypeDeclarationSyntax type)
		{
			if (type == null)
				return document.FilePath;
			return Path.Combine (Path.GetDirectoryName (document.FilePath), type.Identifier + Path.GetExtension (document.FilePath));
		}
	}
}