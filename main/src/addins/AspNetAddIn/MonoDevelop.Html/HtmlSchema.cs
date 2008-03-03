// 
// HtmlSchema.cs
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
using System.Xml;
using System.Collections.Generic;

using Commons.Xml.Relaxng;

namespace MonoDevelop.Html
{
	
	public class HtmlSchema
	{
		string doctype;
		XmlReader schema;
		RelaxngGrammar grammar;
		
		public HtmlSchema (string doctype, XmlReader schema)
		{
			this.doctype = doctype;
			this.schema = schema;
		}
		
		public string Doctype {
			get { return doctype; }
		}
				
		internal bool IsInitialised {
			get { return grammar == null; }
		}
		
		void Initialise ()
		{
			/*
			if (IsInitialised)
				throw new Exception ("Already intialised");
			/*
			RelaxngPattern pattern = RelaxngPattern.Read (schema);
			pattern.Compile ();
			grammar = pattern as RelaxngGrammar;
			if (grammar == null)
				throw new Exception ("Html schema must be a valid RELAX NG grammar");
			*/
		}
		
		public ICollection<string> GetValidChildren (string parentTag)
		{
			//FIXME: pull these from the schema
			string[] tags = { "html", "a", "em", "meta", "strong", "div", "span", "head", "title", "meta"};
			return tags;
		}
		
		public ICollection<string> GetValidAttributes (string tag)
		{
			//FIXME: pull these from the schema
			string[] atts = { "id", "class" };
			string[] imgAtts = { "id", "class", "src", "alt", "title" };
			if (tag == "img")
				return imgAtts;
			return atts;
		}
	}
}
