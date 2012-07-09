// 
// MimeTypeExtensionNode.cs
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

namespace MonoDevelop.Ide.Extensions
{
	[NodeAttribute ("class", typeof(Type), true, Description="Name of the class")]
	[NodeAttribute ("mimeType", typeof(string), true, Description="Mime type to attach to.")]
	public class MimeTypeExtensionNode : ExtensionNode
	{
		public string TypeName {
			get;
			set;
		}
		
		public string MimeType {
			get;
			set;
		}
		
		protected override void Read (NodeElement elem)
		{
			TypeName = elem.GetAttribute ("class");
			if (string.IsNullOrEmpty (TypeName))
				throw new InvalidOperationException ("type not provided");
			MimeType = elem.GetAttribute ("mimeType");
			if (string.IsNullOrEmpty (MimeType))
				throw new InvalidOperationException ("type not provided");
		}
		
		public virtual object CreateInstance ()
		{
			return Activator.CreateInstance (base.Addin.GetType (TypeName, true));
		}
	}
}
