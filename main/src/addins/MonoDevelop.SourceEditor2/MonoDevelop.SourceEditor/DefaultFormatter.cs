// 
// DefaultFormatter.cs
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
using System.Text;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Text;

namespace MonoDevelop.SourceEditor
{
	public class DefaultFormatter : AbstractFormatter
	{
		public override bool CanFormat (string mimeType)
		{
			return true;
		}
		
		static int GetNextTabstop (int currentColumn, int tabSize)
		{
			int result = currentColumn + tabSize;
			return (result / tabSize) * tabSize;
		}
		
		protected override string InternalFormat (SolutionItem policyParent, string mimeType, string input, int startOffset, int endOffset)
		{
			IEnumerable<string> mtypes = MonoDevelop.Core.Gui.Services.PlatformService.GetMimeTypeInheritanceChain (mimeType);
			TextStylePolicy currentPolicy = policyParent != null
					? policyParent.Policies.Get<TextStylePolicy> (mtypes)
					: MonoDevelop.Projects.Policies.PolicyService.GetDefaultPolicy<TextStylePolicy> (mtypes);
			
			input = input ?? "";
			int line = 0, col = 0;
			string eolMarker = currentPolicy.GetEolMarker ();
			StringBuilder result = new StringBuilder ();
			
			for (int i = startOffset; i <= endOffset; i++) {
				char ch = input[i];
				switch (ch) {
				case '\t':
					if (currentPolicy.TabsToSpaces) {
						int tabWidth = GetNextTabstop (col, currentPolicy.TabWidth) - col;
						result.Append (new string (' ', tabWidth));
						col += tabWidth;
					} else 
						goto default;
					break;
				case '\r':
					if (i + 1 < input.Length && input[i + 1] == '\n')
						i++;
					goto case '\n';
				case '\n':
					result.Append (eolMarker);
					line++;
					col = 0;
					break;
				default:
					result.Append (ch);
					col++;
					break;
				}
			}
			return result.ToString ();
		}
	}
}
