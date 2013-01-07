// 
// DelegateDataProvider.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Mike Krüger <mkrueger@novell.com>
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
using System.Collections.Generic;
using System.Text;
using System.Xml;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.CSharp.Formatting;
using MonoDevelop.CSharp.Parser;
using System.Text.RegularExpressions;
using ICSharpCode.NRefactory.CSharp;
using MonoDevelop.CSharp.Resolver;
using Mono.TextEditor;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.CSharp.Resolver;
using MonoDevelop.Ide.TypeSystem;
using ICSharpCode.NRefactory.Completion;
using System.Linq;
using ICSharpCode.NRefactory.CSharp.TypeSystem;

namespace MonoDevelop.CSharp.Completion
{
	class DelegateDataProvider : AbstractParameterDataProvider
	{
//		IType delegateType;
		IMethod delegateMethod;

		ICompilation compilation;
		CSharpUnresolvedFile file;
		
		public DelegateDataProvider (int startOffset, CSharpCompletionTextEditorExtension ext, IType delegateType) : base (ext, startOffset)
		{
			compilation = ext.UnresolvedFileCompilation;
			file = ext.CSharpUnresolvedFile;
			//			this.delegateType = delegateType;
			this.delegateMethod = delegateType.GetDelegateInvokeMethod ();
		}

		public override TooltipInformation CreateTooltipInformation (int overload, int currentParameter, bool smartWrap)
		{
			return MethodParameterDataProvider.CreateTooltipInformation (ext, compilation, file, delegateMethod, currentParameter, smartWrap);
		}

		#region IParameterDataProvider implementation
		public override int GetParameterCount (int overload)
		{
			if (overload >= Count)
				return -1;
			return delegateMethod.Parameters != null ? delegateMethod.Parameters.Count : 0;
		}
		
		public override bool AllowParameterList (int overload)
		{
			if (overload >= Count)
				return false;
			var lastParam = delegateMethod.Parameters.LastOrDefault ();
			return lastParam != null && lastParam.IsParams;
		}

		public override string GetParameterName (int overload, int paramIndex)
		{
			return delegateMethod.Parameters[paramIndex].Name;
		}
		
		public override int Count {
			get {
				return 1;
			}
		}
		#endregion 
	}
}
