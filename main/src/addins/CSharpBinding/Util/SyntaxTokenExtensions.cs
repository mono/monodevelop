// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;
using System.Threading;

namespace ICSharpCode.NRefactory6.CSharp
{
	static class SyntaxTokenExtensions
	{
		public static SyntaxNode GetAncestor(this SyntaxToken token, Func<SyntaxNode, bool> predicate)
		{
			return token.GetAncestor<SyntaxNode>(predicate);
		}

		public static T GetAncestor<T>(this SyntaxToken token, Func<T, bool> predicate = null)
			where T : SyntaxNode
		{
			return token.Parent != null
				? token.Parent.FirstAncestorOrSelf(predicate)
					: default(T);
		}

		public static IEnumerable<T> GetAncestors<T>(this SyntaxToken token)
			where T : SyntaxNode
		{
			return token.Parent != null
				? token.Parent.AncestorsAndSelf ().OfType<T> ()
					: Enumerable.Empty<T> ();
		}

		public static IEnumerable<SyntaxNode> GetAncestors(this SyntaxToken token, Func<SyntaxNode, bool> predicate)
		{
			return token.Parent != null
				? token.Parent.AncestorsAndSelf().Where(predicate)
					: Enumerable.Empty<SyntaxNode>();
		}

		public static SyntaxNode GetCommonRoot(this SyntaxToken token1, SyntaxToken token2)
		{
			// Contract.ThrowIfTrue(token1.RawKind == 0 || token2.RawKind == 0);

			// find common starting node from two tokens.
			// as long as two tokens belong to same tree, there must be at least on common root (Ex, compilation unit)
			if (token1.Parent == null || token2.Parent == null)
			{
				return null;
			}

			return token1.Parent.GetCommonRoot(token2.Parent);
		}

		public static bool CheckParent<T>(this SyntaxToken token, Func<T, bool> valueChecker) where T : SyntaxNode
		{
			var parentNode = token.Parent as T;
			if (parentNode == null)
			{
				return false;
			}

			return valueChecker(parentNode);
		}

		public static int Width(this SyntaxToken token)
		{
			return token.Span.Length;
		}

		public static int FullWidth(this SyntaxToken token)
		{
			return token.FullSpan.Length;
		}

		public static SyntaxToken FindTokenFromEnd(this SyntaxNode root, int position, bool includeZeroWidth = true, bool findInsideTrivia = false)
		{
			var token = root.FindToken(position, findInsideTrivia);
			var previousToken = token.GetPreviousToken(
				includeZeroWidth, findInsideTrivia, findInsideTrivia, findInsideTrivia);

			if (token.SpanStart == position &&
				previousToken.RawKind != 0 &&
				previousToken.Span.End == position)
			{
				return previousToken;
			}

			return token;
		}

		public static bool IsUsingOrExternKeyword(this SyntaxToken token)
		{
			return
				token.Kind() == SyntaxKind.UsingKeyword ||
				token.Kind() == SyntaxKind.ExternKeyword;
		}

		public static bool IsUsingKeywordInUsingDirective(this SyntaxToken token)
		{
			if (token.IsKind(SyntaxKind.UsingKeyword))
			{
				var usingDirective = token.GetAncestor<UsingDirectiveSyntax>();
				if (usingDirective != null &&
					usingDirective.UsingKeyword == token)
				{
					return true;
				}
			}

			return false;
		}

		public static bool IsStaticKeywordInUsingDirective(this SyntaxToken token)
		{
			if (token.IsKind(SyntaxKind.StaticKeyword))
			{
				var usingDirective = token.GetAncestor<UsingDirectiveSyntax>();
				if (usingDirective != null &&
					usingDirective.StaticKeyword == token)
				{
					return true;
				}
			}

			return false;
		}

		public static bool IsBeginningOfStatementContext(this SyntaxToken token)
		{
			// cases:
			//    {
			//      |

			// }
			// |

			// Note, the following is *not* a legal statement context: 
			//    do { } |

			// ...;
			// |

			// case 0:
			//   |

			// default:
			//   |

			// label:
			//   |

			// if (foo)
			//   |

			// while (true)
			//   |

			// do
			//   |

			// for (;;)
			//   |

			// foreach (var v in c)
			//   |

			// else
			//   |

			// using (expr)
			//   |

			// lock (expr)
			//   |

			// for ( ; ; Foo(), |

			if (token.Kind() == SyntaxKind.OpenBraceToken &&
				token.Parent.IsKind(SyntaxKind.Block))
			{
				return true;
			}

			if (token.Kind() == SyntaxKind.SemicolonToken)
			{
				var statement = token.GetAncestor<StatementSyntax>();
				if (statement != null && !statement.IsParentKind(SyntaxKind.GlobalStatement) &&
					statement.GetLastToken(includeZeroWidth: true) == token)
				{
					return true;
				}
			}

			if (token.Kind() == SyntaxKind.CloseBraceToken &&
				token.Parent.IsKind(SyntaxKind.Block))
			{
				if (token.Parent.Parent is StatementSyntax)
				{
					// Most blocks that are the child of statement are places
					// that we can follow with another statement.  i.e.:
					// if { }
					// while () { }
					// There are two exceptions.
					// try {}
					// do {}
					if (!token.Parent.IsParentKind(SyntaxKind.TryStatement) &&
						!token.Parent.IsParentKind(SyntaxKind.DoStatement))
					{
						return true;
					}
				}
				else if (
					token.Parent.IsParentKind(SyntaxKind.ElseClause) ||
					token.Parent.IsParentKind(SyntaxKind.FinallyClause) ||
					token.Parent.IsParentKind(SyntaxKind.CatchClause) ||
					token.Parent.IsParentKind(SyntaxKind.SwitchSection))
				{
					return true;
				}
			}

			if (token.Kind() == SyntaxKind.CloseBraceToken &&
				token.Parent.IsKind(SyntaxKind.SwitchStatement))
			{
				return true;
			}

			if (token.Kind() == SyntaxKind.ColonToken)
			{
				if (token.Parent.IsKind(SyntaxKind.CaseSwitchLabel, SyntaxKind.DefaultSwitchLabel, SyntaxKind.LabeledStatement))
				{
					return true;
				}
			}

			if (token.Kind() == SyntaxKind.DoKeyword &&
				token.Parent.IsKind(SyntaxKind.DoStatement))
			{
				return true;
			}

			if (token.Kind() == SyntaxKind.CloseParenToken)
			{
				var parent = token.Parent;
				if (parent.IsKind(SyntaxKind.ForStatement) ||
					parent.IsKind(SyntaxKind.ForEachStatement) ||
					parent.IsKind(SyntaxKind.WhileStatement) ||
					parent.IsKind(SyntaxKind.IfStatement) ||
					parent.IsKind(SyntaxKind.LockStatement) ||
					parent.IsKind(SyntaxKind.UsingStatement))
				{
					return true;
				}
			}

			if (token.Kind() == SyntaxKind.ElseKeyword)
			{
				return true;
			}

			return false;
		}

		public static bool IsBeginningOfGlobalStatementContext(this SyntaxToken token)
		{
			// cases:
			// }
			// |

			// ...;
			// |

			// extern alias Foo;
			// using System;
			// |

			// [assembly: Foo]
			// |

			if (token.Kind() == SyntaxKind.CloseBraceToken)
			{
				var memberDeclaration = token.GetAncestor<MemberDeclarationSyntax>();
				if (memberDeclaration != null && memberDeclaration.GetLastToken(includeZeroWidth: true) == token &&
					memberDeclaration.IsParentKind(SyntaxKind.CompilationUnit))
				{
					return true;
				}
			}

			if (token.Kind() == SyntaxKind.SemicolonToken)
			{
				var globalStatement = token.GetAncestor<GlobalStatementSyntax>();
				if (globalStatement != null && globalStatement.GetLastToken(includeZeroWidth: true) == token)
				{
					return true;
				}

				var memberDeclaration = token.GetAncestor<MemberDeclarationSyntax>();
				if (memberDeclaration != null && memberDeclaration.GetLastToken(includeZeroWidth: true) == token &&
					memberDeclaration.IsParentKind(SyntaxKind.CompilationUnit))
				{
					return true;
				}

				var compUnit = token.GetAncestor<CompilationUnitSyntax>();
				if (compUnit != null)
				{
					if (compUnit.Usings.Count > 0 && compUnit.Usings.Last().GetLastToken(includeZeroWidth: true) == token)
					{
						return true;
					}

					if (compUnit.Externs.Count > 0 && compUnit.Externs.Last().GetLastToken(includeZeroWidth: true) == token)
					{
						return true;
					}
				}
			}

			if (token.Kind() == SyntaxKind.CloseBracketToken)
			{
				var compUnit = token.GetAncestor<CompilationUnitSyntax>();
				if (compUnit != null)
				{
					if (compUnit.AttributeLists.Count > 0 && compUnit.AttributeLists.Last().GetLastToken(includeZeroWidth: true) == token)
					{
						return true;
					}
				}
			}

			return false;
		}

		public static bool IsAfterPossibleCast(this SyntaxToken token)
		{
			if (token.Kind() == SyntaxKind.CloseParenToken)
			{
				if (token.Parent.IsKind(SyntaxKind.CastExpression))
				{
					return true;
				}

				if (token.Parent.IsKind(SyntaxKind.ParenthesizedExpression))
				{
					var parenExpr = token.Parent as ParenthesizedExpressionSyntax;
					var expr = parenExpr.Expression;

					if (expr is TypeSyntax)
					{
						return true;
					}
				}
			}

			return false;
		}

		public static bool IsLastTokenOfNode<T>(this SyntaxToken token)
			where T : SyntaxNode
		{
			var node = token.GetAncestor<T>();
			return node != null && token == node.GetLastToken(includeZeroWidth: true);
		}

		public static bool IsLastTokenOfQueryClause(this SyntaxToken token)
		{
			if (token.IsLastTokenOfNode<QueryClauseSyntax>())
			{
				return true;
			}

			if (token.Kind() == SyntaxKind.IdentifierToken &&
				token.GetPreviousToken(includeSkipped: true).Kind() == SyntaxKind.IntoKeyword)
			{
				return true;
			}

			return false;
		}

		public static bool IsPreProcessorExpressionContext(this SyntaxToken targetToken)
		{
			// cases:
			//   #if |
			//   #if foo || |
			//   #if foo && |
			//   #if ( |
			//   #if ! |
			// Same for elif

			if (targetToken.GetAncestor<ConditionalDirectiveTriviaSyntax>() == null)
			{
				return false;
			}

			// #if
			// #elif
			if (targetToken.Kind() == SyntaxKind.IfKeyword ||
				targetToken.Kind() == SyntaxKind.ElifKeyword)
			{
				return true;
			}

			// ( |
			if (targetToken.Kind() == SyntaxKind.OpenParenToken &&
				targetToken.Parent.IsKind(SyntaxKind.ParenthesizedExpression))
			{
				return true;
			}

			// ! |
			if (targetToken.Parent is PrefixUnaryExpressionSyntax)
			{
				var prefix = targetToken.Parent as PrefixUnaryExpressionSyntax;
				return prefix.OperatorToken == targetToken;
			}

			// a &&
			// a ||
			if (targetToken.Parent is BinaryExpressionSyntax)
			{
				var binary = targetToken.Parent as BinaryExpressionSyntax;
				return binary.OperatorToken == targetToken;
			}

			return false;
		}

		public static bool IsOrderByDirectionContext(this SyntaxToken targetToken)
		{
			// cases:
			//   orderby a |
			//   orderby a a|
			//   orderby a, b |
			//   orderby a, b a|

			if (!targetToken.IsKind(SyntaxKind.IdentifierToken, SyntaxKind.CloseParenToken, SyntaxKind.CloseBracketToken))
			{
				return false;
			}

			var ordering = targetToken.GetAncestor<OrderingSyntax>();
			if (ordering == null)
			{
				return false;
			}

			// orderby a |
			// orderby a, b |
			var lastToken = ordering.Expression.GetLastToken(includeSkipped: true);

			if (targetToken == lastToken)
			{
				return true;
			}

			return false;
		}

		public static bool IsSwitchLabelContext(this SyntaxToken targetToken)
		{
			// cases:
			//   case X: |
			//   default: |
			//   switch (e) { |
			//
			//   case X: Statement(); |

			if (targetToken.Kind() == SyntaxKind.OpenBraceToken &&
				targetToken.Parent.IsKind(SyntaxKind.SwitchStatement))
			{
				return true;
			}

			if (targetToken.Kind() == SyntaxKind.ColonToken)
			{
				if (targetToken.Parent.IsKind(SyntaxKind.CaseSwitchLabel, SyntaxKind.DefaultSwitchLabel))
				{
					return true;
				}
			}

			if (targetToken.Kind() == SyntaxKind.SemicolonToken ||
				targetToken.Kind() == SyntaxKind.CloseBraceToken)
			{
				var section = targetToken.GetAncestor<SwitchSectionSyntax>();
				if (section != null)
				{
					foreach (var statement in section.Statements)
					{
						if (targetToken == statement.GetLastToken(includeSkipped: true))
						{
							return true;
						}
					}
				}
			}

			return false;
		}

		public static bool IsXmlCrefParameterModifierContext(this SyntaxToken targetToken)
		{
			return targetToken.IsKind(SyntaxKind.CommaToken, SyntaxKind.OpenParenToken)
				&& targetToken.Parent.IsKind(SyntaxKind.CrefBracketedParameterList, SyntaxKind.CrefParameterList);
		}

		public static bool IsConstructorOrMethodParameterArgumentContext(this SyntaxToken targetToken)
		{
			// cases:
			//   Foo( |
			//   Foo(expr, |
			//   Foo(bar: |
			//   new Foo( |
			//   new Foo(expr, |
			//   new Foo(bar: |
			//   Foo : base( |
			//   Foo : base(bar: |
			//   Foo : this( |
			//   Foo : ths(bar: |

			// Foo(bar: |
			if (targetToken.Kind() == SyntaxKind.ColonToken &&
				targetToken.Parent.IsKind(SyntaxKind.NameColon) &&
				targetToken.Parent.IsParentKind(SyntaxKind.Argument) &&
				targetToken.Parent.GetParent().IsParentKind(SyntaxKind.ArgumentList))
			{
				var owner = targetToken.Parent.GetParent().GetParent().GetParent();
				if (owner.IsKind(SyntaxKind.InvocationExpression) ||
					owner.IsKind(SyntaxKind.ObjectCreationExpression) ||
					owner.IsKind(SyntaxKind.BaseConstructorInitializer) ||
					owner.IsKind(SyntaxKind.ThisConstructorInitializer))
				{
					return true;
				}
			}

			if (targetToken.Kind() == SyntaxKind.OpenParenToken ||
				targetToken.Kind() == SyntaxKind.CommaToken)
			{
				if (targetToken.Parent.IsKind(SyntaxKind.ArgumentList))
				{
					if (targetToken.Parent.IsParentKind(SyntaxKind.InvocationExpression) ||
						targetToken.Parent.IsParentKind(SyntaxKind.ObjectCreationExpression) ||
						targetToken.Parent.IsParentKind(SyntaxKind.BaseConstructorInitializer) ||
						targetToken.Parent.IsParentKind(SyntaxKind.ThisConstructorInitializer))
					{
						return true;
					}
				}
			}

			return false;
		}

		public static bool IsUnaryOperatorContext(this SyntaxToken targetToken)
		{
			if (targetToken.Kind() == SyntaxKind.OperatorKeyword &&
				targetToken.GetPreviousToken(includeSkipped: true).IsLastTokenOfNode<TypeSyntax>())
			{
				return true;
			}

			return false;
		}

		public static bool IsUnsafeContext(this SyntaxToken targetToken)
		{
			return
				targetToken.GetAncestors<StatementSyntax>().Any(s => s.IsKind(SyntaxKind.UnsafeStatement)) ||
				targetToken.GetAncestors<MemberDeclarationSyntax>().Any(m => m.GetModifiers().Any(SyntaxKind.UnsafeKeyword));
		}

		public static bool IsAfterYieldKeyword(this SyntaxToken targetToken)
		{
			// yield |
			// yield r|

			if (targetToken.IsKindOrHasMatchingText(SyntaxKind.YieldKeyword))
			{
				return true;
			}

			return false;
		}

		public static bool IsAccessorDeclarationContext<TMemberNode>(this SyntaxToken targetToken, int position, SyntaxKind kind = SyntaxKind.None)
			where TMemberNode : SyntaxNode
		{
			if (!IsAccessorDeclarationContextWorker(targetToken))
			{
				return false;
			}

			var list = targetToken.GetAncestor<AccessorListSyntax>();
			if (list == null)
			{
				return false;
			}

			// Check if we already have this accessor.  (however, don't count it
			// if the user is *on* that accessor.
			var existingAccessor = list.Accessors
				.Select(a => a.Keyword)
				.FirstOrDefault(a => !a.IsMissing && a.IsKindOrHasMatchingText(kind));

			if (existingAccessor.Kind() != SyntaxKind.None)
			{
				var existingAccessorSpan = existingAccessor.Span;
				if (!existingAccessorSpan.IntersectsWith(position))
				{
					return false;
				}
			}

			var decl = targetToken.GetAncestor<TMemberNode>();
			return decl != null;
		}

		private static bool IsAccessorDeclarationContextWorker(SyntaxToken targetToken)
		{
			// cases:
			//   int Foo { |
			//   int Foo { private |
			//   int Foo { set { } |
			//   int Foo { set; |
			//   int Foo { [Bar]|

			// Consume all preceding access modifiers
			while (targetToken.Kind() == SyntaxKind.InternalKeyword ||
				targetToken.Kind() == SyntaxKind.PublicKeyword ||
				targetToken.Kind() == SyntaxKind.ProtectedKeyword ||
				targetToken.Kind() == SyntaxKind.PrivateKeyword)
			{
				targetToken = targetToken.GetPreviousToken(includeSkipped: true);
			}

			// int Foo { |
			// int Foo { private |
			if (targetToken.Kind() == SyntaxKind.OpenBraceToken &&
				targetToken.Parent.IsKind(SyntaxKind.AccessorList))
			{
				return true;
			}

			// int Foo { set { } |
			// int Foo { set { } private |
			if (targetToken.Kind() == SyntaxKind.CloseBraceToken &&
				targetToken.Parent.IsKind(SyntaxKind.Block) &&
				targetToken.Parent.GetParent() is AccessorDeclarationSyntax)
			{
				return true;
			}

			// int Foo { set; |
			if (targetToken.Kind() == SyntaxKind.SemicolonToken &&
				targetToken.Parent is AccessorDeclarationSyntax)
			{
				return true;
			}

			// int Foo { [Bar]|
			if (targetToken.Kind() == SyntaxKind.CloseBracketToken &&
				targetToken.Parent.IsKind(SyntaxKind.AttributeList) &&
				targetToken.Parent.GetParent() is AccessorDeclarationSyntax)
			{
				return true;
			}

			return false;
		}

		private static bool IsGenericInterfaceOrDelegateTypeParameterList(SyntaxNode node)
		{
			if (node.IsKind(SyntaxKind.TypeParameterList))
			{
				if (node.IsParentKind(SyntaxKind.InterfaceDeclaration))
				{
					var decl = node.Parent as TypeDeclarationSyntax;
					return decl.TypeParameterList == node;
				}
				else if (node.IsParentKind(SyntaxKind.DelegateDeclaration))
				{
					var decl = node.Parent as DelegateDeclarationSyntax;
					return decl.TypeParameterList == node;
				}
			}

			return false;
		}

		public static bool IsTypeParameterVarianceContext(this SyntaxToken targetToken)
		{
			// cases:
			// interface IFoo<|
			// interface IFoo<A,|
			// interface IFoo<[Bar]|

			// deletate X D<|
			// deletate X D<A,|
			// deletate X D<[Bar]|
			if (targetToken.Kind() == SyntaxKind.LessThanToken &&
				IsGenericInterfaceOrDelegateTypeParameterList(targetToken.Parent))
			{
				return true;
			}

			if (targetToken.Kind() == SyntaxKind.CommaToken &&
				IsGenericInterfaceOrDelegateTypeParameterList(targetToken.Parent))
			{
				return true;
			}

			if (targetToken.Kind() == SyntaxKind.CloseBracketToken &&
				targetToken.Parent.IsKind(SyntaxKind.AttributeList) &&
				targetToken.Parent.IsParentKind(SyntaxKind.TypeParameter) &&
				IsGenericInterfaceOrDelegateTypeParameterList(targetToken.Parent.GetParent().GetParent()))
			{
				return true;
			}

			return false;
		}

		public static bool IsMandatoryNamedParameterPosition(this SyntaxToken token)
		{
			if (token.Kind() == SyntaxKind.CommaToken && token.Parent is BaseArgumentListSyntax)
			{
				var argumentList = (BaseArgumentListSyntax)token.Parent;

				foreach (var item in argumentList.Arguments.GetWithSeparators())
				{
					if (item.IsToken && item.AsToken() == token)
					{
						return false;
					}

					if (item.IsNode)
					{
						var node = item.AsNode() as ArgumentSyntax;
						if (node != null && node.NameColon != null)
						{
							return true;
						}
					}
				}
			}

			return false;
		}

		public static bool IsKindOrHasMatchingText(this SyntaxToken token, SyntaxKind kind)
		{
			return token.Kind() == kind || token.HasMatchingText(kind);
		}

		public static bool HasMatchingText(this SyntaxToken token, SyntaxKind kind)
		{
			return token.ToString() == SyntaxFacts.GetText(kind);
		}

		public static bool IsKind(this SyntaxToken token, SyntaxKind kind1, SyntaxKind kind2)
		{
			return token.Kind() == kind1
				|| token.Kind() == kind2;
		}

		public static bool IsKind(this SyntaxToken token, SyntaxKind kind1, SyntaxKind kind2, SyntaxKind kind3)
		{
			return token.Kind() == kind1
				|| token.Kind() == kind2
				|| token.Kind() == kind3;
		}

		public static bool IsKind(this SyntaxToken token, params SyntaxKind[] kinds)
		{
			return kinds.Contains(token.Kind());
		}

		public static bool IsLiteral(this SyntaxToken token)
		{
			switch (token.Kind())
			{
			case SyntaxKind.CharacterLiteralToken:
			case SyntaxKind.FalseKeyword:
			case SyntaxKind.NumericLiteralToken:
			case SyntaxKind.StringLiteralToken:
			case SyntaxKind.TrueKeyword:
				return true;

			default:
				return false;
			}
		}

		public static bool IntersectsWith(this SyntaxToken token, int position)
		{
			return token.Span.IntersectsWith(position);
		}

		public static SyntaxToken GetPreviousTokenIfTouchingWord(this SyntaxToken token, int position)
		{
			return token.IntersectsWith(position) && IsWord(token)
				? token.GetPreviousToken(includeSkipped: true)
					: token;
		}

		public static bool IsWord(this SyntaxToken token)
		{
			return token.IsKind(SyntaxKind.IdentifierToken)
				|| SyntaxFacts.IsKeywordKind(token.Kind())
				|| SyntaxFacts.IsContextualKeyword(token.Kind())
				|| SyntaxFacts.IsPreprocessorKeyword(token.Kind());
		}

		public static SyntaxToken GetNextNonZeroWidthTokenOrEndOfFile(this SyntaxToken token)
		{
			return token.GetNextTokenOrEndOfFile();
		}

		public static SyntaxToken GetNextTokenOrEndOfFile(
			this SyntaxToken token,
			bool includeZeroWidth = false,
			bool includeSkipped = false,
			bool includeDirectives = false,
			bool includeDocumentationComments = false)
		{
			var nextToken = token.GetNextToken(includeZeroWidth, includeSkipped, includeDirectives, includeDocumentationComments);

			return nextToken.Kind() == SyntaxKind.None
				? token.GetAncestor<CompilationUnitSyntax>().EndOfFileToken
					: nextToken;
		}

		public static SyntaxToken With(this SyntaxToken token, SyntaxTriviaList leading, SyntaxTriviaList trailing)
		{
			return token.WithLeadingTrivia(leading).WithTrailingTrivia(trailing);
		}

		/// <summary>
		/// Determines whether the given SyntaxToken is the first token on a line in the specified SourceText.
		/// </summary>
		public static bool IsFirstTokenOnLine(this SyntaxToken token, SourceText text)
		{
			var previousToken = token.GetPreviousToken(includeSkipped: true, includeDirectives: true, includeDocumentationComments: true);
			if (previousToken.Kind() == SyntaxKind.None)
			{
				return true;
			}

			var tokenLine = text.Lines.IndexOf(token.SpanStart);
			var previousTokenLine = text.Lines.IndexOf(previousToken.SpanStart);
			return tokenLine > previousTokenLine;
		}

		public static bool SpansPreprocessorDirective(this IEnumerable<SyntaxToken> tokens)
		{
			// we want to check all leading trivia of all tokens (except the 
			// first one), and all trailing trivia of all tokens (except the
			// last one).

			var first = true;
			var previousToken = default(SyntaxToken);

			foreach (var token in tokens)
			{
				if (first)
				{
					first = false;
				}
				else
				{
					// check the leading trivia of this token, and the trailing trivia
					// of the previous token.
					if (SpansPreprocessorDirective(token.LeadingTrivia) ||
						SpansPreprocessorDirective(previousToken.TrailingTrivia))
					{
						return true;
					}
				}

				previousToken = token;
			}

			return false;
		}

		private static bool SpansPreprocessorDirective(SyntaxTriviaList list)
		{
			return list.Any(t => t.GetStructure() is DirectiveTriviaSyntax);
		}

		public static SyntaxToken WithoutTrivia(
			this SyntaxToken token,
			params SyntaxTrivia[] trivia)
		{
			if (!token.LeadingTrivia.Any() && !token.TrailingTrivia.Any())
			{
				return token;
			}

			return token.With(new SyntaxTriviaList(), new SyntaxTriviaList());
		}

		public static SyntaxToken WithPrependedLeadingTrivia(
			this SyntaxToken token,
			params SyntaxTrivia[] trivia)
		{
			if (trivia.Length == 0)
			{
				return token;
			}

			return token.WithPrependedLeadingTrivia((IEnumerable<SyntaxTrivia>)trivia);
		}

		public static SyntaxToken WithPrependedLeadingTrivia(
			this SyntaxToken token,
			SyntaxTriviaList trivia)
		{
			if (trivia.Count == 0)
			{
				return token;
			}

			return token.WithLeadingTrivia(trivia.Concat(token.LeadingTrivia));
		}

		public static SyntaxToken WithPrependedLeadingTrivia(
			this SyntaxToken token,
			IEnumerable<SyntaxTrivia> trivia)
		{
			return token.WithPrependedLeadingTrivia(trivia.ToSyntaxTriviaList());
		}

		public static SyntaxToken WithAppendedTrailingTrivia(
			this SyntaxToken token,
			IEnumerable<SyntaxTrivia> trivia)
		{
			return token.WithTrailingTrivia(token.TrailingTrivia.Concat(trivia));
		}

		/// <summary>
		/// Retrieves all trivia after this token, including it's trailing trivia and
		/// the leading trivia of the next token.
		/// </summary>
		public static IEnumerable<SyntaxTrivia> GetAllTrailingTrivia(this SyntaxToken token)
		{
			foreach (var trivia in token.TrailingTrivia)
			{
				yield return trivia;
			}

			var nextToken = token.GetNextTokenOrEndOfFile(includeZeroWidth: true, includeSkipped: true, includeDirectives: true, includeDocumentationComments: true);

			foreach (var trivia in nextToken.LeadingTrivia)
			{
				yield return trivia;
			}
		}

		public static bool TryParseGenericName(this SyntaxToken genericIdentifier, CancellationToken cancellationToken, out GenericNameSyntax genericName)
		{
			if (genericIdentifier.GetNextToken(includeSkipped: true).Kind() == SyntaxKind.LessThanToken)
			{
				var lastToken = genericIdentifier.FindLastTokenOfPartialGenericName();

				var syntaxTree = genericIdentifier.SyntaxTree;
				var name = SyntaxFactory.ParseName(syntaxTree.GetText(cancellationToken).ToString(TextSpan.FromBounds(genericIdentifier.SpanStart, lastToken.Span.End)));

				genericName = name as GenericNameSyntax;
				return genericName != null;
			}

			genericName = null;
			return false;
		}

		/// <summary>
		/// Lexically, find the last token that looks like it's part of this generic name.
		/// </summary>
		/// <param name="genericIdentifier">The "name" of the generic identifier, last token before
		/// the "&amp;"</param>
		/// <returns>The last token in the name</returns>
		/// <remarks>This is related to the code in <see cref="SyntaxTreeExtensions.IsInPartiallyWrittenGeneric(SyntaxTree, int, CancellationToken)"/></remarks>
		public static SyntaxToken FindLastTokenOfPartialGenericName(this SyntaxToken genericIdentifier)
		{
			//Contract.ThrowIfFalse(genericIdentifier.Kind() == SyntaxKind.IdentifierToken);

			// advance to the "<" token
			var token = genericIdentifier.GetNextToken(includeSkipped: true);
			//Contract.ThrowIfFalse(token.Kind() == SyntaxKind.LessThanToken);

			int stack = 0;

			do
			{
				// look forward one token
				{
					var next = token.GetNextToken(includeSkipped: true);
					if (next.Kind() == SyntaxKind.None)
					{
						return token;
					}

					token = next;
				}

				if (token.Kind() == SyntaxKind.GreaterThanToken)
				{
					if (stack == 0)
					{
						return token;
					}
					else
					{
						stack--;
						continue;
					}
				}

				switch (token.Kind())
				{
				case SyntaxKind.LessThanLessThanToken:
					stack++;
					goto case SyntaxKind.LessThanToken;

					// fall through
				case SyntaxKind.LessThanToken:
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
				case SyntaxKind.CommaToken:
					break;

					// If we see a member declaration keyword, we know we've gone too far
				case SyntaxKind.ClassKeyword:
				case SyntaxKind.StructKeyword:
				case SyntaxKind.InterfaceKeyword:
				case SyntaxKind.DelegateKeyword:
				case SyntaxKind.EnumKeyword:
				case SyntaxKind.PrivateKeyword:
				case SyntaxKind.PublicKeyword:
				case SyntaxKind.InternalKeyword:
				case SyntaxKind.ProtectedKeyword:
				case SyntaxKind.VoidKeyword:
					return token.GetPreviousToken(includeSkipped: true);

				default:
					// user might have typed "in" on the way to typing "int"
					// don't want to disregard this genericname because of that
					if (SyntaxFacts.IsKeywordKind(token.Kind()))
					{
						break;
					}

					// anything else and we're sunk. Go back to the token before.
					return token.GetPreviousToken(includeSkipped: true);
				}
			}
			while (true);
		}

		public static bool IsRegularStringLiteral(this SyntaxToken token)
		{
			return token.Kind() == SyntaxKind.StringLiteralToken && !token.IsVerbatimStringLiteral();
		}

		public static bool IsValidAttributeTarget(this SyntaxToken token)
		{
			switch (token.Kind())
			{
			case SyntaxKind.AssemblyKeyword:
			case SyntaxKind.ModuleKeyword:
			case SyntaxKind.FieldKeyword:
			case SyntaxKind.EventKeyword:
			case SyntaxKind.MethodKeyword:
			case SyntaxKind.ParamKeyword:
			case SyntaxKind.PropertyKeyword:
			case SyntaxKind.ReturnKeyword:
			case SyntaxKind.TypeKeyword:
				return true;

			default:
				return false;
			}
		}

	}
}
