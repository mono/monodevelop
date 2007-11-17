//
// LinkItemCodon.cs
//
// Author:
//   Lluis Sanchez Gual
//

//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
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
using System.Collections;
using System.ComponentModel;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using Mono.Addins;

namespace MonoDevelop.Components.Commands.ExtensionNodes
{
	[ExtensionNode (Description="A menu or toolbar item that opens an URL when activated")]
	internal class LinkItemCodon : InstanceExtensionNode
	{
		[NodeAttribute ("_label", "Label of the item.", Localizable=true)]
		string label;
		
		[NodeAttribute ("link", "URL to open.")]
		string link;
		
//		[XmlMemberAttribute("description")]
//		string description;
		
		public override object CreateInstance ()
		{
			// FIXME: Use internal resource
			return new LinkCommandEntry (label, link, "md-web-search-icon");
		}
	}
}
