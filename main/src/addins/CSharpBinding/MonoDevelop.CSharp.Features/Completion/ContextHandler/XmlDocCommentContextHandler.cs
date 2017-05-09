//
// XmlDocCommentContextHandler.cs
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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using MonoDevelop.Ide.CodeCompletion;
using Roslyn.Utilities;

namespace ICSharpCode.NRefactory6.CSharp.Completion
{
	class XmlDocCommentContextHandler : CompletionContextHandler
	{
		protected async override Task<IEnumerable<CompletionData>> GetItemsWorkerAsync (CompletionResult completionResult, CompletionEngine engine, CompletionContext completionContext, CompletionTriggerInfo info, SyntaxContext ctx, CancellationToken cancellationToken)
		{
			if (info.IsDebugger)
			{
				return null;
			}
			if (info.CompletionTriggerReason == CompletionTriggerReason.BackspaceOrDeleteCommand)
				return null;
			var document = completionContext.Document;
			var position = completionContext.Position;

			var tree = await document.GetCSharpSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
			var token = tree.FindTokenOnLeftOfPosition(position, cancellationToken);
			var parentTrivia = token.GetAncestor<DocumentationCommentTriviaSyntax>();

			if (parentTrivia == null)
			{
				return null;
			}

			var items = new List<CompletionData>();
			var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
			var span = GetTextChangeSpan(text, position);

			var attachedToken = parentTrivia.ParentTrivia.Token;
			if (attachedToken.Kind() == SyntaxKind.None)
			{
				return null;
			}

			var semanticModel = await document.GetCSharpSemanticModelForNodeAsync(attachedToken.Parent, cancellationToken).ConfigureAwait(false);

			ISymbol declaredSymbol = null;
			var memberDeclaration = attachedToken.GetAncestor<MemberDeclarationSyntax>();
			if (memberDeclaration != null)
			{
				declaredSymbol = semanticModel.GetDeclaredSymbol(memberDeclaration, cancellationToken);
			}
			else
			{
				var typeDeclaration = attachedToken.GetAncestor<TypeDeclarationSyntax>();
				if (typeDeclaration != null)
				{
					declaredSymbol = semanticModel.GetDeclaredSymbol(typeDeclaration, cancellationToken);
				}
			}

			if (declaredSymbol != null)
			{
				items.AddRange(GetTagsForSymbol(engine, declaredSymbol, span, parentTrivia, token));
			}

			if (token.Parent.Kind() == SyntaxKind.XmlEmptyElement || token.Parent.Kind() == SyntaxKind.XmlText ||
				(token.Parent.IsKind(SyntaxKind.XmlElementEndTag) && token.IsKind(SyntaxKind.GreaterThanToken)) ||
				(token.Parent.IsKind(SyntaxKind.XmlName) && token.Parent.IsParentKind(SyntaxKind.XmlEmptyElement)))
			{
				if (token.Parent.Parent.Kind() == SyntaxKind.XmlElement)
				{
					items.AddRange(GetNestedTags(engine, span));
				}

				if (token.Parent.Parent.Kind() == SyntaxKind.XmlElement && ((XmlElementSyntax)token.Parent.Parent).StartTag.Name.LocalName.ValueText == "list")
				{
					items.AddRange(GetListItems(engine, span));
				}

				if (token.Parent.IsParentKind(SyntaxKind.XmlEmptyElement) & token.Parent.Parent.IsParentKind(SyntaxKind.XmlElement))
				{
					var element = (XmlElementSyntax)token.Parent.Parent.Parent;
					if (element.StartTag.Name.LocalName.ValueText == "list")
					{
						items.AddRange(GetListItems(engine, span));
					}
				}

				if (token.Parent.Parent.Kind() == SyntaxKind.XmlElement && ((XmlElementSyntax)token.Parent.Parent).StartTag.Name.LocalName.ValueText == "listheader")
				{
					items.AddRange(GetListHeaderItems(engine, span));
				}

				if (token.Parent.Parent is DocumentationCommentTriviaSyntax)
				{
					items.AddRange(GetTopLevelSingleUseNames(engine, parentTrivia, span));
					items.AddRange(GetTopLevelRepeatableItems(engine, span));
				}
			}

			if (token.Parent.Kind() == SyntaxKind.XmlElementStartTag)
			{
				var startTag = (XmlElementStartTagSyntax)token.Parent;

				if (token == startTag.GreaterThanToken && startTag.Name.LocalName.ValueText == "list")
				{
					items.AddRange(GetListItems(engine, span));
				}

				if (token == startTag.GreaterThanToken && startTag.Name.LocalName.ValueText == "listheader")
				{
					items.AddRange(GetListHeaderItems(engine, span));
				}
			}

			items.AddRange(GetAlwaysVisibleItems(engine, span));
			return items;
		}

		public override bool IsCommitCharacter (CompletionData ICompletionData, char ch, string textTypedSoFar)
		{
			if ((ch == '"' || ch == ' ')
				&& ICompletionData.DisplayText.Contains(ch))
			{
				return false;
			}

			return base.IsCommitCharacter(ICompletionData, ch, textTypedSoFar) || ch == '>' || ch == '\t';
		}

		public override bool IsTriggerCharacter (SourceText text, int position)
		{
			return text[position] == '<';
		}
//
//		public override bool SendEnterThroughToEditor(ICompletionData ICompletionData, string textTypedSoFar)
//		{
//			return false;
//		}

		private IEnumerable<CompletionData> GetTopLevelSingleUseNames(CompletionEngine engine, DocumentationCommentTriviaSyntax parentTrivia, TextSpan span)
		{
			var names = new HashSet<string>(new[] { "summary", "remarks", "example", "completionlist" });

			RemoveExistingTags(parentTrivia, names, (x) => x.StartTag.Name.LocalName.ValueText);

			return names.Select(n => GetItem(engine, n, span));
		}

		private void RemoveExistingTags(DocumentationCommentTriviaSyntax parentTrivia, ISet<string> names, Func<XmlElementSyntax, string> selector)
		{
			if (parentTrivia != null)
			{
				foreach (var node in parentTrivia.Content)
				{
					var element = node as XmlElementSyntax;
					if (element != null)
					{
						names.Remove(selector(element));
					}
				}
			}
		}

		private IEnumerable<CompletionData> GetTagsForSymbol(CompletionEngine engine, ISymbol symbol, TextSpan filterSpan, DocumentationCommentTriviaSyntax trivia, SyntaxToken token)
		{
			if (symbol is IMethodSymbol)
			{
				return GetTagsForMethod(engine, (IMethodSymbol)symbol, filterSpan, trivia, token);
			}

			if (symbol is IPropertySymbol)
			{
				return GetTagsForProperty(engine, (IPropertySymbol)symbol, filterSpan, trivia);
			}

			if (symbol is INamedTypeSymbol)
			{
				return GetTagsForType(engine, (INamedTypeSymbol)symbol, filterSpan, trivia);
			}

			return SpecializedCollections.EmptyEnumerable<CompletionData>();
		}

		private IEnumerable<CompletionData> GetTagsForType(CompletionEngine engine, INamedTypeSymbol symbol, TextSpan filterSpan, DocumentationCommentTriviaSyntax trivia)
		{
			var items = new List<CompletionData>();

			var typeParameters = symbol.TypeParameters.Select(p => p.Name).ToSet();

			RemoveExistingTags(trivia, typeParameters, x => AttributeSelector(x, "typeparam"));

			items.AddRange(typeParameters.Select(t => engine.Factory.CreateXmlDocCompletionData (
				this,
				FormatParameter("typeparam", t))));
			return items;
		}

		private string AttributeSelector(XmlElementSyntax element, string attribute)
		{
			if (!element.StartTag.IsMissing && !element.EndTag.IsMissing)
			{
				var startTag = element.StartTag;
				var nameAttribute = startTag.Attributes.OfType<XmlNameAttributeSyntax>().FirstOrDefault(a => a.Name.LocalName.ValueText == "name");
				if (nameAttribute != null)
				{
					if (startTag.Name.LocalName.ValueText == attribute)
					{
						return nameAttribute.Identifier.Identifier.ValueText;
					}
				}
			}

			return null;
		}

		private IEnumerable<CompletionData> GetTagsForProperty(CompletionEngine engine, IPropertySymbol symbol, TextSpan filterSpan, DocumentationCommentTriviaSyntax trivia)
		{
			var items = new List<CompletionData>();

			var typeParameters = symbol.GetTypeArguments().Select(p => p.Name).ToSet();

			RemoveExistingTags(trivia, typeParameters, x => AttributeSelector(x, "typeparam"));

			items.AddRange(typeParameters.Select(t => engine.Factory.CreateXmlDocCompletionData(this, "typeparam", null, "name$" + t)));
			items.Add(engine.Factory.CreateXmlDocCompletionData(this, "value"));
			return items;
		}

		private IEnumerable<CompletionData> GetTagsForMethod(CompletionEngine engine, IMethodSymbol symbol, TextSpan filterSpan, DocumentationCommentTriviaSyntax trivia, SyntaxToken token)
		{
			var items = new List<CompletionData>();

			var parameters = symbol.GetParameters().Select(p => p.Name).ToSet();
			var typeParameters = symbol.TypeParameters.Select(t => t.Name).ToSet();

			// User is trying to write a name, try to suggest only names.
			if (token.Parent.IsKind(SyntaxKind.XmlNameAttribute) ||
				(token.Parent.IsKind(SyntaxKind.IdentifierName) && token.Parent.IsParentKind(SyntaxKind.XmlNameAttribute)))
			{
				string parentElementName = null;

				var emptyElement = token.GetAncestor<XmlEmptyElementSyntax>();
				if (emptyElement != null)
				{
					parentElementName = emptyElement.Name.LocalName.Text;
				}

				// We're writing the name of a paramref or typeparamref
				if (parentElementName == "paramref")
				{
					items.AddRange(parameters.Select(p => engine.Factory.CreateXmlDocCompletionData (this, p)));
				}
				else if (parentElementName == "typeparamref")
				{
					items.AddRange(typeParameters.Select(t => engine.Factory.CreateXmlDocCompletionData (this, t)));
				}

				return items;
			}

			var returns = true;

			RemoveExistingTags(trivia, parameters, x => AttributeSelector(x, "param"));
			RemoveExistingTags(trivia, typeParameters, x => AttributeSelector(x, "typeparam"));

			foreach (var node in trivia.Content)
			{
				var element = node as XmlElementSyntax;
				if (element != null && !element.StartTag.IsMissing && !element.EndTag.IsMissing)
				{
					var startTag = element.StartTag;

					if (startTag.Name.LocalName.ValueText == "returns")
					{
						returns = false;
						break;
					}
				}
			}

			items.AddRange(parameters.Select(p => engine.Factory.CreateXmlDocCompletionData (this, FormatParameter("param", p))));
			items.AddRange(typeParameters.Select(t => engine.Factory.CreateXmlDocCompletionData (this, FormatParameter("typeparam", t))));

			if (returns && !symbol.ReturnsVoid)
			{
				items.Add(engine.Factory.CreateXmlDocCompletionData (this, "returns"));
			}

			return items;
		}


		readonly Dictionary<string, string[]> _tagMap = new Dictionary<string, string[]> {
			{ "exception", new[] { "<exception cref=\"", "\">" } },
			{ "!--", new[] { "<!--", "-->" } },
			{ "![CDATA[", new[] { "<![CDATA[", "]]>" } },
			{ "include", new[] { "<include file=\'", "\' path=\'[@name=\"\"]\'/>" } },
			{ "permission", new[] { "<permission cref=\"", "\"" } },
			{ "see", new[] { "<see cref=\"", "\"/>" } },
			{ "seealso", new[] { "<seealso cref=\"", "\"/>" } },
			{ "list", new[] { "<list type=\"", "\">" } },
			{ "paramref", new[] { "<paramref name=\"", "\"/>" } },
			{ "typeparamref", new[] { "<typeparamref name=\"", "\"/>" } },
			{ "completionlist", new[] { "<completionlist cref=\"", "\"/>" } },
		};

		readonly string[][] _attributeMap =  {
			new [] { "exception", "cref", "cref=\"", "\"" },
			new [] { "permission",  "cref", "cref=\"", "\"" },
			new [] { "see", "cref", "cref=\"", "\"" },
			new [] { "seealso", "cref", "cref=\"", "\"" },
			new [] { "list", "type", "type=\"", "\"" },
			new [] { "param", "name", "name=\"", "\"" },
			new [] { "include", "file", "file=\"", "\"" },
			new [] { "include", "path", "path=\"", "\"" }
		};

		protected CompletionData GetItem(CompletionEngine engine, string n, TextSpan span)
		{
			if (_tagMap.ContainsKey(n))
			{
				var value = _tagMap[n];
				return engine.Factory.CreateXmlDocCompletionData (this, n, null, value [0] + "|" + value [1]);
			}
			return engine.Factory.CreateXmlDocCompletionData (this, n);
		}

		protected IEnumerable<CompletionData> GetAttributeItem(CompletionEngine engine, string n, TextSpan span)
		{
			var items = _attributeMap.Where(x => x[0] == n).Select(x => engine.Factory.CreateXmlDocCompletionData(this, x[1],null, x[2] + "|" + x[3]));
			if (items.Any ())
				return items;
			
			return new [] { engine.Factory.CreateXmlDocCompletionData (this, n) };
		}

		protected IEnumerable<CompletionData> GetAlwaysVisibleItems(CompletionEngine engine, TextSpan filterSpan)
		{
			return new[] { "see", "seealso", "![CDATA[", "!--" }
				.Select(t => GetItem(engine, t, filterSpan));
		}

		protected IEnumerable<CompletionData> GetNestedTags(CompletionEngine engine, TextSpan filterSpan)
		{
			return new[] { "c", "code", "para", "list", "paramref", "typeparamref" }
				.Select(t => GetItem(engine, t, filterSpan));
		}

		protected IEnumerable<CompletionData> GetTopLevelRepeatableItems(CompletionEngine engine, TextSpan filterSpan)
		{
			return new[] { "exception", "include", "permission" }
				.Select(t => GetItem(engine, t, filterSpan));
		}

		protected IEnumerable<CompletionData> GetListItems(CompletionEngine engine, TextSpan span)
		{
			return new[] { "listheader", "term", "item", "description" }
				.Select(t => GetItem(engine, t, span));
		}

		protected IEnumerable<CompletionData> GetListHeaderItems(CompletionEngine engine, TextSpan span)
		{
			return new[] { "term", "description" }
				.Select(t => GetItem(engine, t, span));
		}

		protected string FormatParameter(string kind, string name)
		{
			return string.Format("{0} name=\"{1}\"", kind, name);
		}
	}
}