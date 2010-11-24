// 
// Comment.cs
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

using MonoDevelop.Projects.Dom;

namespace MonoDevelop.CSharp.Dom
{
	public enum CommentType {
		SingleLine,
		MultiLine,
		Documentation
	}
	
	public class Comment : DomNode
	{
		public override NodeType NodeType {
			get {
				return NodeType.Unknown;
			}
		}
		
		public CommentType CommentType {
			get;
			set;
		}
		
		public bool StartsLine {
			get;
			set;
		}
		
		public string Content {
			get;
			set;
		}
		
		DomLocation startLocation;
		public override DomLocation StartLocation {
			get { 
				return startLocation;
			}
		}
		
		DomLocation endLocation;
		public override DomLocation EndLocation {
			get {
				return endLocation;
			}
		}
		
		public Comment (CommentType commentType, DomLocation startLocation, DomLocation endLocation)
		{
			this.CommentType = commentType;
			this.startLocation = startLocation;
			this.endLocation = endLocation;
		}

		public override S AcceptVisitor<T, S> (DomVisitor<T, S> visitor, T data)
		{
			return visitor.VisitComment (this, data);
		}
	}
}

