// 
// VariableCompletionData.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2011 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Ide.CodeCompletion;
using ICSharpCode.NRefactory.Completion;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.CSharp.Completion
{
	public class VariableCompletionData : CompletionData, IVariableCompletionData
	{
		readonly CSharpCompletionTextEditorExtension ext;

		public IVariable Variable {
			get;
			private set;
		}

		public override TooltipInformation CreateTooltipInformation (bool smartWrap)
		{
			var tooltipInfo = new TooltipInformation ();
			var resolver = ext.CSharpUnresolvedFile.GetResolver (ext.Compilation, ext.Document.Editor.Caret.Location);
			var sig = new SignatureMarkupCreator (resolver, ext.FormattingPolicy.CreateOptions ());
			sig.BreakLineAfterReturnType = smartWrap;
			tooltipInfo.SignatureMarkup = sig.GetLocalVariableMarkup (Variable);
			return tooltipInfo;
		}

		public VariableCompletionData (CSharpCompletionTextEditorExtension ext, IVariable variable) : base (variable.Name, variable.GetStockIcon ())
		{
			this.ext = ext;
			this.Variable = variable;
		}
	}
}

