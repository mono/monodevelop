// 
// BinaryOperatorExpression.cs
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

namespace MonoDevelop.CSharp.Dom
{
	public class BinaryOperatorExpression : DomNode
	{
		public const int LeftExpressionRole = 100;
		public const int RightExpressionRole = 101;
		public const int OperatorRole = 102;
		
		public override NodeType NodeType {
			get {
				return NodeType.Expression;
			}
		}

		public BinaryOperatorType BinaryOperatorType {
			get;
			set;
		}
		
		public CSharpTokenNode Operator {
			get { return (CSharpTokenNode)GetChildByRole (OperatorRole) ?? CSharpTokenNode.Null; }
		}
		
		public DomNode Left {
			get { return GetChildByRole (LeftExpressionRole) ?? DomNode.Null; }
		}
		
		public DomNode Right {
			get { return GetChildByRole (RightExpressionRole) ?? DomNode.Null; }
		}
		
		public override S AcceptVisitor<T, S> (DomVisitor<T, S> visitor, T data)
		{
			return visitor.VisitBinaryOperatorExpression (this, data);
		}
	}
	
	public enum BinaryOperatorType
	{
		/// <summary>left &amp; right</summary>
		BitwiseAnd,
		/// <summary>left | right</summary>
		BitwiseOr,
		/// <summary>left &amp;&amp; right</summary>
		LogicalAnd,
		/// <summary>left || right</summary>
		LogicalOr,
		/// <summary>left ^ right</summary>
		ExclusiveOr,
		
		/// <summary>left &gt; right</summary>
		GreaterThan,
		/// <summary>left &gt;= right</summary>
		GreaterThanOrEqual,
		/// <summary>left == right</summary>
		Equality,
		/// <summary>left != right</summary>
		InEquality,
		/// <summary>left &lt; right</summary>
		LessThan,
		/// <summary>left &lt;= right</summary>
		LessThanOrEqual,
		
		/// <summary>left + right</summary>
		Add,
		/// <summary>left - right</summary>
		Subtract,
		/// <summary>left * right</summary>
		Multiply,
		/// <summary>left / right</summary>
		Divide,
		/// <summary>left % right</summary>
		Modulus,
		
		/// <summary>left &lt;&lt; right</summary>
		ShiftLeft,
		/// <summary>left &gt;&gt; right</summary>
		ShiftRight,
		
		/// <summary>left ?? right</summary>
		NullCoalescing
	}
}
