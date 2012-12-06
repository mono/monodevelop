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
using MonoDevelop.Ide.Gui.Content;
using ICSharpCode.NRefactory.CSharp;
using MonoDevelop.Core;
using MonoDevelop.Ide.CodeCompletion;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Ide.TypeSystem;
using System.Collections.Generic;
using MonoDevelop.Projects;
using MonoDevelop.CSharp.Project;
using System.Linq;
using MonoDevelop.CSharp.Formatting;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using ICSharpCode.NRefactory.CSharp.Refactoring;
using System.Text;
using MonoDevelop.Ide.CodeTemplates;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.Completion;
using ICSharpCode.NRefactory.CSharp.Completion;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using Mono.TextEditor;

namespace MonoDevelop.CSharp.Completion
{
	class ArrayTypeParameterDataProvider : AbstractParameterDataProvider
	{
		ArrayType arrayType;

		public ArrayTypeParameterDataProvider (int startOffset, CSharpCompletionTextEditorExtension ext, ArrayType arrayType, AstNode resolvedNode) : base (ext, startOffset)
		{
			this.arrayType = arrayType;
		}

		public override TooltipInformation CreateTooltipInformation (int overload, int currentParameter, bool smartWrap)
		{
			var tooltipInfo = new TooltipInformation ();
			var file = ext.CSharpUnresolvedFile;
			var compilation = ext.UnresolvedFileCompilation;
			var textEditorData = ext.TextEditorData;
			var formattingPolicy = ext.FormattingPolicy;
			var resolver = file.GetResolver (compilation, textEditorData.Caret.Location);
			var sig = new SignatureMarkupCreator (resolver, formattingPolicy.CreateOptions ());
			sig.HighlightParameter = currentParameter;
			tooltipInfo.SignatureMarkup = sig.GetArrayIndexerMarkup (arrayType);
			return tooltipInfo;
		}


		#region IParameterDataProvider implementation
		public override int GetParameterCount (int overload)
		{
			if (overload >= Count)
				return -1;
			return arrayType.Dimensions;
		}

		public override string GetParameterName (int overload, int paramIndex)
		{
			// unused
			return "";
		}

		public override bool AllowParameterList (int overload)
		{
			return false;
		}
		
		public override int Count {
			get {
				return 1;
			}
		}
		#endregion
	}

}
