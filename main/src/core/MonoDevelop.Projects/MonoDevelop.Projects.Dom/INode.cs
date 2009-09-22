// 
// IDomVisitor.cs
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
using System.Collections.Generic;
namespace MonoDevelop.Projects.Dom
{
	public interface INode
	{
/*		INode Parent { 
			get; 
		}
		
		INode NextSibling {
			get;
		}
		
		INode PrevSibling {
			get;
		}
		
		INode FirstChild {
			get;
		}
		
		INode LastChild {
			get;
		}
		*/
		S AcceptVisitor<T, S> (IDomVisitor<T, S> visitor, T data);
	}
	
	public abstract class AbstractNode : INode
	{
		// some pre defined constants for common roles
		public const int Identifier    = 1;
		public const int KeywordRole   = 2;
		public const int ArgumentRole  = 3;
		public const int AttributeRole = 4;
		public const int ReturnTypeRole = 4;
		
		// some pre defined constants for most used punctuation 
		
		public const int LPar  = 50; // (
		public const int RPar  = 51; // )
		
		public const int LBrace  = 52; // {
		public const int RBrace  = 53; // }
		
		public const int LBracket = 54; // [
		public const int RBracket= 55; // ]
		
		public const int LChevron = 56; // <
		public const int RChevron= 57; // >
		
		public const int Dot = 58; // ,
		public const int Comma = 59; // ,
		public const int Colon = 60; // :
		public const int Semicolon = 61; // ;
		public const int QuestionMark = 62; // ?
		
		public INode Parent {
			get;
			set;
		}
		
		public int Role {
			get;
			set;
		}
		
		public INode NextSibling {
			get;
			set;
		}
		
		public INode PrevSibling {
			get;
			set;
		}
		
		public INode FirstChild {
			get;
			set;
		}
		
		public INode LastChild {
			get;
			set;
		}
		
		protected INode GetChildByRole (int role)
		{
			AbstractNode cur = (AbstractNode)FirstChild;
			while (cur != null) {
				if (cur.Role == role)
					return cur;
				cur = (AbstractNode)cur.NextSibling;
			}
			return null;
		}
		
		protected IEnumerable<INode> GetChildrenByRole (int role)
		{
			AbstractNode cur = (AbstractNode)FirstChild;
			while (cur != null) {
				if (cur.Role == role)
					yield return cur;
				cur = (AbstractNode)cur.NextSibling;
			}
		}
		
		public virtual S AcceptVisitor<T, S> (IDomVisitor<T, S> visitor, T data)
		{
			return default(S);
		}
		
	}
	
}
