//
// Comment.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Ide.Editor;
using MonoDevelop.Core.Text;

namespace MonoDevelop.Ide.TypeSystem
{
	public enum CommentType
	{
		Block,
		SingleLine,
		Documentation
	}

	[Serializable]
	public class Comment
	{
		public string OpenTag {
			get;
			set;
		}

		public string ClosingTag {
			get;
			set;
		}

		public string Text {
			get;
			set;
		}

		public bool HasRegion {
			get {
				return !Region.IsEmpty;
			}
		}

		public DocumentRegion Region {
			get;
			set;
		}

		public bool IsDocumentation {
			get;
			set;
		}
		
		public bool CommentStartsLine {
			get;
			set;
		}
		
		public CommentType CommentType {
			get;
			set;
		}
		
		public Comment ()
		{
		}
		
		public Comment (string text)
		{
			this.Text = text;
		}
		
		public override string ToString ()
		{
			return $"[Comment: OpenTag={OpenTag}, ClosingTag={ClosingTag}, Region={Region}, Text={Text}, IsDocumentation={IsDocumentation}, CommentStartsLine={CommentStartsLine}, CommentType={CommentType}]";
		}
	}
}
