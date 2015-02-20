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
	class RoslynCompletionData : CompletionData, ICSharpCode.NRefactory6.CSharp.Completion.ICompletionData
	{
		List<ICSharpCode.NRefactory6.CSharp.Completion.ICompletionData> overloads;
		
		public override bool HasOverloads {
			get {
				return overloads != null;
			}
		}
		
		void ICSharpCode.NRefactory6.CSharp.Completion.ICompletionData.AddOverload (ICSharpCode.NRefactory6.CSharp.Completion.ICompletionData data)
		{
			if (overloads == null)
				overloads = new List<ICSharpCode.NRefactory6.CSharp.Completion.ICompletionData> ();
			overloads.Add (data);
			sorted = null;
		}

		ICSharpCode.NRefactory6.CSharp.Completion.ICompletionCategory ICSharpCode.NRefactory6.CSharp.Completion.ICompletionData.CompletionCategory { 
			get {
				return (ICSharpCode.NRefactory6.CSharp.Completion.ICompletionCategory)base.CompletionCategory;
			} 
			set {
				base.CompletionCategory = (CompletionCategory)value;
			} 
		}

		ICSharpCode.NRefactory6.CSharp.Completion.DisplayFlags ICSharpCode.NRefactory6.CSharp.Completion.ICompletionData.DisplayFlags { 
			get {
				return (ICSharpCode.NRefactory6.CSharp.Completion.DisplayFlags)base.DisplayFlags;
			}
			set {
				base.DisplayFlags = (DisplayFlags)value;
			}
		}

		List<ICSharpCode.NRefactory6.CSharp.Completion.ICompletionData> sorted;

		IEnumerable<ICSharpCode.NRefactory6.CSharp.Completion.ICompletionData> ICSharpCode.NRefactory6.CSharp.Completion.ICompletionData.OverloadedData {
			get {
				if (overloads == null)
					return new ICSharpCode.NRefactory6.CSharp.Completion.ICompletionData[] { this };
				
				if (sorted == null) {
					sorted = new List<ICSharpCode.NRefactory6.CSharp.Completion.ICompletionData> (overloads);
					sorted.Add (this);
					// sorted.Sort (new OverloadSorter ());
				}
				return sorted;
			}
		}

		ICSharpCode.NRefactory6.CSharp.Completion.ICompletionKeyHandler keyHandler;

		ICSharpCode.NRefactory6.CSharp.Completion.ICompletionKeyHandler ICSharpCode.NRefactory6.CSharp.Completion.ICompletionData.KeyHandler {
			get {
				return keyHandler;
			}
		}

		public RoslynCompletionData (ICSharpCode.NRefactory6.CSharp.Completion.ICompletionKeyHandler keyHandler)
		{
			this.keyHandler = keyHandler;
		}

		public RoslynCompletionData (ICSharpCode.NRefactory6.CSharp.Completion.ICompletionKeyHandler keyHandler, string text) : base (text)
		{
			this.keyHandler = keyHandler;
		}

		public RoslynCompletionData (ICSharpCode.NRefactory6.CSharp.Completion.ICompletionKeyHandler keyHandler, string text, IconId icon) : base (text, icon)
		{
			this.keyHandler = keyHandler;
		}

		public RoslynCompletionData (ICSharpCode.NRefactory6.CSharp.Completion.ICompletionKeyHandler keyHandler, string text, IconId icon, string description) : base (text, icon, description)
		{
			this.keyHandler = keyHandler;
		}
		
		public RoslynCompletionData (ICSharpCode.NRefactory6.CSharp.Completion.ICompletionKeyHandler keyHandler, string displayText, IconId icon, string description, string completionText) : base (displayText, icon, description, completionText)
		{
			this.keyHandler = keyHandler;
		}
		
//		class OverloadSorter : IComparer<ICSharpCode.NRefactory6.CSharp.Completion.ICompletionData>
//		{
//			public OverloadSorter ()
//			{
//			}
//
//			public int Compare (ICSharpCode.NRefactory6.CSharp.Completion.ICompletionData x, ICSharpCode.NRefactory6.CSharp.Completion.ICompletionData y)
//			{
//				var mx = ((RoslynCompletionData)x).Entity as IMember;
//				var my = ((RoslynCompletionData)y).Entity as IMember;
//				int result;
//				
//				if (mx is ITypeDefinition && my is ITypeDefinition) {
//					result = ((((ITypeDefinition)mx).TypeParameters.Count).CompareTo (((ITypeDefinition)my).TypeParameters.Count));
//					if (result != 0)
//						return result;
//				}
//				
//				if (mx is IMethod && my is IMethod) {
//					return MethodParameterDataProvider.MethodComparer ((IMethod)mx, (IMethod)my);
//				}
//				string sx = mx.ReflectionName;// ambience.GetString (mx, flags);
//				string sy = my.ReflectionName;// ambience.GetString (my, flags);
//				result = sx.Length.CompareTo (sy.Length);
//				return result == 0 ? string.Compare (sx, sy) : result;
//			}
//		}

	}

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

		string text;
		protected readonly CSharpCompletionTextEditorExtension ext;

		public RoslynSymbolCompletionData (ICSharpCode.NRefactory6.CSharp.Completion.ICompletionKeyHandler keyHandler, CSharpCompletionTextEditorExtension ext, ISymbol symbol, string text = null) : base (keyHandler)
		{
			this.ext = ext;
			this.text = text;
			this.symbol = symbol;
		}
		
		public override TooltipInformation CreateTooltipInformation (bool smartWrap)
		{
			return CreateTooltipInformation (ext.Editor, ext.DocumentContext, Symbol, smartWrap);
		}
		
		public static TooltipInformation CreateTooltipInformation (MonoDevelop.Ide.Editor.TextEditor editor, MonoDevelop.Ide.Editor.DocumentContext ctx, ISymbol entity, bool smartWrap, bool createFooter = false)
		{
			if (editor == null)
				throw new ArgumentNullException ("editor");
			if (ctx == null)
				throw new ArgumentNullException ("ctx");
			if (ctx.ParsedDocument == null || ctx.AnalysisDocument == null)
				LoggingService.LogError ("Signature markup creator created with invalid context." + Environment.NewLine + Environment.StackTrace);

			var tooltipInfo = new TooltipInformation ();
//			if (resolver == null)
//				resolver = file != null ? file.GetResolver (compilation, textEditorData.Caret.Location) : new CSharpResolver (compilation);
			var sig = new SignatureMarkupCreator (editor, ctx, editor != null ? editor.CaretOffset : 0);
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
			string text = CompletionText;
			string partialWord = GetCurrentWord (window);
			int skipChars = 0;
			bool runParameterCompletionCommand = false;
			bool runCompletionCompletionCommand = false;
			var method = Symbol as IMethodSymbol;
			bool addParens = CompletionTextEditorExtension.AddParenthesesAfterCompletion;
			bool addOpeningOnly = CompletionTextEditorExtension.AddOpeningOnly;
			bool IsDelegateExpected = false;
			var Editor = ext.Editor;
			var Policy = ext.FormattingPolicy;
			if (addParens && !IsDelegateExpected && method != null && !HasNonMethodMembersWithSameName (Symbol) && !IsBracketAlreadyInserted (method)) {
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
				bool insertSemicolon = InsertSemicolon(exprStart);
				if (Symbol is IMethodSymbol && ((IMethodSymbol)Symbol).MethodKind == MethodKind.Constructor)
					insertSemicolon = false;
				//int pos;

				var keys = new [] { SpecialKey.Return, SpecialKey.Tab, SpecialKey.Space };
				if (keys.Contains (descriptor.SpecialKey) || descriptor.KeyChar == '.') {
					if (HasAnyOverloadWithParameters (method)) {
						if (addOpeningOnly) {
							text += RequireGenerics (method) ? "<|" : (addSpace ? " (|" : "(|");
							skipChars = 0;
						} else {
							if (descriptor.KeyChar == '.') {
								if (RequireGenerics (method)) {
									text += addSpace ? "<> ()" : "<>()";
								} else {
									text += addSpace ? " ()" : "()";
								}
								skipChars = 0;
							} else {
								if (insertSemicolon) {
									if (RequireGenerics (method)) {
										text += addSpace ? "<|> ();" : "<|>();";
										skipChars = addSpace ? 5 : 4;
									} else {
										text += addSpace ? " (|);" : "(|);";
										skipChars = 2;
									}
								} else {
									if (RequireGenerics (method)) {
										text += addSpace ? "<|> ()" :  "<|>()";
										skipChars = addSpace ? 4 : 3;
									} else {
										text += addSpace ? " (|)" : "(|)";
										skipChars = 1;
									}
								}
							}
						}
						runParameterCompletionCommand = true;
					} else {
						if (addOpeningOnly) {
							text += RequireGenerics (method) ? "<|" : (addSpace ? " (|" : "(|");
							skipChars = 0;
						} else {
							if (descriptor.KeyChar == '.') {
								if (RequireGenerics (method)) {
									text += addSpace ? "<> ().|" : "<>().|";
								} else {
									text += addSpace ? " ().|" : "().|";
								}
								skipChars = 0;
							} else {
								if (insertSemicolon) {
									if (RequireGenerics (method)) {
										text += addSpace ? "<|> ();" : "<|>();";
									} else {
										text += addSpace ? " ();|" : "();|";
									}

								} else {
									if (RequireGenerics (method)) {
										text += addSpace ? "<|> ()" : "<|>()";
									} else {
										text += addSpace ? " ()|" : "()|";
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
				ka |= KeyActions.Ignore;
			}
			if ((DisplayFlags & DisplayFlags.NamedArgument) == DisplayFlags.NamedArgument &&
				CompletionTextEditorExtension.AddParenthesesAfterCompletion &&
				(descriptor.SpecialKey == SpecialKey.Tab ||
					descriptor.SpecialKey == SpecialKey.Return ||
					descriptor.SpecialKey == SpecialKey.Space)) {
				if (true/*Policy.AroundAssignmentParentheses */)
					text += " ";
				text += "=";
				if (/*Policy.AroundAssignmentParentheses && */descriptor.SpecialKey != SpecialKey.Space)
					text += " ";
				runCompletionCompletionCommand = true;
			}
			window.CompletionWidget.SetCompletionText (window.CodeCompletionContext, partialWord, text);
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

		bool IsBracketAlreadyInserted (IMethodSymbol method)
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



		bool InsertSemicolon (int exprStart)
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

		static bool HasAnyOverloadWithParameters (IMethodSymbol method)
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
			return member.ContainingType
				.GetMembers ()
				.Any (e => e.Kind != member.Kind && e.Name == member.Name);
		}

		bool RequireGenerics (IMethodSymbol method)
		{
			if (method.MethodKind == MethodKind.Constructor)
				return method.ContainingType.TypeParameters.Length  > 0;
			var testMethod = method.ReducedFrom ?? method;
			return testMethod.TypeArguments.Any (t => !testMethod.Parameters.Any (p => ContainsType(p.Type as INamedTypeSymbol, t)));
		}

		bool ContainsType (INamedTypeSymbol testType, ITypeSymbol searchType)
		{
			if (testType == null)
				return false;
			if (testType == searchType)
				return true;
			foreach (var arg in testType.TypeParameters)
				if (ContainsType (arg as INamedTypeSymbol, searchType))
					return true;
			return false;
		}

		public override int CompareTo (object obj)
		{
			var anonymousMethodCompletionData = obj as AnonymousMethodCompletionData;
			if (anonymousMethodCompletionData == null)
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
