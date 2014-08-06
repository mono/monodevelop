//
// MonoDevelop XML Editor
//
// Copyright (C) 2006-2007 Matthew Ward
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

namespace MonoDevelop.Xml.Editor
{
	class XmlFileAssociationNode : ExtensionNode
	{
		//ignore warning: field is never assigned
#pragma warning disable 649
		
		[NodeAttribute ("extension", true)]
		string fileExtension;
		
		[NodeAttribute ("namespacePrefix", false)]
		string namespacePrefix;
		
		[NodeAttribute ("namespaceUri", false)]
		string namespaceUri;
		
		[NodeAttribute ("schemaFile", false)]
		string schemaFile;
		
		public XmlFileAssociation GetAssociation ()
		{
			var ns = namespaceUri;
			if (!string.IsNullOrEmpty (schemaFile))
				ns = new Uri (Addin.GetFilePath (schemaFile), UriKind.Absolute).ToString ();
			return new XmlFileAssociation (fileExtension, ns, namespacePrefix);
		}
	}
}