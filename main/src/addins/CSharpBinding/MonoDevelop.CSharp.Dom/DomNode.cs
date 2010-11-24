// 
// AstNode.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
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
using MonoDevelop.Projects.Dom;
using System.Collections.Generic;

namespace MonoDevelop.CSharp.Dom
{
	public abstract class DomNode
	{
		public static readonly DomNode Null = new NullAstNode ();
		class NullAstNode : DomNode
		{
			public override NodeType NodeType {
				get {
					return NodeType.Unknown;
				}
			}
			
			public override bool IsNull {
				get {
					return true;
				}
			}
			
			public override S AcceptVisitor<T, S> (DomVisitor<T, S> visitor, T data)
			{
				return default (S);
			}
		}
		
		public abstract NodeType NodeType {
			get;
		}
		
		public virtual bool IsNull {
			get {
				return false;
			}
		}
		
		public virtual DomLocation StartLocation {
			get { 
				var child = FirstChild;
				if (child == null)
					return DomLocation.Empty;
				return child.StartLocation;
			}
		}
		
		public virtual DomLocation EndLocation {
			get { 
				var child = LastChild;
				if (child == null)
					return DomLocation.Empty;
				return child.EndLocation;
			}
		}
		
		public DomNode Parent {
			get;
			set;
		}
		
		public int Role {
			get;
			set;
		}
		
		public DomNode NextSibling {
			get;
			set;
		}
		
		public DomNode PrevSibling {
			get;
			set;
		}
		
		public DomNode FirstChild {
			get;
			set;
		}
		
		public DomNode LastChild {
			get;
			set;
		}
		
		public IEnumerable<DomNode> Children {
			get {
				var cur = FirstChild;
				while (cur != null) {
					yield return cur;
					cur = cur.NextSibling;
				}
			}
		}
		
		public DomNode GetChildByRole (int role)
		{
			var cur = FirstChild;
			while (cur != null) {
				if (cur.Role == role)
					return cur;
				cur = cur.NextSibling;
			}
			return null;
		}
		
		public IEnumerable<DomNode> GetChildrenByRole (int role)
		{
			var cur = FirstChild;
			while (cur != null) {
				if (cur.Role == role)
					yield return cur;
				cur = cur.NextSibling;
			}
		}
		
		public void AddChild (DomNode child)
		{
			if (child == null)
				return;
			child.Parent = this;
			if (FirstChild == null) {
				LastChild = FirstChild = child;
			} else {
				LastChild.NextSibling = child;
				child.PrevSibling = LastChild;
				LastChild = child;
			}
		}
		
		public void AddChild (DomNode child, int role)
		{
			if (child == null)
				return;
			child.Role = role;
			AddChild (child);
		}
		
		public void InsertChildBefore (DomNode nextSibling, DomNode child, int role)
		{
			if (child == null)
				return;
			
			if (FirstChild == null || nextSibling == null) {
				AddChild (child, role);
				return;
			}
			child.Parent = this;
			child.Role = role;
			
			child.NextSibling = nextSibling;
			
			if (nextSibling.PrevSibling != null) {
				child.PrevSibling = nextSibling.PrevSibling;
				nextSibling.PrevSibling.NextSibling = child;
			}
			nextSibling.PrevSibling = child;
		}
		
		public abstract S AcceptVisitor<T, S> (DomVisitor<T, S> visitor, T data);
		
		public static class Roles
		{
			// some pre defined constants for common roles
			public const int Identifier    = 1;
			public const int Keyword   = 2;
			public const int Parameter  = 3;
			public const int Attribute = 4;
			public const int ReturnType = 5;
			public const int Modifier  = 6;
			public const int Body       = 7;
			public const int Initializer = 8;
			public const int Condition = 9;
			public const int EmbeddedStatement = 10;
			public const int Iterator = 11;
			public const int Expression = 12;
			public const int Statement = 13;
			public const int TargetExpression = 14;
			public const int Member = 15;
			
			// some pre defined constants for most used punctuation 
			
			public const int LPar = 50; // (
			public const int RPar = 51; // )
			
			public const int LBrace = 52; // {
			public const int RBrace = 53; // }
			
			public const int LBracket = 54; // [
			public const int RBracket = 55; // ]
			
			public const int LChevron = 56; // <
			public const int RChevron = 57; // >
			
			public const int Dot = 58; // ,
			public const int Comma = 59; // ,
			public const int Colon = 60; // :
			public const int Semicolon = 61; // ;
			public const int QuestionMark = 62; // ?
			
			public const int Assign = 63; // =
			
			public const int TypeParameter = 64;
			public const int Constraint = 65;
			
			public const int Comment = 66;
		}
	}
}
