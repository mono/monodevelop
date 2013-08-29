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
using Mono.TextEditor;
using System.Collections.Generic;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.Semantics;
using MonoDevelop.Core;

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
			try {
				return formatter.FormatText (policyParent ?? PolicyService.DefaultPolicies, mimeTypeChain, input);
			} catch (Exception e) {
				LoggingService.LogError ("Error while formatting text.", e);
			}
			return input;
		}
		
		/// <summary>Formats a subrange of the input text.</summary>
		/// <returns>The formatted text of the range.</returns>
		/// <exception cref="T:System.ArgumentOutOfRangeException">When the offsets are out of bounds.</exception>
		public string FormatText (PolicyContainer policyParent, string input, int fromOffset, int toOffset)
		{
			if (fromOffset < 0 || fromOffset > input.Length)
				throw new ArgumentOutOfRangeException ("fromOffset", "should be >= 0 && < " + input.Length + " was:" + fromOffset);
			if (toOffset < 0 || toOffset > input.Length)
				throw new ArgumentOutOfRangeException ("fromOffset", "should be >= 0 && < " + input.Length + " was:" + toOffset);
			try {
				return formatter.FormatText (policyParent ?? PolicyService.DefaultPolicies, mimeTypeChain, input, fromOffset, toOffset);
			} catch (Exception e) {
				LoggingService.LogError ("Error while formatting text.", e);
			}
			return input.Substring (fromOffset, toOffset - fromOffset);
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
		public void OnTheFlyFormat (MonoDevelop.Ide.Gui.Document doc, int startOffset, int endOffset)
		{
			var adv = formatter as IAdvancedCodeFormatter;
			if (adv == null || !adv.SupportsOnTheFlyFormatting)
				throw new InvalidOperationException ("On the fly formatting not supported");
			
			adv.OnTheFlyFormat (doc, startOffset, endOffset);
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

