//
// ProtocolCompletionData.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
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
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.CodeGeneration;
using MonoDevelop.Ide.Editor.Extension;
using Microsoft.CodeAnalysis;
using MonoDevelop.CSharp.Refactoring;
using System.Linq;
using MonoDevelop.CSharp.Formatting;

namespace MonoDevelop.CSharp.Completion
{
	class ProtocolCompletionData : RoslynSymbolCompletionData
	{
		readonly int declarationBegin;
		readonly ITypeSymbol currentType;

		public bool GenerateBody { get; set; }

		static readonly SymbolDisplayFormat NameFormat =
			new SymbolDisplayFormat(
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

		internal static readonly SymbolDisplayFormat overrideNameFormat = NameFormat.WithParameterOptions(
			SymbolDisplayParameterOptions.IncludeDefaultValue |
			SymbolDisplayParameterOptions.IncludeExtensionThis |
			SymbolDisplayParameterOptions.IncludeType |
			SymbolDisplayParameterOptions.IncludeName |
			SymbolDisplayParameterOptions.IncludeParamsRefOut);

		string displayText;

		bool afterKeyword;

		public override string DisplayText {
			get {
				if (displayText == null) {
					var model = ext.ParsedDocument.GetAst<SemanticModel> ();
					displayText = base.Symbol.ToMinimalDisplayString (model, ext.Editor.CaretOffset, overrideNameFormat);
					if (!afterKeyword)
						displayText = "override " + displayText;
				}

				return displayText;
			}
		}


		public ProtocolCompletionData (ICSharpCode.NRefactory6.CSharp.Completion.ICompletionKeyHandler keyHandler, RoslynCodeCompletionFactory factory, int declarationBegin, ITypeSymbol currentType, Microsoft.CodeAnalysis.ISymbol member, bool afterKeyword) : base (keyHandler, factory, member, member.ToDisplayString ())
		{
			this.afterKeyword = afterKeyword;
			this.currentType = currentType;
			this.declarationBegin = declarationBegin;
			this.GenerateBody = true;
		}

		public override void InsertCompletionText (CompletionListWindow window, ref KeyActions ka, KeyDescriptor descriptor)
		{
			var editor = ext.Editor;
			bool isExplicit = false;
			//			if (member.DeclaringTypeDefinition.Kind == TypeKind.Interface) {
			//				foreach (var m in type.Members) {
			//					if (m.Name == member.Name && !m.ReturnType.Equals (member.ReturnType)) {
			//						isExplicit = true;
			//						break;
			//					}
			//				}
			//			}
			//			var resolvedType = type.Resolve (ext.Project).GetDefinition ();
			//			if (ext.Project != null)
			//				generator.PolicyParent = ext.Project.Policies;

			var result = CSharpCodeGenerator.CreateProtocolMemberImplementation (ext.DocumentContext, ext.Editor, currentType, currentType.Locations.First (), Symbol, isExplicit, factory.SemanticModel);
			string sb = result.Code.TrimStart ();
			int trimStart = result.Code.Length - sb.Length;
			sb = sb.TrimEnd ();

			var lastRegion = result.BodyRegions.LastOrDefault ();
			var region = lastRegion == null? null
				: new CodeGeneratorBodyRegion (lastRegion.StartOffset - trimStart, lastRegion.EndOffset - trimStart);

			int targetCaretPosition;
			int selectionEndPosition = -1;
			if (region != null && region.IsValid) {
				targetCaretPosition = declarationBegin + region.StartOffset;
				if (region.Length > 0) {
					if (GenerateBody) {
						selectionEndPosition = declarationBegin + region.EndOffset;
					} else {
						//FIXME: if there are multiple regions, remove all of them
						sb = sb.Substring (0, region.StartOffset) + sb.Substring (region.EndOffset); 
					}
				}
			} else {
				targetCaretPosition = declarationBegin + sb.Length;
			}

			editor.ReplaceText (declarationBegin, editor.CaretOffset - declarationBegin, sb);
			if (selectionEndPosition > 0) {
				editor.CaretOffset = selectionEndPosition;
				editor.SetSelection (targetCaretPosition, selectionEndPosition);
			} else {
				editor.CaretOffset = targetCaretPosition;
			}

			OnTheFlyFormatter.Format (editor, ext.DocumentContext, declarationBegin, declarationBegin + sb.Length);
		}
	}
}

