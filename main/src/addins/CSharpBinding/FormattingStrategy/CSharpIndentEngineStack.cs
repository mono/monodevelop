//
// CSharpIndentEngineStack.cs
//
// Author: Jeffrey Stedfast <fejj@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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
using System.Text;

using CSharpBinding.FormattingStrategy.Properties;

namespace CSharpBinding.FormattingStrategy {
	public partial class CSharpIndentEngine : ICloneable {
		public enum Inside {
			Empty              = 0,
			
			PreProcessor       = (1 << 0),
			
			MultiLineComment   = (1 << 1),
			LineComment        = (1 << 2),
			DocComment         = (1 << 11),
			Comment            = (MultiLineComment | LineComment | DocComment),
			
			VerbatimString     = (1 << 3),
			StringLiteral      = (1 << 4),
			CharLiteral        = (1 << 5),
			String             = (VerbatimString | StringLiteral),
			StringOrChar       = (String | CharLiteral),
			
			Attribute          = (1 << 6),
			ParenList          = (1 << 7),
			
			FoldedStatement    = (1 << 8),
			Block              = (1 << 9),
			Case               = (1 << 10),
			
			FoldedOrBlock      = (FoldedStatement | Block),
			FoldedBlockOrCase  = (FoldedStatement | Block | Case)
		}
		
		private class IndentStack : ICloneable {
			readonly static int INITIAL_CAPACITY = 16;
			
			struct Node {
				public Inside inside;
				public string keyword;
				public string indent;
				public int nSpaces;
				public int lineNr;
			};
			
			Node[] stack;
			int size;
			
			public IndentStack () : this (INITIAL_CAPACITY)
			{
			}
			
			public IndentStack (int capacity)
			{
				if (capacity < INITIAL_CAPACITY)
					capacity = INITIAL_CAPACITY;
				
				this.stack = new Node [capacity];
				this.size = 0;
			}
			
			public bool IsEmpty {
				get { return size == 0; }
			}
			
			public int Count {
				get { return size; }
			}
			
			public object Clone ()
			{
				IndentStack clone = new IndentStack (stack.Length);
				
				clone.stack = (Node[]) stack.Clone ();
				clone.size = size;
				
				return clone;
			}
			
			public void Reset ()
			{
				for (int i = 0; i < size; i++) {
					stack[i].keyword = null;
					stack[i].indent = null;
				}
				
				size = 0;
			}
			
			public void Push (Inside inside, string keyword, int lineNr, int nSpaces)
			{
				StringBuilder indentBuilder;
				int sp = size - 1;
				Node node;
				int n = 0;
				
				indentBuilder = new StringBuilder ();
				if ((inside & (Inside.Attribute | Inside.ParenList)) != 0) {
					if (size > 0 && stack[sp].inside == inside) {
						indentBuilder.Append (stack[sp].indent);
						if (stack[sp].lineNr == lineNr)
							n = stack[sp].nSpaces;
					} else {
						while (sp >= 0) {
							if ((stack[sp].inside & Inside.FoldedBlockOrCase) != 0) {
								indentBuilder.Append (stack[sp].indent);
								break;
							}
							
							sp--;
						}
					}
					
					indentBuilder.Append (' ', nSpaces - n);
				} else if (inside == Inside.MultiLineComment) {
					if (size > 0) {
						indentBuilder.Append (stack[sp].indent);
						if (stack[sp].lineNr == lineNr)
							n = stack[sp].nSpaces;
					}
					
					indentBuilder.Append (' ', nSpaces - n);
				} else if (inside == Inside.Case) {
					while (sp >= 0) {
						if ((stack[sp].inside & Inside.FoldedOrBlock) != 0) {
							indentBuilder.Append (stack[sp].indent);
							break;
						}
						
						sp--;
					}
					
					if (FormattingProperties.IndentCaseLabels)
						indentBuilder.Append ('\t');
					
					nSpaces = 0;
				} else if ((inside & (Inside.FoldedOrBlock)) != 0) {
					while (sp >= 0) {
						if ((stack[sp].inside & Inside.FoldedBlockOrCase) != 0) {
							indentBuilder.Append (stack[sp].indent);
							break;
						}
						
						sp--;
					}
					
					Inside parent = size > 0 ? stack[size - 1].inside : Inside.Empty;
					
					// This is a workaround to make anonymous methods indent nicely
					if (parent == Inside.ParenList)
						stack[size - 1].indent = indentBuilder.ToString ();
					
					if (inside == Inside.FoldedStatement) {
						indentBuilder.Append ('\t');
					} else if (inside == Inside.Block) {
						if (parent != Inside.Case || nSpaces != -1)
							indentBuilder.Append ('\t');
					}
					
					nSpaces = 0;
				} else if ((inside & (Inside.PreProcessor | Inside.StringOrChar)) != 0) {
					// if these fold, do not indent
					nSpaces = 0;
					
					//pop regions back out
					if (keyword == "region" || keyword == "endregion") {
						for (; sp >= 0; sp--) {
							if ((stack[sp].inside & Inside.FoldedBlockOrCase) != 0) {
								indentBuilder.Append (stack[sp].indent);
								break;
							}
						}
					}
				} else if (inside == Inside.LineComment || inside == Inside.DocComment) {
					// can't actually fold, but we still want to push it onto the stack
					nSpaces = 0;
				} else {
					// not a valid argument?
					throw new ArgumentOutOfRangeException ();
				}
				
				node.indent = indentBuilder.ToString ();
				node.keyword = keyword;
				node.nSpaces = nSpaces;
				node.lineNr = lineNr;
				node.inside = inside;
				
				if (size == stack.Length)
					Array.Resize <Node> (ref stack, 2 * size);
				
				stack[size++] = node;
			}
			
			public void Push (Inside inside, string keyword, int lineNr, int nSpaces, string indent)
			{
				Node node;
				
				node.indent = indent;
				node.keyword = keyword;
				node.nSpaces = nSpaces;
				node.lineNr = lineNr;
				node.inside = inside;
				
				if (size == stack.Length)
					Array.Resize <Node> (ref stack, 2 * size);
				
				stack[size++] = node;
			}
			
			public void Pop ()
			{
				if (size == 0)
					throw new InvalidOperationException ();
				
				int sp = size - 1;
				stack[sp].keyword = null;
				stack[sp].indent = null;
				size = sp;
			}
			
			public Inside PeekInside (int up)
			{
				if (up < 0)
					throw new ArgumentOutOfRangeException ();
				
				if (up >= size)
					return Inside.Empty;
				
				return stack[size - up - 1].inside;
			}
			
			public string PeekKeyword (int up)
			{
				if (up < 0)
					throw new ArgumentOutOfRangeException ();
				
				if (up >= size)
					return String.Empty;
				
				return stack[size - up - 1].keyword;
			}
			
			public string PeekIndent (int up)
			{
				if (up < 0)
					throw new ArgumentOutOfRangeException ();
				
				if (up >= size)
					return String.Empty;
				
				return stack[size - up - 1].indent;
			}
			
			public int PeekLineNr (int up)
			{
				if (up < 0)
					throw new ArgumentOutOfRangeException ();
				
				if (up >= size)
					return -1;
				
				return stack[size - up - 1].lineNr;
			}
		}
	}
}
