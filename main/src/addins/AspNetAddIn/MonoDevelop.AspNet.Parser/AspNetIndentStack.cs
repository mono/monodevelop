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
		
		//we only need a couple of chars of state backwards
		char charBackOne;
		char charBackTwo;
		
		private AspNetIndentStack (AspNetIndentStack copyFrom)
		{
			stack = new Stack<AspNetStackTag> (copyFrom.stack);
			mode = copyFrom.mode;
			position = copyFrom.position;
			charBackOne = copyFrom.charBackOne;
			charBackTwo = copyFrom.charBackTwo;
		}
		
		public AspNetIndentStack ()
		{
			stack = new Stack<AspNetStackTag> ();
			mode = AspNetStackMode.Free;
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
		
		public void PushChar (char charCurrent)
		{
			position++;
			charBackTwo = charBackOne;
			charBackOne = charCurrent;
			
			//in xml, the < char opens a new element amywhere except in a CDATA or comment section
			if (charCurrent == '<' && mode != AspNetStackMode.Comment && mode != AspNetStackMode.CData) {
				mode = AspNetStackMode.TagName;
				stack.Push (new AspNetStackTag (position));
				return;
			}
			
			//XML entities
			if (charCurrent == '&') {
				switch (mode) {
				case AspNetStackMode.Free:
				case AspNetStackMode.AttributeDoubleQuotes:
				case AspNetStackMode.AttributeQuotes:
				case AspNetStackMode.Tag:
				case AspNetStackMode.TagName:
				case AspNetStackMode.ClosingTag:
				case AspNetStackMode.ClosingTagName:
					throw new System.NotImplementedException ();
				default:
					break;
				}
			}
			
			switch (mode) {
			
			case AspNetStackMode.Tag:
				switch (charCurrent) {
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
				if (charCurrent == '"') {
					mode = AspNetStackMode.Tag;
					return;
				}
				break;
			
			case AspNetStackMode.AttributeQuotes:
				if (charCurrent == '\'') {
					mode = AspNetStackMode.Tag;
					return;
				}
				break;
			
			case AspNetStackMode.TagName:
				if (char.IsWhiteSpace (charCurrent)) {
					//look for tag name, and switch into other modes (comment, ClosingTag etc) if appropriate
					throw new System.NotImplementedException ();
					//return;?
				}
				break;
			
			case AspNetStackMode.ClosingTag:
				throw new System.NotImplementedException ();
				
			case AspNetStackMode.Comment:
				if (charCurrent == '>' && charBackOne == '-' && charBackTwo == '-')
					mode = AspNetStackMode.Free;
				return;
				
			case AspNetStackMode.CData:
				if (charCurrent == '>' && charBackOne == ']' && charBackTwo == ']')
					mode = AspNetStackMode.Free;
				return;
				
			case AspNetStackMode.Free:
				return;
				
			default:
				throw new System.InvalidOperationException ("Unknown mode " + mode.ToString ());
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
	
	/// <summary>
	/// Defines the nature of the current location within the document.
	/// </summary>
	public enum AspNetStackMode
	{
		/// <summary>Not within a tag definition or special section</summary>
		Free,
		
		//parts of tag definitions
		
		/// <summary>An opening tag's declaration.</summary>
		Tag,
		/// <summary>A closing tag's declaration.</summary>
		ClosingTag,
		/// <summary>A tag's name.</summary>
		TagName,
		/// <summary>A closing tag's name.</summary>
		ClosingTagName,
		/// <summary>An attribute name.</summary>
		AttributeName,
		/// <summary>A single-quoted attribute value.</summary>
		AttributeQuotes,
		/// <summary>A double-quoted attribute value.</summary>
		AttributeDoubleQuotes,
		
		//special XML sections
		
		/// <summary>An XML/HTML comment.</summary>
		Comment,
		/// <summary>A CDATA section.</summary>
		CData,
		/// <summary>An XML/HTML character entity.</summary>
		Entity,
		
		//ASP.NET expressions
		
		/// <summary>An ASP.NET databinding expression.</summary>
		AspNetDataExpression,
		/// <summary>An ASP.NET render expression.</summary>
		AspNetRenderExpression,
		/// <summary>An ASP.NET render block.</summary>
		AspNetRenderBlock,
		/// <summary>An ASP.NET directive.</summary>
		AspNetDirective,
		/// <summary>An ASP.NET resource expression.</summary>
		AspNetResourceExpression,
		/// <summary>An ASP.NET server-side comment.</summary>
	}
}
