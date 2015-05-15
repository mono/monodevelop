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
using System.Collections.Generic;

namespace MonoDevelop.CSharp.Completion
{
	class ObjectCreationCompletionData : RoslynSymbolCompletionData
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

		readonly ITypeSymbol type;
		readonly ISymbol symbol;
		ISymbol insertSymbol;
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

		public override bool HasOverloads {
			get {
				return true;
			}
		}

		List<CompletionData> overloads;
		public override IReadOnlyList<CompletionData> OverloadedData {
			get {
				if (overloads == null) {
					overloads = new List<CompletionData> ();
					foreach (var constructor in type.GetMembers ().OfType<IMethodSymbol> ().Where (m => m.MethodKind == MethodKind.Constructor)) {
						overloads.Add (new ObjectCreationCompletionData (keyHandler, factory, semanticModel, type, constructor, declarationBegin, afterKeyword) { insertSymbol = this.insertSymbol }); 
					}
				}
				return overloads;
			}
		}

		public ObjectCreationCompletionData (ICSharpCode.NRefactory6.CSharp.Completion.ICompletionKeyHandler keyHandler, RoslynCodeCompletionFactory factory, SemanticModel semanticModel, ITypeSymbol type, ISymbol symbol, int declarationBegin, bool afterKeyword) : base(keyHandler, factory, symbol)
		{
			this.type = type;
			this.semanticModel = semanticModel;
			this.afterKeyword = afterKeyword;
			this.declarationBegin = declarationBegin;
			this.symbol = insertSymbol = symbol;
		}

		protected override string GetInsertionText ()
		{
			var sb = new StringBuilder ();
			if (!afterKeyword)
				sb.Append ("new ");
			sb.Append (CropGlobal (insertSymbol.ToMinimalDisplayString (semanticModel, declarationBegin, HideParameters)));
			return sb.ToString () ;
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
	}

}
