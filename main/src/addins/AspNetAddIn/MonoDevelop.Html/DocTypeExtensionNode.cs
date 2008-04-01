// 
// DocTypeExtensionNode.cs
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
using Mono.Addins;

namespace MonoDevelop.Html
{
	
	public class DocTypeExtensionNode : ExtensionNode
	{
		[NodeAttribute("name", true, Description = "The human-readable name of the schema. It's expected to be a substring of the full doctype declaration that's uniquely identifiable." )]
		string name;
		
		[NodeAttribute("fullName", true, Description = "The full doctype declaration." )]
		string fullName;
		
		[NodeAttribute("xsdFile", false, Description = "An XML schema that can be used to provide code completion for documents using this doctype." )]
		string xsdFile;
		
		[NodeAttribute("completionDocTypeName", false, Description = "The name of another doctype declaration that can be used to provide useful code completion for for documents using this doctype.")]
		string completionDocTypeName;
		
		public string Name {
			get { return name; }
		}
		
		public string FullName {
			get { return fullName; }
		}
		
		public string XsdFile {
			get { return xsdFile; }
		}
		
		public string CompletionDocTypeName {
			get { return completionDocTypeName; }
		}
	}
}
