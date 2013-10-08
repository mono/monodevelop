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

namespace MonoDevelop.CSharp.Completion
{
	class ProtocolCompletionData : CompletionData
	{
		readonly MonoCSharpCompletionEngine engine;
		readonly IMember member;
		readonly static Ambience ambience = new CSharpAmbience ();
		readonly int    declarationBegin;

		public bool GenerateBody { get; set; }

		public override TooltipInformation CreateTooltipInformation (bool smartWrap)
		{
			return MemberCompletionData.CreateTooltipInformation (engine.Ext, null, member, smartWrap);
		}

		public ProtocolCompletionData (MonoCSharpCompletionEngine engine, int declarationBegin, IMember member) : base (null)
		{
			this.engine = engine;
			this.member = member;

			this.declarationBegin = declarationBegin;
			this.GenerateBody = true;
			this.Icon = member.GetStockIcon ();
			this.DisplayText = ambience.GetString (member, OutputFlags.IncludeParameters | OutputFlags.IncludeParameterName | OutputFlags.IncludeGenerics | OutputFlags.HideExtensionsParameter| OutputFlags.IncludeAccessor);
			this.CompletionText = member.SymbolKind == SymbolKind.Indexer ? "this" : member.Name;
		}

		public override void InsertCompletionText (CompletionListWindow window, ref KeyActions ka, Gdk.Key closeChar, char keyChar, Gdk.ModifierType modifier)
		{
			var ext = engine.Ext;
			var editor = ext.TextEditorData;
			var generator = CodeGenerator.CreateGenerator (ext.Document);
			if (ext.Project != null)
				generator.PolicyParent = ext.Project.Policies;
			var builder = engine.MDRefactoringCtx.CreateTypeSystemAstBuilder ();

			string sb = BaseExportCodeGenerator.GenerateMemberCode (engine.MDRefactoringCtx, builder, member);
			sb = sb.TrimEnd ();

			string indent = editor.GetIndentationString (editor.Caret.Location); 
			sb = sb.Replace (editor.EolMarker, editor.EolMarker + indent);

			int targetCaretPosition = sb.LastIndexOf ("throw", StringComparison.Ordinal);
			int selectionEndPosition = sb.LastIndexOf (";", StringComparison.Ordinal);

			editor.Replace (declarationBegin, editor.Caret.Offset - declarationBegin, sb);
			if (selectionEndPosition > 0) {
				targetCaretPosition += declarationBegin;
				selectionEndPosition += declarationBegin;
				editor.Caret.Offset = selectionEndPosition;
				editor.SetSelection (targetCaretPosition, selectionEndPosition);
			}
		}
	}
}

