// 
// CommentTag.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using System.Collections;
using System.Collections.Generic;

namespace MonoDevelop.Projects.Dom.Parser
{
	public class CommentTagSet: IEnumerable<CommentTag>
	{
		List<CommentTag> list;
		
		public CommentTagSet (IEnumerable<CommentTag> tags)
		{
			list = new List<CommentTag> (tags);
		}
		
		public CommentTagSet (string tagListString)
		{
			list = new List<CommentTag> ();
			if (string.IsNullOrEmpty (tagListString))
				return;
			
			string[] tags = tagListString.Split (';');
			for (int n=0; n<tags.Length; n++) {
				string[] split = tags [n].Split (':');
				int priority;
				if (split.Length == 2 && int.TryParse (split[1], out priority))
					list.Add (new CommentTag (split[0], priority));
				else
					MonoDevelop.Core.LoggingService.LogWarning ("Invalid tag list in CommentTagSet: '{0}'", tagListString);
			}
		}
		
		public IEnumerator<CommentTag> GetEnumerator ()
		{
			return list.GetEnumerator ();
		}

		IEnumerator IEnumerable.GetEnumerator ()
		{
			return ((IEnumerable)list).GetEnumerator ();
		}
		
		public string[] GetNames ()
		{
			string[] names = new string[list.Count];
			for (int n=0; n<list.Count; n++)
				names [n] = list [n].Tag;
			return names;
		}
		
		public bool ContainsTag (string name)
		{
			foreach (CommentTag tag in list)
				if (tag.Tag == name)
					return true;
			return false;
		}
		
		public override string ToString ()
		{
			string res = "";
			for (int n=0; n<list.Count; n++) {
				if (n > 0)
					res += ";";
				res += list [n].Tag + ":" + list [n].Priority;
			}
			return res;
		}
		
		public bool Equals (CommentTagSet other)
		{
			if (other.list.Count != list.Count)
				return false;
			List<CommentTag> otags = new List<CommentTag> (other.list);
			foreach (CommentTag tag in list) {
				bool found = false;
				for (int n=0; n<otags.Count; n++) {
					CommentTag otag = otags [n];
					if (otag != null && tag.Tag == otag.Tag && tag.Priority == otag.Priority) {
						otags [n] = null;
						found = true;
						break;
					}
				}
				if (!found)
					return false;
			}
			return true;
		}
	}
	
	public class CommentTag
	{
		public CommentTag (string tag, int priority)
		{
			Tag = tag;
			Priority = priority;
		}
		
		public string Tag { get; internal set; }
		public int Priority { get; internal set; }
	}
}
