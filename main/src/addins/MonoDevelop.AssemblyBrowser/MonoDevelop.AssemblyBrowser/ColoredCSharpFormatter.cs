// 
// ColoredCSharpFormatter.cs
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

using System;
using Cecil.Decompiler.Languages;
using System.Text;

namespace MonoDevelop.AssemblyBrowser
{


	public class ColoredCSharpFormatter : IFormatter
	{
		StringBuilder sb = new StringBuilder ();
		bool write_indent;
		int indent;
		
		public string Text {
			get {
				return sb.ToString ();
			}
		}
		
		#region IFormatter implementation
		
		void InternalWrite (string str)
		{
			WriteIndent ();
			sb.Append (str);
			write_indent = false;
		}
		
		void WriteIndent ()
		{
			if (!write_indent)
				return;
			
			for (int i = 0; i < indent; i++)
				sb.Append ("\t");
		}

		public void Write (string str)
		{
			InternalWrite (GLib.Markup.EscapeText (str));
		}
		
		
		public void WriteLine ()
		{
			sb.AppendLine ();
			write_indent = true;
		}
		
		
		public void WriteSpace ()
		{
			InternalWrite (" ");
		}
		
		
		public void WriteToken (string token)
		{
			
			InternalWrite (DomTypeNodeBuilder.MarkupKeyword (GLib.Markup.EscapeText (token)));
		}
		
		
		public void WriteComment (string comment)
		{
			InternalWrite ("<span style=\"comment\">" + GLib.Markup.EscapeText (comment) + "</span>");
			WriteLine ();
		}
		
		
		public void WriteKeyword (string keyword)
		{
			InternalWrite (DomTypeNodeBuilder.MarkupKeyword (GLib.Markup.EscapeText (keyword)));
		}
		
		
		public void WriteLiteral (string literal)
		{
			InternalWrite ("<span style=\"constant\">" + GLib.Markup.EscapeText (literal) + "</span>");
		}
		
		
		public void WriteDefinition (string value, object definition)
		{
			InternalWrite (GLib.Markup.EscapeText (value));
		}
		
		
		public void WriteReference (string value, object reference)
		{
			InternalWrite (DomTypeNodeBuilder.MarkupKeyword (GLib.Markup.EscapeText (value)));
		}
		
		
		public void WriteIdentifier (string value, object identifier)
		{
			InternalWrite (GLib.Markup.EscapeText (value));
		}
		
		
		public void Indent ()
		{
			indent++;
		}
		
		
		public void Outdent ()
		{
			indent--;
		}
		
		#endregion
	}
}

