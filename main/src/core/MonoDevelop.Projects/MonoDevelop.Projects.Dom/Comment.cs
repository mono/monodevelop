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

namespace MonoDevelop.Projects.Dom
{	
	public class Comment
	{
		string openTag;
		string closingTag;
		string text;
		DomRegion region = DomRegion.Empty;
		CommentType commentType;
		bool isDocumentation;
		bool commentStartsLine;
		
		public string OpenTag {
			get {
				return openTag;
			}
			set {
				openTag = value;
			}
		}

		public string ClosingTag {
			get {
				return closingTag;
			}
			set {
				closingTag = value;
			}
		}

		public string Text {
			get {
				return text;
			}
			set {
				text = value;
			}
		}

		public DomRegion Region {
			get {
				return region;
			}
			set {
				region = value;
			}
		}

		public bool IsDocumentation {
			get {
				return isDocumentation;
			}
			set {
				isDocumentation = value;
			}
		}
		
		public bool CommentStartsLine {
			get {
				return commentStartsLine;
			}
			set {
				commentStartsLine = value;
			}
		}
		
		public CommentType CommentType {
			get {
				return commentType;
			}
			set {
				commentType = value;
			}
		}
		
		public Comment ()
		{
		}
		
		public Comment (string text)
		{
			this.text = text;
		}
	}
}
