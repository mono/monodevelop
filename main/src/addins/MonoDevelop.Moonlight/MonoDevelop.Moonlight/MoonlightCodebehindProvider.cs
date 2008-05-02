// 
// MoonlightCodebehindProvider.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;
using System.Text.RegularExpressions;

using MonoDevelop.Projects;
using MonoDevelop.Projects.Text;
using MonoDevelop.DesignerSupport;
using MonoDevelop.DesignerSupport.CodeBehind;

namespace MonoDevelop.Moonlight
{
	
	public class MoonlightCodebehindProvider// : ICodeBehindProvider 
	{
		Regex rx;
		
		public MoonlightCodebehindProvider ()
		{
			//FIXME: this isn't 100% accurate, e.g. if ">\w*<" exists in one of the first element's attributes
			rx = new Regex (@"(?:x:Class\s*=\s*""([\w.]*)[;""]|>\s*<)",
			    RegexOptions.Compiled | RegexOptions.Singleline);
		}
		
		public string GetCodeBehindClassName (ProjectFile file)
		{
			if (Path.GetExtension (file.FilePath).ToLowerInvariant () != ".xaml")
				return null;
			
			ITextFile tf = OpenDocumentFileProvider.Instance.GetEditableTextFile (file.FilePath);
			if (tf == null)
				tf = new TextFile (file.FilePath);
			
			Match match = rx.Match (tf.Text);
			if (match.Groups.Count == 2) {
				string val = match.Groups [1].Value;
				if (!string.IsNullOrEmpty (val))
					return val;
			}
			return null;
		}
	}
}
