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
using System;
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
using Microsoft.CodeAnalysis.ExtractMethod;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Refactoring;

namespace MonoDevelop.CSharp.Completion.Provider
{
	[ExportCompletionProvider ("DelegateCompletionProvider", LanguageNames.CSharp)]
	class DelegateCompletionProvider : CommonCompletionProvider
	{
		internal static readonly SymbolDisplayFormat NameFormat =
		new SymbolDisplayFormat (
			globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
			typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
			propertyStyle: SymbolDisplayPropertyStyle.NameOnly,
			genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeVariance,
			memberOptions: SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeExplicitInterface,
			parameterOptions:
			SymbolDisplayParameterOptions.IncludeParamsRefOut |
			SymbolDisplayParameterOptions.IncludeExtensionThis |
			SymbolDisplayParameterOptions.IncludeType |
			SymbolDisplayParameterOptions.IncludeName,
			miscellaneousOptions:
			SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
			SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

		internal static readonly SymbolDisplayFormat overrideNameFormat = NameFormat.WithParameterOptions (
			SymbolDisplayParameterOptions.IncludeDefaultValue |
			SymbolDisplayParameterOptions.IncludeExtensionThis |
			SymbolDisplayParameterOptions.IncludeType |
			SymbolDisplayParameterOptions.IncludeName |
			SymbolDisplayParameterOptions.IncludeParamsRefOut);

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
					delegateName = GuessEventHandlerBaseName (ctx.TargetToken.Parent, ctx.ContainingTypeDeclaration);
				}

				AddDelegateHandlers (context, ctx.TargetToken.Parent, model, type, position, delegateName, cancellationToken);
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

		static CompletionItemRules DelegateRules = CompletionItemRules.Create (matchPriority: 9999);
		static CompletionItemRules NewMethodRules = CompletionItemRules.Create (matchPriority: 10000);

		const string thisLineIndentMarker = "$thisLineIndent$";
		const string oneIndentMarker = "$oneIndent$";
		const string eolMarker = "\n";

		void AddDelegateHandlers (CompletionContext context, SyntaxNode parent, SemanticModel semanticModel, ITypeSymbol delegateType, int position, string optDelegateName, CancellationToken cancellationToken)
		{
			var delegateMethod = delegateType.GetDelegateInvokeMethod ();

			string EolMarker = "\n";
			bool addSemicolon = true;
			bool addDefault = true;

			string delegateEndString = EolMarker + thisLineIndentMarker + "}" + (addSemicolon ? ";" : "");
			CompletionItem item;
			if (addDefault) {
				item = CreateCompletionItem (
				   "delegate",
				   "Creates anonymous delegate.",
				   "delegate {" + EolMarker + thisLineIndentMarker + eolMarker,
				   delegateEndString,
				   position
			   );

				if (!context.Items.Any (i => i.DisplayText == item.DisplayText))
					context.AddItem (item);

				//if (LanguageVersion.Major >= 5)

				item = CreateCompletionItem (
					"async delegate",
					"Creates anonymous async delegate.",
					"async delegate {" + EolMarker + thisLineIndentMarker + eolMarker,
					delegateEndString,
					position
				);
				if (!context.Items.Any (i => i.DisplayText == item.DisplayText))
					context.AddItem (item);
			}

			var sb = new StringBuilder ("(");
			var sbWithoutTypes = new StringBuilder ("(");
			for (int k = 0; k < delegateMethod.Parameters.Length; k++) {
				if (k > 0) {
					sb.Append (", ");
					sbWithoutTypes.Append (", ");
				}
				sb.Append (CSharpAmbience.SafeMinimalDisplayString (delegateMethod.Parameters [k], semanticModel, position, overrideNameFormat));
				sbWithoutTypes.Append (delegateMethod.Parameters [k].Name);
			}

			sb.Append (")");
			sbWithoutTypes.Append (")");
			var signature = sb.ToString ()
				.Replace (", params ", ", ")
				.Replace ("(params ", "(");

			if (context.Items.All (data => data.DisplayText != signature)) {
				item = CreateCompletionItem (
					signature + " =>",
					"Creates typed lambda expression.",
					signature + " => ",
					(addSemicolon ? ";" : ""),
					position
				);
				//item.CompletionCategory = category;
				if (!context.Items.Any (i => i.DisplayText == item.DisplayText))
					context.AddItem (item);

				// if (LanguageVersion.Major >= 5) {

				item = CreateCompletionItem (
					"async " + signature + " =>",
					"Creates typed async lambda expression.",
					"async " + signature + " => ",
					(addSemicolon ? ";" : ""),
					position
				);
				//item.CompletionCategory = category;
				if (!context.Items.Any (i => i.DisplayText == item.DisplayText))
					context.AddItem (item);

				var signatureWithoutTypes = sbWithoutTypes.ToString ();
				if (!delegateMethod.Parameters.Any (p => p.RefKind != RefKind.None) && context.Items.All (data => data.DisplayText != signatureWithoutTypes)) {
					item = CreateCompletionItem (
						signatureWithoutTypes + " =>",
						"Creates typed lambda expression.",
						signatureWithoutTypes + " => ",
						(addSemicolon ? ";" : ""),
						position
					);
					//item.CompletionCategory = category;
					if (!context.Items.Any (i => i.DisplayText == item.DisplayText)) {
						context.AddItem (item);
						context.SuggestionModeItem = item;
					}

					//if (LanguageVersion.Major >= 5) {
					item = CreateCompletionItem (
						"async " + signatureWithoutTypes + " =>",
						"Creates typed async lambda expression.",
						"async " + signatureWithoutTypes + " => ",
						(addSemicolon ? ";" : ""),
						position
					);
					//item.CompletionCategory = category;
					if (!context.Items.Any (i => i.DisplayText == item.DisplayText))
						context.AddItem (item);

					//}
				}
			}
			item = CreateNewMethodCreationItem (parent, semanticModel, delegateType, position, optDelegateName, delegateMethod, cancellationToken);
			// item.CompletionCategory = category;
			if (!context.Items.Any (i => i.DisplayText == item.DisplayText)) {
				context.AddItem (item);
				context.SuggestionModeItem = item;
			}
		}

		CompletionItem CreateNewMethodCreationItem (SyntaxNode parent, SemanticModel semanticModel, ITypeSymbol delegateType, int position, string optDelegateName, IMethodSymbol delegateMethod, CancellationToken cancellationToken)
		{
			var sb = new StringBuilder ();
			string varName = optDelegateName ?? "Handle" + delegateType.Name;

			var curType = semanticModel.GetEnclosingSymbol<INamedTypeSymbol> (position, cancellationToken);
			var uniqueName = new UniqueNameGenerator (semanticModel).CreateUniqueMethodName (parent, varName);
			var pDict = ImmutableDictionary<string, string>.Empty;
			pDict = pDict.Add ("RightSideMarkup", "<span size='small'>" + GettextCatalog.GetString ("Creates new method") + "</span>");
			var indent = "\t";
			sb = new StringBuilder ();
			var enclosingSymbol = semanticModel.GetEnclosingSymbol (position, default (CancellationToken));
			if (enclosingSymbol != null && enclosingSymbol.IsStatic)
				sb.Append ("static ");
			sb.Append ("void ");
			int pos2 = sb.Length;
			sb.Append (uniqueName);
			sb.Append (' ');
			sb.Append ("(");

			for (int k = 0; k < delegateMethod.Parameters.Length; k++) {
				if (k > 0) {
					sb.Append (", ");
				}
				sb.Append (CSharpAmbience.SafeMinimalDisplayString (delegateMethod.Parameters [k], semanticModel, position, MonoDevelop.Ide.TypeSystem.Ambience.LabelFormat));
			}
			sb.Append (")");

			sb.Append (eolMarker);
			sb.Append (indent);
			sb.Append ("{");
			sb.Append (eolMarker);
			sb.Append (indent);
			sb.Append (oneIndentMarker);
			//int cursorPos = pos + sb.Length;
			sb.Append (indent);
			sb.Append ("}");
			sb.Append (eolMarker);
			pDict = pDict.Add ("Position", position.ToString ());
			pDict = pDict.Add ("NewMethod", sb.ToString ());
			pDict = pDict.Add ("MethodName", varName);

			return CompletionItem.Create (uniqueName, properties: pDict, tags: newMethodTags, rules: NewMethodRules);
		}
		static readonly ImmutableArray<string> newMethodTags = ImmutableArray<string>.Empty.AddRange (new [] { "NewMethod" });

		CompletionItem CreateCompletionItem (string displayString, string description, string insertBefore, string insertAfter, int position)
		{
			var pDict = ImmutableDictionary<string, string>.Empty;
			if (description != null)
				pDict = pDict.Add ("DescriptionMarkup", "- <span foreground=\"darkgray\" size='small'>" + description + "</span>");
			pDict = pDict.Add ("Position", position.ToString ());
			pDict = pDict.Add ("InsertBefore", insertBefore);
			pDict = pDict.Add ("InsertAfter", insertAfter);

			return CompletionItem.Create (displayString, properties: pDict, tags: newMethodTags, rules: DelegateRules);
		}

		public override async Task<CompletionChange> GetChangeAsync (Document doc, CompletionItem item, char? commitKey = default (char?), CancellationToken cancellationToken = default (CancellationToken))
		{
			GetInsertText (item.Properties, out string beforeText, out string afterText, out string newMethod);

			TextChange change;
			if (newMethod != null) {
				change = new TextChange (new TextSpan (item.Span.Start, item.Span.Length), item.Properties ["MethodName"] + ";");
				var document = IdeApp.Workbench.ActiveDocument;
				var editor = document.Editor;
				var parsedDocument = document.ParsedDocument;
				var semanticModel = await doc.GetSemanticModelAsync (cancellationToken);
				var declaringType = semanticModel.GetEnclosingSymbolMD<INamedTypeSymbol> (item.Span.Start, default (CancellationToken));
				var insertionPoints = InsertionPointService.GetInsertionPoints (
					document.Editor,
					parsedDocument,
					declaringType,
					editor.CaretOffset
				);
				var options = new InsertionModeOptions (
					GettextCatalog.GetString ("Create new method"),
					insertionPoints,
					point => {
						if (!point.Success)
							return;
						point.InsertionPoint.Insert (document.Editor, document, newMethod);
					}
				);

				editor.StartInsertionMode (options);

				return CompletionChange.Create (change);
			}
			change = new TextChange (new TextSpan (item.Span.Start, item.Span.Length), beforeText + afterText);

			return CompletionChange.Create (change, item.Span.Start + beforeText.Length);
		}

		protected override Task<TextChange?> GetTextChangeAsync (CompletionItem selectedItem, char? ch, CancellationToken cancellationToken)
		{
			GetInsertText (selectedItem.Properties, out string beforeText, out string afterText, out string newMethod);
			var change = new TextChange (new TextSpan (selectedItem.Span.Start, selectedItem.Span.Length), beforeText + afterText);
			return Task.FromResult<TextChange?> (change);
		}

		void GetInsertText (ImmutableDictionary<string, string> properties, out string beforeText, out string afterText, out string newMethod)
		{
			string thisLineIndent;
			string oneIndent;
			var editor = IdeApp.Workbench?.ActiveDocument?.Editor;
			if (editor != null) {
				thisLineIndent = editor.IndentationTracker.GetIndentationString (editor.OffsetToLineNumber (int.Parse (properties ["Position"])));
				oneIndent = editor.Options.TabsToSpaces ? new string (' ', editor.Options.TabSize) : "\t";
			} else {
				thisLineIndent = oneIndent = "\t";
			}
			var eol = editor?.EolMarker ?? "\n";

			properties.TryGetValue ("InsertBefore", out beforeText);
			properties.TryGetValue ("InsertAfter", out afterText);
			properties.TryGetValue ("NewMethod", out newMethod);

			beforeText = beforeText?.Replace ("\n", eol).Replace ("$thisLineIndent$", thisLineIndent).Replace ("$oneIndent$", oneIndent);
			afterText = afterText?.Replace ("\n", eol).Replace ("$thisLineIndent$", thisLineIndent).Replace ("$oneIndent$", oneIndent);
			newMethod = newMethod?.Replace ("\n", eol).Replace ("$thisLineIndent$", thisLineIndent).Replace ("$oneIndent$", oneIndent);
		}
	}
}