//
// StackMatchExpression.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2016 Microsoft Corporation
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
using System.Collections.Immutable;
using System.Text;
using Roslyn.Utilities;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Editor.Highlighting
{
	public abstract class StackMatchExpression
	{
		public abstract (bool, ScopeStack) MatchesStack (ScopeStack scopeStack, ref string matchExpr);

		public static StackMatchExpression Parse (string expression)
		{
			var sb = StringBuilderCache.Allocate ();

			var stackStack = new Stack<Stack<StackMatchExpression>> ();
			var exprStack = new Stack<StackMatchExpression> ();
			char lastChar = '\0';

			foreach (var ch in expression) {
				switch (ch) {
				case ' ':
					if (sb.Length > 0)
						exprStack.Push (CreateMatchExpression (sb));
					lastChar = ch;
					continue;
				case '(':
					stackStack.Push (exprStack);
					exprStack = new Stack<StackMatchExpression> ();
					lastChar = ch;
					continue;
				case ')':
					if (sb.Length > 0)
						exprStack.Push (CreateMatchExpression (sb));
					ShrinkStack (exprStack);
					var newStack = stackStack.Pop ();
					newStack.Push (exprStack.Peek ());
					exprStack = newStack;
					lastChar = ch;
					continue;
				case ',':
				case '|':
					if (sb.Length > 0)
						exprStack.Push (CreateMatchExpression (sb));
					ShrinkStack (exprStack);
					exprStack.Push (new OrExpression (exprStack.Pop ())); ;
					lastChar = ch;
					continue;
				case '-':
					if (lastChar != ' ')
						break;
					if (sb.Length > 0)
						exprStack.Push (CreateMatchExpression (sb));
					ShrinkStack (exprStack);
					exprStack.Push (new MinusExpression (exprStack.Pop ())); ;
					lastChar = ch;
					continue;
				}
				sb.Append (ch);
				lastChar = ch;
			}
			if (sb.Length > 0)
				exprStack.Push (CreateMatchExpression (sb));
			StringBuilderCache.Free (sb);
			ShrinkStack (exprStack);
			if (exprStack.IsEmpty ())
				return new StringMatchExpression ("");
			return exprStack.Peek ();
		}

		static void ShrinkStack (Stack<StackMatchExpression> exprStack)
		{
			while (exprStack.Count > 1) {
				var right = exprStack.Pop ();
				var left = exprStack.Pop ();
				if (left is BinaryExpression) {
					((BinaryExpression)left).SetRightSide (right);
					exprStack.Push (left);
					continue;
				}
				exprStack.Push (new ContinueExpression (left, right));
			}
		}

		static StackMatchExpression CreateMatchExpression (StringBuilder sb)
		{
			var result = new StringMatchExpression (sb.ToString ());
			sb.Length = 0;
			return result;
		}

		abstract class BinaryExpression : StackMatchExpression
		{
			protected StackMatchExpression left, right;

			public BinaryExpression (StackMatchExpression left)
			{
				this.left = left;
			}

			public void SetRightSide (StackMatchExpression right)
			{
				this.right = right;
			}
		}

		class OrExpression : BinaryExpression
		{
			public OrExpression (StackMatchExpression left) : base (left)
			{
			}

			public override (bool, ScopeStack) MatchesStack (ScopeStack scopeStack, ref string matchExpr)
			{
				var leftResult = left.MatchesStack (scopeStack, ref matchExpr);
				if (leftResult.Item1)
					return leftResult;
				return right.MatchesStack (scopeStack, ref matchExpr);
			}

			public override string ToString ()
			{
				return string.Format ("[OrExpression: left={0}, right={1}]", left, right);
			}
		}

		class MinusExpression : BinaryExpression
		{
			public MinusExpression (StackMatchExpression left) : base (left)
			{
			}

			public override (bool, ScopeStack) MatchesStack (ScopeStack scopeStack, ref string matchExpr)
			{
				var leftResult = left.MatchesStack (scopeStack, ref matchExpr);
				if (!leftResult.Item1)
					return leftResult;
				string tmp = "";
				var rightResult = right.MatchesStack (scopeStack, ref tmp);
				if (rightResult.Item1)
					return (false, rightResult.Item2);
				return leftResult;
			}

			public override string ToString ()
			{
				return string.Format ("[MinusExpression: left={0}, right={1}]", left, right);
			}
		}

		class ContinueExpression : StackMatchExpression
		{
			StackMatchExpression left, right;

			public ContinueExpression (StackMatchExpression left, StackMatchExpression right)
			{
				this.left = left;
				this.right = right;
			}

			public override (bool, ScopeStack) MatchesStack (ScopeStack scopeStack, ref string matchExpr)
			{
				var secondResult = right.MatchesStack (scopeStack, ref matchExpr);
				if (secondResult.Item1)
					return left.MatchesStack (secondResult.Item2, ref matchExpr);
				return secondResult;
			}

			public override string ToString ()
			{
				return string.Format ("[ContinueExpression: left={0}, right={1}]", left, right);
			}
		}

		class StringMatchExpression : StackMatchExpression
		{
			readonly string scope;
			static readonly (bool, ScopeStack) mismatch = (false, (ScopeStack)null);

			public StringMatchExpression (string scope)
			{
				this.scope = scope;
			}

			public override (bool, ScopeStack) MatchesStack (ScopeStack scopeStack, ref string matchExpr)
			{
				if (scopeStack.IsEmpty)
					return mismatch;
				bool found = scopeStack.Peek ().StartsWith (scope, StringComparison.Ordinal);
				if (found) {
					matchExpr = scope;
					return (found, scopeStack.Pop ());
				}
				return mismatch;
			}

			public override string ToString ()
			{
				return string.Format ("[StringMatchExpression: scope={0}]", scope);
			}
		}
	}
}