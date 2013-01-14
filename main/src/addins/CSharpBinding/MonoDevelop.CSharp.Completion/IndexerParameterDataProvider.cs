// 
// NRefactoryIndexerParameterDataProvider.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Xml;

using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.CSharp.Formatting;
using MonoDevelop.CSharp.Parser;

using System.Text.RegularExpressions;
using ICSharpCode.NRefactory.CSharp;
using Mono.TextEditor;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.Completion;
using MonoDevelop.Ide.TypeSystem;
using ICSharpCode.NRefactory.CSharp.TypeSystem;

namespace MonoDevelop.CSharp.Completion
{
	class IndexerParameterDataProvider : AbstractParameterDataProvider
	{
//		AstNode resolvedExpression;
		List<IProperty> indexers;

		ICompilation compilation;
		CSharpUnresolvedFile file;
		
		public IndexerParameterDataProvider (int startOffset, CSharpCompletionTextEditorExtension ext, IType type, IEnumerable<IProperty> indexers, AstNode resolvedExpression) : base (ext, startOffset)
		{
			compilation = ext.UnresolvedFileCompilation;
			file = ext.CSharpUnresolvedFile;
			//			this.resolvedExpression = resolvedExpression;
			this.indexers = new List<IProperty> (indexers);
		}

		public override TooltipInformation CreateTooltipInformation (int overload, int currentParameter, bool smartWrap)
		{
			return MethodParameterDataProvider.CreateTooltipInformation (ext, compilation, file, indexers[overload], currentParameter, smartWrap);
		}

		#region IParameterDataProvider implementation
		public override int GetParameterCount (int overload)
		{
			if (overload >= Count)
				return -1;
			var indexer = indexers[overload];
			return indexer != null && indexer.Parameters != null ? indexer.Parameters.Count : 0;
		}

		public override bool AllowParameterList (int overload)
		{
			if (overload >= Count)
				return false;
			var lastParam = indexers[overload].Parameters.LastOrDefault ();
			return lastParam != null && lastParam.IsParams;
		}

		public override string GetParameterName (int overload, int paramIndex)
		{
			var indexer = indexers[overload];
			return indexer.Parameters[paramIndex].Name;
		}

		public override int Count {
			get {
				return indexers != null ? indexers.Count : 0;
			}
		}
		#endregion
	}
}
