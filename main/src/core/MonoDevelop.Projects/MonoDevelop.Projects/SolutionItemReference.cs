// SolutionItemReference.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using MonoDevelop.Core.Serialization;

namespace MonoDevelop.Projects
{
	public class SolutionItemReference
	{
		[ProjectPathItemProperty]
		string path;
		
		[ItemProperty]
		string id;
		
		internal SolutionItemReference ()
		{
		}
		
		public SolutionItemReference (SolutionItem item)
		{
			if (item is SolutionEntityItem) {
				path = ((SolutionEntityItem)item).FileName;
			} else {
				path = item.ParentSolution.FileName;
				if ((item is SolutionFolder) && ((SolutionFolder)item).IsRoot)
					id = ":root:";
				else
					id = item.ItemId;
			}
		}
		
		public SolutionItemReference (string path)
		{
			this.path = path;
		}
		
		public SolutionItemReference (string path, string id)
		{
			this.path = path;
			this.id = id;
		}
		
		internal string Path {
			get { return path; }
		}
		
		internal string Id {
			get { return id; }
		}
		
		public override bool Equals (object o)
		{
			SolutionItemReference sr = o as SolutionItemReference;
			if (o == null)
				return false;
			return (path == sr.path) && (id == sr.id);
		}
		
		public override int GetHashCode ()
		{
			return (Path + id).GetHashCode ();
		}
		
		public static bool operator == (SolutionItemReference r1, SolutionItemReference r2)
		{
			if (object.ReferenceEquals (r1, r2))
				return true;
			if ((object)r1 == null || (object)r2 == null)
				return false;
			return r1.Equals (r2);
		}
		
		public static bool operator != (SolutionItemReference r1, SolutionItemReference r2)
		{
			return !(r1 == r2);
		}
	}
}
