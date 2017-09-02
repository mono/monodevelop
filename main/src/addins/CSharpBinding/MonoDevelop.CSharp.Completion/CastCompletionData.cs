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
using MonoDevelop.Core;
using MonoDevelop.Ide.CodeCompletion;
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Ide.Editor.Extension;

namespace MonoDevelop.CSharp.Completion
{
	class CastCompletionData : RoslynSymbolCompletionData
	{
		readonly SemanticModel semanticModel;
		readonly SyntaxNode nodeToCast;
		readonly ITypeSymbol targetType;

		public CastCompletionData (ICompletionDataKeyHandler keyHandler, RoslynCodeCompletionFactory factory, SemanticModel semanticModel, ISymbol symbol, SyntaxNode nodeToCast, ITypeSymbol targetType) : base(keyHandler, factory, symbol)
		{
			this.targetType = targetType;
			this.nodeToCast = nodeToCast;
			this.semanticModel = semanticModel;
		}

		public override string GetDisplayDescription (bool isSelected)
		{
			var description = "<span font='11'>(cast to " + SafeMinimalDisplayString (targetType, semanticModel, nodeToCast.SpanStart, Ambience.LabelFormat) + ")<span>";
			if (isSelected)
				return description;
			return "<span foreground=\"darkgray\">" + description + "</span>";
		}

		public override void InsertCompletionText (CompletionListWindow window, ref KeyActions ka, KeyDescriptor descriptor)
		{
			var editor = factory.Ext.Editor;
			var offset = window.CodeCompletionContext.TriggerOffset;
			using (var undo = editor.OpenUndoGroup ()) {
				base.InsertCompletionText (window, ref ka, descriptor);
				var span = nodeToCast.Span;
				var type = SafeMinimalDisplayString (targetType, semanticModel, nodeToCast.SpanStart, Ambience.LabelFormat);
				editor.ReplaceText (span.Start, span.Length, "((" + type + ")" + nodeToCast + ")");
			}
		}

		public override bool IsOverload (CompletionData other)
		{
			return false;
		}
	}
}
