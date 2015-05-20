// CreateOverrideCompletionData.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
//

using System.Linq;
using MonoDevelop.Ide.CodeCompletion;
using Microsoft.CodeAnalysis;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using System.Collections.Generic;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.CSharp.Refactoring;
using MonoDevelop.CSharp.Formatting;
using System;

namespace MonoDevelop.CSharp.Completion
{
	class CreateOverrideCompletionData : RoslynSymbolCompletionData
	{
		readonly int declarationBegin;
		readonly ITypeSymbol currentType;

		bool afterKeyword;

		public bool GenerateBody { get; set; }

		string displayText;
		public override string DisplayText {
			get {
				if (displayText == null) {
					var model = ext.ParsedDocument.GetAst<SemanticModel> ();
					try {
						displayText = base.Symbol.ToMinimalDisplayString (model, declarationBegin, Ambience.LabelFormat) + " {...}";
					} catch (ArgumentOutOfRangeException) {
						displayText = base.Symbol.ToMinimalDisplayString (model, 0, Ambience.LabelFormat) + " {...}";
					}
					if (!afterKeyword)
						displayText = "override " + displayText;
				}

				return displayText;
			}
		}

		public override string GetDisplayTextMarkup ()
		{
			var model = ext.ParsedDocument.GetAst<SemanticModel> ();

			var result = base.Symbol.ToMinimalDisplayString (model, declarationBegin, Ambience.LabelFormat) + " {...}";
			var idx = result.IndexOf (Symbol.Name);
			if (idx >= 0) {
				result = 
					result.Substring(0, idx) +
					"<b>" + Symbol.Name + "</b>"+
					result.Substring(idx + Symbol.Name.Length);
			}

			if (!afterKeyword)
				result = "override " + result;
			
			return ApplyDiplayFlagsFormatting (result);
		}

		public CreateOverrideCompletionData (ICSharpCode.NRefactory6.CSharp.Completion.ICompletionKeyHandler keyHandler, RoslynCodeCompletionFactory factory, int declarationBegin, ITypeSymbol currentType, Microsoft.CodeAnalysis.ISymbol member, bool afterKeyword) : base (keyHandler, factory, member, member.ToDisplayString ())
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
			
			var result = CSharpCodeGenerator.CreateOverridenMemberImplementation (ext.DocumentContext, ext.Editor, currentType, currentType.Locations.First (), Symbol, isExplicit, factory.SemanticModel);
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
