// PythonExpressionFinder.cs
// 
// Copyright (c) 2009 Christian Hergert <chris@dronelabs.com>
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
using System.Text;

using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;

namespace PyBinding.Parser
{
	public class PythonExpressionFinder: IExpressionFinder
	{
//		ProjectDom m_dom;

		public PythonExpressionFinder (ProjectDom dom)
		{
//			m_dom = dom;
		}
		
		#region IExpressionFinder implementation
		bool IExpressionFinder.IsExpression (string text)
		{
			return true;
		}
		
		ExpressionResult IExpressionFinder.FindExpression (string text, int offset)
		{
			int begin, typebegin;
			var word = GetWordAtOffset (text, offset, out begin);
			var type = GetWordAtOffset (text, begin, out typebegin);
			
//			Console.WriteLine ("Expression word: {0}", word);
//			Console.WriteLine ("Expression type: {0}", type);
			
			return new PythonExpressionResult (word, type);
		}
		
		ExpressionResult IExpressionFinder.FindFullExpression (string text, int offset)
		{
			throw new System.NotImplementedException();
		}
		#endregion
		
		string GetWordAtOffset (string text, int offset, out int begin)
		{
			if (offset < 0 || offset >= text.Length)
			{
				begin = offset;
				return String.Empty;
			}
			
			StringBuilder sb = new StringBuilder ();
			int i = offset;
			char c;
			
			// Look forward for break char
			for (i = offset; i < text.Length; i++)
			{
				c = text [i];
				
				if (Char.IsWhiteSpace (c))
					break;
				
				bool needsBreak = false;
				
				switch (c) {
				case '(':
				case ')':
				case '[':
				case ']':
				case '{':
				case '}':
				case ':':
				case ',':
				case '@':
				case '.':
					needsBreak = true;
					break;
				default:
					sb.Append (c);
					break;
				}
				
				if (Char.IsWhiteSpace (c) || needsBreak)
					break;
			}
			
			if (offset > 0)
			{
				// look backwards for break char
				for (i = offset - 1; i > 0; i--)
				{
					c = text [i];
					
					if (Char.IsWhiteSpace (c))
						break;
					
					bool needsBreak = false;
					
					switch (c) {
					case '(':
					case ')':
					case '[':
					case ']':
					case '{':
					case '}':
					case ':':
					case ',':
					case '@':
					case '.':
						needsBreak = true;
						break;
					default:
						sb.Insert (0, c);
						break;
					}
					
					if (needsBreak)
						break;
				}
			}
			
			begin = i;
			
			return sb.ToString ();
		}
	}
	
	public class PythonExpressionResult: ExpressionResult
	{
		public string Word {
			get;
			set;
		}
		
		public string Type {
			get;
			set;
		}
		
		public PythonExpressionResult (string word, string type) : base (word)
		{
			Word = word;
			Type = type;
		}
	}
}
