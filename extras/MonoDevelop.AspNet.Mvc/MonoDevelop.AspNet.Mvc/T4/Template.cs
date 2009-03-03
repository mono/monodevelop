// 
// Template.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
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
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MonoDevelop.AspNet.Mvc.T4
{
	
	
	class Template
	{
		List<Directive> directives = new List<Directive> ();
		List<TemplateSegment> segments = new List<TemplateSegment> ();
		CompilerErrorCollection errors = new CompilerErrorCollection ();
		
		private Template ()
		{
		}
		
		public List<Directive> Directives {
			get { return directives; }
		}
		
		public List<TemplateSegment> Segments {
			get { return segments; }
		}
		
		public CompilerErrorCollection Errors {
			get { return errors; }
		}
		
		public static Template Parse (string content)
		{
			Template template = new Template ();
			
			Tokeniser tokeniser = new Tokeniser (content);
			while (tokeniser.Advance ()) {
				
				
			}
			
			throw new NotImplementedException ();
			
			return template;
		}
		
		void LogError (string message, int line)
		{
			CompilerError err = new CompilerError ();
			err.ErrorText = message;
			err.Line = line;
			errors.Add (err);
		}
	}
	
	class TemplateSegment
	{
		public TemplateSegment (SegmentType type, string text, int line)
		{
			this.Type = type;
			this.Line = line;
			this.Text = text;
		}
		
		public SegmentType Type { get; set; }
		public int Line { get; set; }
		public string Text { get; set; }
	}
	
	class Directive
	{
		public Directive (string name)
		{
			this.Name = name;
			Attributes = new Dictionary<string, string> ();
		}
		
		public string Name { get; private set; }
		public Dictionary<string,string> Attributes { get; private set; }
	}
	
	enum SegmentType
	{
		Block,
		Expression,
		Content
	}
}
