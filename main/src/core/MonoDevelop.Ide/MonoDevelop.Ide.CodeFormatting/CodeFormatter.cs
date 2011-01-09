// 
// Formatter.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Projects.Policies;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using Mono.TextEditor;
using System.Collections.Generic;

namespace MonoDevelop.Ide.CodeFormatting
{
	public sealed class CodeFormatter
	{
		ICodeFormatter formatter;
		IList<string> mimeTypeChain;
		
		internal CodeFormatter (IList<string> mimeTypeChain, ICodeFormatter formatter)
		{
			this.mimeTypeChain = mimeTypeChain;
			this.formatter = formatter;
		}
		
		public string FormatText (PolicyContainer policyParent, string input)
		{
			return formatter.FormatText (policyParent ?? PolicyService.DefaultPolicies, mimeTypeChain, input);
		}
		
		/// <summary>Formats a subrange of the input text.
		/// <returns>The formatted text of the range.</returns>
		public string FormatText (PolicyContainer policyParent, string input, int fromOffset, int toOffset)
		{
			return formatter.FormatText (policyParent ?? PolicyService.DefaultPolicies, mimeTypeChain,
				input, fromOffset, toOffset);
		}
		
		public bool SupportsOnTheFlyFormatting {
			get {
				var adv = formatter as IAdvancedCodeFormatter;
				return adv != null && adv.SupportsOnTheFlyFormatting;
			}
		}
		
		public bool SupportsCorrectingIndent {
			get {
				var adv = formatter as IAdvancedCodeFormatter;
				return adv != null && adv.SupportsCorrectingIndent;
			}
		}
		
		public bool IsDefault {
			get {
				return formatter is DefaultCodeFormatter;
			}
		}
		
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
		public void OnTheFlyFormat (PolicyContainer policyParent, TextEditorData data,
			IType callingType, IMember callingMember, ProjectDom dom, ICompilationUnit unit,
			DomLocation endLocation)
		{
			var adv = formatter as IAdvancedCodeFormatter;
			if (adv == null || !adv.SupportsOnTheFlyFormatting)
				throw new InvalidOperationException ("On the fly formatting not supported");
			
			adv.OnTheFlyFormat (policyParent ?? PolicyService.DefaultPolicies, mimeTypeChain,
				data, callingType, callingMember, dom, unit, endLocation);
		}
		
		public void OnTheFlyFormat (PolicyContainer policyParent, TextEditorData data,
			int startOffset, int endOffset)
		{
			var adv = formatter as IAdvancedCodeFormatter;
			if (adv == null || !adv.SupportsOnTheFlyFormatting)
				throw new InvalidOperationException ("On the fly formatting not supported");
			adv.OnTheFlyFormat (policyParent ?? PolicyService.DefaultPolicies, mimeTypeChain,
				data, startOffset, endOffset);
		}
		
		public void CorrectIndenting (PolicyContainer policyParent, TextEditorData data, int line)
		{
			var adv = formatter as IAdvancedCodeFormatter;
			if (adv == null || !adv.SupportsCorrectingIndent)
				throw new InvalidOperationException ("Indent correction not supported");
			adv.CorrectIndenting (policyParent ?? PolicyService.DefaultPolicies, mimeTypeChain, data, line);
		}
	}
}

