// 
// CSharpCompletionTextEditorExtension.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2011 Xamarin <http://xamarin.com>
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
using MonoDevelop.Ide.CodeCompletion;
using Microsoft.CodeAnalysis;
using GLib;
using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Ide.Editor.Extension;
using Xwt;
using MonoDevelop.Ide;

namespace MonoDevelop.CSharp.Completion
{
	class RoslynSymbolCompletionData : RoslynCompletionData, ICSharpCode.NRefactory6.CSharp.Completion.ISymbolCompletionData
	{
		readonly ISymbol symbol;

		public ISymbol Symbol {
			get {
				return symbol;
			}
		}
		
		public override string DisplayText {
			get {
				return text ?? symbol.Name;
			}
			set {
				text = value;
			}
		}

		public override string CompletionText {
			get {
				return text ?? symbol.Name;
			}
			set {
				throw new NotSupportedException ();
			}
		}

		public override MonoDevelop.Core.IconId Icon {
			get {
				return MonoDevelop.Ide.TypeSystem.Stock.GetStockIcon (symbol);
			}
			set {
				throw new NotSupportedException ();
			}
		}

		public bool IsDelegateExpected { get; set; }


		string text;
		protected readonly RoslynCodeCompletionFactory factory;

		protected CSharpCompletionTextEditorExtension ext { get { return factory.Ext; } }

		public RoslynSymbolCompletionData (ICSharpCode.NRefactory6.CSharp.Completion.ICompletionKeyHandler keyHandler, RoslynCodeCompletionFactory factory, ISymbol symbol, string text = null) : base (keyHandler)
		{
			this.factory = factory;
			this.text = text;
			this.symbol = symbol;
		}

		static readonly SymbolDisplayFormat nameOnlyFormat =
			new SymbolDisplayFormat(
				globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
				typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
				propertyStyle: SymbolDisplayPropertyStyle.NameOnly,
				genericsOptions: SymbolDisplayGenericsOptions.None,
				memberOptions: SymbolDisplayMemberOptions.None,
				parameterOptions:
				SymbolDisplayParameterOptions.None,
				miscellaneousOptions:
				SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
				SymbolDisplayMiscellaneousOptions.UseSpecialTypes);
		
		protected virtual string GetInsertionText ()
		{
			if (text != null)
				return text;
			return symbol.ToDisplayString (nameOnlyFormat);
		}

		public override TooltipInformation CreateTooltipInformation (bool smartWrap)
		{
			return CreateTooltipInformation (ext.Editor, ext.DocumentContext, Symbol, smartWrap, model: factory.SemanticModel);
		}
		
		public static TooltipInformation CreateTooltipInformation (MonoDevelop.Ide.Editor.TextEditor editor, MonoDevelop.Ide.Editor.DocumentContext ctx, ISymbol entity, bool smartWrap, bool createFooter = false, SemanticModel model = null)
		{
			if (ctx != null) {
				if (ctx.ParsedDocument == null || ctx.AnalysisDocument == null)
					LoggingService.LogError ("Signature markup creator created with invalid context." + Environment.NewLine + Environment.StackTrace);
			}

			var tooltipInfo = new TooltipInformation ();
//			if (resolver == null)
//				resolver = file != null ? file.GetResolver (compilation, textEditorData.Caret.Location) : new CSharpResolver (compilation);
			var sig = new SignatureMarkupCreator (ctx, editor != null ? editor.CaretOffset : 0);
			sig.SemanticModel = model;
			sig.BreakLineAfterReturnType = smartWrap;
			try {
				tooltipInfo.SignatureMarkup = sig.GetMarkup (entity);
			} catch (Exception e) {
				LoggingService.LogError ("Got exception while creating markup for :" + entity, e);
				return new TooltipInformation ();
			}
			tooltipInfo.SummaryMarkup = Ambience.GetSummaryMarkup (entity) ?? "";
			
//			if (entity is IMember) {
//				var evt = (IMember)entity;
//				if (evt.ReturnType.Kind == TypeKind.Delegate) {
//					tooltipInfo.AddCategory (GettextCatalog.GetString ("Delegate Info"), sig.GetDelegateInfo (evt.ReturnType));
//				}
//			}
			if (entity is IMethodSymbol) {
				var method = (IMethodSymbol)entity;
				if (method.IsExtensionMethod) {
					tooltipInfo.AddCategory (GettextCatalog.GetString ("Extension Method from"), method.ContainingType.Name);
				}
			}
			if (createFooter) {
				tooltipInfo.FooterMarkup = sig.CreateFooter (entity);
			}
			return tooltipInfo;
		}

		public override void InsertCompletionText (CompletionListWindow window, ref KeyActions ka, MonoDevelop.Ide.Editor.Extension.KeyDescriptor descriptor)
		{
			string partialWord = GetCurrentWord (window);
			int skipChars = 0;
			bool runParameterCompletionCommand = false;
			bool runCompletionCompletionCommand = false;
			var method = Symbol as IMethodSymbol;

			bool addParens = IdeApp.Preferences.AddParenthesesAfterCompletion;
			bool addOpeningOnly = IdeApp.Preferences.AddOpeningOnly;
			var Editor = ext.Editor;
			var Policy = ext.FormattingPolicy;
			string insertionText = this.GetInsertionText();

			if (addParens && !IsDelegateExpected && method != null && !HasNonMethodMembersWithSameName (Symbol) && !IsBracketAlreadyInserted (ext, method)) {
				var line = Editor.GetLine (Editor.CaretLine);
				//var start = window.CodeCompletionContext.TriggerOffset + partialWord.Length + 2;
				//var end = line.Offset + line.Length;
				//string textToEnd = start < end ? Editor.GetTextBetween (start, end) : "";
				bool addSpace = Policy.SpaceAfterMethodCallName && MonoDevelop.Ide.Editor.DefaultSourceEditorOptions.Instance.OnTheFlyFormatting;

				int exprStart = window.CodeCompletionContext.TriggerOffset - 1;
				while (exprStart > line.Offset) {
					char ch = Editor.GetCharAt (exprStart);
					if (ch != '.' && ch != '_' && !char.IsLetterOrDigit (ch))
						break;
					exprStart--;
				}
				bool insertSemicolon = InsertSemicolon(ext, exprStart);
				if (Symbol is IMethodSymbol && ((IMethodSymbol)Symbol).MethodKind == MethodKind.Constructor)
					insertSemicolon = false;
				//int pos;

				var keys = new [] { SpecialKey.Return, SpecialKey.Tab, SpecialKey.Space };
				if (keys.Contains (descriptor.SpecialKey) || descriptor.KeyChar == '.') {
					if (HasAnyOverloadWithParameters (method)) {
						if (addOpeningOnly) {
							insertionText += RequireGenerics (method) ? "<|" : (addSpace ? " (|" : "(|");
							skipChars = 0;
						} else {
							if (descriptor.KeyChar == '.') {
								if (RequireGenerics (method)) {
									insertionText += addSpace ? "<> ()" : "<>()";
								} else {
									insertionText += addSpace ? " ()" : "()";
								}
								skipChars = 0;
							} else {
								if (insertSemicolon) {
									if (RequireGenerics (method)) {
										insertionText += addSpace ? "<|> ();" : "<|>();";
										skipChars = addSpace ? 5 : 4;
									} else {
										insertionText += addSpace ? " (|);" : "(|);";
										skipChars = 2;
									}
								} else {
									if (RequireGenerics (method)) {
										insertionText += addSpace ? "<|> ()" :  "<|>()";
										skipChars = addSpace ? 4 : 3;
									} else {
										insertionText += addSpace ? " (|)" : "(|)";
										skipChars = 1;
									}
								}
							}
						}
						runParameterCompletionCommand = true;
					} else {
						if (addOpeningOnly) {
							insertionText += RequireGenerics (method) ? "<|" : (addSpace ? " (|" : "(|");
							skipChars = 0;
						} else {
							if (descriptor.KeyChar == '.') {
								if (RequireGenerics (method)) {
									insertionText += addSpace ? "<> ().|" : "<>().|";
								} else {
									insertionText += addSpace ? " ().|" : "().|";
								}
								skipChars = 0;
							} else {
								if (insertSemicolon) {
									if (RequireGenerics (method)) {
										insertionText += addSpace ? "<|> ();" : "<|>();";
									} else {
										insertionText += addSpace ? " ();|" : "();|";
									}

								} else {
									if (RequireGenerics (method)) {
										insertionText += addSpace ? "<|> ()" : "<|>()";
									} else {
										insertionText += addSpace ? " ()|" : "()|";
									}

								}
							}
						}
					}
					if (descriptor.KeyChar == '(') {
						var skipCharList = Editor.SkipChars;
						if (skipCharList.Count > 0) {
							var lastSkipChar = skipCharList[skipCharList.Count - 1];
							if (lastSkipChar.Offset == (window.CodeCompletionContext.TriggerOffset + partialWord.Length) && lastSkipChar.Char == ')')
								Editor.RemoveText (lastSkipChar.Offset, 1);
						}
					}
				}
				if (descriptor.KeyChar == ';') {
					insertionText += addSpace ? " ()" : "()";

				}
				ka |= KeyActions.Ignore;
			}
			if ((DisplayFlags & DisplayFlags.NamedArgument) == DisplayFlags.NamedArgument &&
				IdeApp.Preferences.AddParenthesesAfterCompletion &&
				(descriptor.SpecialKey == SpecialKey.Tab ||
					descriptor.SpecialKey == SpecialKey.Return ||
					descriptor.SpecialKey == SpecialKey.Space)) {
				if (true/*Policy.AroundAssignmentParentheses */)
					insertionText += " ";
				insertionText += "=";
				if (/*Policy.AroundAssignmentParentheses && */descriptor.SpecialKey != SpecialKey.Space)
					insertionText += " ";
				runCompletionCompletionCommand = true;
			}
			window.CompletionWidget.SetCompletionText (window.CodeCompletionContext, partialWord, insertionText);
			int offset = Editor.CaretOffset;
			for (int i = 0; i < skipChars; i++) {
				Editor.AddSkipChar (offset, Editor.GetCharAt (offset));
				offset++;
			}

			if (runParameterCompletionCommand && IdeApp.Workbench != null) {
				Application.Invoke (delegate {
					ext.RunParameterCompletionCommand ();
				});
			}

			if (runCompletionCompletionCommand && IdeApp.Workbench != null) {
				Application.Invoke (delegate {
					ext.RunCompletionCommand ();
				});
			}
		}

		static bool IsBracketAlreadyInserted (CSharpCompletionTextEditorExtension ext, IMethodSymbol method)
		{
			var Editor = ext.Editor;
			int offset = Editor.CaretOffset;
			while (offset < Editor.Length) {
				char ch = Editor.GetCharAt (offset);
				if (!char.IsLetterOrDigit (ch))
					break;
				offset++;
			}
			while (offset < Editor.Length) {
				char ch = Editor.GetCharAt (offset);
				if (!char.IsWhiteSpace (ch))
					return ch == '(' || ch == '<' && RequireGenerics (method);
				offset++;
			}
			return false;
		}



		internal static bool InsertSemicolon (CSharpCompletionTextEditorExtension ext, int exprStart)
		{
			var Editor = ext.Editor;
			int offset = exprStart;
			while (offset > 0) {
				char ch = Editor.GetCharAt (offset);
				if (!char.IsWhiteSpace (ch)) {
					if (ch != '{' && ch != '}' && ch != ';')
						return false;
					break;
				}
				offset--;
			}

			offset = Editor.CaretOffset;
			while (offset < Editor.Length) {
				char ch = Editor.GetCharAt (offset);
				if (!char.IsLetterOrDigit (ch))
					break;
				offset++;
			}
			while (offset < Editor.Length) {
				char ch = Editor.GetCharAt (offset);
				if (!char.IsWhiteSpace (ch))
					return char.IsLetter (ch) || ch == '}';
				offset++;
			}
			return true;
		}

		internal static bool HasAnyOverloadWithParameters (IMethodSymbol method)
		{
			if (method.MethodKind == MethodKind.Constructor) 
				return method.ContainingType.GetMembers()
					.OfType<IMethodSymbol>()
					.Where(m => m.MethodKind == MethodKind.Constructor)
					.Any (m => m.Parameters.Length > 0);
			return method.ContainingType
				.GetMembers()
				.OfType<IMethodSymbol>()
				.Any (m => m.Name == method.Name && m.Parameters.Length > 0);
		}

		static bool HasNonMethodMembersWithSameName (ISymbol member)
		{
			var method = member as IMethodSymbol;
			if (method != null && method.MethodKind == MethodKind.Constructor)
				return false;
			if (member.ContainingType == null)
				return false;
			return member.ContainingType
				.GetMembers ()
				.Any (e => e.Kind != SymbolKind.Method && e.Name == member.Name);
		}

		static bool RequireGenerics (IMethodSymbol method)
		{
			System.Collections.Immutable.ImmutableArray<ITypeSymbol> typeArgs;
			if (method.MethodKind == MethodKind.Constructor) {
				typeArgs = method.ContainingType.TypeArguments;
			} else {
				typeArgs = method.TypeArguments;
			}
			
			if (!typeArgs.Any (ta => ta.TypeKind == TypeKind.TypeParameter))
				return false;
			var testMethod = method.ReducedFrom ?? method;
			return typeArgs.Any (t => !testMethod.Parameters.Any (p => ContainsType(p.Type, t)));
		}

		static bool ContainsType (ITypeSymbol testType, ITypeSymbol searchType)
		{
			if (testType == null)
				return false;
			if (testType == searchType)
				return true;
			var namedTypeSymbol = testType as INamedTypeSymbol;
			if (namedTypeSymbol != null) {
				foreach (var arg in namedTypeSymbol.TypeParameters)
					if (ContainsType (arg, searchType))
						return true;
			}
			return false;
		}

		public override int CompareTo (object obj)
		{
			var anonymousMethodCompletionData = obj as AnonymousMethodCompletionData;
			if (anonymousMethodCompletionData == null)
				return 1;
			var objectCreationData = obj as ObjectCreationCompletionData;
			if (objectCreationData == null)
				return 1;
			

			return base.CompareTo (obj);
		}


//		public static TooltipInformation CreateTooltipInformation (ICompilation compilation, CSharpUnresolvedFile file, TextEditorData textEditorData, MonoDevelop.CSharp.Formatting.CSharpFormattingPolicy formattingPolicy, IType type, bool smartWrap, bool createFooter = false)
//		{
//			var tooltipInfo = new TooltipInformation ();
//			var resolver = file != null ? file.GetResolver (compilation, textEditorData.Caret.Location) : new CSharpResolver (compilation);
//			var sig = new SignatureMarkupCreator (resolver, formattingPolicy.CreateOptions ());
//			sig.BreakLineAfterReturnType = smartWrap;
//			try {
//				tooltipInfo.SignatureMarkup = sig.GetMarkup (type.IsParameterized ? type.GetDefinition () : type);
//			} catch (Exception e) {
//				LoggingService.LogError ("Got exception while creating markup for :" + type, e);
//				return new TooltipInformation ();
//			}
//			if (type.IsParameterized) {
//				var typeInfo = new StringBuilder ();
//				for (int i = 0; i < type.TypeParameterCount; i++) {
//					typeInfo.AppendLine (type.GetDefinition ().TypeParameters [i].Name + " is " + sig.GetTypeReferenceString (type.TypeArguments [i]));
//				}
//				tooltipInfo.AddCategory ("Type Parameters", typeInfo.ToString ());
//			}
//
//			var def = type.GetDefinition ();
//			if (def != null) {
//				if (createFooter)
//					tooltipInfo.FooterMarkup = sig.CreateFooter (def);
//				tooltipInfo.SummaryMarkup = AmbienceService.GetSummaryMarkup (def) ?? "";
//			}
//			return tooltipInfo;
//		}
	}

}
