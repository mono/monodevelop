// 
// DefaultFormatter.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Mike Krüger
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
using System.Text;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Text;

namespace MonoDevelop.SourceEditor
{
	public class DefaultFormatter : IFormatter
	{
		public bool CanFormat (string mimeType)
		{
			return true;
		}
		
		public string FormatText (SolutionItem policyParent, string input)
		{
			TextStylePolicy currentPolicy = policyParent.Policies.Get<TextStylePolicy> ();
			if (currentPolicy == null)
				return input;
			StringBuilder result = new StringBuilder ();
			for (int i = 0; i < input.Length; i++) {
				char ch = input[i];
				if (ch == '\t') {
					if (currentPolicy.TabsToSpaces) {
						result.Append (new string (' ' , currentPolicy.TabWidth));
					} else {
						result.Append ('\t');
					}
				} else {
					result.Append (ch);
				}
			}
			
			return result.ToString ();
		}
	}
}
