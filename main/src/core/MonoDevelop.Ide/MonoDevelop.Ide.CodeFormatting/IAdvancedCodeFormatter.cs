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

using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Projects.Policies;

namespace MonoDevelop.Ide.CodeFormatting
{
	public interface IAdvancedCodeFormatter : ICodeFormatter
	{
		bool SupportsOnTheFlyFormatting { get; }
		bool SupportsCorrectingIndent { get; }
		
		void CorrectIndenting (PolicyContainer policyParent, IEnumerable<string> mimeTypeChain,
			TextEditorData textEditorData, int line);
		
		/// <summary>
		/// Formats a text document directly with insert/remove operations.
		/// </summary>
		/// <param name="textEditorData">
		/// A <see cref="System.Object"/> that must be from type Mono.TextEditorData.
		/// </param>
		/// <param name="dom">
		/// A <see cref="ProjectDom"/>
		/// </param>
		/// <param name="unit">
		/// A <see cref="ICompilationUnit"/>
		/// </param>
		/// <param name="caretLocation">
		/// A <see cref="DomLocation"/> that should be the end location to which the parsing should occur.
		/// </param>
		void OnTheFlyFormat (PolicyContainer policyParent, IEnumerable<string> mimeTypeChain,
			TextEditorData textEditorData, IType callingType, IMember callingMember, ProjectDom dom,
			ICompilationUnit unit, DomLocation endLocation);
		
		void OnTheFlyFormat (PolicyContainer policyParent, IEnumerable<string> mimeTypeChain,
			TextEditorData textEditorData, int startOffset, int endOffset);
	}
	
	public abstract class AbstractAdvancedFormatter : AbstractCodeFormatter, IAdvancedCodeFormatter
	{
		public virtual bool SupportsOnTheFlyFormatting { get { return false; } }
		public virtual bool SupportsCorrectingIndent { get { return false; } }
		
		public virtual void OnTheFlyFormat (PolicyContainer policyParent, IEnumerable<string> mimeTypeChain,
			TextEditorData data, IType callingType, IMember callingMember, ProjectDom dom,
			ICompilationUnit unit, DomLocation endLocation)
		{
			throw new NotSupportedException ();
		}
		
		public virtual void OnTheFlyFormat (PolicyContainer policyParent, IEnumerable<string> mimeTypeChain,
			TextEditorData data, int startOffset, int endOffset)
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
