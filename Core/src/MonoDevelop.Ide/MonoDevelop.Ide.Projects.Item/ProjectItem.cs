//
// ProjectItem.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
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
using System.Collections.Generic;

namespace MonoDevelop.Ide.Projects.Item
{
	public class ProjectItem
	{
		string                     include;
		Dictionary<string, string> metaData = new Dictionary<string, string>();
		
		public string Include {
			get { return this.include; }
			set { this.include = value; }
		}
		
		public ProjectItem ()
		{
		}
		
		public ProjectItem (string include)
		{
			this.Include = include;
		}
		
		public bool HasMetadata (string name)
		{
			return metaData.ContainsKey (name);
		}
		
		public string GetMetadata (string name)
		{
			return metaData[name];
		}
		
		public void SetMetadata (string name, string value)
		{
			//Console.WriteLine("set:" + name + " to " + value);
			if (String.IsNullOrEmpty (value)) {
				metaData.Remove (name);
				return;
			}
			metaData[name] = value;
		}
	}
}
