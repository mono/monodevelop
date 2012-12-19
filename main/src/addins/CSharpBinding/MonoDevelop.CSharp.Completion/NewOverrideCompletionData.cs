// NewOverrideCompletionData.cs
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

using System;
using System.Linq;
using System.Text;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using System.Collections.Generic;
using ICSharpCode.NRefactory.CSharp;
using Mono.TextEditor;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.CSharp.Completion
{
	public class NewOverrideCompletionData : CompletionData
	{
		CSharpCompletionTextEditorExtension ext;
		IMember member;
		static Ambience ambience = new CSharpAmbience ();
		int    declarationBegin;
		IUnresolvedTypeDefinition  type;
		
		public bool GenerateBody { get; set; }

		public override TooltipInformation CreateTooltipInformation (bool smartWrap)
		{
			return MemberCompletionData.CreateTooltipInformation (ext, null, member, smartWrap);
		}

		public NewOverrideCompletionData (CSharpCompletionTextEditorExtension ext, int declarationBegin, IUnresolvedTypeDefinition type, IMember member) : base (null)
		{
			this.ext = ext;
			this.type   = type;
			this.member = member;
			
			this.declarationBegin = declarationBegin;
			this.GenerateBody = true;
			this.Icon = member.GetStockIcon ();
			this.DisplayText = ambience.GetString (member, OutputFlags.IncludeParameters | OutputFlags.IncludeParameterName | OutputFlags.IncludeGenerics | OutputFlags.HideExtensionsParameter| OutputFlags.IncludeAccessor);
			this.CompletionText = member.EntityType == EntityType.Indexer ? "this" : member.Name;
		}
		
		public override void InsertCompletionText (CompletionListWindow window, ref KeyActions ka, Gdk.Key closeChar, char keyChar, Gdk.ModifierType modifier)
		{
			var editor = ext.TextEditorData;
			var generator = CodeGenerator.CreateGenerator (ext.Document);
			bool isExplicit = false;
			if (member.DeclaringTypeDefinition.Kind == TypeKind.Interface) {
				foreach (var m in type.Members) {
					if (m.Name == member.Name && !m.ReturnType.Equals (member.ReturnType)) {
						isExplicit = true;
						break;
					}
				}
			}
			var resolvedType = type.Resolve (ext.ParsedDocument.GetTypeResolveContext (ext.Compilation, editor.Caret.Location)).GetDefinition ();
			if (ext.Project != null)
				generator.PolicyParent = ext.Project.Policies;
			var result = generator.CreateMemberImplementation (resolvedType, type, member, isExplicit);
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
			
			editor.Replace (declarationBegin, editor.Caret.Offset - declarationBegin, sb);
			if (selectionEndPosition > 0) {
				editor.Caret.Offset = selectionEndPosition;
				editor.SetSelection (targetCaretPosition, selectionEndPosition);
			} else {
				editor.Caret.Offset = targetCaretPosition;
			}
		}
	}
}
