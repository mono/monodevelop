//
// SyntaxTreeExtensions.cs
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
using Microsoft.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Text;

namespace ICSharpCode.NRefactory6.CSharp
{
	public static class SyntaxTreeExtensions
	{
//		/// <summary>
//		/// Returns the identifier, keyword, contextual keyword or preprocessor keyword touching this
//		/// position, or a token of Kind = None if the caret is not touching either.
//		/// </summary>
//		public static SyntaxToken GetTouchingWord(
//			this SyntaxTree syntaxTree,
//			int position,
//			ISyntaxFactsService syntaxFacts,
//			CancellationToken cancellationToken,
//			bool findInsideTrivia = false)
//		{
//			return GetTouchingToken(syntaxTree, position, syntaxFacts.IsWord, cancellationToken, findInsideTrivia);
//		}

		public static SyntaxToken GetTouchingToken(
			this SyntaxTree syntaxTree,
			int position,
			CancellationToken cancellationToken,
			bool findInsideTrivia = false)
		{
			return GetTouchingToken(syntaxTree, position, _ => true, cancellationToken, findInsideTrivia);
		}

		public static SyntaxToken GetTouchingToken(
			this SyntaxTree syntaxTree,
			int position,
			Predicate<SyntaxToken> predicate,
			CancellationToken cancellationToken,
			bool findInsideTrivia = false)
		{
			// Contract.ThrowIfNull(syntaxTree);

			if (position >= syntaxTree.Length)
			{
				return default(SyntaxToken);
			}

			var token = syntaxTree.GetRoot(cancellationToken).FindToken(position, findInsideTrivia);

			if ((token.Span.Contains(position) || token.Span.End == position) && predicate(token))
			{
				return token;
			}

			token = token.GetPreviousToken();

			if (token.Span.End == position && predicate(token))
			{
				return token;
			}

			// SyntaxKind = None
			return default(SyntaxToken);
		}

		public static bool OverlapsHiddenPosition(this SyntaxTree tree, TextSpan span, CancellationToken cancellationToken)
		{
			if (tree == null)
			{
				return false;
			}

			var text = tree.GetText(cancellationToken);

			return text.OverlapsHiddenPosition(span, (position, cancellationToken2) =>
				{
					// implements the ASP.Net IsHidden rule
					var lineVisibility = tree.GetLineVisibility(position, cancellationToken2);
					return lineVisibility == LineVisibility.Hidden || lineVisibility == LineVisibility.BeforeFirstLineDirective;
				},
				cancellationToken);
		}

		public static bool IsEntirelyHidden(this SyntaxTree tree, TextSpan span, CancellationToken cancellationToken)
		{
			if (!tree.HasHiddenRegions())
			{
				return false;
			}

			var text = tree.GetText(cancellationToken);
			var startLineNumber = text.Lines.IndexOf(span.Start);
			var endLineNumber = text.Lines.IndexOf(span.End);

			for (var lineNumber = startLineNumber; lineNumber <= endLineNumber; lineNumber++)
			{
				cancellationToken.ThrowIfCancellationRequested();

				var linePosition = text.Lines[lineNumber].Start;
				if (!tree.IsHiddenPosition(linePosition, cancellationToken))
				{
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Returns <c>true</c> if the provided position is in a hidden region inaccessible to the user.
		/// </summary>
		public static bool IsHiddenPosition(this SyntaxTree tree, int position, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!tree.HasHiddenRegions())
			{
				return false;
			}

			var lineVisibility = tree.GetLineVisibility(position, cancellationToken);
			return lineVisibility == LineVisibility.Hidden || lineVisibility == LineVisibility.BeforeFirstLineDirective;
		}

		public static bool IsInteractiveOrScript(this SyntaxTree syntaxTree)
		{
			return syntaxTree.Options.Kind != SourceCodeKind.Regular;
		}

		public static ISet<SyntaxKind> GetPrecedingModifiers(
			this SyntaxTree syntaxTree, int position, SyntaxToken tokenOnLeftOfPosition, CancellationToken cancellationToken)
		{
			var token = tokenOnLeftOfPosition;
			token = token.GetPreviousTokenIfTouchingWord(position);

			var result = new HashSet<SyntaxKind>(SyntaxFacts.EqualityComparer);
			while (true)
			{
				switch (token.Kind())
				{
				case SyntaxKind.PublicKeyword:
				case SyntaxKind.InternalKeyword:
				case SyntaxKind.ProtectedKeyword:
				case SyntaxKind.PrivateKeyword:
				case SyntaxKind.SealedKeyword:
				case SyntaxKind.AbstractKeyword:
				case SyntaxKind.StaticKeyword:
				case SyntaxKind.VirtualKeyword:
				case SyntaxKind.ExternKeyword:
				case SyntaxKind.NewKeyword:
				case SyntaxKind.OverrideKeyword:
				case SyntaxKind.ReadOnlyKeyword:
				case SyntaxKind.VolatileKeyword:
				case SyntaxKind.UnsafeKeyword:
				case SyntaxKind.AsyncKeyword:
					result.Add(token.Kind());
					token = token.GetPreviousToken(includeSkipped: true);
					continue;
				case SyntaxKind.IdentifierToken:
					if (token.HasMatchingText(SyntaxKind.AsyncKeyword))
					{
						result.Add(SyntaxKind.AsyncKeyword);
						token = token.GetPreviousToken(includeSkipped: true);
						continue;
					}

					break;
				}

				break;
			}

			return result;
		}

		public static TypeDeclarationSyntax GetContainingTypeDeclaration(
			this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
		{
			return syntaxTree.GetContainingTypeDeclarations(position, cancellationToken).FirstOrDefault();
		}

		public static BaseTypeDeclarationSyntax GetContainingTypeOrEnumDeclaration(
			this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
		{
			return syntaxTree.GetContainingTypeOrEnumDeclarations(position, cancellationToken).FirstOrDefault();
		}

		public static IEnumerable<TypeDeclarationSyntax> GetContainingTypeDeclarations(
			this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
		{
			var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);

			return token.GetAncestors<TypeDeclarationSyntax>().Where(t =>
				{
					return BaseTypeDeclarationContainsPosition(t, position);
				});
		}

		private static bool BaseTypeDeclarationContainsPosition(BaseTypeDeclarationSyntax declaration, int position)
		{
			if (position <= declaration.OpenBraceToken.SpanStart)
			{
				return false;
			}

			if (declaration.CloseBraceToken.IsMissing)
			{
				return true;
			}

			return position <= declaration.CloseBraceToken.SpanStart;
		}

		public static IEnumerable<BaseTypeDeclarationSyntax> GetContainingTypeOrEnumDeclarations(
			this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
		{
			var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);

			return token.GetAncestors<BaseTypeDeclarationSyntax>().Where(t => BaseTypeDeclarationContainsPosition(t, position));
		}

//		/// <summary>
//		/// If the position is inside of token, return that token; otherwise, return the token to the right.
//		/// </summary>
//		public static SyntaxToken FindTokenOnRightOfPosition(
//			this SyntaxTree syntaxTree,
//			int position,
//			CancellationToken cancellationToken,
//			bool includeSkipped = true,
//			bool includeDirectives = false,
//			bool includeDocumentationComments = false)
//		{
//			return syntaxTree.GetRoot(cancellationToken).FindTokenOnRightOfPosition(
//				position, includeSkipped, includeDirectives, includeDocumentationComments);
//		}

		/// <summary>
		/// If the position is inside of token, return that token; otherwise, return the token to the left.
		/// </summary>
		public static SyntaxToken FindTokenOnLeftOfPosition(
			this SyntaxTree syntaxTree,
			int position,
			CancellationToken cancellationToken,
			bool includeSkipped = true,
			bool includeDirectives = false,
			bool includeDocumentationComments = false)
		{
			return syntaxTree.GetRoot(cancellationToken).FindTokenOnLeftOfPosition(
				position, includeSkipped, includeDirectives, includeDocumentationComments);
		}

		private static readonly Func<SyntaxKind, bool> s_isDotOrArrow = k => k == SyntaxKind.DotToken || k == SyntaxKind.MinusGreaterThanToken;
		private static readonly Func<SyntaxKind, bool> s_isDotOrArrowOrColonColon =
			k => k == SyntaxKind.DotToken || k == SyntaxKind.MinusGreaterThanToken || k == SyntaxKind.ColonColonToken;

		public static bool IsNamespaceDeclarationNameContext(this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
		{
			var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);
			var namespaceName = token.GetAncestor<NamespaceDeclarationSyntax>();
			if (namespaceName == null)
			{
				return false;
			}

			return namespaceName.Name.Span.IntersectsWith(position);
		}

		public static bool IsRightOfDotOrArrowOrColonColon(this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
		{
			return syntaxTree.IsRightOf(position, s_isDotOrArrowOrColonColon, cancellationToken);
		}

		public static bool IsRightOfDotOrArrow(this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
		{
			return syntaxTree.IsRightOf(position, s_isDotOrArrow, cancellationToken);
		}

		private static bool IsRightOf(
			this SyntaxTree syntaxTree, int position, Func<SyntaxKind, bool> predicate, CancellationToken cancellationToken)
		{
			var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);
			token = token.GetPreviousTokenIfTouchingWord(position);

			if (token.Kind() == SyntaxKind.None)
			{
				return false;
			}

			return predicate(token.Kind());
		}

		public static bool IsRightOfNumericLiteral(this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
		{
			var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);
			return token.Kind() == SyntaxKind.NumericLiteralToken;
		}

		public static bool IsPrimaryFunctionExpressionContext(this SyntaxTree syntaxTree, int position, SyntaxToken tokenOnLeftOfPosition, CancellationToken cancellationToken)
		{
			return
				syntaxTree.IsTypeOfExpressionContext(position, tokenOnLeftOfPosition, cancellationToken) ||
				syntaxTree.IsDefaultExpressionContext(position, tokenOnLeftOfPosition, cancellationToken) ||
				syntaxTree.IsSizeOfExpressionContext(position, tokenOnLeftOfPosition, cancellationToken);
		}

		public static bool IsAfterKeyword(this SyntaxTree syntaxTree, int position, SyntaxKind kind, CancellationToken cancellationToken)
		{
			var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);
			token = token.GetPreviousTokenIfTouchingWord(position);

			return token.Kind() == kind;
		}

		public static bool IsInNonUserCode(this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
		{
			return
				syntaxTree.IsEntirelyWithinNonUserCodeComment(position, cancellationToken) ||
				syntaxTree.IsEntirelyWithinStringOrCharLiteral(position, cancellationToken) ||
				syntaxTree.IsInInactiveRegion(position, cancellationToken);
		}

		public static bool IsEntirelyWithinNonUserCodeComment(this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
		{
			var inNonUserSingleLineDocComment =
				syntaxTree.IsEntirelyWithinSingleLineDocComment(position, cancellationToken) && !syntaxTree.IsEntirelyWithinCrefSyntax(position, cancellationToken);
			return
				syntaxTree.IsEntirelyWithinTopLevelSingleLineComment(position, cancellationToken) ||
				syntaxTree.IsEntirelyWithinPreProcessorSingleLineComment(position, cancellationToken) ||
				syntaxTree.IsEntirelyWithinMultiLineComment(position, cancellationToken) ||
				syntaxTree.IsEntirelyWithinMultiLineDocComment(position, cancellationToken) ||
				inNonUserSingleLineDocComment;
		}

		public static bool IsEntirelyWithinComment(this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
		{
			return
				syntaxTree.IsEntirelyWithinTopLevelSingleLineComment(position, cancellationToken) ||
				syntaxTree.IsEntirelyWithinPreProcessorSingleLineComment(position, cancellationToken) ||
				syntaxTree.IsEntirelyWithinMultiLineComment(position, cancellationToken) ||
				syntaxTree.IsEntirelyWithinMultiLineDocComment(position, cancellationToken) ||
				syntaxTree.IsEntirelyWithinSingleLineDocComment(position, cancellationToken);
		}

		public static bool IsCrefContext(this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
		{
			var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken, includeDocumentationComments: true);
			token = token.GetPreviousTokenIfTouchingWord(position);

			if (token.Parent is XmlCrefAttributeSyntax)
			{
				var attribute = (XmlCrefAttributeSyntax)token.Parent;
				return token == attribute.StartQuoteToken;
			}

			return false;
		}

		public static bool IsEntirelyWithinCrefSyntax(this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
		{
			if (syntaxTree.IsCrefContext(position, cancellationToken))
			{
				return true;
			}

			var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken, includeDocumentationComments: true);
			return token.GetAncestor<CrefSyntax>() != null;
		}

		public static bool IsEntirelyWithinSingleLineDocComment(
			this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
		{
			var root = syntaxTree.GetRoot(cancellationToken) as CompilationUnitSyntax;
			var trivia = root.FindTrivia(position);

			// If we ask right at the end of the file, we'll get back nothing.
			// So move back in that case and ask again.
			var eofPosition = root.FullWidth();
			if (position == eofPosition)
			{
				var eof = root.EndOfFileToken;
				if (eof.HasLeadingTrivia)
				{
					trivia = eof.LeadingTrivia.Last();
				}
			}

			if (trivia.IsSingleLineDocComment())
			{
				var span = trivia.Span;
				var fullSpan = trivia.FullSpan;
				var endsWithNewLine = trivia.GetStructure().GetLastToken(includeSkipped: true).Kind() == SyntaxKind.XmlTextLiteralNewLineToken;

				if (endsWithNewLine)
				{
					if (position > fullSpan.Start && position < fullSpan.End)
					{
						return true;
					}
				}
				else
				{
					if (position > fullSpan.Start && position <= fullSpan.End)
					{
						return true;
					}
				}
			}

			return false;
		}

		public static bool IsEntirelyWithinMultiLineDocComment(
			this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
		{
			var trivia = syntaxTree.GetRoot(cancellationToken).FindTrivia(position);

			if (trivia.IsMultiLineDocComment())
			{
				var span = trivia.FullSpan;

				if (position > span.Start && position < span.End)
				{
					return true;
				}
			}

			return false;
		}

		public static bool IsEntirelyWithinMultiLineComment(
			this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
		{
			var trivia = FindTriviaAndAdjustForEndOfFile(syntaxTree, position, cancellationToken);

			if (trivia.IsMultiLineComment())
			{
				var span = trivia.FullSpan;

				return trivia.IsCompleteMultiLineComment()
					? position > span.Start && position < span.End
						: position > span.Start && position <= span.End;
			}

			return false;
		}

		public static bool IsEntirelyWithinTopLevelSingleLineComment(
			this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
		{
			var trivia = FindTriviaAndAdjustForEndOfFile(syntaxTree, position, cancellationToken);

			if (trivia.Kind() == SyntaxKind.EndOfLineTrivia)
			{
				// Check if we're on the newline right at the end of a comment
				trivia = trivia.GetPreviousTrivia(syntaxTree, cancellationToken);
			}

			if (trivia.IsSingleLineComment())
			{
				var span = trivia.FullSpan;

				if (position > span.Start && position <= span.End)
				{
					return true;
				}
			}

			return false;
		}

		public static bool IsEntirelyWithinPreProcessorSingleLineComment(
			this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
		{
			// Search inside trivia for directives to ensure that we recognize
			// single-line comments at the end of preprocessor directives.
			var trivia = FindTriviaAndAdjustForEndOfFile(syntaxTree, position, cancellationToken, findInsideTrivia: true);

			if (trivia.Kind() == SyntaxKind.EndOfLineTrivia)
			{
				// Check if we're on the newline right at the end of a comment
				trivia = trivia.GetPreviousTrivia(syntaxTree, cancellationToken, findInsideTrivia: true);
			}

			if (trivia.IsSingleLineComment())
			{
				var span = trivia.FullSpan;

				if (position > span.Start && position <= span.End)
				{
					return true;
				}
			}

			return false;
		}

		private static SyntaxTrivia FindTriviaAndAdjustForEndOfFile(
			SyntaxTree syntaxTree, int position, CancellationToken cancellationToken, bool findInsideTrivia = false)
		{
			var root = syntaxTree.GetRoot(cancellationToken) as CompilationUnitSyntax;
			var trivia = root.FindTrivia(position, findInsideTrivia);

			// If we ask right at the end of the file, we'll get back nothing.
			// We handle that case specially for now, though SyntaxTree.FindTrivia should
			// work at the end of a file.
			if (position == root.FullWidth())
			{
				var endOfFileToken = root.EndOfFileToken;
				if (endOfFileToken.HasLeadingTrivia)
				{
					trivia = endOfFileToken.LeadingTrivia.Last();
				}
				else
				{
					var token = endOfFileToken.GetPreviousToken(includeSkipped: true);
					if (token.HasTrailingTrivia)
					{
						trivia = token.TrailingTrivia.Last();
					}
				}
			}

			return trivia;
		}

		private static bool AtEndOfIncompleteStringOrCharLiteral(SyntaxToken token, int position, char lastChar)
		{
			if (!token.IsKind(SyntaxKind.StringLiteralToken, SyntaxKind.CharacterLiteralToken))
			{
				throw new ArgumentException("Expected string or char literal.", "token");
			}

			int startLength = 1;
			if (token.IsVerbatimStringLiteral())
			{
				startLength = 2;
			}

			return position == token.Span.End &&
				(token.Span.Length == startLength || (token.Span.Length > startLength && token.ToString().Cast<char>().LastOrDefault() != lastChar));
		}

		public static bool IsEntirelyWithinStringOrCharLiteral(
			this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
		{
			return
				syntaxTree.IsEntirelyWithinStringLiteral(position, cancellationToken) ||
				syntaxTree.IsEntirelyWithinCharLiteral(position, cancellationToken);
		}

		public static bool IsEntirelyWithinStringLiteral(
			this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
		{
			var token = syntaxTree.GetRoot(cancellationToken).FindToken(position, findInsideTrivia: true);

			// If we ask right at the end of the file, we'll get back nothing. We handle that case
			// specially for now, though SyntaxTree.FindToken should work at the end of a file.
			if (token.IsKind(SyntaxKind.EndOfDirectiveToken, SyntaxKind.EndOfFileToken))
			{
				token = token.GetPreviousToken(includeSkipped: true, includeDirectives: true);
			}

			if (token.IsKind(SyntaxKind.StringLiteralToken))
			{
				var span = token.Span;

				// cases:
				// "|"
				// "|  (e.g. incomplete string literal)
				return (position > span.Start && position < span.End)
					|| AtEndOfIncompleteStringOrCharLiteral(token, position, '"');
			}

			// TODO: Uncomment InterpolatedStringTextToken on roslyn update !!!
			if (token.IsKind(SyntaxKind.InterpolatedStringStartToken, /* SyntaxKind.InterpolatedStringTextToken, */SyntaxKind.InterpolatedStringEndToken))
			{
				return token.SpanStart < position && token.Span.End > position;
			}

			return false;
		}

		public static bool IsEntirelyWithinCharLiteral(
			this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
		{
			var root = syntaxTree.GetRoot(cancellationToken) as CompilationUnitSyntax;
			var token = root.FindToken(position, findInsideTrivia: true);

			// If we ask right at the end of the file, we'll get back nothing.
			// We handle that case specially for now, though SyntaxTree.FindToken should
			// work at the end of a file.
			if (position == root.FullWidth())
			{
				token = root.EndOfFileToken.GetPreviousToken(includeSkipped: true, includeDirectives: true);
			}

			if (token.Kind() == SyntaxKind.CharacterLiteralToken)
			{
				var span = token.Span;

				// cases:
				// '|'
				// '|  (e.g. incomplete char literal)
				return (position > span.Start && position < span.End)
					|| AtEndOfIncompleteStringOrCharLiteral(token, position, '\'');
			}

			return false;
		}

		public static bool IsInInactiveRegion(
			this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
		{
			// Contract.ThrowIfNull(syntaxTree);

			// cases:
			// $ is EOF

			// #if false
			//    |

			// #if false
			//    |$

			// #if false
			// |

			// #if false
			// |$

			if (syntaxTree.IsPreProcessorKeywordContext(position, cancellationToken))
			{
				return false;
			}

			// The latter two are the hard cases we don't actually have an 
			// DisabledTextTrivia yet. 
			var trivia = syntaxTree.GetRoot(cancellationToken).FindTrivia(position, findInsideTrivia: false);
			if (trivia.Kind() == SyntaxKind.DisabledTextTrivia)
			{
				return true;
			}

			var token = syntaxTree.FindTokenOrEndToken(position, cancellationToken);
			var text = syntaxTree.GetText(cancellationToken);
			var lineContainingPosition = text.Lines.IndexOf(position);
			if (token.Kind() == SyntaxKind.EndOfFileToken)
			{
				var triviaList = token.LeadingTrivia;
				foreach (var triviaTok in triviaList.Reverse())
				{
					if (triviaTok.HasStructure)
					{
						var structure = triviaTok.GetStructure();
						if (structure is BranchingDirectiveTriviaSyntax)
						{
							var triviaLine = text.Lines.IndexOf(triviaTok.SpanStart);
							if (triviaLine < lineContainingPosition)
							{
								var branch = (BranchingDirectiveTriviaSyntax)structure;
								return !branch.IsActive || !branch.BranchTaken;
							}
						}
					}
				}
			}

			return false;
		}

		public static bool IsBeforeFirstToken(
			this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
		{
			var firstToken = syntaxTree.GetRoot(cancellationToken).GetFirstToken(includeZeroWidth: true, includeSkipped: true);

			return position <= firstToken.SpanStart;
		}

		public static SyntaxToken FindTokenOrEndToken(
			this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
		{
			// Contract.ThrowIfNull(syntaxTree);

			var root = syntaxTree.GetRoot(cancellationToken) as CompilationUnitSyntax;
			var result = root.FindToken(position, findInsideTrivia: true);
			if (result.Kind() != SyntaxKind.None)
			{
				return result;
			}

			// Special cases.  See if we're actually at the end of a:
			// a) doc comment
			// b) pp directive
			// c) file

			var triviaList = root.EndOfFileToken.LeadingTrivia;
			foreach (var trivia in triviaList.Reverse())
			{
				if (trivia.HasStructure)
				{
					var token = trivia.GetStructure().GetLastToken(includeZeroWidth: true);
					if (token.Span.End == position)
					{
						return token;
					}
				}
			}

			if (position == root.FullSpan.End)
			{
				return root.EndOfFileToken;
			}

			return default(SyntaxToken);
		}

		public static IList<MemberDeclarationSyntax> GetMembersInSpan(
			this SyntaxTree syntaxTree,
			TextSpan textSpan,
			CancellationToken cancellationToken)
		{
			var token = syntaxTree.GetRoot(cancellationToken).FindToken(textSpan.Start);
			var firstMember = token.GetAncestors<MemberDeclarationSyntax>().FirstOrDefault();
			if (firstMember != null)
			{
				var containingType = firstMember.Parent as TypeDeclarationSyntax;
				if (containingType != null)
				{
					var members = GetMembersInSpan(textSpan, containingType, firstMember);
					if (members != null)
					{
						return members;
					}
				}
			}

			return SpecializedCollections.EmptyList<MemberDeclarationSyntax>();
		}

		private static List<MemberDeclarationSyntax> GetMembersInSpan(
			TextSpan textSpan,
			TypeDeclarationSyntax containingType,
			MemberDeclarationSyntax firstMember)
		{
			List<MemberDeclarationSyntax> selectedMembers = null;

			var members = containingType.Members;
			var fieldIndex = members.IndexOf(firstMember);
			if (fieldIndex < 0)
			{
				return null;
			}

			for (var i = fieldIndex; i < members.Count; i++)
			{
				var member = members[i];
				if (textSpan.Contains(member.Span))
				{
					selectedMembers = selectedMembers ?? new List<MemberDeclarationSyntax>();
					selectedMembers.Add(member);
				}
				else if (textSpan.OverlapsWith(member.Span))
				{
					return null;
				}
				else
				{
					break;
				}
			}

			return selectedMembers;
		}

		public static bool IsInPartiallyWrittenGeneric(
			this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
		{
			SyntaxToken genericIdentifier;
			SyntaxToken lessThanToken;
			return syntaxTree.IsInPartiallyWrittenGeneric(position, cancellationToken, out genericIdentifier, out lessThanToken);
		}

		public static bool IsInPartiallyWrittenGeneric(
			this SyntaxTree syntaxTree,
			int position,
			CancellationToken cancellationToken,
			out SyntaxToken genericIdentifier)
		{
			SyntaxToken lessThanToken;
			return syntaxTree.IsInPartiallyWrittenGeneric(position, cancellationToken, out genericIdentifier, out lessThanToken);
		}

		public static bool IsInPartiallyWrittenGeneric(
			this SyntaxTree syntaxTree,
			int position,
			CancellationToken cancellationToken,
			out SyntaxToken genericIdentifier,
			out SyntaxToken lessThanToken)
		{
			genericIdentifier = default(SyntaxToken);
			lessThanToken = default(SyntaxToken);
			int index = 0;

			var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);
			if (token.Kind() == SyntaxKind.None)
			{
				return false;
			}

			// check whether we are under type or member decl
			if (token.GetAncestor<TypeParameterListSyntax>() != null)
			{
				return false;
			}

			int stack = 0;
			while (true)
			{
				switch (token.Kind())
				{
				case SyntaxKind.LessThanToken:
					if (stack == 0)
					{
						// got here so we read successfully up to a < now we have to read the
						// name before that and we're done!
						lessThanToken = token;
						token = token.GetPreviousToken(includeSkipped: true);
						if (token.Kind() == SyntaxKind.None)
						{
							return false;
						}

						// ok
						// so we've read something like:
						// ~~~~~~~~~<a,b,...
						// but we need to know the simple name that precedes the <
						// it could be
						// ~~~~~~foo<a,b,...
						if (token.Kind() == SyntaxKind.IdentifierToken)
						{
							// okay now check whether it is actually partially written
							if (IsFullyWrittenGeneric(token, lessThanToken))
							{
								return false;
							}

							genericIdentifier = token;
							return true;
						}

						return false;
					}
					else
					{
						stack--;
						break;
					}

				case SyntaxKind.GreaterThanGreaterThanToken:
					stack++;
					goto case SyntaxKind.GreaterThanToken;

					// fall through
				case SyntaxKind.GreaterThanToken:
					stack++;
					break;

				case SyntaxKind.AsteriskToken:      // for int*
				case SyntaxKind.QuestionToken:      // for int?
				case SyntaxKind.ColonToken:         // for global::  (so we don't dismiss help as you type the first :)
				case SyntaxKind.ColonColonToken:    // for global::
				case SyntaxKind.CloseBracketToken:
				case SyntaxKind.OpenBracketToken:
				case SyntaxKind.DotToken:
				case SyntaxKind.IdentifierToken:
					break;

				case SyntaxKind.CommaToken:
					if (stack == 0)
					{
						index++;
					}

					break;

				default:
					// user might have typed "in" on the way to typing "int"
					// don't want to disregard this genericname because of that
					if (SyntaxFacts.IsKeywordKind(token.Kind()))
					{
						break;
					}

					// anything else and we're sunk.
					return false;
				}

				// look backward one token, include skipped tokens, because the parser frequently
				// does skip them in cases like: "Func<A, B", which get parsed as: expression
				// statement "Func<A" with missing semicolon, expression statement "B" with missing
				// semicolon, and the "," is skipped.
				token = token.GetPreviousToken(includeSkipped: true);
				if (token.Kind() == SyntaxKind.None)
				{
					return false;
				}
			}
		}

		private static bool IsFullyWrittenGeneric(SyntaxToken token, SyntaxToken lessThanToken)
		{
			var genericName = token.Parent as GenericNameSyntax;

			return genericName != null && genericName.TypeArgumentList != null &&
				genericName.TypeArgumentList.LessThanToken == lessThanToken && !genericName.TypeArgumentList.GreaterThanToken.IsMissing;
		}


		public static bool IsAttributeNameContext(this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
		{
			var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);
			token = token.GetPreviousTokenIfTouchingWord(position);

			// cases:
			//   [ |
			if (token.Kind() == SyntaxKind.OpenBracketToken &&
				token.Parent.IsKind(SyntaxKind.AttributeList))
			{
				return true;
			}

			// cases:
			//   [Foo(1), |
			if (token.Kind() == SyntaxKind.CommaToken &&
				token.Parent.IsKind(SyntaxKind.AttributeList))
			{
				return true;
			}

			// cases:
			//   [specifier: |
			if (token.Kind() == SyntaxKind.ColonToken &&
				token.Parent.IsKind(SyntaxKind.AttributeTargetSpecifier))
			{
				return true;
			}

			// cases:
			//   [Namespace.|
			if (token.Parent.IsKind(SyntaxKind.QualifiedName) &&
				token.Parent.IsParentKind(SyntaxKind.Attribute))
			{
				return true;
			}

			// cases:
			//   [global::|
			if (token.Parent.IsKind(SyntaxKind.AliasQualifiedName) &&
				token.Parent.IsParentKind(SyntaxKind.Attribute))
			{
				return true;
			}

			return false;
		}

		public static bool IsGlobalMemberDeclarationContext(
			this SyntaxTree syntaxTree,
			int position,
			ISet<SyntaxKind> validModifiers,
			CancellationToken cancellationToken)
		{
			if (!syntaxTree.IsInteractiveOrScript())
			{
				return false;
			}

			var tokenOnLeftOfPosition = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);
			var token = tokenOnLeftOfPosition.GetPreviousTokenIfTouchingWord(position);

			var modifierTokens = syntaxTree.GetPrecedingModifiers(position, tokenOnLeftOfPosition, cancellationToken);
			if (!modifierTokens.Any())
			{
				return false;
			}

			if (modifierTokens.IsSubsetOf(validModifiers))
			{
				// the parent is the member
				// the grandparent is the container of the member
				// in interactive, it's possible that there might be an intervening "incomplete" member for partially
				// typed declarations that parse ambiguously. For example, "internal e".
				if (token.Parent.IsKind(SyntaxKind.CompilationUnit) ||
					(token.Parent.IsKind(SyntaxKind.IncompleteMember) && token.Parent.IsParentKind(SyntaxKind.CompilationUnit)))
				{
					return true;
				}
			}

			return false;
		}

		public static bool IsMemberDeclarationContext(
			this SyntaxTree syntaxTree, int position, SyntaxToken tokenOnLeftOfPosition, CancellationToken cancellationToken)
		{
			// cases:
			// class C {
			//   |

			// class C {
			//   void Foo() {
			//   }
			//   |

			// class C {
			//   int i;
			//   |

			// class C {
			//   public |

			// class C {
			//   [Foo]
			//   |

			var originalToken = tokenOnLeftOfPosition;
			var token = originalToken;

			// If we're touching the right of an identifier, move back to
			// previous token.
			token = token.GetPreviousTokenIfTouchingWord(position);

			if (token.Kind() == SyntaxKind.OpenBraceToken)
			{
				if (token.Parent is BaseTypeDeclarationSyntax)
				{
					return true;
				}
			}

			// class C {
			//   int i;
			//   |
			if (token.Kind() == SyntaxKind.SemicolonToken)
			{
				if (token.Parent is MemberDeclarationSyntax &&
					token.Parent.GetParent() is BaseTypeDeclarationSyntax)
				{
					return true;
				}
			}

			// class A {
			//   class C {}
			//   |

			// class C {
			//    void Foo() {
			//    }
			//    |
			if (token.Kind() == SyntaxKind.CloseBraceToken)
			{
				if (token.Parent is BaseTypeDeclarationSyntax &&
					token.Parent.GetParent() is BaseTypeDeclarationSyntax)
				{
					// after a nested type
					return true;
				}
				else if (token.Parent is AccessorListSyntax)
				{
					// after a property
					return true;
				}
				else if (
					token.Parent.IsKind(SyntaxKind.Block) &&
					token.Parent.GetParent() is MemberDeclarationSyntax)
				{
					// after a method/operator/etc.
					return true;
				}
			}

			// namespace Foo {
			//   [Bar]
			//   |

			if (token.Kind() == SyntaxKind.CloseBracketToken &&
				token.Parent.IsKind(SyntaxKind.AttributeList))
			{
				// attributes belong to a member which itself is in a
				// container.

				// the parent is the attribute
				// the grandparent is the owner of the attribute
				// the great-grandparent is the container that the owner is in
				var container = token.Parent.GetParent().GetParent();
				if (container is BaseTypeDeclarationSyntax)
				{
					return true;
				}
			}

			return false;
		}

		public static bool IsMemberDeclarationContext(
			this SyntaxTree syntaxTree,
			int position,
			CSharpSyntaxContext contextOpt,
			ISet<SyntaxKind> validModifiers,
			ISet<SyntaxKind> validTypeDeclarations,
			bool canBePartial,
			CancellationToken cancellationToken)
		{
			var typeDecl = contextOpt != null
				? contextOpt.ContainingTypeOrEnumDeclaration
				: syntaxTree.GetContainingTypeOrEnumDeclaration(position, cancellationToken);

			if (typeDecl == null)
			{
				return false;
			}

			if (!validTypeDeclarations.Contains(typeDecl.Kind()))
			{
				return false;
			}

			validTypeDeclarations = validTypeDeclarations ?? SpecializedCollections.EmptySet<SyntaxKind>();

			// Check many of the simple cases first.
			var leftToken = contextOpt != null
				? contextOpt.LeftToken
				: syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);

			if (syntaxTree.IsMemberDeclarationContext(position, leftToken, cancellationToken))
			{
				return true;
			}

			var token = contextOpt != null
				? contextOpt.TargetToken
				: leftToken.GetPreviousTokenIfTouchingWord(position);

			// A member can also show up after certain types of modifiers
			if (canBePartial &&
				token.IsKindOrHasMatchingText(SyntaxKind.PartialKeyword))
			{
				return true;
			}

			var modifierTokens = contextOpt != null
				? contextOpt.PrecedingModifiers
				: syntaxTree.GetPrecedingModifiers(position, leftToken, cancellationToken);

			if (!modifierTokens.Any())
			{
				return false;
			}

			validModifiers = validModifiers ?? SpecializedCollections.EmptySet<SyntaxKind>();

			if (modifierTokens.IsSubsetOf(validModifiers))
			{
				var member = token.Parent;
				if (token.HasMatchingText(SyntaxKind.AsyncKeyword))
				{
					// second appearance of "async", not followed by modifier: treat it as type
					if (syntaxTree.GetPrecedingModifiers(token.SpanStart, token, cancellationToken).Any(x => x == SyntaxKind.AsyncKeyword))
					{
						return false;
					}

					// rule out async lambdas inside a method
					if (token.GetAncestor<StatementSyntax>() == null)
					{
						member = token.GetAncestor<MemberDeclarationSyntax>();
					}
				}

				// cases:
				// public |
				// async |
				// public async |
				return member != null &&
					member.Parent is BaseTypeDeclarationSyntax;
			}

			return false;
		}

		public static bool IsTypeDeclarationContext(
			this SyntaxTree syntaxTree, int position, SyntaxToken tokenOnLeftOfPosition, CancellationToken cancellationToken)
		{
			// cases:
			// root: |

			// extern alias a;
			// |

			// using Foo;
			// |

			// using Foo = Bar;
			// |

			// namespace N {}
			// |

			// namespace N {
			// |

			// class C {}
			// |

			// class C {
			// |

			// class C {
			//   void Foo() {
			//   }
			//   |

			// class C {
			//   int i;
			//   |

			// class C {
			//   public |

			// class C {
			//   [Foo]
			//   |

			var originalToken = tokenOnLeftOfPosition;
			var token = originalToken;

			// If we're touching the right of an identifier, move back to
			// previous token.
			token = token.GetPreviousTokenIfTouchingWord(position);

			// a type decl can't come before usings/externs
			if (originalToken.GetNextToken(includeSkipped: true).IsUsingOrExternKeyword())
			{
				return false;
			}

			// root: |
			if (token.Kind() == SyntaxKind.None)
			{
				// root namespace

				// a type decl can't come before usings/externs
				var compilationUnit = syntaxTree.GetRoot(cancellationToken) as CompilationUnitSyntax;
				if (compilationUnit != null &&
					(compilationUnit.Externs.Count > 0 ||
						compilationUnit.Usings.Count > 0))
				{
					return false;
				}

				return true;
			}

			if (token.Kind() == SyntaxKind.OpenBraceToken)
			{
				if (token.Parent.IsKind(SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration))
				{
					return true;
				}
				else if (token.Parent.IsKind(SyntaxKind.NamespaceDeclaration))
				{
					return true;
				}
			}

			// extern alias a;
			// |

			// using Foo;
			// |

			// class C {
			//   int i;
			//   |
			if (token.Kind() == SyntaxKind.SemicolonToken)
			{
				if (token.Parent.IsKind(SyntaxKind.ExternAliasDirective, SyntaxKind.UsingDirective))
				{
					return true;
				}
				else if (token.Parent is MemberDeclarationSyntax)
				{
					return true;
				}
			}

			// class C {}
			// |

			// namespace N {}
			// |

			// class C {
			//    void Foo() {
			//    }
			//    |
			if (token.Kind() == SyntaxKind.CloseBraceToken)
			{
				if (token.Parent is BaseTypeDeclarationSyntax)
				{
					return true;
				}
				else if (token.Parent.IsKind(SyntaxKind.NamespaceDeclaration))
				{
					return true;
				}
				else if (token.Parent is AccessorListSyntax)
				{
					return true;
				}
				else if (
					token.Parent.IsKind(SyntaxKind.Block) &&
					token.Parent.GetParent() is MemberDeclarationSyntax)
				{
					return true;
				}
			}

			// namespace Foo {
			//   [Bar]
			//   |

			if (token.Kind() == SyntaxKind.CloseBracketToken &&
				token.Parent.IsKind(SyntaxKind.AttributeList))
			{
				// assembly attributes belong to the containing compilation unit
				if (token.Parent.IsParentKind(SyntaxKind.CompilationUnit))
				{
					return true;
				}

				// other attributes belong to a member which itself is in a
				// container.

				// the parent is the attribute
				// the grandparent is the owner of the attribute
				// the great-grandparent is the container that the owner is in
				var container = token.Parent.GetParent().GetParent();
				if (container.IsKind(SyntaxKind.CompilationUnit) ||
					container.IsKind(SyntaxKind.NamespaceDeclaration) ||
					container.IsKind(SyntaxKind.ClassDeclaration) ||
					container.IsKind(SyntaxKind.StructDeclaration))
				{
					return true;
				}
			}

			return false;
		}

		public static bool IsTypeDeclarationContext(
			this SyntaxTree syntaxTree,
			int position,
			CSharpSyntaxContext contextOpt,
			ISet<SyntaxKind> validModifiers,
			ISet<SyntaxKind> validTypeDeclarations,
			bool canBePartial,
			CancellationToken cancellationToken)
		{
			// We only allow nested types inside a class or struct, not inside a
			// an interface or enum.
			var typeDecl = contextOpt != null
				? contextOpt.ContainingTypeDeclaration
				: syntaxTree.GetContainingTypeDeclaration(position, cancellationToken);

			validTypeDeclarations = validTypeDeclarations ?? SpecializedCollections.EmptySet<SyntaxKind>();

			if (typeDecl != null)
			{
				if (!validTypeDeclarations.Contains(typeDecl.Kind()))
				{
					return false;
				}
			}

			// Check many of the simple cases first.
			var leftToken = contextOpt != null
				? contextOpt.LeftToken
				: syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);

			if (syntaxTree.IsTypeDeclarationContext(position, leftToken, cancellationToken))
			{
				return true;
			}

			// If we're touching the right of an identifier, move back to
			// previous token.
			var token = contextOpt != null
				? contextOpt.TargetToken
				: leftToken.GetPreviousTokenIfTouchingWord(position);

			// A type can also show up after certain types of modifiers
			if (canBePartial &&
				token.IsKindOrHasMatchingText(SyntaxKind.PartialKeyword))
			{
				return true;
			}

			// using static | is never a type declaration context
			if (token.IsStaticKeywordInUsingDirective())
			{
				return false;
			}

			var modifierTokens = contextOpt != null
				? contextOpt.PrecedingModifiers
				: syntaxTree.GetPrecedingModifiers(position, leftToken, cancellationToken);

			if (!modifierTokens.Any())
			{
				return false;
			}

			validModifiers = validModifiers ?? SpecializedCollections.EmptySet<SyntaxKind>();

			if (modifierTokens.IsProperSubsetOf(validModifiers))
			{
				// the parent is the member
				// the grandparent is the container of the member
				var container = token.Parent.GetParent();
				if (container.IsKind(SyntaxKind.CompilationUnit) ||
					container.IsKind(SyntaxKind.NamespaceDeclaration) ||
					container.IsKind(SyntaxKind.ClassDeclaration) ||
					container.IsKind(SyntaxKind.StructDeclaration))
				{
					return true;
				}
			}

			return false;
		}

		public static bool IsNamespaceContext(
			this SyntaxTree syntaxTree,
			int position,
			CancellationToken cancellationToken,
			SemanticModel semanticModelOpt = null)
		{
			// first do quick exit check
			if (syntaxTree.IsInNonUserCode(position, cancellationToken) ||
				syntaxTree.IsRightOfDotOrArrow(position, cancellationToken))
			{
				return false;
			}

			var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken)
				.GetPreviousTokenIfTouchingWord(position);

			// global::
			if (token.Kind() == SyntaxKind.ColonColonToken &&
				token.GetPreviousToken(includeSkipped: true).Kind() == SyntaxKind.GlobalKeyword)
			{
				return true;
			}

			// using |
			// but not:
			// using | = Bar

			// Note: we take care of the using alias case in the IsTypeContext
			// call below.

			if (token.Kind() == SyntaxKind.UsingKeyword)
			{
				var usingDirective = token.GetAncestor<UsingDirectiveSyntax>();
				if (usingDirective != null)
				{
					if (token.GetNextToken(includeSkipped: true).Kind() != SyntaxKind.EqualsToken &&
						usingDirective.Alias == null)
					{
						return true;
					}
				}
			}

			// using static |
			if (token.IsStaticKeywordInUsingDirective())
			{
				return true;
			}

			// if it is not using directive location, most of places where 
			// type can appear, namespace can appear as well
			return syntaxTree.IsTypeContext(position, cancellationToken, semanticModelOpt);
		}

		public static bool IsDefinitelyNotTypeContext(this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
		{
			return
				syntaxTree.IsInNonUserCode(position, cancellationToken) ||
				syntaxTree.IsRightOfDotOrArrow(position, cancellationToken);
		}

		public static bool IsTypeContext(
			this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken, SemanticModel semanticModelOpt = null)
		{
			// first do quick exit check
			if (syntaxTree.IsDefinitelyNotTypeContext(position, cancellationToken))
			{
				return false;
			}

			// okay, now it is a case where we can't use parse tree (valid or error recovery) to
			// determine whether it is a right place to put type. use lex based one Cyrus created.

			var tokenOnLeftOfPosition = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);
			return
				syntaxTree.IsAfterKeyword(position, SyntaxKind.ConstKeyword, cancellationToken) ||
				syntaxTree.IsAfterKeyword(position, SyntaxKind.CaseKeyword, cancellationToken) ||
				syntaxTree.IsAfterKeyword(position, SyntaxKind.EventKeyword, cancellationToken) ||
				syntaxTree.IsAfterKeyword(position, SyntaxKind.StackAllocKeyword, cancellationToken) ||
				syntaxTree.IsAttributeNameContext(position, cancellationToken) ||
				syntaxTree.IsBaseClassOrInterfaceContext(position, cancellationToken) ||
				syntaxTree.IsCatchVariableDeclarationContext(position, cancellationToken) ||
				syntaxTree.IsDefiniteCastTypeContext(position, tokenOnLeftOfPosition, cancellationToken) ||
				syntaxTree.IsDelegateReturnTypeContext(position, tokenOnLeftOfPosition, cancellationToken) ||
				syntaxTree.IsExpressionContext(position, tokenOnLeftOfPosition, attributes: true, cancellationToken: cancellationToken, semanticModelOpt: semanticModelOpt) ||
				syntaxTree.IsPrimaryFunctionExpressionContext(position, tokenOnLeftOfPosition, cancellationToken) ||
				syntaxTree.IsGenericTypeArgumentContext(position, tokenOnLeftOfPosition, cancellationToken, semanticModelOpt) ||
				syntaxTree.IsFixedVariableDeclarationContext(position, tokenOnLeftOfPosition, cancellationToken) ||
				syntaxTree.IsImplicitOrExplicitOperatorTypeContext(position, tokenOnLeftOfPosition, cancellationToken) ||
				syntaxTree.IsIsOrAsTypeContext(position, tokenOnLeftOfPosition, cancellationToken) ||
				syntaxTree.IsLocalVariableDeclarationContext(position, tokenOnLeftOfPosition, cancellationToken) ||
				syntaxTree.IsObjectCreationTypeContext(position, tokenOnLeftOfPosition, cancellationToken) ||
				syntaxTree.IsParameterTypeContext(position, tokenOnLeftOfPosition, cancellationToken) ||
				syntaxTree.IsPossibleLambdaOrAnonymousMethodParameterTypeContext(position, tokenOnLeftOfPosition, cancellationToken) ||
				syntaxTree.IsStatementContext(position, tokenOnLeftOfPosition, cancellationToken) ||
				syntaxTree.IsTypeParameterConstraintContext(position, tokenOnLeftOfPosition, cancellationToken) ||
				syntaxTree.IsUsingAliasContext(position, cancellationToken) ||
				syntaxTree.IsUsingStaticContext(position, cancellationToken) ||
				syntaxTree.IsGlobalMemberDeclarationContext(position, SyntaxKindSet.AllGlobalMemberModifiers, cancellationToken) ||
				syntaxTree.IsMemberDeclarationContext(
					position,
					contextOpt: null,
					validModifiers: SyntaxKindSet.AllMemberModifiers,
					validTypeDeclarations: SyntaxKindSet.ClassInterfaceStructTypeDeclarations,
					canBePartial: false,
					cancellationToken: cancellationToken);
		}

		public static bool IsBaseClassOrInterfaceContext(this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
		{
			// class C : |
			// class C : Bar, |

			var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);
			token = token.GetPreviousTokenIfTouchingWord(position);

			if (token.Kind() == SyntaxKind.ColonToken ||
				token.Kind() == SyntaxKind.CommaToken)
			{
				if (token.Parent.IsKind(SyntaxKind.BaseList))
				{
					return true;
				}
			}

			return false;
		}

		public static bool IsUsingAliasContext(this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
		{
			// using Foo = |

			var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);
			token = token.GetPreviousTokenIfTouchingWord(position);

			if (token.Kind() == SyntaxKind.EqualsToken &&
				token.GetAncestor<UsingDirectiveSyntax>() != null)
			{
				return true;
			}

			return false;
		}

		public static bool IsUsingStaticContext(this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
		{
			// using static |

			var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);
			token = token.GetPreviousTokenIfTouchingWord(position);

			return token.IsStaticKeywordInUsingDirective();
		}

		public static bool IsTypeArgumentOfConstraintClause(
			this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
		{
			// cases:
			//   where |
			//   class Foo<T> : Object where |

			var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);
			token = token.GetPreviousTokenIfTouchingWord(position);

			if (token.Kind() == SyntaxKind.WhereKeyword &&
				token.Parent.IsKind(SyntaxKind.TypeParameterConstraintClause))
			{
				return true;
			}

			if (token.Kind() == SyntaxKind.IdentifierToken &&
				token.HasMatchingText(SyntaxKind.WhereKeyword) &&
				token.Parent.IsKind(SyntaxKind.IdentifierName) &&
				token.Parent.IsParentKind(SyntaxKind.SimpleBaseType) &&
				token.Parent.Parent.IsParentKind(SyntaxKind.BaseList))
			{
				return true;
			}

			return false;
		}

		public static bool IsTypeParameterConstraintStartContext(
			this SyntaxTree syntaxTree, int position, SyntaxToken tokenOnLeftOfPosition, CancellationToken cancellationToken)
		{
			// cases:
			//   where T : |

			var token = tokenOnLeftOfPosition;
			token = token.GetPreviousTokenIfTouchingWord(position);

			if (token.Kind() == SyntaxKind.ColonToken &&
				token.GetPreviousToken(includeSkipped: true).Kind() == SyntaxKind.IdentifierToken &&
				token.GetPreviousToken(includeSkipped: true).GetPreviousToken().Kind() == SyntaxKind.WhereKeyword)
			{
				return true;
			}

			return false;
		}

		public static bool IsTypeParameterConstraintContext(this SyntaxTree syntaxTree, int position, SyntaxToken tokenOnLeftOfPosition, CancellationToken cancellationToken)
		{
			if (syntaxTree.IsTypeParameterConstraintStartContext(position, tokenOnLeftOfPosition, cancellationToken))
			{
				return true;
			}

			var token = tokenOnLeftOfPosition;
			token = token.GetPreviousTokenIfTouchingWord(position);

			// Can't come after new()
			//
			//    where T : |
			//    where T : class, |
			//    where T : struct, |
			//    where T : Foo, |
			if (token.Kind() == SyntaxKind.CommaToken &&
				token.Parent.IsKind(SyntaxKind.TypeParameterConstraintClause))
			{
				var constraintClause = token.Parent as TypeParameterConstraintClauseSyntax;

				// Check if there's a 'new()' constraint.  If there isn't, or we're before it, then
				// this is a type parameter constraint context. 
				var firstConstructorConstraint = constraintClause.Constraints.FirstOrDefault(t => t is ConstructorConstraintSyntax);
				if (firstConstructorConstraint == null || firstConstructorConstraint.SpanStart > token.Span.End)
				{
					return true;
				}
			}

			return false;
		}

		public static bool IsTypeOfExpressionContext(this SyntaxTree syntaxTree, int position, SyntaxToken tokenOnLeftOfPosition, CancellationToken cancellationToken)
		{
			var token = tokenOnLeftOfPosition.GetPreviousTokenIfTouchingWord(position);

			if (token.Kind() == SyntaxKind.OpenParenToken && token.Parent.IsKind(SyntaxKind.TypeOfExpression))
			{
				return true;
			}

			return false;
		}

		public static bool IsDefaultExpressionContext(this SyntaxTree syntaxTree, int position, SyntaxToken tokenOnLeftOfPosition, CancellationToken cancellationToken)
		{
			var token = tokenOnLeftOfPosition.GetPreviousTokenIfTouchingWord(position);

			if (token.Kind() == SyntaxKind.OpenParenToken && token.Parent.IsKind(SyntaxKind.DefaultExpression))
			{
				return true;
			}

			return false;
		}

		public static bool IsSizeOfExpressionContext(
			this SyntaxTree syntaxTree, int position, SyntaxToken tokenOnLeftOfPosition, CancellationToken cancellationToken)
		{
			var token = tokenOnLeftOfPosition.GetPreviousTokenIfTouchingWord(position);

			if (token.Kind() == SyntaxKind.OpenParenToken && token.Parent.IsKind(SyntaxKind.SizeOfExpression))
			{
				return true;
			}

			return false;
		}

		public static bool IsGenericTypeArgumentContext(
			this SyntaxTree syntaxTree,
			int position,
			SyntaxToken tokenOnLeftOfPosition,
			CancellationToken cancellationToken,
			SemanticModel semanticModelOpt = null)
		{
			// cases: 
			//    Foo<|
			//    Foo<Bar,|
			//    Foo<Bar<Baz<int[],|
			var token = tokenOnLeftOfPosition;
			token = token.GetPreviousTokenIfTouchingWord(position);

			if (token.Kind() != SyntaxKind.LessThanToken && token.Kind() != SyntaxKind.CommaToken)
			{
				return false;
			}

			if (token.Parent is TypeArgumentListSyntax)
			{
				// Easy case, it was known to be a generic name, so this is a type argument context.
				return true;
			}

			SyntaxToken nameToken;
			if (!syntaxTree.IsInPartiallyWrittenGeneric(position, cancellationToken, out nameToken))
			{
				return false;
			}

			var name = nameToken.Parent as NameSyntax;
			if (name == null)
			{
				return false;
			}

			// Looks viable!  If they provided a binding, then check if it binds properly to
			// an actual generic entity.
			if (semanticModelOpt == null)
			{
				// No binding.  Just make the decision based on the syntax tree.
				return true;
			}

			// '?' is syntactically ambiguous in incomplete top-level statements:
			//
			// T ? foo<| 
			//
			// Might be an incomplete conditional expression or an incomplete declaration of a method returning a nullable type.
			// Bind T to see if it is a type. If it is we don't show signature help.
			if (name.IsParentKind(SyntaxKind.LessThanExpression) &&
				name.Parent.IsParentKind(SyntaxKind.ConditionalExpression) &&
				name.Parent.Parent.IsParentKind(SyntaxKind.ExpressionStatement) &&
				name.Parent.Parent.Parent.IsParentKind(SyntaxKind.GlobalStatement))
			{
				var conditionOrType = semanticModelOpt.GetSymbolInfo(
					((ConditionalExpressionSyntax)name.Parent.Parent).Condition, cancellationToken);
				if (conditionOrType.GetBestOrAllSymbols().FirstOrDefault() != null &&
					conditionOrType.GetBestOrAllSymbols().FirstOrDefault().Kind == SymbolKind.NamedType)
				{
					return false;
				}
			}

			var symbols = semanticModelOpt.LookupName(nameToken, namespacesAndTypesOnly: SyntaxFacts.IsInNamespaceOrTypeContext(name), cancellationToken: cancellationToken);
			return symbols.Any(s =>
				s.TypeSwitch(
					(INamedTypeSymbol nt) => nt.Arity > 0,
					(IMethodSymbol m) => m.Arity > 0));
		}

		public static bool IsParameterModifierContext(
			this SyntaxTree syntaxTree,
			int position,
			SyntaxToken tokenOnLeftOfPosition,
			CancellationToken cancellationToken,
			int? allowableIndex = null)
		{
			// cases:
			//   Foo(|
			//   Foo(int i, |
			//   Foo([Bar]|
			var token = tokenOnLeftOfPosition;
			token = token.GetPreviousTokenIfTouchingWord(position);

			if (token.Kind() == SyntaxKind.OpenParenToken &&
				token.Parent.IsDelegateOrConstructorOrMethodParameterList())
			{
				if (allowableIndex.HasValue)
				{
					if (allowableIndex.Value != 0)
					{
						return false;
					}
				}

				return true;
			}

			if (token.Kind() == SyntaxKind.CommaToken &&
				token.Parent.IsDelegateOrConstructorOrMethodParameterList())
			{
				if (allowableIndex.HasValue)
				{
					var parameterList = token.GetAncestor<ParameterListSyntax>();
					var commaIndex = parameterList.Parameters.GetWithSeparators().IndexOf(token);
					var index = commaIndex / 2 + 1;
					if (index != allowableIndex.Value)
					{
						return false;
					}
				}

				return true;
			}

			if (token.Kind() == SyntaxKind.CloseBracketToken &&
				token.Parent.IsKind(SyntaxKind.AttributeList) &&
				token.Parent.IsParentKind(SyntaxKind.Parameter) &&
				token.Parent.GetParent().GetParent().IsDelegateOrConstructorOrMethodParameterList())
			{
				if (allowableIndex.HasValue)
				{
					var parameter = token.GetAncestor<ParameterSyntax>();
					var parameterList = parameter.GetAncestorOrThis<ParameterListSyntax>();

					int parameterIndex = parameterList.Parameters.IndexOf(parameter);
					if (allowableIndex.Value != parameterIndex)
					{
						return false;
					}
				}

				return true;
			}

			return false;
		}

		public static bool IsDelegateReturnTypeContext(
			this SyntaxTree syntaxTree, int position, SyntaxToken tokenOnLeftOfPosition, CancellationToken cancellationToken)
		{
			var token = tokenOnLeftOfPosition;
			token = token.GetPreviousTokenIfTouchingWord(position);

			if (token.Kind() == SyntaxKind.DelegateKeyword &&
				token.Parent.IsKind(SyntaxKind.DelegateDeclaration))
			{
				return true;
			}

			return false;
		}

		public static bool IsImplicitOrExplicitOperatorTypeContext(
			this SyntaxTree syntaxTree, int position, SyntaxToken tokenOnLeftOfPosition, CancellationToken cancellationToken)
		{
			var token = tokenOnLeftOfPosition;
			token = token.GetPreviousTokenIfTouchingWord(position);

			if (token.Kind() == SyntaxKind.OperatorKeyword)
			{
				if (token.GetPreviousToken(includeSkipped: true).Kind() == SyntaxKind.ImplicitKeyword ||
					token.GetPreviousToken(includeSkipped: true).Kind() == SyntaxKind.ExplicitKeyword)
				{
					return true;
				}
			}

			return false;
		}

		public static bool IsParameterTypeContext(this SyntaxTree syntaxTree, int position, SyntaxToken tokenOnLeftOfPosition, CancellationToken cancellationToken)
		{
			var token = tokenOnLeftOfPosition.GetPreviousTokenIfTouchingWord(position);

			if (token.Kind() == SyntaxKind.RefKeyword ||
				token.Kind() == SyntaxKind.OutKeyword ||
				token.Kind() == SyntaxKind.ParamsKeyword ||
				token.Kind() == SyntaxKind.ThisKeyword)
			{
				position = token.SpanStart;
				tokenOnLeftOfPosition = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);
			}

			if (syntaxTree.IsParameterModifierContext(position, tokenOnLeftOfPosition, cancellationToken))
			{
				return true;
			}

			// int this[ |
			// int this[int i, |
			if (token.Kind() == SyntaxKind.OpenParenToken ||
				token.Kind() == SyntaxKind.OpenBracketToken ||
				token.Kind() == SyntaxKind.CommaToken)
			{
				if (token.Parent.IsKind(SyntaxKind.ParameterList, SyntaxKind.BracketedParameterList))
				{
					return true;
				}
			}

			return false;
		}

		public static bool IsPossibleLambdaParameterModifierContext(
			this SyntaxTree syntaxTree, int position, SyntaxToken tokenOnLeftOfPosition, CancellationToken cancellationToken)
		{
			var token = tokenOnLeftOfPosition;
			token = token.GetPreviousTokenIfTouchingWord(position);

			if (token.Kind() == SyntaxKind.OpenParenToken ||
				token.Kind() == SyntaxKind.CommaToken)
			{
				if (token.Parent.IsKind(SyntaxKind.ParameterList) &&
					token.Parent.IsParentKind(SyntaxKind.ParenthesizedLambdaExpression))
				{
					return true;
				}

				// TODO(cyrusn): Tie into semantic analysis system to only 
				// consider this a lambda if this is a location where the
				// lambda's type would be inferred because of a delegate
				// or Expression<T> type.
				if (token.Parent.IsKind(SyntaxKind.ParenthesizedExpression))
				{
					return true;
				}
			}

			return false;
		}

		public static bool IsAnonymousMethodParameterModifierContext(
			this SyntaxTree syntaxTree, int position, SyntaxToken tokenOnLeftOfPosition, CancellationToken cancellationToken)
		{
			var token = tokenOnLeftOfPosition;
			token = token.GetPreviousTokenIfTouchingWord(position);

			if (token.Kind() == SyntaxKind.OpenParenToken ||
				token.Kind() == SyntaxKind.CommaToken)
			{
				if (token.Parent.IsKind(SyntaxKind.ParameterList) &&
					token.Parent.IsParentKind(SyntaxKind.AnonymousMethodExpression))
				{
					return true;
				}
			}

			return false;
		}

		public static bool IsPossibleLambdaOrAnonymousMethodParameterTypeContext(
			this SyntaxTree syntaxTree, int position, SyntaxToken tokenOnLeftOfPosition, CancellationToken cancellationToken)
		{
			var token = tokenOnLeftOfPosition;
			token = token.GetPreviousTokenIfTouchingWord(position);

			if (token.Kind() == SyntaxKind.RefKeyword ||
				token.Kind() == SyntaxKind.OutKeyword)
			{
				position = token.SpanStart;
				tokenOnLeftOfPosition = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);
			}

			if (IsAnonymousMethodParameterModifierContext(syntaxTree, position, tokenOnLeftOfPosition, cancellationToken) ||
				IsPossibleLambdaParameterModifierContext(syntaxTree, position, tokenOnLeftOfPosition, cancellationToken))
			{
				return true;
			}

			return false;
		}

		public static bool IsValidContextForFromClause(
			this SyntaxTree syntaxTree,
			int position,
			SyntaxToken tokenOnLeftOfPosition,
			CancellationToken cancellationToken,
			SemanticModel semanticModelOpt = null)
		{
			if (syntaxTree.IsExpressionContext(position, tokenOnLeftOfPosition, attributes: false, cancellationToken: cancellationToken, semanticModelOpt: semanticModelOpt) &&
				!syntaxTree.IsConstantExpressionContext(position, tokenOnLeftOfPosition, cancellationToken))
			{
				return true;
			}

			// cases:
			//   var q = |
			//   var q = f|
			//
			//   var q = from x in y
			//           |
			//
			//   var q = from x in y
			//           f|
			//
			// this list is *not* exhaustive.
			// the first two are handled by 'IsExpressionContext'

			var token = tokenOnLeftOfPosition;
			token = token.GetPreviousTokenIfTouchingWord(position);

			// var q = from x in y
			//         |
			if (!token.IntersectsWith(position) &&
				token.IsLastTokenOfQueryClause())
			{
				return true;
			}

			return false;
		}

		public static bool IsValidContextForJoinClause(
			this SyntaxTree syntaxTree, int position, SyntaxToken tokenOnLeftOfPosition, CancellationToken cancellationToken)
		{
			var token = tokenOnLeftOfPosition;
			token = token.GetPreviousTokenIfTouchingWord(position);

			// var q = from x in y
			//         |
			if (!token.IntersectsWith(position) &&
				token.IsLastTokenOfQueryClause())
			{
				return true;
			}

			return false;
		}

		public static bool IsDeclarationExpressionContext(
			this SyntaxTree syntaxTree, int position, SyntaxToken tokenOnLeftOfPosition, CancellationToken cancellationToken)
		{
			// cases:
			//  M(out var
			//  var x = var

			var token = tokenOnLeftOfPosition;
			token = token.GetPreviousTokenIfTouchingWord(position);

			if (token.IsKind (SyntaxKind.OutKeyword) &&
				token.Parent.IsKind(SyntaxKind.Argument))
			{
				return true;
			}

			if (token.IsKind(SyntaxKind.EqualsToken) &&
				token.Parent.IsKind(SyntaxKind.EqualsValueClause) &&
				token.Parent.IsParentKind(SyntaxKind.VariableDeclarator))
			{
				return true;
			}

			return false;
		}

		public static bool IsLocalVariableDeclarationContext(
			this SyntaxTree syntaxTree, int position, SyntaxToken tokenOnLeftOfPosition, CancellationToken cancellationToken)
		{
			// cases:
			//  const var
			//  for (var
			//  foreach (var
			//  using (var
			//  from var
			//  join var

			var token = tokenOnLeftOfPosition;
			token = token.GetPreviousTokenIfTouchingWord(position);

			if (token.Kind() == SyntaxKind.ConstKeyword &&
				token.Parent.IsKind(SyntaxKind.LocalDeclarationStatement))
			{
				return true;
			}

			if (token.Kind() == SyntaxKind.OpenParenToken)
			{
				var previous = token.GetPreviousToken(includeSkipped: true);
				if (previous.Kind() == SyntaxKind.ForKeyword ||
					previous.Kind() == SyntaxKind.ForEachKeyword ||
					previous.Kind() == SyntaxKind.UsingKeyword)
				{
					return true;
				}
			}

			var tokenOnLeftOfStart = syntaxTree.FindTokenOnLeftOfPosition(token.SpanStart, cancellationToken);
			if (token.IsKindOrHasMatchingText(SyntaxKind.FromKeyword) &&
				syntaxTree.IsValidContextForFromClause(token.SpanStart, tokenOnLeftOfStart, cancellationToken))
			{
				return true;
			}

			if (token.IsKind(SyntaxKind.JoinKeyword) &&
				syntaxTree.IsValidContextForJoinClause(token.SpanStart, tokenOnLeftOfStart, cancellationToken))
			{
				return true;
			}

			return false;
		}

		public static bool IsFixedVariableDeclarationContext(
			this SyntaxTree syntaxTree, int position, SyntaxToken tokenOnLeftOfPosition, CancellationToken cancellationToken)
		{
			// cases:
			//  fixed (var

			var token = tokenOnLeftOfPosition;
			token = token.GetPreviousTokenIfTouchingWord(position);

			if (token.Kind() == SyntaxKind.OpenParenToken &&
				token.GetPreviousToken(includeSkipped: true).Kind() == SyntaxKind.FixedKeyword)
			{
				return true;
			}

			return false;
		}

		public static bool IsCatchVariableDeclarationContext(this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
		{
			// cases:
			//  catch (var

			var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);
			token = token.GetPreviousTokenIfTouchingWord(position);

			if (token.Kind() == SyntaxKind.OpenParenToken &&
				token.GetPreviousToken(includeSkipped: true).Kind() == SyntaxKind.CatchKeyword)
			{
				return true;
			}

			return false;
		}

		public static bool IsIsOrAsTypeContext(this SyntaxTree syntaxTree, int position, SyntaxToken tokenOnLeftOfPosition, CancellationToken cancellationToken)
		{
			var token = tokenOnLeftOfPosition;
			token = token.GetPreviousTokenIfTouchingWord(position);

			if (token.Kind() == SyntaxKind.IsKeyword ||
				token.Kind() == SyntaxKind.AsKeyword)
			{
				return true;
			}

			return false;
		}

		public static bool IsObjectCreationTypeContext(
			this SyntaxTree syntaxTree, int position, SyntaxToken tokenOnLeftOfPosition, CancellationToken cancellationToken)
		{
			var token = tokenOnLeftOfPosition.GetPreviousTokenIfTouchingWord(position);

			if (token.Kind() == SyntaxKind.NewKeyword)
			{
				// we can follow a 'new' if it's the 'new' for an expression.
				var start = token.SpanStart;
				var tokenOnLeftOfStart = syntaxTree.FindTokenOnLeftOfPosition(start, cancellationToken);
				return
					IsNonConstantExpressionContext(syntaxTree, token.SpanStart, tokenOnLeftOfStart, cancellationToken) ||
					syntaxTree.IsStatementContext(token.SpanStart, tokenOnLeftOfStart, cancellationToken) ||
					syntaxTree.IsGlobalStatementContext(token.SpanStart, cancellationToken);
			}

			return false;
		}

		private static bool IsNonConstantExpressionContext(SyntaxTree syntaxTree, int position, SyntaxToken tokenOnLeftOfPosition, CancellationToken cancellationToken)
		{
			return
				syntaxTree.IsExpressionContext(position, tokenOnLeftOfPosition, attributes: true, cancellationToken: cancellationToken) &&
				!syntaxTree.IsConstantExpressionContext(position, tokenOnLeftOfPosition, cancellationToken);
		}

		public static bool IsPreProcessorDirectiveContext(this SyntaxTree syntaxTree, int position, SyntaxToken preProcessorTokenOnLeftOfPosition, CancellationToken cancellationToken)
		{
			var token = preProcessorTokenOnLeftOfPosition;
			var directive = token.GetAncestor<DirectiveTriviaSyntax>();

			// Directives contain the EOL, so if the position is within the full span of the
			// directive, then it is on that line, the only exception is if the directive is on the
			// last line, the position at the end if technically not contained by the directive but
			// its also not on a new line, so it should be considered part of the preprocessor
			// context.
			if (directive == null)
			{
				return false;
			}

			return
				directive.FullSpan.Contains(position) ||
				directive.FullSpan.End == syntaxTree.GetRoot(cancellationToken).FullSpan.End;
		}

		public static bool IsPreProcessorDirectiveContext(this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
		{
			var leftToken = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken, includeDirectives: true);

			return syntaxTree.IsPreProcessorDirectiveContext(position, leftToken, cancellationToken);
		}

		public static bool IsPreProcessorKeywordContext(this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
		{
			return IsPreProcessorKeywordContext(
				syntaxTree, position,
				syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken, includeDirectives: true),
				cancellationToken);
		}

		public static bool IsPreProcessorKeywordContext(this SyntaxTree syntaxTree, int position, SyntaxToken preProcessorTokenOnLeftOfPosition, CancellationToken cancellationToken)
		{
			// cases:
			//  #|
			//  #d|
			//  # |
			//  # d|

			// note: comments are not allowed between the # and item.
			var token = preProcessorTokenOnLeftOfPosition;
			token = token.GetPreviousTokenIfTouchingWord(position);

			if (token.Kind() == SyntaxKind.HashToken)
			{
				return true;
			}

			return false;
		}

		public static bool IsStatementContext(this SyntaxTree syntaxTree, int position, SyntaxToken tokenOnLeftOfPosition, CancellationToken cancellationToken)
		{
			#if false
			// we're in a statement if the thing that comes before allows for
			// statements to follow.  Or if we're on a just started identifier
			// in the first position where a statement can go.
			if (syntaxTree.IsInPreprocessorDirectiveContext(position, cancellationToken))
			{
			return false;
			}
			#endif

			var token = tokenOnLeftOfPosition;
			token = token.GetPreviousTokenIfTouchingWord(position);

			return token.IsBeginningOfStatementContext();
		}

		public static bool IsGlobalStatementContext(this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
		{
			if (!syntaxTree.IsInteractiveOrScript())
			{
				return false;
			}

			#if false
			if (syntaxTree.IsInPreprocessorDirectiveContext(position, cancellationToken))
			{
			return false;
			}
			#endif

			var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken)
				.GetPreviousTokenIfTouchingWord(position);

			if (token.Kind() == SyntaxKind.None)
			{
				// global statements can't come before usings/externs
				var compilationUnit = syntaxTree.GetRoot(cancellationToken) as CompilationUnitSyntax;
				if (compilationUnit != null &&
					(compilationUnit.Externs.Count > 0 ||
						compilationUnit.Usings.Count > 0))
				{
					return false;
				}

				return true;
			}

			return token.IsBeginningOfGlobalStatementContext();
		}

		public static bool IsInstanceContext(this SyntaxTree syntaxTree, int position, SyntaxToken tokenOnLeftOfPosition, CancellationToken cancellationToken)
		{
			#if false
			if (syntaxTree.IsInPreprocessorDirectiveContext(position, cancellationToken))
			{
			return false;
			}
			#endif

			var token = tokenOnLeftOfPosition;

			// We're in an instance context if we're in the body of an instance member
			var containingMember = token.GetAncestor<MemberDeclarationSyntax>();
			if (containingMember == null)
			{
				return false;
			}

			var modifiers = containingMember.GetModifiers();
			if (modifiers.Any(SyntaxKind.StaticKeyword))
			{
				return false;
			}

			// Must be a property or something method-like.
			if (containingMember.HasMethodShape())
			{
				var body = containingMember.GetBody();
				return IsInBlock(body, position);
			}

			var accessor = token.GetAncestor<AccessorDeclarationSyntax>();
			if (accessor != null)
			{
				return IsInBlock(accessor.Body, position);
			}

			return false;
		}

		private static bool IsInBlock(BlockSyntax bodyOpt, int position)
		{
			if (bodyOpt == null)
			{
				return false;
			}

			return bodyOpt.OpenBraceToken.Span.End <= position &&
				(bodyOpt.CloseBraceToken.IsMissing || position <= bodyOpt.CloseBraceToken.SpanStart);
		}

		public static bool IsPossibleCastTypeContext(
			this SyntaxTree syntaxTree, int position, SyntaxToken tokenOnLeftOfPosition, CancellationToken cancellationToken)
		{
			var token = tokenOnLeftOfPosition.GetPreviousTokenIfTouchingWord(position);

			if (token.IsKind(SyntaxKind.OpenParenToken) &&
				syntaxTree.IsExpressionContext(token.SpanStart, syntaxTree.FindTokenOnLeftOfPosition(token.SpanStart, cancellationToken), false, cancellationToken))
			{
				return true;
			}

			return false;
		}

		public static bool IsDefiniteCastTypeContext(this SyntaxTree syntaxTree, int position, SyntaxToken tokenOnLeftOfPosition, CancellationToken cancellationToken)
		{
			var token = tokenOnLeftOfPosition;
			token = token.GetPreviousTokenIfTouchingWord(position);

			if (token.Kind() == SyntaxKind.OpenParenToken &&
				token.Parent.IsKind(SyntaxKind.CastExpression))
			{
				return true;
			}

			return false;
		}

		public static bool IsConstantExpressionContext(this SyntaxTree syntaxTree, int position, SyntaxToken tokenOnLeftOfPosition, CancellationToken cancellationToken)
		{
			var token = tokenOnLeftOfPosition.GetPreviousTokenIfTouchingWord(position);

			// case |
			if (token.Kind() == SyntaxKind.CaseKeyword &&
				token.Parent.IsKind(SyntaxKind.CaseSwitchLabel))
			{
				return true;
			}

			// goto case |
			if (token.Kind() == SyntaxKind.CaseKeyword &&
				token.Parent.IsKind(SyntaxKind.GotoCaseStatement))
			{
				return true;
			}

			if (token.Kind() == SyntaxKind.EqualsToken &&
				token.Parent.IsKind(SyntaxKind.EqualsValueClause))
			{
				var equalsValue = (EqualsValueClauseSyntax)token.Parent;

				if (equalsValue.IsParentKind(SyntaxKind.VariableDeclarator) &&
					equalsValue.Parent.IsParentKind(SyntaxKind.VariableDeclaration))
				{
					// class C { const int i = |
					var fieldDeclaration = equalsValue.GetAncestor<FieldDeclarationSyntax>();
					if (fieldDeclaration != null)
					{
						return fieldDeclaration.Modifiers.Any(SyntaxKind.ConstKeyword);
					}

					// void M() { const int i = |
					var localDeclaration = equalsValue.GetAncestor<LocalDeclarationStatementSyntax>();
					if (localDeclaration != null)
					{
						return localDeclaration.Modifiers.Any(SyntaxKind.ConstKeyword);
					}
				}

				// enum E { A = |
				if (equalsValue.IsParentKind(SyntaxKind.EnumMemberDeclaration))
				{
					return true;
				}

				// void M(int i = |
				if (equalsValue.IsParentKind(SyntaxKind.Parameter))
				{
					return true;
				}
			}

			// [Foo( |
			// [Foo(x, |
			if (token.Parent.IsKind(SyntaxKind.AttributeArgumentList) &&
				(token.Kind() == SyntaxKind.CommaToken ||
					token.Kind() == SyntaxKind.OpenParenToken))
			{
				return true;
			}

			// [Foo(x: |
			if (token.Kind() == SyntaxKind.ColonToken &&
				token.Parent.IsKind(SyntaxKind.NameColon) &&
				token.Parent.IsParentKind(SyntaxKind.AttributeArgument))
			{
				return true;
			}

			// [Foo(X = |
			if (token.Kind() == SyntaxKind.EqualsToken &&
				token.Parent.IsKind(SyntaxKind.NameEquals) &&
				token.Parent.IsParentKind(SyntaxKind.AttributeArgument))
			{
				return true;
			}

			// TODO: Fixed-size buffer declarations

			return false;
		}

		public static bool IsLabelContext(this SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
		{
			var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);

			var gotoStatement = token.GetAncestor<GotoStatementSyntax>();
			if (gotoStatement != null)
			{
				if (gotoStatement.GotoKeyword == token)
				{
					return true;
				}

				if (gotoStatement.Expression != null &&
					!gotoStatement.Expression.IsMissing &&
					gotoStatement.Expression is IdentifierNameSyntax &&
					((IdentifierNameSyntax)gotoStatement.Expression).Identifier == token &&
					token.IntersectsWith(position))
				{
					return true;
				}
			}

			return false;
		}

		public static bool IsExpressionContext(
			this SyntaxTree syntaxTree,
			int position,
			SyntaxToken tokenOnLeftOfPosition,
			bool attributes,
			CancellationToken cancellationToken,
			SemanticModel semanticModelOpt = null)
		{
			// cases:
			//   var q = |
			//   var q = a|
			// this list is *not* exhaustive.

			var token = tokenOnLeftOfPosition.GetPreviousTokenIfTouchingWord(position);

			if (token.GetAncestor<ConditionalDirectiveTriviaSyntax>() != null)
			{
				return false;
			}

			if (!attributes)
			{
				if (token.GetAncestor<AttributeListSyntax>() != null)
				{
					return false;
				}
			}

			if (syntaxTree.IsConstantExpressionContext(position, tokenOnLeftOfPosition, cancellationToken))
			{
				return true;
			}

			// no expressions after .   ::   ->
			if (token.Kind() == SyntaxKind.DotToken ||
				token.Kind() == SyntaxKind.ColonColonToken ||
				token.Kind() == SyntaxKind.MinusGreaterThanToken)
			{
				return false;
			}

			// Normally you can have any sort of expression after an equals. However, this does not
			// apply to a "using Foo = ..." situation.
			if (token.Kind() == SyntaxKind.EqualsToken)
			{
				if (token.Parent.IsKind(SyntaxKind.NameEquals) &&
					token.Parent.IsParentKind(SyntaxKind.UsingDirective))
				{
					return false;
				}
			}

			// q = |
			// q -= |
			// q *= |
			// q += |
			// q /= |
			// q ^= |
			// q %= |
			// q &= |
			// q |= |
			// q <<= |
			// q >>= |
			if (token.Kind() == SyntaxKind.EqualsToken ||
				token.Kind() == SyntaxKind.MinusEqualsToken ||
				token.Kind() == SyntaxKind.AsteriskEqualsToken ||
				token.Kind() == SyntaxKind.PlusEqualsToken ||
				token.Kind() == SyntaxKind.SlashEqualsToken ||
				token.Kind() == SyntaxKind.ExclamationEqualsToken ||
				token.Kind() == SyntaxKind.CaretEqualsToken ||
				token.Kind() == SyntaxKind.AmpersandEqualsToken ||
				token.Kind() == SyntaxKind.BarEqualsToken ||
				token.Kind() == SyntaxKind.PercentEqualsToken ||
				token.Kind() == SyntaxKind.LessThanLessThanEqualsToken ||
				token.Kind() == SyntaxKind.GreaterThanGreaterThanEqualsToken)
			{
				return true;
			}

			// ( |
			if (token.Kind() == SyntaxKind.OpenParenToken &&
				token.Parent.IsKind(SyntaxKind.ParenthesizedExpression))
			{
				return true;
			}

			// - |
			// + |
			// ~ |
			// ! |
			if (token.Parent is PrefixUnaryExpressionSyntax)
			{
				var prefix = token.Parent as PrefixUnaryExpressionSyntax;
				return prefix.OperatorToken == token;
			}

			// not sure about these:
			//   ++ |
			//   -- |
			#if false
			token.Kind == SyntaxKind.PlusPlusToken ||
			token.Kind == SyntaxKind.DashDashToken)
			#endif
			// await |
			if (token.Parent is AwaitExpressionSyntax)
			{
				var awaitExpression = token.Parent as AwaitExpressionSyntax;
				return awaitExpression.AwaitKeyword == token;
			}

			// Check for binary operators.
			// Note:
			//   - We handle < specially as it can be ambiguous with generics.
			//   - We handle * specially because it can be ambiguous with pointer types.

			// a *
			// a /
			// a %
			// a +
			// a -
			// a <<
			// a >>
			// a <
			// a >
			// a &&
			// a ||
			// a &
			// a |
			// a ^
			if (token.Parent is BinaryExpressionSyntax)
			{
				// If the client provided a binding, then check if this is actually generic.  If so,
				// then this is not an expression context. i.e. if we have "Foo < |" then it could
				// be an expression context, or it could be a type context if Foo binds to a type or
				// method.
				if (semanticModelOpt != null && syntaxTree.IsGenericTypeArgumentContext(position, tokenOnLeftOfPosition, cancellationToken, semanticModelOpt))
				{
					return false;
				}

				var binary = token.Parent as BinaryExpressionSyntax;
				if (binary.OperatorToken == token)
				{
					// If this is a multiplication expression and a semantic model was passed in,
					// check to see if the expression to the left is a type name. If it is, treat
					// this as a pointer type.
					if (token.Kind() == SyntaxKind.AsteriskToken && semanticModelOpt != null)
					{
						var type = binary.Left as TypeSyntax;
						if (type != null && type.IsPotentialTypeName(semanticModelOpt, cancellationToken))
						{
							return false;
						}
					}

					return true;
				}
			}

			// Special case:
			//    Foo * bar
			//    Foo ? bar
			// This parses as a local decl called bar of type Foo* or Foo?
			if (tokenOnLeftOfPosition.IntersectsWith(position) &&
				tokenOnLeftOfPosition.Kind() == SyntaxKind.IdentifierToken)
			{
				var previousToken = tokenOnLeftOfPosition.GetPreviousToken(includeSkipped: true);
				if (previousToken.Kind() == SyntaxKind.AsteriskToken ||
					previousToken.Kind() == SyntaxKind.QuestionToken)
				{
					if (previousToken.Parent.IsKind(SyntaxKind.PointerType) ||
						previousToken.Parent.IsKind(SyntaxKind.NullableType))
					{
						var type = previousToken.Parent as TypeSyntax;
						if (type.IsParentKind(SyntaxKind.VariableDeclaration) &&
							type.Parent.IsParentKind(SyntaxKind.LocalDeclarationStatement))
						{
							// var declStatement = type.Parent.Parent as LocalDeclarationStatementSyntax;

							// note, this doesn't apply for cases where we know it 
							// absolutely is not multiplcation or a conditional expression.
							var underlyingType = type is PointerTypeSyntax
								? ((PointerTypeSyntax)type).ElementType
								: ((NullableTypeSyntax)type).ElementType;

							if (!underlyingType.IsPotentialTypeName(semanticModelOpt, cancellationToken))
							{
								return true;
							}
						}
					}
				}
			}

			// new int[|
			// new int[expr, |
			if (token.Kind() == SyntaxKind.OpenBracketToken ||
				token.Kind() == SyntaxKind.CommaToken)
			{
				if (token.Parent.IsKind(SyntaxKind.ArrayRankSpecifier))
				{
					return true;
				}
			}

			// foo ? |
			if (token.Kind() == SyntaxKind.QuestionToken &&
				token.Parent.IsKind(SyntaxKind.ConditionalExpression))
			{
				// If the condition is simply a TypeSyntax that binds to a type, treat this as a nullable type.
				var conditionalExpression = (ConditionalExpressionSyntax)token.Parent;
				var type = conditionalExpression.Condition as TypeSyntax;

				return type == null
					|| !type.IsPotentialTypeName(semanticModelOpt, cancellationToken);
			}

			// foo ? bar : |
			if (token.Kind() == SyntaxKind.ColonToken &&
				token.Parent.IsKind(SyntaxKind.ConditionalExpression))
			{
				return true;
			}

			// typeof(|
			// default(|
			// sizeof(|
			if (token.Kind() == SyntaxKind.OpenParenToken)
			{
				if (token.Parent.IsKind(SyntaxKind.TypeOfExpression, SyntaxKind.DefaultExpression, SyntaxKind.SizeOfExpression))
				{
					return false;
				}
			}

			// Foo(|
			// Foo(expr, |
			// this[|
			if (token.Kind() == SyntaxKind.OpenParenToken ||
				token.Kind() == SyntaxKind.OpenBracketToken ||
				token.Kind() == SyntaxKind.CommaToken)
			{
				if (token.Parent.IsKind(SyntaxKind.ArgumentList, SyntaxKind.BracketedArgumentList))
				{
					return true;
				}
			}

			// [Foo(|
			// [Foo(expr, |
			if (attributes)
			{
				if (token.Kind() == SyntaxKind.OpenParenToken ||
					token.Kind() == SyntaxKind.CommaToken)
				{
					if (token.Parent.IsKind(SyntaxKind.AttributeArgumentList))
					{
						return true;
					}
				}
			}

			// Foo(ref |
			// Foo(bar |
			if (token.Kind() == SyntaxKind.RefKeyword ||
				token.Kind() == SyntaxKind.OutKeyword)
			{
				if (token.Parent.IsKind(SyntaxKind.Argument))
				{
					return true;
				}
			}

			// Foo(bar: |
			if (token.Kind() == SyntaxKind.ColonToken &&
				token.Parent.IsKind(SyntaxKind.NameColon) &&
				token.Parent.IsParentKind(SyntaxKind.Argument))
			{
				return true;
			}

			// a => |
			if (token.Kind() == SyntaxKind.EqualsGreaterThanToken)
			{
				return true;
			}

			// new List<int> { |
			// new List<int> { expr, |
			if (token.Kind() == SyntaxKind.OpenBraceToken ||
				token.Kind() == SyntaxKind.CommaToken)
			{
				if (token.Parent is InitializerExpressionSyntax)
				{
					// The compiler treats the ambiguous case as an object initializer, so we'll say
					// expressions are legal here
					if (token.Parent.Kind() == SyntaxKind.ObjectInitializerExpression && token.Kind() == SyntaxKind.OpenBraceToken)
					{
						// In this position { a$$ =, the user is trying to type an object initializer.
						if (!token.IntersectsWith(position) && token.GetNextToken().GetNextToken().Kind() == SyntaxKind.EqualsToken)
						{
							return false;
						}

						return true;
					}

					// Perform a semantic check to determine whether or not the type being created
					// can support a collection initializer. If not, this must be an object initializer
					// and can't be an expression context.
					if (semanticModelOpt != null &&
						token.Parent.IsParentKind(SyntaxKind.ObjectCreationExpression))
					{
						var objectCreation = (ObjectCreationExpressionSyntax)token.Parent.Parent;
						var type = semanticModelOpt.GetSymbolInfo(objectCreation.Type, cancellationToken).Symbol as ITypeSymbol;
						if (type != null && !type.CanSupportCollectionInitializer())
						{
							return false;
						}
					}

					return true;
				}
			}

			// for (; |
			// for (; ; |
			if (token.Kind() == SyntaxKind.SemicolonToken &&
				token.Parent.IsKind(SyntaxKind.ForStatement))
			{
				var forStatement = (ForStatementSyntax)token.Parent;
				if (token == forStatement.FirstSemicolonToken ||
					token == forStatement.SecondSemicolonToken)
				{
					return true;
				}
			}

			// for ( |
			if (token.Kind() == SyntaxKind.OpenParenToken &&
				token.Parent.IsKind(SyntaxKind.ForStatement))
			{
				var forStatement = (ForStatementSyntax)token.Parent;
				if (token == forStatement.OpenParenToken)
				{
					return true;
				}
			}

			// for (; ; Foo(), | 
			// for ( Foo(), |
			if (token.Kind() == SyntaxKind.CommaToken &&
				token.Parent.IsKind(SyntaxKind.ForStatement))
			{
				return true;
			}

			// foreach (var v in |
			// from a in |
			// join b in |
			if (token.Kind() == SyntaxKind.InKeyword)
			{
				if (token.Parent.IsKind(SyntaxKind.ForEachStatement, SyntaxKind.FromClause, SyntaxKind.JoinClause))
				{
					return true;
				}
			}

			// join x in y on |
			// join x in y on a equals |
			if (token.Kind() == SyntaxKind.OnKeyword ||
				token.Kind() == SyntaxKind.EqualsKeyword)
			{
				if (token.Parent.IsKind(SyntaxKind.JoinClause))
				{
					return true;
				}
			}

			// where |
			if (token.Kind() == SyntaxKind.WhereKeyword &&
				token.Parent.IsKind(SyntaxKind.WhereClause))
			{
				return true;
			}

			// orderby |
			// orderby a, |
			if (token.Kind() == SyntaxKind.OrderByKeyword ||
				token.Kind() == SyntaxKind.CommaToken)
			{
				if (token.Parent.IsKind(SyntaxKind.OrderByClause))
				{
					return true;
				}
			}

			// select |
			if (token.Kind() == SyntaxKind.SelectKeyword &&
				token.Parent.IsKind(SyntaxKind.SelectClause))
			{
				return true;
			}

			// group |
			// group expr by |
			if (token.Kind() == SyntaxKind.GroupKeyword ||
				token.Kind() == SyntaxKind.ByKeyword)
			{
				if (token.Parent.IsKind(SyntaxKind.GroupClause))
				{
					return true;
				}
			}

			// return |
			// yield return |
			// but not: [return |
			if (token.Kind() == SyntaxKind.ReturnKeyword)
			{
				if (token.GetPreviousToken(includeSkipped: true).Kind() != SyntaxKind.OpenBracketToken)
				{
					return true;
				}
			}

			// throw |
			if (token.Kind() == SyntaxKind.ThrowKeyword)
			{
				return true;
			}

			// while ( |
			if (token.Kind() == SyntaxKind.OpenParenToken &&
				token.GetPreviousToken(includeSkipped: true).Kind() == SyntaxKind.WhileKeyword)
			{
				return true;
			}

			// todo: handle 'for' cases.

			// using ( |
			if (token.Kind() == SyntaxKind.OpenParenToken &&
				token.GetPreviousToken(includeSkipped: true).Kind() == SyntaxKind.UsingKeyword)
			{
				return true;
			}

			// lock ( |
			if (token.Kind() == SyntaxKind.OpenParenToken &&
				token.GetPreviousToken(includeSkipped: true).Kind() == SyntaxKind.LockKeyword)
			{
				return true;
			}

			// lock ( |
			if (token.Kind() == SyntaxKind.OpenParenToken &&
				token.GetPreviousToken(includeSkipped: true).Kind() == SyntaxKind.IfKeyword)
			{
				return true;
			}

			// switch ( |
			if (token.Kind() == SyntaxKind.OpenParenToken &&
				token.GetPreviousToken(includeSkipped: true).Kind() == SyntaxKind.SwitchKeyword)
			{
				return true;
			}

			// checked ( |
			if (token.Kind() == SyntaxKind.OpenParenToken &&
				token.GetPreviousToken(includeSkipped: true).Kind() == SyntaxKind.CheckedKeyword)
			{
				return true;
			}

			// unchecked ( |
			if (token.Kind() == SyntaxKind.OpenParenToken &&
				token.GetPreviousToken(includeSkipped: true).Kind() == SyntaxKind.UncheckedKeyword)
			{
				return true;
			}

			// TODO: Uncomment on roslyn update when keyord currently missing.
			// when ( |
//			if (token.Kind() == SyntaxKind.OpenParenToken &&
//				token.GetPreviousToken(includeSkipped: true).Kind() == SyntaxKind.WhenKeyword)
//			{
//				return true;
//			}

			// (SomeType) |
			if (token.IsAfterPossibleCast())
			{
				return true;
			}

			// In anonymous type initializer.
			//
			// new { | We allow new inside of anonymous object member declarators, so that the user
			// can dot into a member afterward. For example:
			//
			// var a = new { new C().Foo };
			if (token.Kind() == SyntaxKind.OpenBraceToken || token.Kind() == SyntaxKind.CommaToken)
			{
				if (token.Parent.IsKind(SyntaxKind.AnonymousObjectCreationExpression))
				{
					return true;
				}
			}

			// $"{ |
			// $@"{ |
			// $"{x} { |
			// $@"{x} { |
			// TODO: Uncomment on roslyn update.
//			if (token.Kind() == SyntaxKind.OpenBraceToken)
//			{
//				return token.Parent.IsKind(SyntaxKind.Interpolation)
//					&& ((InterpolationSyntax)token.Parent).OpenBraceToken == token;
//			}
//
			return false;
		}

		public static bool IsNameOfContext(this SyntaxTree syntaxTree, int position, SemanticModel semanticModelOpt = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			var token = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken);
			token = token.GetPreviousTokenIfTouchingWord(position);

			// nameof(Foo.|
			// nameof(Foo.Bar.|
			// Locate the open paren.
			if (token.IsKind(SyntaxKind.DotToken))
			{
				// Could have been parsed as member access
				if (token.Parent.IsKind(SyntaxKind.SimpleMemberAccessExpression))
				{
					var parentMemberAccess = token.Parent;
					while (parentMemberAccess.IsParentKind(SyntaxKind.SimpleMemberAccessExpression))
					{
						parentMemberAccess = parentMemberAccess.Parent;
					}

					if (parentMemberAccess.IsParentKind(SyntaxKind.Argument) &&
						parentMemberAccess.Parent.IsChildNode<ArgumentListSyntax>(a => a.Arguments.FirstOrDefault()))
					{
						token = ((ArgumentListSyntax)parentMemberAccess.Parent.Parent).OpenParenToken;
					}
				}

				// Could have been parsed as a qualified name.
				if (token.Parent.IsKind(SyntaxKind.QualifiedName))
				{
					var parentQualifiedName = token.Parent;
					while (parentQualifiedName.IsParentKind(SyntaxKind.QualifiedName))
					{
						parentQualifiedName = parentQualifiedName.Parent;
					}

					if (parentQualifiedName.IsParentKind(SyntaxKind.Argument) &&
						parentQualifiedName.Parent.IsChildNode<ArgumentListSyntax>(a => a.Arguments.FirstOrDefault()))
					{
						token = ((ArgumentListSyntax)parentQualifiedName.Parent.Parent).OpenParenToken;
					}
				}
			}

			ExpressionSyntax parentExpression = null;

			// if the nameof expression has a missing close paren, it is parsed as an invocation expression.
			if (token.Parent.IsKind(SyntaxKind.ArgumentList) &&
				token.Parent.IsParentKind(SyntaxKind.InvocationExpression))
			{
				var invocationExpression = (InvocationExpressionSyntax)token.Parent.Parent;
				if (!invocationExpression.IsParentKind(SyntaxKind.ConditionalAccessExpression) &&
					!invocationExpression.IsParentKind(SyntaxKind.SimpleMemberAccessExpression) &&
					!invocationExpression.IsParentKind(SyntaxKind.PointerMemberAccessExpression) &&
					invocationExpression.Expression.IsKind(SyntaxKind.IdentifierName) &&
					((IdentifierNameSyntax)invocationExpression.Expression).Identifier.IsKindOrHasMatchingText(SyntaxKind.NameOfKeyword))
				{
					parentExpression = invocationExpression;
				}
			}

			if (parentExpression != null)
			{
				if (semanticModelOpt == null)
				{
					return true;
				}

				return semanticModelOpt.GetSymbolInfo(parentExpression, cancellationToken).Symbol == null;
			}

			return false;
		}

		public static bool IsIsOrAsContext(this SyntaxTree syntaxTree, int position, SyntaxToken tokenOnLeftOfPosition, CancellationToken cancellationToken)
		{
			// cases:
			//    expr |

			var token = tokenOnLeftOfPosition;
			token = token.GetPreviousTokenIfTouchingWord(position);

			if (token.GetAncestor<BlockSyntax>() == null)
			{
				return false;
			}

			// is/as are valid after expressions.
			if (token.IsLastTokenOfNode<ExpressionSyntax>())
			{
				// However, many names look like expressions.  For example:
				//    foreach (var |
				// ('var' is a TypeSyntax which is an expression syntax.

				var type = token.GetAncestors<TypeSyntax>().LastOrDefault();
				if (type == null)
				{
					return true;
				}

				if (type.IsKind(SyntaxKind.GenericName) ||
					type.IsKind(SyntaxKind.AliasQualifiedName) ||
					type.IsKind(SyntaxKind.PredefinedType))
				{
					return false;
				}

				ExpressionSyntax nameExpr = type;
				if (IsRightSideName(nameExpr))
				{
					nameExpr = (ExpressionSyntax)nameExpr.Parent;
				}

				// If this name is the start of a local variable declaration context, we
				// shouldn't show is or as. For example: for(var |
				if (syntaxTree.IsLocalVariableDeclarationContext(token.SpanStart, syntaxTree.FindTokenOnLeftOfPosition(token.SpanStart, cancellationToken), cancellationToken))
				{
					return false;
				}

				// Not on the left hand side of an object initializer
				if (token.IsKind(SyntaxKind.IdentifierToken) &&
					token.Parent.IsKind(SyntaxKind.IdentifierName) &&
					(token.Parent.IsParentKind(SyntaxKind.ObjectInitializerExpression) || token.Parent.IsParentKind(SyntaxKind.CollectionInitializerExpression)))
				{
					return false;
				}

				// Not after an 'out' declaration expression. For example: M(out var |
				if (token.IsKind(SyntaxKind.IdentifierToken) &&
					token.Parent.IsKind(SyntaxKind.IdentifierName))
				{
					if (token.Parent.IsParentKind(SyntaxKind.Argument) &&
						((ArgumentSyntax)token.Parent.Parent).RefOrOutKeyword.IsKind(SyntaxKind.OutKeyword))
					{
						return false;
					}
				}

				// Now, make sure the name was actually in a location valid for
				// an expression.  If so, then we know we can follow it.
				if (syntaxTree.IsExpressionContext(nameExpr.SpanStart, syntaxTree.FindTokenOnLeftOfPosition(nameExpr.SpanStart, cancellationToken), attributes: false, cancellationToken: cancellationToken))
				{
					return true;
				}

				return false;
			}

			return false;
		}

		private static bool IsRightSideName(ExpressionSyntax name)
		{
			if (name.Parent != null)
			{
				switch (name.Parent.Kind())
				{
				case SyntaxKind.QualifiedName:
					return ((QualifiedNameSyntax)name.Parent).Right == name;
				case SyntaxKind.AliasQualifiedName:
					return ((AliasQualifiedNameSyntax)name.Parent).Name == name;
				case SyntaxKind.SimpleMemberAccessExpression:
					return ((MemberAccessExpressionSyntax)name.Parent).Name == name;
				}
			}

			return false;
		}

		public static bool IsCatchOrFinallyContext(
			this SyntaxTree syntaxTree, int position, SyntaxToken tokenOnLeftOfPosition, CancellationToken cancellationToken)
		{
			// cases:
			// try { 
			// } |

			// try {
			// } c|

			// try {
			// } catch {
			// } |

			// try {
			// } catch {
			// } c|

			var token = tokenOnLeftOfPosition;
			token = token.GetPreviousTokenIfTouchingWord(position);

			if (token.Kind() == SyntaxKind.CloseBraceToken)
			{
				var block = token.GetAncestor<BlockSyntax>();

				if (block != null && token == block.GetLastToken(includeSkipped: true))
				{
					if (block.IsParentKind(SyntaxKind.TryStatement) ||
						block.IsParentKind(SyntaxKind.CatchClause))
					{
						return true;
					}
				}
			}

			return false;
		}

		public static bool IsCatchFilterContext(this SyntaxTree syntaxTree, int position, SyntaxToken tokenOnLeftOfPosition)
		{
			// cases:
			//  catch |
			//  catch i|
			//  catch (declaration) |
			//  catch (declaration) i|

			var token = tokenOnLeftOfPosition;
			token = token.GetPreviousTokenIfTouchingWord(position);

			if (token.IsKind(SyntaxKind.CatchKeyword))
			{
				return true;
			}

			if (token.IsKind(SyntaxKind.CloseParenToken) &&
				token.Parent.IsKind(SyntaxKind.CatchDeclaration))
			{
				return true;
			}

			return false;
		}

		public static bool IsEnumBaseListContext(this SyntaxTree syntaxTree, int position, SyntaxToken tokenOnLeftOfPosition, CancellationToken cancellationToken)
		{
			var token = tokenOnLeftOfPosition;
			token = token.GetPreviousTokenIfTouchingWord(position);

			// Options:
			//  enum E : |
			//  enum E : i|

			return
				token.Kind() == SyntaxKind.ColonToken &&
				token.Parent.IsKind(SyntaxKind.BaseList) &&
				token.Parent.IsParentKind(SyntaxKind.EnumDeclaration);
		}

		public static bool IsEnumTypeMemberAccessContext(this SyntaxTree syntaxTree, SemanticModel semanticModel, int position, CancellationToken cancellationToken)
		{
			var token = syntaxTree
				.FindTokenOnLeftOfPosition(position, cancellationToken)
				.GetPreviousTokenIfTouchingWord(position);

			if (!token.IsKind(SyntaxKind.DotToken) ||
				!token.Parent.IsKind(SyntaxKind.SimpleMemberAccessExpression))
			{
				return false;
			}

			var memberAccess = (MemberAccessExpressionSyntax)token.Parent;
			var leftHandBinding = semanticModel.GetSymbolInfo(memberAccess.Expression, cancellationToken);
			var symbol = leftHandBinding.GetBestOrAllSymbols().FirstOrDefault();

			if (symbol == null)
			{
				return false;
			}

			switch (symbol.Kind)
			{
			case SymbolKind.NamedType:
				return ((INamedTypeSymbol)symbol).TypeKind == TypeKind.Enum;
			case SymbolKind.Alias:
				var target = ((IAliasSymbol)symbol).Target;
				return target.IsType && ((ITypeSymbol)target).TypeKind == TypeKind.Enum;
			}

			return false;
		}
	}
}

