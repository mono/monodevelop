//
// TextFileDescriptionTemplate.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
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
using System.Xml;
using MonoDevelop.Core;
using System.IO;
using MonoDevelop.Projects;

namespace MonoDevelop.Ide.Templates
{
	public class TextFileDescriptionTemplate: SingleFileDescriptionTemplate
	{
		string content;
		FilePath contentSrcFile;
		
		public override void Load (XmlElement filenode, FilePath baseDirectory)
		{
			base.Load (filenode, baseDirectory);
			var srcAtt = filenode.Attributes["src"];
			if (srcAtt != null) {
				contentSrcFile = MakePathNative (srcAtt.Value);
				if (contentSrcFile.IsNullOrEmpty)
					throw new InvalidOperationException ("Template's Src attribute is empty");
				contentSrcFile = contentSrcFile.ToAbsolute (baseDirectory);
			} else {
				content = filenode.InnerText;
			}
		}
		
		static internal string MakePathNative (string path)
		{
			if (path == null || path.Length == 0)
				return path;
			char c = Path.DirectorySeparatorChar == '\\'? '/' : '\\'; 
			return path.Replace (c, Path.DirectorySeparatorChar);
		}
		
		public override string CreateContent (string language)
		{
			return contentSrcFile.IsNullOrEmpty? content : File.ReadAllText (contentSrcFile);
		}
	}
	
	public class RawFileDescriptionTemplate : SingleFileDescriptionTemplate
	{
		FilePath contentSrcFile;
		
		public override void Load (XmlElement filenode, FilePath baseDirectory)
		{
			base.Load (filenode, baseDirectory);
			var srcAtt = filenode.Attributes["src"];
			if (srcAtt == null)
				throw new InvalidOperationException ("Template is missing Src attribute");
			
			contentSrcFile = TextFileDescriptionTemplate.MakePathNative (srcAtt.Value);
			if (contentSrcFile.IsNullOrEmpty)
				throw new InvalidOperationException ("Template's Src attribute is empty");
			contentSrcFile = contentSrcFile.ToAbsolute (baseDirectory);
		}
		
		public override Stream CreateFileContent (SolutionItem policyParent, Project project, string language,
			string fileName, string identifier)
		{
			return File.OpenRead (contentSrcFile);
		}
	}
}
