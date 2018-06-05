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
			Flags |= ParsedDocumentFlags.SkipFoldings;
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

		// Tags are done via Ide.Tasks.CommentTasksProvider.
		public override Task<IReadOnlyList<Tag>> GetTagCommentsAsync (CancellationToken cancellationToken = default(CancellationToken))
		{
			return Task.FromResult<IReadOnlyList<Tag>> (null);
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

		static readonly Task<IReadOnlyList<FoldingRegion>> foldings  = Task.FromResult((IReadOnlyList<FoldingRegion>)new FoldingRegion[0]);

		public override Task<IReadOnlyList<FoldingRegion>> GetFoldingsAsync (CancellationToken cancellationToken = default (CancellationToken)) => foldings;

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

		static readonly IReadOnlyList<Error> emptyErrors = Array.Empty<Error> ();
		public override async Task<IReadOnlyList<Error>> GetErrorsAsync (CancellationToken cancellationToken = default(CancellationToken))
		{
			if (Ide.IdeApp.Preferences.EnableSourceAnalysis)
				return emptyErrors;

			// FIXME: remove this fallback, error squiggles should always be handled via the source analysis mechanism
			#pragma warning disable 618
			var model = GetAst<SemanticModel> ();
			#pragma warning disable 618

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