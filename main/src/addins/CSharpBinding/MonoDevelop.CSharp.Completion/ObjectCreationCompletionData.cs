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
using Microsoft.CodeAnalysis;
using MonoDevelop.Ide.CodeCompletion;
using System.Text;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Ide.Editor.Extension;
using System.Linq;
using Gtk;
using MonoDevelop.Ide;

namespace MonoDevelop.CSharp.Completion
{
	class ObjectCreationCompletionData : RoslynCompletionData
	{
		public static readonly SymbolDisplayFormat HideParameters =
			new SymbolDisplayFormat(
				globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
				typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
				propertyStyle: SymbolDisplayPropertyStyle.NameOnly,
				genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
				memberOptions: SymbolDisplayMemberOptions.None,
				parameterOptions:
				SymbolDisplayParameterOptions.None,
				miscellaneousOptions:
				SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
				SymbolDisplayMiscellaneousOptions.UseSpecialTypes
			);
		readonly CSharpCompletionTextEditorExtension ext;
		readonly ITypeSymbol type;
		readonly ISymbol symbol;
		readonly int declarationBegin;
		readonly bool afterKeyword;
		readonly SemanticModel semanticModel;

		string displayText;


		public override string DisplayText {
			get {
				if (displayText == null) {
					displayText = CropGlobal(symbol.ToMinimalDisplayString (semanticModel, declarationBegin, Ambience.LabelFormat)) + "()";
					if (!afterKeyword)
						displayText = "new " + displayText;
				}
				return displayText;
			}
		}

		public override string GetDisplayTextMarkup ()
		{
			var model = ext.ParsedDocument.GetAst<SemanticModel> ();

			var result = "<b>" + Ambience.EscapeText (CropGlobal(symbol.ToMinimalDisplayString (model, declarationBegin, HideParameters))) + "</b>()";
			if (!afterKeyword)
				result = "new " + result;
			return ApplyDiplayFlagsFormatting (result);
		}

		static string CropGlobal (string str)
		{
			// shouldn't happen according to the display format - but happens. bug ?
			if (str.StartsWith ("global::"))
				return str.Substring ("global::".Length);
			return str;
		}

		public ObjectCreationCompletionData (ICSharpCode.NRefactory6.CSharp.Completion.ICompletionKeyHandler keyHandler, CSharpCompletionTextEditorExtension ext, SemanticModel semanticModel, ITypeSymbol type, ISymbol symbol, int declarationBegin, bool afterKeyword) : base(keyHandler)
		{
			this.type = type;
			this.semanticModel = semanticModel;
			this.afterKeyword = afterKeyword;
			this.declarationBegin = declarationBegin;
			this.symbol = symbol;
			this.ext = ext;
		}

		public override int CompareTo (object obj)
		{
			var objCrCompData = obj as ObjectCreationCompletionData;
			if (objCrCompData == null)
				return -1;

			return DisplayText.CompareTo(objCrCompData.DisplayText);
		}

		internal static bool HasAnyConstructorWithParameters (ITypeSymbol symbol)
		{
			return symbol.GetMembers()
				.OfType<IMethodSymbol>()
				.Where(m => m.MethodKind == MethodKind.Constructor)
				.Any (m => m.Parameters.Length > 0);
		}


		public override void InsertCompletionText (CompletionListWindow window, ref KeyActions ka, KeyDescriptor descriptor)
		{
			var editor = ext.Editor;
			var sb = new StringBuilder ();
			if (!afterKeyword)
				sb.Append ("new ");
			sb.Append (symbol.ToMinimalDisplayString (semanticModel, declarationBegin, HideParameters));
			string partialWord = GetCurrentWord (window);
			bool runParameterCompletionCommand = false;
			bool runCompletionCompletionCommand = false;

			bool addParens = CompletionTextEditorExtension.AddParenthesesAfterCompletion;
			bool addOpeningOnly = CompletionTextEditorExtension.AddOpeningOnly;

			var Policy = ext.FormattingPolicy;
			int skipChars = 0;

			if (addParens) {
				var line = editor.GetLine (editor.CaretLine);
				//var start = window.CodeCompletionContext.TriggerOffset + partialWord.Length + 2;
				//var end = line.Offset + line.Length;
				//string textToEnd = start < end ? Editor.GetTextBetween (start, end) : "";
				bool addSpace = Policy.SpaceAfterMethodCallName && MonoDevelop.Ide.Editor.DefaultSourceEditorOptions.Instance.OnTheFlyFormatting;

				int exprStart = window.CodeCompletionContext.TriggerOffset - 1;
				while (exprStart > line.Offset) {
					char ch = editor.GetCharAt (exprStart);
					if (ch != '.' && ch != '_' && !char.IsLetterOrDigit (ch))
						break;
					exprStart--;
				}
				bool insertSemicolon = RoslynSymbolCompletionData.InsertSemicolon(ext, exprStart);
//				if (Symbol is IMethodSymbol && ((IMethodSymbol)Symbol).MethodKind == MethodKind.Constructor)
//					insertSemicolon = false;

				var keys = new [] { SpecialKey.Return, SpecialKey.Tab, SpecialKey.Space };
				if (keys.Contains (descriptor.SpecialKey) || descriptor.KeyChar == '.') {
					if (HasAnyConstructorWithParameters (type)) {
						if (addOpeningOnly) {
							sb.Append (addSpace ? " (|" : "(|");
						} else {
							if (descriptor.KeyChar == '.') {
								sb.Append (addSpace ? " ()" : "()");
							} else {
								if (insertSemicolon) {
									sb.Append (addSpace ? " (|);" : "(|);");
									skipChars = 2;
								} else {
									sb.Append (addSpace ? " (|)" : "(|)");
									skipChars = 1;
								}
							}
						}
						runParameterCompletionCommand = true;
					} else {
						if (addOpeningOnly) {
							sb.Append (addSpace ? " (|" : "(|");
							skipChars = 0;
						} else {
							if (descriptor.KeyChar == '.') {
								sb.Append (addSpace ? " ().|" : "().|");
								skipChars = 0;
							} else {
								if (insertSemicolon) {
									sb.Append (addSpace ? " ();|" : "();|");
								} else {
									sb.Append (addSpace ? " ()|" : "()|");
								}
							}
						}
					}
					if (descriptor.KeyChar == '(') {
						var skipCharList = editor.SkipChars;
						if (skipCharList.Count > 0) {
							var lastSkipChar = skipCharList[skipCharList.Count - 1];
							if (lastSkipChar.Offset == (window.CodeCompletionContext.TriggerOffset + partialWord.Length) && lastSkipChar.Char == ')')
								editor.RemoveText (lastSkipChar.Offset, 1);
						}
					}
				}
				ka |= KeyActions.Ignore;
			}
			window.CompletionWidget.SetCompletionText (window.CodeCompletionContext, partialWord, sb.ToString ());
			int offset = editor.CaretOffset;
			for (int i = 0; i < skipChars; i++) {
				editor.AddSkipChar (offset, editor.GetCharAt (offset));
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
	}

}
