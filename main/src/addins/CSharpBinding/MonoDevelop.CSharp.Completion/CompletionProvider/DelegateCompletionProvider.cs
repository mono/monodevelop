//
// DelegateCompletionProvider.cs
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
/*using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ICSharpCode.NRefactory6.CSharp;
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

namespace MonoDevelop.CSharp.Completion.Provider
{
	[ExportCompletionProvider ("DelegateCompletionProvider", LanguageNames.CSharp)]
	class DelegateCompletionProvider : CommonCompletionProvider
	{
		public override bool ShouldTriggerCompletion (SourceText text, int position, CompletionTrigger trigger, Microsoft.CodeAnalysis.Options.OptionSet options)
		{
			return trigger.Character == '(' || trigger.Character == '[' || trigger.Character == ',' || base.ShouldTriggerCompletion (text, position, trigger, options);
		}


		public override async Task ProvideCompletionsAsync (CompletionContext context)
		{
			var document = context.Document;
			var position = context.Position;
			var cancellationToken = context.CancellationToken;

			var model = await document.GetSemanticModelForSpanAsync (new TextSpan (position, 0), cancellationToken).ConfigureAwait (false);

			var workspace = document.Project.Solution.Workspace;
			var ctx = CSharpSyntaxContext.CreateContext (workspace, model, position, cancellationToken);

			var syntaxTree = ctx.SyntaxTree;

			var enclosingType = model.GetEnclosingNamedType (position, cancellationToken);
			if (enclosingType == null)
				return;

			foreach (var type in ctx.InferredTypes) {
				if (type.TypeKind != TypeKind.Delegate)
					continue;


				string delegateName = null;

				if (ctx.TargetToken.IsKind (SyntaxKind.PlusEqualsToken)) {
					delegateName = GuessEventHandlerBaseName (ctx.LeftToken.Parent, ctx.ContainingTypeDeclaration);
				}

				AddDelegateHandlers (ctx.TargetToken.Parent, model, engine, result, type, position, delegateName, cancellationToken);
			}
		}

		static string GuessEventHandlerBaseName (SyntaxNode node, TypeDeclarationSyntax containingTypeDeclaration)
		{
			var addAssign = node as AssignmentExpressionSyntax;
			if (addAssign == null)
				return null;

			var ident = addAssign.Left as IdentifierNameSyntax;
			if (ident != null)
				return ToPascalCase (containingTypeDeclaration.Identifier + "_" + ident);

			var memberAccess = addAssign.Left as MemberAccessExpressionSyntax;
			if (memberAccess != null)
				return ToPascalCase (GetMemberAccessBaseName (memberAccess) + "_" + memberAccess.Name);

			return null;
		}

		static string GetMemberAccessBaseName (MemberAccessExpressionSyntax memberAccess)
		{
			var ident = memberAccess.Expression as IdentifierNameSyntax;
			if (ident != null)
				return ident.ToString ();

			var ma = memberAccess.Expression as MemberAccessExpressionSyntax;
			if (ma != null)
				return ma.Name.ToString ();

			return "Handle";
		}

		static string ToPascalCase (string str)
		{
			var result = new StringBuilder ();
			result.Append (char.ToUpper (str [0]));
			bool nextUpper = false;
			for (int i = 1; i < str.Length; i++) {
				var ch = str [i];
				if (nextUpper && char.IsLetter (ch)) {
					ch = char.ToUpper (ch);
					nextUpper = false;
				}
				result.Append (ch);
				if (ch == '_')
					nextUpper = true;
			}

			return result.ToString ();
		}

		//public static CompletionCategory category = new DelegateCreationCategory ();

		//public class DelegateCreationCategory : CompletionCategory
		//{
		//	public DelegateCreationCategory ()
		//	{
		//		this.DisplayText = GettextCatalog.GetString ("Delegate Handlers");
		//	}

		//	public override int CompareTo (CompletionCategory other)
		//	{
		//		return -1;
		//	}
		//}


		void AddDelegateHandlers (CompletionContext context, SyntaxNode parent, SemanticModel semanticModel, ITypeSymbol delegateType, int position, string optDelegateName, CancellationToken cancellationToken)
		{
			var delegateMethod = delegateType.GetDelegateInvokeMethod ();
			result.PossibleDelegates.Add (delegateMethod);

			var thisLineIndent = "";
			string EolMarker = "\n";
			bool addSemicolon = true;
			bool addDefault = true;

			string delegateEndString = EolMarker + thisLineIndent + "}" + (addSemicolon ? ";" : "");
			//bool containsDelegateData = completionList.Result.Any(d => d.DisplayText.StartsWith("delegate("));
			CompletionData item;
			if (addDefault) {
				item = engine.Factory.CreateAnonymousMethod (
					this,
					"delegate",
					"Creates anonymous delegate.",
					"delegate {" + EolMarker + thisLineIndent,
					delegateEndString
				);
				item.CompletionCategory = category;
				if (!completionList.Any (i => i.DisplayText == item.DisplayText))
					completionList.Add (item);

				//if (LanguageVersion.Major >= 5)

				item = engine.Factory.CreateAnonymousMethod (
					this,
					"async delegate",
					"Creates anonymous async delegate.",
					"async delegate {" + EolMarker + thisLineIndent,
					delegateEndString
				);
				item.CompletionCategory = category;
				if (!completionList.Any (i => i.DisplayText == item.DisplayText))
					completionList.Add (item);
			}

			var sb = new StringBuilder ("(");
			var sbWithoutTypes = new StringBuilder ("(");
			for (int k = 0; k < delegateMethod.Parameters.Length; k++) {
				if (k > 0) {
					sb.Append (", ");
					sbWithoutTypes.Append (", ");
				}
				sb.Append (RoslynCompletionData.SafeMinimalDisplayString (delegateMethod.Parameters [k], semanticModel, position, overrideNameFormat));
				sbWithoutTypes.Append (delegateMethod.Parameters [k].Name);
			}

			sb.Append (")");
			sbWithoutTypes.Append (")");
			var signature = sb.ToString ()
				.Replace (", params ", ", ")
				.Replace ("(params ", "(");

			if (completionList.All (data => data.DisplayText != signature)) {
				item = engine.Factory.CreateAnonymousMethod (
					this,
					signature + " =>",
					"Creates typed lambda expression.",
					signature + " => ",
					(addSemicolon ? ";" : "")
				);
				item.CompletionCategory = category;
				if (!completionList.Any (i => i.DisplayText == item.DisplayText))
					completionList.Add (item);

				// if (LanguageVersion.Major >= 5) {

				item = engine.Factory.CreateAnonymousMethod (
					this,
					"async " + signature + " =>",
					"Creates typed async lambda expression.",
					"async " + signature + " => ",
					(addSemicolon ? ";" : "")
				);
				item.CompletionCategory = category;
				if (!completionList.Any (i => i.DisplayText == item.DisplayText))
					completionList.Add (item);

				var signatureWithoutTypes = sbWithoutTypes.ToString ();
				if (!delegateMethod.Parameters.Any (p => p.RefKind != RefKind.None) && completionList.All (data => data.DisplayText != signatureWithoutTypes)) {
					item = engine.Factory.CreateAnonymousMethod (
						this,
						signatureWithoutTypes + " =>",
						"Creates typed lambda expression.",
						signatureWithoutTypes + " => ",
						(addSemicolon ? ";" : "")
					);
					item.CompletionCategory = category;
					if (!completionList.Any (i => i.DisplayText == item.DisplayText)) {
						completionList.Add (item);
						result.DefaultCompletionString = item.DisplayText;
					}

					//if (LanguageVersion.Major >= 5) {
					item = engine.Factory.CreateAnonymousMethod (
						this,
						"async " + signatureWithoutTypes + " =>",
						"Creates typed async lambda expression.",
						"async " + signatureWithoutTypes + " => ",
						(addSemicolon ? ";" : "")
					);
					item.CompletionCategory = category;
					if (!completionList.Any (i => i.DisplayText == item.DisplayText))
						completionList.Add (item);

					//}
				}
			}
			string varName = optDelegateName ?? "Handle" + delegateType.Name;


			var curType = semanticModel.GetEnclosingSymbol<INamedTypeSymbol> (position, cancellationToken);
			var uniqueName = new UniqueNameGenerator (semanticModel).CreateUniqueMethodName (parent, varName);
			item = engine.Factory.CreateNewMethodDelegate (this, delegateType, uniqueName, curType);
			item.CompletionCategory = category;
			if (!completionList.Any (i => i.DisplayText == item.DisplayText)) {
				completionList.Add (item);
				if (string.IsNullOrEmpty (result.DefaultCompletionString))
					result.DefaultCompletionString = item.DisplayText;
			}
		}

	}
}
*/