// 
// StringTagModel.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
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

namespace MonoDevelop.Core.StringParsing
{
	public class StringTagDescription
	{
		string name;
		string description;
		bool important = true;
		
		IStringTagProvider provider;
		object instance;
		
		public StringTagDescription (string name, string description)
		{
			this.name = name;
			this.description = description;
		}
		
		public StringTagDescription (string name, string description, bool important)
		{
			this.name = name;
			this.description = description;
			this.important = important;
		}
		
		internal void SetSource (IStringTagProvider provider, object instance)
		{
			this.provider = provider;
			this.instance = instance;
		}
		
		internal object GetValue ()
		{
			return provider.GetTagValue (instance, name.ToUpperInvariant ());
		}
		
		public string Name {
			get { return this.name; }
		}

		public string Description {
			get { return this.description; }
		}

		public bool Important {
			get { return this.important; }
		}
	}
}
