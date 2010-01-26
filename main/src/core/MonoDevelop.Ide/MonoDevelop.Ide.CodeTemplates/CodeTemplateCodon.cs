// 
// CodeTemplateCodon.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
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
using Mono.Addins;
using System.Xml;
using System.IO;

namespace MonoDevelop.Ide.CodeTemplates
{
	[ExtensionNode (Description="A template for color and syntax shemes.")]
	public class CodeTemplateCodon : ExtensionNode
	{
		[NodeAttribute("resource", "Name of the resource where the template is stored.")]
		string resource;
		
		[NodeAttribute("file", "Name of the file where the template is stored.")]
		string file;
		
		public CodeTemplateCodon ()
		{
			resource = file = null;
		}
		
		public XmlReader Open ()
		{
			Stream stream;
			if (!string.IsNullOrEmpty (file)) {
				stream = File.OpenRead (Addin.GetFilePath (file));
			} else if (!string.IsNullOrEmpty (resource)) {
				stream = Addin.GetResource (resource);
				if (stream == null)
					throw new ApplicationException ("Template " + resource + " not found");
			} else {
				throw new InvalidOperationException ("Template file or resource not provided");
			}
			
			XmlReaderSettings settings = new XmlReaderSettings ();
			settings.CloseInput = true;
			return XmlTextReader.Create (stream, settings);
		}
	}
}