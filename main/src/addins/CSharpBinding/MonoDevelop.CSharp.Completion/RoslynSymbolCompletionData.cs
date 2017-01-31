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
using System.Threading.Tasks;
using System.Threading;
using MonoDevelop.Ide.Editor;
using ICSharpCode.NRefactory6.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MonoDevelop.CSharp.Completion
{
	class RoslynSymbolCompletionData : RoslynCompletionData
	{
		public override string DisplayText {
			get {
				return text ?? Symbol.Name;
			}
			set {
				text = value;
			}
		}

		public override string CompletionText {
			get {
				return text ?? Symbol.Name;
			}
			set {
				throw new NotSupportedException ();
			}
		}

		public override MonoDevelop.Core.IconId Icon {
			get {
				return MonoDevelop.Ide.TypeSystem.Stock.GetStockIcon (Symbol);
			}
			set {
				throw new NotSupportedException ();
			}
		}

		public bool IsDelegateExpected { get; set; }


		string text;
		protected readonly RoslynCodeCompletionFactory factory;

		protected CSharpCompletionTextEditorExtension ext { get { return factory?.Ext; } }

		public RoslynSymbolCompletionData (ICompletionDataKeyHandler keyHandler, RoslynCodeCompletionFactory factory, ISymbol symbol, string text = null) : base (keyHandler)
		{
			this.factory = factory;
			this.text = text;
			Symbol = symbol;
			if (IsObsolete (Symbol))
				DisplayFlags |= DisplayFlags.Obsolete;
			rightSideDescription = new Lazy<string> (delegate {
				var returnType = symbol.GetReturnType ();
				if (returnType == null || factory == null)
					return null;
				try {
					return "<span font='Sans 10'>" + GLib.Markup.EscapeText (SafeMinimalDisplayString (returnType, factory.SemanticModel, ext.Editor.CaretOffset)) + "</span>";
				} catch (Exception e) {
					LoggingService.LogError ("Format error.", e);
				}
				return null;
			});
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
			return Symbol.ToDisplayString (nameOnlyFormat);
		}

		Lazy<string> rightSideDescription;
		public override string GetRightSideDescription (bool isSelected)
		{
			return rightSideDescription.Value;
		}


		public override Task<TooltipInformation> CreateTooltipInformation (bool smartWrap, CancellationToken ctoken)
		{
			return CreateTooltipInformation (ctoken, ext.Editor, ext.DocumentContext, Symbol, smartWrap, model: factory.SemanticModel);
		}
		
		public static Task<TooltipInformation> CreateTooltipInformation (CancellationToken ctoken, MonoDevelop.Ide.Editor.TextEditor editor, MonoDevelop.Ide.Editor.DocumentContext ctx, ISymbol entity, bool smartWrap, bool createFooter = false, SemanticModel model = null)
		{
			var tooltipInfo = new TooltipInformation ();
//			if (resolver == null)
//				resolver = file != null ? file.GetResolver (compilation, textEditorData.Caret.Location) : new CSharpResolver (compilation);
			var sig = new SignatureMarkupCreator (ctx, editor != null ? editor.CaretOffset : 0);
			sig.SemanticModel = model;
			sig.BreakLineAfterReturnType = smartWrap;

			return Task.Run (() => {
				if (ctoken.IsCancellationRequested)
					return null;
				try {
					tooltipInfo.SignatureMarkup = sig.GetMarkup (entity);
				} catch (Exception e) {
					LoggingService.LogError ("Got exception while creating markup for :" + entity, e);
					return new TooltipInformation ();
				}

				if (ctoken.IsCancellationRequested)
					return null;
				
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
			});
		}
		 
		static string GetCurrentWordForMethods (CompletionListWindow window, MonoDevelop.Ide.Editor.Extension.KeyDescriptor descriptor)
		{
			int partialWordLength = window.PartialWord != null ? window.PartialWord.Length : 0;
			int replaceLength;
			if (descriptor.SpecialKey == SpecialKey.Return || descriptor.SpecialKey == SpecialKey.Tab) {
				replaceLength = window.CodeCompletionContext.TriggerWordLength + partialWordLength - window.InitialWordLength;
			} else {
				replaceLength = partialWordLength;
			}
			int endOffset = Math.Min (window.StartOffset + replaceLength, window.CompletionWidget.TextLength);
			if (descriptor.KeyChar == '(' && IdeApp.Preferences.AddParenthesesAfterCompletion) {
				endOffset++;
				if (DefaultSourceEditorOptions.Instance.AutoInsertMatchingBracket) 
					endOffset++;
			}
			var result = window.CompletionWidget.GetText (window.StartOffset, endOffset);
			return result;
		}
		public override void InsertCompletionText (CompletionListWindow window, ref KeyActions ka, MonoDevelop.Ide.Editor.Extension.KeyDescriptor descriptor)
		{
			string partialWord = GetCurrentWord (window, descriptor);
			int skipChars = 0;
			bool runParameterCompletionCommand = false;
			bool runCompletionCompletionCommand = false;
			var method = Symbol as IMethodSymbol;

			bool addParens = IdeApp.Preferences.AddParenthesesAfterCompletion;
			bool addOpeningOnly = IdeApp.Preferences.AddOpeningOnly;
			var Editor = ext.Editor;
			var Policy = ext.FormattingPolicy;
			var ctx = window.CodeCompletionContext;
			string insertionText = this.GetInsertionText();

			if (addParens && !IsDelegateExpected && method != null && !IsBracketAlreadyInserted (ext, method)) {
				partialWord = GetCurrentWordForMethods (window, descriptor);
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
				if (keys.Contains (descriptor.SpecialKey) || descriptor.KeyChar == '.' || descriptor.KeyChar == '(') {
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
									insertionText += addSpace ? "<> ()" : "<>()";
								} else {
									insertionText += addSpace ? " ()" : "()";
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
			window.CompletionWidget.SetCompletionText (ctx, partialWord, insertionText);
			int offset = Editor.CaretOffset;
			for (int i = skipChars; i --> 0;) {
				Editor.StartSession (new SkipCharSession (Editor.GetCharAt (offset + i)));
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

			var parameterTypes = new List<ITypeSymbol> (method.Parameters.Select (p => p.Type));
			if (method.IsExtensionMethod) {
				parameterTypes.Add (method.ReducedFrom.Parameters [0].Type);
			}

			return typeArgs.Any (t => !parameterTypes.Any (pt => ContainsType (pt, t)));
		}

		static bool ContainsType (ITypeSymbol testType, ITypeSymbol searchType)
		{
			if (testType == null)
				return false;
			Console.WriteLine (testType +" == " + searchType + " ? " + (testType == searchType) + "/" + testType.Equals (searchType));
			if (testType == searchType)
				return true;
			var namedTypeSymbol = testType as INamedTypeSymbol;
			if (namedTypeSymbol != null) {
				foreach (var arg in namedTypeSymbol.TypeArguments)
					if (ContainsType (arg, searchType))
						return true;
			}
			return false;
		}

		public override int CompareTo (object obj)
		{
			var anonymousMethodCompletionData = obj as AnonymousMethodCompletionData;
			if (anonymousMethodCompletionData != null)
				return -1;
			var objectCreationData = obj as ObjectCreationCompletionData;
			if (objectCreationData != null)
				return -1;
			int ret = base.CompareTo (obj);
			if (ret == 0) {
				var sym = Symbol;
				var other = obj as RoslynSymbolCompletionData;
				if (other == null)
					return 0;
				if (sym.Kind == other.Symbol.Kind) {
					var m1 = sym as IMethodSymbol;
					var m2 = other.Symbol as IMethodSymbol;
					if (m1 != null)
						return m1.Parameters.Length.CompareTo (m2.Parameters.Length);
					var p1 = sym as IPropertySymbol;
					var p2 = other.Symbol as IPropertySymbol;
					if (p1 != null)
						return p1.Parameters.Length.CompareTo (p2.Parameters.Length);
				}
			}
			return ret;
		}

		static bool IsObsolete (ISymbol symbol)
		{
			return symbol.GetAttributes ().Any (attr => attr.AttributeClass.Name == "ObsoleteAttribute" && attr.AttributeClass.ContainingNamespace.GetFullName () == "System");
		}


		public override bool IsOverload (CompletionData other)
		{
			var os = other as RoslynSymbolCompletionData;
			if (os != null) {
				return Symbol.Kind == os.Symbol.Kind && 
					   Symbol.Name == os.Symbol.Name;
			}

			return false;
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
