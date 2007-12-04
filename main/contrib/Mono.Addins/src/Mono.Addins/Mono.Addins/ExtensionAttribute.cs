//
// ExtensionAttribute.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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

namespace Mono.Addins
{
	[AttributeUsage (AttributeTargets.Class, AllowMultiple=true)]
	public class ExtensionAttribute: Attribute
	{
		string path;
		string nodeName;
		string id;
		string insertBefore;
		string insertAfter;
		
		public ExtensionAttribute ()
		{
		}
		
		public ExtensionAttribute (string path)
		{
			this.path = path;
		}
		
		public string Path {
			get { return path != null ? path : string.Empty; }
			set { path = value; }
		}
		
		public string NodeName {
			get { return nodeName != null && nodeName.Length > 0 ? nodeName : "Type"; }
			set { nodeName = value; }
		}
		
		public string Id {
			get { return id != null ? id : string.Empty; }
			set { id = value; }
		}
		
		public string InsertBefore {
			get { return insertBefore != null ? insertBefore : string.Empty; }
			set { insertBefore = value; }
		}
		
		public string InsertAfter {
			get { return insertAfter != null ? insertAfter : string.Empty; }
			set { insertAfter = value; }
		}
	}
}
