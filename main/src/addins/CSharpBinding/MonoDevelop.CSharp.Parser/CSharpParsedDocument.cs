//
// CSharpParsedDocument.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Ide.TypeSystem;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Ide.Editor;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MonoDevelop.Core;
using MonoDevelop.Core.Text;

namespace MonoDevelop.CSharp.Parser
{
	class CSharpParsedDocument : ParsedDocument
	{
		static string[] tagComments;

		internal SyntaxTree Unit {
			get;
			set;
		}

		static CSharpParsedDocument ()
		{
			UpdateTags ();
			MonoDevelop.Ide.Tasks.CommentTag.SpecialCommentTagsChanged += delegate {
				UpdateTags ();
			};
		}

		static void UpdateTags ()
		{
			tagComments = MonoDevelop.Ide.Tasks.CommentTag.SpecialCommentTags.Select (t => t.Tag).ToArray ();
		}
		bool isAdHocProject;

		public CSharpParsedDocument (Ide.TypeSystem.ParseOptions options,  string fileName) : base (fileName)
		{
			isAdHocProject = options.IsAdhocProject;
		}
		

		#region implemented abstract members of ParsedDocument

		IReadOnlyList<Comment> comments;
		object commentLock = new object ();

		public override Task<IReadOnlyList<Comment>> GetCommentsAsync (CancellationToken cancellationToken = default(CancellationToken))
		{
			if (comments == null) {
				return Task.Run (delegate {
					lock (commentLock) {
						if (comments == null) {
							var visitor = new CommentVisitor (cancellationToken);
							if (Unit != null)
								try {
									visitor.Visit (Unit.GetRoot (cancellationToken));
								} catch (OperationCanceledException) {
								}
							comments = visitor.Comments;
						}
					}
					return comments;
				});
			}
			return Task.FromResult (comments);
		}


		class CommentVisitor : CSharpSyntaxWalker
		{
			public readonly List<Comment> Comments = new List<Comment> ();

			CancellationToken cancellationToken;

			public CommentVisitor (CancellationToken cancellationToken) : base(SyntaxWalkerDepth.Trivia)
			{
				this.cancellationToken = cancellationToken;
			}

			DocumentRegion GetRegion (SyntaxTrivia trivia)
			{
				var fullSpan = trivia.FullSpan;
				if (fullSpan.Length > 2) {
					var text = trivia.SyntaxTree.GetText (cancellationToken);
					if (text [fullSpan.End - 2] == '\r' && text [fullSpan.End - 1] == '\n')
						fullSpan = new Microsoft.CodeAnalysis.Text.TextSpan (fullSpan.Start, fullSpan.Length - 2);
					else if (NewLine.IsNewLine (text [fullSpan.End - 1]))
						fullSpan = new Microsoft.CodeAnalysis.Text.TextSpan (fullSpan.Start, fullSpan.Length - 1);
				}
				try {
					var lineSpan = trivia.SyntaxTree.GetLineSpan (fullSpan);
					return (DocumentRegion)lineSpan;
				} catch (Exception) {
					return DocumentRegion.Empty;
				}
			}

			public override void VisitBlock (BlockSyntax node)
			{
				cancellationToken.ThrowIfCancellationRequested ();
				base.VisitBlock (node);
			}

			bool StartsLine (SyntaxTrivia trivia)
			{
				var sourceText = trivia.SyntaxTree.GetText (cancellationToken);
				Microsoft.CodeAnalysis.Text.TextLine textLine;
				try {
					textLine = sourceText.Lines.GetLineFromPosition (trivia.SpanStart);
				} catch (ArgumentOutOfRangeException) {
					return false;
				}
				//We need start of trivia.FullSpan and not trivia.SpanStart
				//because in case of documentation /// <summary...
				//trivia.SpanStart is space after /// and not 1st /
				//so with trivia.FullSpan.Start we get index of 1st /
				var startSpan = trivia.FullSpan.Start;
				for (int i = textLine.Start; i < startSpan; i++) {
					char ch = sourceText [i];
					if (!char.IsWhiteSpace (ch))
						return false;
				}
				return true;
			}

			string CropStart (SyntaxTrivia trivia, string crop)
			{
				var sourceText = trivia.SyntaxTree.GetText (cancellationToken);
				var span = trivia.Span;
				int i = span.Start;
				int end = span.End;

				// Trim leading whitespace.
				while (char.IsWhiteSpace (sourceText[i]) && i < end)
					i++;
				
				while (char.IsWhiteSpace (sourceText[end - 1]) && end - 1 > i)
					end--;

				// Poor man's allocation-less offset-ed startswith
				int j;
				for (j = 0; j < crop.Length && i < end; ++j)
					if (sourceText[i] == crop[j])
						i++;

				// Go back if we didn't do a full match, else trim leading whitespace again
				if (j != crop.Length)
					i -= j;
				else
					while (char.IsWhiteSpace (sourceText[i]) && i < end)
						i++;

				return sourceText.ToString (new Microsoft.CodeAnalysis.Text.TextSpan (i, end - i));
			}

			public override void VisitTrivia (SyntaxTrivia trivia)
			{
				cancellationToken.ThrowIfCancellationRequested ();
				base.VisitTrivia (trivia);
				switch (trivia.Kind ()) {
				case SyntaxKind.MultiLineCommentTrivia:
				case SyntaxKind.MultiLineDocumentationCommentTrivia:
					{
						var cmt = new Comment (CropStart (trivia, "/*"));
						cmt.CommentStartsLine = StartsLine(trivia);
						cmt.CommentType = CommentType.Block;
						cmt.OpenTag = "/*";
						cmt.ClosingTag = "*/";
						cmt.Region = GetRegion (trivia);
						Comments.Add (cmt);
						break;
					}
				case SyntaxKind.SingleLineCommentTrivia:
					{
						var cmt = new Comment (CropStart (trivia, "//"));
						cmt.CommentStartsLine = StartsLine(trivia);
						cmt.CommentType = CommentType.SingleLine;
						cmt.OpenTag = "//";
						cmt.Region = GetRegion (trivia);
						Comments.Add (cmt);
						break;
					}
				case SyntaxKind.SingleLineDocumentationCommentTrivia:
					{
						var cmt = new Comment (CropStart (trivia, "///"));
						cmt.CommentStartsLine = StartsLine(trivia);
						cmt.IsDocumentation = true;
						cmt.CommentType = CommentType.Documentation;
						cmt.OpenTag = "///";
						cmt.ClosingTag = "///";
						cmt.Region = GetRegion (trivia);
						Comments.Add (cmt);
						break;
					}

				}

			}
		}

		IReadOnlyList<Tag> tags;
		object tagLock = new object ();
		public override Task<IReadOnlyList<Tag>> GetTagCommentsAsync (CancellationToken cancellationToken = default(CancellationToken))
		{
			if (tags == null) {
				return Task.Run (delegate {
					lock (tagLock) {
						if (tags == null) {
							var visitor = new SemanticTagVisitor (cancellationToken);
							if (Unit != null) {
								try {
									visitor.Visit (Unit.GetRoot (cancellationToken));
								} catch {
								}
							}
							tags = visitor.Tags;
						}
						return tags;
					}
				});
			}
			return Task.FromResult (tags);
		}

		sealed class SemanticTagVisitor : CSharpSyntaxWalker
		{
			public List<Tag> Tags =  new List<Tag> ();
			CancellationToken cancellationToken;

			public SemanticTagVisitor () : base (SyntaxWalkerDepth.Trivia)
			{
			}

			public SemanticTagVisitor (CancellationToken cancellationToken) : base (SyntaxWalkerDepth.Trivia)
			{
				this.cancellationToken = cancellationToken;
			}

			public override void VisitBlock (BlockSyntax node)
			{
				cancellationToken.ThrowIfCancellationRequested ();
				base.VisitBlock (node);
			}

			public override void VisitTrivia (SyntaxTrivia trivia)
			{
				cancellationToken.ThrowIfCancellationRequested ();
				if (trivia.IsKind (SyntaxKind.SingleLineCommentTrivia) || 
					trivia.IsKind (SyntaxKind.MultiLineCommentTrivia) || 
					trivia.IsKind (SyntaxKind.SingleLineDocumentationCommentTrivia)) {
					var trimmedContent = trivia.ToString ().TrimStart ('/', ' ', '*');
					foreach (string tag in tagComments) {
						if (!trimmedContent.StartsWith (tag, StringComparison.Ordinal))
							continue;
						var loc = trivia.GetLocation ().GetLineSpan ();
						Tags.Add (new Tag (tag, trimmedContent, new DocumentRegion (loc.StartLinePosition, loc.EndLinePosition)));
						break;
					}
				}
			}

			public override void VisitThrowStatement (Microsoft.CodeAnalysis.CSharp.Syntax.ThrowStatementSyntax node)
			{
				cancellationToken.ThrowIfCancellationRequested ();
				base.VisitThrowStatement (node);
				var createExpression = node.Expression as ObjectCreationExpressionSyntax;
				if (createExpression == null)
					return;
				var st = createExpression.Type.ToString ();
				if (st == "NotImplementedException" || st == "System.NotImplementedException") {
					var loc = node.GetLocation ().GetLineSpan ();
					if (createExpression.ArgumentList.Arguments.Count > 0) {
						Tags.Add (new Tag ("High", GettextCatalog.GetString ("NotImplementedException({0}) thrown.", createExpression.ArgumentList.Arguments.First ().ToString ()), new DocumentRegion (loc.StartLinePosition, loc.EndLinePosition)));
					} else {
						Tags.Add (new Tag ("High", GettextCatalog.GetString ("NotImplementedException thrown."), new DocumentRegion (loc.StartLinePosition, loc.EndLinePosition)));
					}
				}
			}
		}

		IReadOnlyList<FoldingRegion> foldings;
		SemaphoreSlim foldingsSemaphore = new SemaphoreSlim (1, 1);

		public override Task<IReadOnlyList<FoldingRegion>> GetFoldingsAsync (CancellationToken cancellationToken = default(CancellationToken))
		{
			if (foldings == null) {
				return Task.Run (async delegate {
					bool locked = false;
					try {
						locked = await foldingsSemaphore.WaitAsync (Timeout.Infinite, cancellationToken);
						if (foldings == null)
							foldings = (await GenerateFoldings (cancellationToken)).ToList ();
					} catch (OperationCanceledException) {
						return new List<FoldingRegion> ();
					} finally {
						if (locked)
							foldingsSemaphore.Release ();
					}
					return foldings;
				});
			}

			return Task.FromResult (foldings);
		}

		async Task<IEnumerable<FoldingRegion>> GenerateFoldings (CancellationToken cancellationToken)
		{
			return GenerateFoldingsInternal (await GetCommentsAsync (cancellationToken), cancellationToken);
		}

		IEnumerable<FoldingRegion> GenerateFoldingsInternal (IReadOnlyList<Comment> comments, CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
				yield break;

			foreach (var fold in comments.ToFolds ())
				yield return fold;

			if (cancellationToken.IsCancellationRequested)
				yield break;

			var visitor = new FoldingVisitor (cancellationToken);
			if (Unit != null) {
				try {
					visitor.Visit (Unit.GetRoot (cancellationToken));
				} catch (Exception) { }
			}

			if (cancellationToken.IsCancellationRequested)
				yield break;
			foreach (var fold in visitor.Foldings)
				yield return fold;
		}

		class FoldingVisitor : CSharpSyntaxWalker
		{
			public readonly List<FoldingRegion> Foldings = new List<FoldingRegion> ();
			CancellationToken cancellationToken;

			public FoldingVisitor (CancellationToken cancellationToken) : base(SyntaxWalkerDepth.Trivia)
			{
				this.cancellationToken = cancellationToken;
			}

			void AddUsings (SyntaxNode parent)
			{
				SyntaxNode firstChild = null, lastChild = null;
				foreach (var child in parent.ChildNodes ()) {
					cancellationToken.ThrowIfCancellationRequested ();
					if (child is UsingDirectiveSyntax) {
						if (firstChild == null) {
							firstChild = child;
						}
						lastChild = child;
						continue;
					}
					if (firstChild != null)
						break;
				}

				if (firstChild != null && firstChild != lastChild) {
					var first = firstChild.GetLocation ().GetLineSpan ();
					var last = lastChild.GetLocation ().GetLineSpan ();

					Foldings.Add (new FoldingRegion (new DocumentRegion (first.StartLinePosition, last.EndLinePosition), FoldType.Undefined));
				}
			}

			public override void VisitCompilationUnit (Microsoft.CodeAnalysis.CSharp.Syntax.CompilationUnitSyntax node)
			{
				cancellationToken.ThrowIfCancellationRequested ();
				AddUsings (node);
				base.VisitCompilationUnit (node);
			}

			void AddFolding (SyntaxToken openBrace, SyntaxToken closeBrace, FoldType type)
			{
				cancellationToken.ThrowIfCancellationRequested ();
				openBrace = openBrace.GetPreviousToken (false, false, true, true);

				try {
					var first = openBrace.GetLocation ().GetLineSpan ();
					var last = closeBrace.GetLocation ().GetLineSpan ();

					if (first.EndLinePosition.Line != last.EndLinePosition.Line)
						Foldings.Add (new FoldingRegion (new DocumentRegion (first.EndLinePosition, last.EndLinePosition), type));
				} catch (ArgumentOutOfRangeException) {}
			}

			Stack<SyntaxTrivia> regionStack = new Stack<SyntaxTrivia> ();
			public override void VisitTrivia (SyntaxTrivia trivia)
			{
				cancellationToken.ThrowIfCancellationRequested ();
				base.VisitTrivia (trivia);
				if (trivia.IsKind (SyntaxKind.RegionDirectiveTrivia)) {
					regionStack.Push (trivia);
				} else if (trivia.IsKind (SyntaxKind.EndRegionDirectiveTrivia)) {
					if (regionStack.Count == 0)
						return;
					var regionStart = regionStack.Pop ();
					try {
						var first = regionStart.GetLocation ().GetLineSpan ();
						var last = trivia.GetLocation ().GetLineSpan ();
						var v = regionStart.ToString ();
						v = v.Substring ("#region".Length).Trim ();
						if (v.Length == 0)
							v = "...";
						Foldings.Add (new FoldingRegion(v, new DocumentRegion(first.StartLinePosition, last.EndLinePosition), FoldType.UserRegion, true));
					} catch (ArgumentOutOfRangeException) { }
				}
			}

			public override void VisitNamespaceDeclaration (Microsoft.CodeAnalysis.CSharp.Syntax.NamespaceDeclarationSyntax node)
			{
				cancellationToken.ThrowIfCancellationRequested ();
				AddUsings (node);
				AddFolding (node.OpenBraceToken, node.CloseBraceToken, FoldType.Undefined);
				base.VisitNamespaceDeclaration (node);
			}

			public override void VisitClassDeclaration (Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax node)
			{
				cancellationToken.ThrowIfCancellationRequested ();
				AddFolding (node.OpenBraceToken, node.CloseBraceToken, FoldType.Type);
				base.VisitClassDeclaration (node);
			}

			public override void VisitStructDeclaration (Microsoft.CodeAnalysis.CSharp.Syntax.StructDeclarationSyntax node)
			{
				cancellationToken.ThrowIfCancellationRequested ();
				AddFolding (node.OpenBraceToken, node.CloseBraceToken, FoldType.Type);
				base.VisitStructDeclaration (node);
			}

			public override void VisitInterfaceDeclaration (Microsoft.CodeAnalysis.CSharp.Syntax.InterfaceDeclarationSyntax node)
			{
				cancellationToken.ThrowIfCancellationRequested ();
				AddFolding (node.OpenBraceToken, node.CloseBraceToken, FoldType.Type);
				base.VisitInterfaceDeclaration (node);
			}

			public override void VisitEnumDeclaration (Microsoft.CodeAnalysis.CSharp.Syntax.EnumDeclarationSyntax node)
			{
				cancellationToken.ThrowIfCancellationRequested ();
				AddFolding (node.OpenBraceToken, node.CloseBraceToken, FoldType.Type);
				base.VisitEnumDeclaration (node);
			}

			public override void VisitBlock (Microsoft.CodeAnalysis.CSharp.Syntax.BlockSyntax node)
			{
				cancellationToken.ThrowIfCancellationRequested ();
				AddFolding (node.OpenBraceToken, node.CloseBraceToken, node.Parent is MemberDeclarationSyntax ? FoldType.Member : FoldType.Undefined);
				base.VisitBlock (node);
			}
		}

		static readonly IReadOnlyList<Error> emptyErrors = new Error[0];

		SemaphoreSlim errorLock = new SemaphoreSlim (1, 1);

		static string [] lexicalError = {
			"CS0594", // ERR_FloatOverflow
			"CS0595", // ERR_InvalidReal
			"CS1009", // ERR_IllegalEscape
			"CS1010", // ERR_NewlineInConst
			"CS1011", // ERR_EmptyCharConst
			"CS1012", // ERR_TooManyCharsInConst
			"CS1015", // ERR_TypeExpected
			"CS1021", // ERR_IntOverflow
			"CS1032", // ERR_PPDefFollowsTokenpp
			"CS1035", // ERR_OpenEndedComment
			"CS1039", // ERR_UnterminatedStringLit
			"CS1040", // ERR_BadDirectivePlacementpp
			"CS1056", // ERR_UnexpectedCharacter
			"CS1056", // ERR_UnexpectedCharacter_EscapedBackslash
			"CS1646", // ERR_ExpectedVerbatimLiteral
			"CS0078", // WRN_LowercaseEllSuffix
			"CS1002", // ; expected
			"CS1519", // Invalid token ';' in class, struct, or interface member declaration
			"CS1031", // Type expected
			"CS0106", // The modifier 'readonly' is not valid for this item
			"CS1576", // The line number specified for #line directive is missing or invalid
			"CS1513" // } expected
		};

		static bool SkipError (bool isAdhocProject, string errorId)
		{
			return isAdhocProject && !lexicalError.Contains (errorId);
		}

		public override async Task<IReadOnlyList<Error>> GetErrorsAsync (CancellationToken cancellationToken = default(CancellationToken))
		{
			var model = GetAst<SemanticModel> ();
			if (model == null)
				return emptyErrors;

			bool locked = await errorLock.WaitAsync (Timeout.Infinite, cancellationToken).ConfigureAwait (false);
			IReadOnlyList<Error> errors;
			try {
				try {
					errors = model
						.GetDiagnostics (null, cancellationToken)
						.Where (diag => !SkipError(isAdHocProject, diag.Id) && (diag.Severity == DiagnosticSeverity.Error || diag.Severity == DiagnosticSeverity.Warning))
						.Select ((Diagnostic diag) => new Error (GetErrorType (diag.Severity), diag.Id, diag.GetMessage (), GetRegion (diag)) { Tag = diag })
						.ToList ();
				} catch (OperationCanceledException) {
					errors = emptyErrors;
				} catch (Exception e) {
					LoggingService.LogError ("Error while getting diagnostics.", e);
					errors = emptyErrors;
				}
			} finally {
				if (locked)
					errorLock.Release ();			}
			
			return errors;
		}

		static DocumentRegion GetRegion (Diagnostic diagnostic)
		{
			try {
				var lineSpan = diagnostic.Location.GetLineSpan ();
				return new DocumentRegion (lineSpan.StartLinePosition, lineSpan.EndLinePosition);
			} catch (Exception) {
				return DocumentRegion.Empty;
			}
		}

		static ErrorType GetErrorType (DiagnosticSeverity severity)
		{
			switch (severity) {
			case DiagnosticSeverity.Error:
				return ErrorType.Error;
			case DiagnosticSeverity.Warning:
				return ErrorType.Warning;
			}
			return ErrorType.Unknown;
		}

		#endregion
	}
}