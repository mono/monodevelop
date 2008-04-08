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
		
		public AspNetStackMode Mode {
			get { return mode; }
		}
		
		public AspNetStackTag CurrentTag {
			get { return stack.Peek (); }
		}
		
		public AspNetStackTag[] GetTagParentsList ()
		{
			return stack.ToArray ();
		}
		
		public void PushChar (char c)
		{
			//in xml, the < char opens a new element amywhere except in a CDATA section
			if (c == '<') {
				switch (mode) {
					
				default:
					break;
				}
			}
			
			switch (mode) {
			case AspNetStackMode.Free:
				break;
			}
			
			throw new NotImplementedException ();
		}
		
		public void Push (string text)
		{
			foreach (char c in text)
				PushChar (c);
		}

		public object Clone ()
		{
			return new AspNetIndentStack (this);
		}
		
		public override string ToString ()
		{
			StringBuilder s = new StringBuilder ();
			s.AppendFormat ("[IndentStack Position={0} Mode={1}", position, mode);
			
			if (stack.Count > 0) {
				foreach (AspNetStackTag t in stack) {
					s.AppendLine ();
					s.AppendFormat ("\t[{0}({1})]\n", t.Name, t.Position);
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
		int position;
		bool closed;
		bool error;
		
		public AspNetStackTag (int position)
		{
			this.position = position;
		}
		
		public string Name {
			get { return name; }
		}
		
		public int Position {
			get { return position; }
		}
		
		public bool Closed {
			get { return closed; }
		}
		
		public bool Error {
			get { return error; }
		}
		
	}
	
	public enum AspNetStackMode
	{
		Free,
		OpenBracket,
		Tag,
		TagName,
		Attribute,
		AttributeQuotes,
		Comment,
		CData,
	}
}
