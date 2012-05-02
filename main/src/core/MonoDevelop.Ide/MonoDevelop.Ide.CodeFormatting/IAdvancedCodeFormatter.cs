// 
// IPrettyPrinter.cs
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
using System.Collections.Generic;
using Mono.TextEditor;
using MonoDevelop.Projects.Policies;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.Semantics;

namespace MonoDevelop.Ide.CodeFormatting
{
	public interface IAdvancedCodeFormatter : ICodeFormatter
	{
		bool SupportsOnTheFlyFormatting { get; }
		bool SupportsCorrectingIndent { get; }
		
		void CorrectIndenting (PolicyContainer policyParent, IEnumerable<string> mimeTypeChain, TextEditorData textEditorData, int line);
		
		void OnTheFlyFormat (MonoDevelop.Ide.Gui.Document doc, int startOffset, int endOffset);
		
	}
	
	public abstract class AbstractAdvancedFormatter : AbstractCodeFormatter, IAdvancedCodeFormatter
	{
		public virtual bool SupportsOnTheFlyFormatting { get { return false; } }
		public virtual bool SupportsCorrectingIndent { get { return false; } }
		
		public virtual void OnTheFlyFormat (MonoDevelop.Ide.Gui.Document doc, int startOffset, int endOffset)
		{
			throw new NotSupportedException ();
		}
		
		public virtual void CorrectIndenting (PolicyContainer policyParent, IEnumerable<string> mimeTypeChain,
			TextEditorData data, int line)
		{
			throw new NotSupportedException ();
		}
	}
}
