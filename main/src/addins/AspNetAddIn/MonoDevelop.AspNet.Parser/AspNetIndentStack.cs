// 
// AspNetIndentStack.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
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
using System.Collections.Generic;
using System.Text;

namespace MonoDevelop.AspNet.Parser
{
	
	
	public class AspNetIndentStack : ICloneable
	{
		Stack<AspNetStackTag> stack;
		AspNetStackMode mode;
		int position;
		StringBuilder buffer;
		
		private AspNetIndentStack (AspNetIndentStack copyFrom)
		{
			stack = new Stack<AspNetStackTag> (copyFrom.stack);
			mode = copyFrom.mode;
			buffer = new System.Text.StringBuilder (copyFrom.buffer.ToString ());
			position = copyFrom.position;
		}
		
		public AspNetIndentStack ()
		{
			stack = new Stack<AspNetStackTag> ();
			mode = AspNetStackMode.Free;
			buffer = new StringBuilder ();
			position = 0;
		}

		#region Properties
		
		public AspNetStackMode Mode {
			get { return mode; }
		}
		
		public AspNetStackTag CurrentTag {
			get { return stack.Peek (); }
		}
		
		public int Position {
			get { return position; }
		}
		
		public AspNetStackTag[] TagParentsList {
			get { return stack.ToArray (); }
		}
		
		#endregion
		
		public void PushChar (char c)
		{
			position++;
			
			//in xml, the < char opens a new element amywhere except in a CDATA or comment section
			if (c == '<' && mode != AspNetStackMode.Comment && mode != AspNetStackMode.CData) {
				mode = AspNetStackMode.TagName;
				stack.Push (new AspNetStackTag (position));
				return;
			}
			
			switch (mode) {
			
			case AspNetStackMode.Tag:
				switch (c) {
				case '"':
					mode = AspNetStackMode.AttributeDoubleQuotes;
					return;
				case '\'':
					mode = AspNetStackMode.AttributeQuotes;
					return;
				case '>':
					stack.Pop ().Close (this.Position);
					return;
				}
				return;
			
			case AspNetStackMode.AttributeDoubleQuotes:
				if (c == '"') {
					mode = AspNetStackMode.Tag;
					return;
				}
				break;
			
			case AspNetStackMode.AttributeQuotes:
				if (c == '\'') {
					mode = AspNetStackMode.Tag;
					return;
				}
				break;
			
			case AspNetStackMode.TagName:
				if (char.IsWhiteSpace (c)) {
					//look for tag name, and switch into other modes (comment, ClosingTag etc) if appropriate
					throw new System.NotImplementedException ();
					//return;?
				}
				break;
			
			case AspNetStackMode.ClosingTag:
				throw new System.NotImplementedException ();
				
			case AspNetStackMode.Comment:
				if (c == '>') {
					//look backwards for end comment
					throw new System.NotImplementedException ();
					//return;?
				}
				break;
			
			case AspNetStackMode.CData:
				if (c == '>') {
					//look backwards to check if end
					throw new System.NotImplementedException ();
					//return;?
				}
				break;
			
			case AspNetStackMode.Free:
				break;
				
			default:
				throw new System.InvalidOperationException ("Unknown mode " + mode);
			}
		}
		
		public void Push (string text)
		{
			foreach (char c in text)
				PushChar (c);
		}
		
		#region ICloneable
		
		public object Clone ()
		{
			return new AspNetIndentStack (this);
		}
		
		#endregion
		
		public override string ToString ()
		{
			StringBuilder s = new StringBuilder ();
			s.AppendFormat ("[IndentStack Position={0} Mode={1}", position, mode);
			
			if (stack.Count > 0) {
				foreach (AspNetStackTag t in stack) {
					s.AppendLine ();
					s.AppendFormat ("\t[{0}({1})]\n", t.Name, t.Start);
				}
				s.AppendLine ();
			}
			s.Append ("]");
			
			return s.ToString ();
		}

	}
	
	public class AspNetStackTag
	{
		string name;
		int start;
		int end = -1;
		
		public AspNetStackTag (int start)
		{
			this.start = start;
		}
		
		public string Name {
			get { return name; }
		}
		
		public int Start {
			get { return start; }
		}
		
		public int End {
			get { return end; }
		}
		
		public bool Complete {
			get { return end > -1; }
		}
		
		public void Close (int endLocation)
		{
			if (this.end == -1)
				throw new System.InvalidOperationException ("Tag already closed");
			this.end = endLocation;
		}
	}
	
	public enum AspNetStackMode
	{
		Free,                  //not within a tag
		Tag,                   //within a tag
		ClosingTag,            //within a closing tag
		TagName,               //within a tag name string
		ClosingTagName,        //within the name of a closing tag
		AttributeQuotes,       //within an attribute's quotes
		AttributeDoubleQuotes, //within an attribute's quotes
		Comment,               //within a comment
		CData,                 //within a CDATA section
		
		AspNetDataExpression,
		AspNetRenderExpression,
		AspNetRenderBlock,
		AspNetDirective,
	}
}
