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
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using MonoDevelop.Core;
using MonoDevelop.Core.StringParsing;
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
				contentSrcFile = FileService.MakePathSeparatorsNative (srcAtt.Value);
				if (contentSrcFile.IsNullOrEmpty)
					throw new InvalidOperationException ("Template's Src attribute is empty");
				contentSrcFile = contentSrcFile.ToAbsolute (baseDirectory);
			} else {
				// Taking InnerText doesn't work, because there because there can be child 
				// AssemblyReference nodes, and we don't want their values too. To fix this, look 
				// for a single child CData node (which is the common pattern) and use only that.
				var node = GetSingleCDataChild (filenode) ?? filenode;
				content = node.InnerText;
			}
		}
		
		static XmlNode GetSingleCDataChild (XmlElement el)
		{
			var child = el.FirstChild;
			XmlNode singleCData = null;
			while (child != null) {
				if (child.NodeType == XmlNodeType.CDATA) {
					if (singleCData != null)
						return null;
					singleCData = child;
				}
				child = child.NextSibling;
			}
			return singleCData;
		}
		
		public override string CreateContent (string language)
		{
			return contentSrcFile.IsNullOrEmpty? content : File.ReadAllText (contentSrcFile);
		}

		protected override string ProcessContent (string content, IStringTagModel tags)
		{
			content = FileTemplateParser.Parse (content, tags);
			return base.ProcessContent (content, tags);
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
			
			contentSrcFile = FileService.MakePathSeparatorsNative (srcAtt.Value);
			if (contentSrcFile.IsNullOrEmpty)
				throw new InvalidOperationException ("Template's Src attribute is empty");
			contentSrcFile = contentSrcFile.ToAbsolute (baseDirectory);
		}
		
		public override Task<Stream> CreateFileContentAsync (SolutionFolderItem policyParent, Project project, string language,
			string fileName, string identifier)
		{
			return Task.FromResult<Stream>(File.OpenRead (contentSrcFile));
		}
	}
}
