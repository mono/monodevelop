// 
// CommentTag.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Mike Krüger <mkrueger@novell.com>
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
using System.Collections.Generic;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Tasks
{
	public class CommentTag
	{
		public CommentTag (string tag, int priority)
		{
			Tag = tag;
			Priority = priority;
		}
		
		public string Tag { get; internal set; }
		
		public int Priority { get; internal set; }
		
		
		public static event EventHandler SpecialCommentTagsChanged;
		
		const string defaultTags = "FIXME:2;TODO:1;HACK:1;UNDONE:0";
		static List<CommentTag> specialCommentTags;
		
		public static List<CommentTag> SpecialCommentTags {
			get {
				if (specialCommentTags == null) {
					string tags = PropertyService.Get ("Monodevelop.TaskListTokens", defaultTags);
					specialCommentTags = CreateCommentTags (tags);
				}
				return specialCommentTags;
			}
			set {
				if (!SpecialCommentTags.Equals (value)) {
					specialCommentTags = value;
					PropertyService.Set ("Monodevelop.TaskListTokens", ToString (specialCommentTags));
					if (SpecialCommentTagsChanged != null)
						SpecialCommentTagsChanged (null, EventArgs.Empty);
				}
			}
		}		
		

		static List<CommentTag> CreateCommentTags (string tagListString)
		{
			var list = new List<CommentTag> ();
			if (string.IsNullOrEmpty (tagListString))
				return list;
			
			string[] tags = tagListString.Split (';');
			for (int n=0; n<tags.Length; n++) {
				string[] split = tags [n].Split (':');
				int priority;
				if (split.Length == 2 && int.TryParse (split [1], out priority))
					list.Add (new CommentTag (split [0], priority));
				else
					MonoDevelop.Core.LoggingService.LogWarning ("Invalid tag list in CommentTagSet: '{0}'", tagListString);
			}
			return list;
		}
		
		static string ToString (List<CommentTag> list)
		{
			string res = "";
			for (int n=0; n<list.Count; n++) {
				if (n > 0)
					res += ";";
				res += list [n].Tag + ":" + list [n].Priority;
			}
			return res;
		}
	}
}

