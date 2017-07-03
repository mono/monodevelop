//
// EnumMemberCompletionProvider.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2017 Microsoft Corporation
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

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using ICSharpCode.NRefactory6.CSharp;
using ICSharpCode.NRefactory6.CSharp.Analysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using MonoDevelop.Core;
using MonoDevelop.Ide.TypeSystem;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;

namespace MonoDevelop.CSharp.Completion.Provider
{
	[ExportCompletionProvider ("EnumMemberCompletionProvider", LanguageNames.CSharp)]
	class EnumMemberCompletionProvider : CommonCompletionProvider
	{
		public override bool ShouldTriggerCompletion (SourceText text, int position, CompletionTrigger trigger, Microsoft.CodeAnalysis.Options.OptionSet options)
		{
			// Only commit on dot.
			return trigger.Character == '.';
		}

		internal override bool IsInsertionTrigger (SourceText text, int insertedCharacterPosition, Microsoft.CodeAnalysis.Options.OptionSet options)
		{
			
			// Bring up on space or at the start of a word, or after a ( or [.
			//
			// Note: we don't want to bring this up after traditional enum operators like & or |.
			// That's because we don't like the experience where the enum appears directly after the
			// operator.  Instead, the user normally types <space> and we will bring up the list
			// then.
			var ch = text [insertedCharacterPosition];
			return
				ch == ' ' ||
				ch == '[' ||
				ch == '(' ||
				(CompletionUtilities.IsStartingNewWord (text, insertedCharacterPosition));
		}
		static readonly ImmutableArray<string> tags = ImmutableArray<string>.Empty.AddRange (new [] { "EnumMember", "Public" });

		public override async Task ProvideCompletionsAsync (Microsoft.CodeAnalysis.Completion.CompletionContext context)
		{
			var document = context.Document;
			var position = context.Position;
			var cancellationToken = context.CancellationToken;

			var model = await document.GetSemanticModelForSpanAsync (new TextSpan (position, 0), cancellationToken).ConfigureAwait (false);

			var workspace = document.Project.Solution.Workspace;
			var ctx = CSharpSyntaxContext.CreateContext (workspace, model, position, cancellationToken);

			var tree = ctx.SyntaxTree;
			if (tree.IsInNonUserCode (context.Position, cancellationToken))
				return;

			var token = tree.FindTokenOnLeftOfPosition (context.Position, cancellationToken);
			if (token.IsKind (SyntaxKind.DotToken) || token.IsMandatoryNamedParameterPosition ())
				return;

			// check if it's the first parameter and set autoselect == false if a parameterless version exists.
			if (token.IsKind (SyntaxKind.OpenParenToken)) {
				var parent = token.Parent?.Parent;
				if (parent == null)
					return;
				var symbolInfo = model.GetSymbolInfo (parent);
				foreach (var symbol in new [] { symbolInfo.Symbol }.Concat (symbolInfo.CandidateSymbols)) {
					if (symbol != null && symbol.IsKind (SymbolKind.Method)) {
						if (symbol.GetParameters ().Length == 0) {
							// completionResult.AutoSelect = false;
							break;
						}
					}
				}
			}

			foreach (var _type in ctx.InferredTypes) {
				var type = _type;
				if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T) {
					type = type.GetTypeArguments ().FirstOrDefault ();
					if (type == null)
						continue;
				}

				if (type.TypeKind != TypeKind.Enum)
					continue;
				if (!type.IsEditorBrowsable ())
					continue;

				// Does type have any aliases?
				ISymbol alias = await type.FindApplicableAlias (context.Position, model, cancellationToken).ConfigureAwait (false);

				if (!IsReachable (model, type, token.Parent)) {
					var pDict = ImmutableDictionary<string, string>.Empty;
					var displayString = CSharpAmbience.SafeMinimalDisplayString (type, model, context.Position, SymbolDisplayFormat.CSharpErrorMessageFormat);
					var item = CompletionItem.Create (displayString, properties: pDict, tags: tags);
					context.AddItem (item);
					context.SuggestionModeItem = item;
				}

				foreach (IFieldSymbol field in type.GetMembers ().OfType<IFieldSymbol> ()) {
					if (field.DeclaredAccessibility == Accessibility.Public && (field.IsConst || field.IsStatic)) {
						var displayString = CSharpAmbience.SafeMinimalDisplayString (alias ?? field.Type, model, context.Position, SymbolDisplayFormat.CSharpErrorMessageFormat) + "." + field.Name;
						var pDict = ImmutableDictionary<string, string>.Empty;
						context.AddItem (CompletionItem.Create (displayString, properties: pDict, tags: tags));
					}
				}
			}
		}

		bool IsReachable (SemanticModel model, ITypeSymbol type, SyntaxNode node)
		{
			return type.ToMinimalDisplayString (model, node.SpanStart).IndexOf ('.') < 0;
		}
	}
}