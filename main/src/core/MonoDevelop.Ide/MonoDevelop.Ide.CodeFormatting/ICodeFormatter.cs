// 
// IFormatter.cs
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
using MonoDevelop.Projects.Policies;

namespace MonoDevelop.Ide.CodeFormatting
{
	public interface ICodeFormatter
	{
		string FormatText (PolicyContainer policyParent, IEnumerable<string> mimeTypeChain, string input);
		
		/// <summary>
		/// Formats the text in a range. Returns only the modified range, or null if formatting failed.
		/// </summary>
		string FormatText (PolicyContainer policyParent, IEnumerable<string> mimeTypeChain,
			string input, int startOffset, int endOffset);
	}
	
	public abstract class AbstractCodeFormatter : ICodeFormatter
	{
		public abstract string FormatText (PolicyContainer policyParent, IEnumerable<string> mimeTypeChain,
			string input, int startOffset, int endOffset);
		
		public string FormatText (PolicyContainer policyParent, IEnumerable<string> mimeTypeChain, string input)
		{
			if (string.IsNullOrEmpty (input))
				return input;
			return FormatText (policyParent, mimeTypeChain, input, 0, input.Length);
		}
	}
}