//
// AddinAttribute.cs
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
	[AttributeUsage (AttributeTargets.Assembly)]
	public class AddinAttribute: Attribute
	{
		string id;
		string version;
		string ns;
		string category;
		
		public AddinAttribute ()
		{
		}
		
		public AddinAttribute (string id)
		{
			this.id = id;
		}
		
		public AddinAttribute (string id, string version)
		{
			this.id = id;
			this.version = version;
		}
		
		public string Id {
			get { return id != null ? id : string.Empty; }
			set { id = value; }
		}
		
		public string Version {
			get { return version != null ? version : string.Empty; }
			set { version = value; }
		}
		
		public string Namespace {
			get { return ns != null ? ns : string.Empty; }
			set { ns = value; }
		}
		
		public string Category {
			get { return category != null ? category : string.Empty; }
			set { category = value; }
		}
	}
}
