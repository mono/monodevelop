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
	
	
	class ParsedTemplate
	{
		List<Directive> directives = new List<Directive> ();
		List<TemplateSegment> segments = new List<TemplateSegment> ();
		CompilerErrorCollection errors = new CompilerErrorCollection ();
		string rootFileName;
		
		private ParsedTemplate ()
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
		
		public static ParsedTemplate FromText (string content, ITextTemplatingEngineHost host)
		{
			ParsedTemplate template = new ParsedTemplate ();
			template.rootFileName = host.TemplateFile;
			try {
				template.Parse (host, new Tokeniser (host.TemplateFile, content));
			} catch (ParserException ex) {
				template.LogError (ex.Message, ex);
			}
			return template;
		}
		
		void Parse (ITextTemplatingEngineHost host, Tokeniser tokeniser)
		{
			bool skip = false;
			while (skip || tokeniser.Advance ()) {
				skip = false;
				switch (tokeniser.State) {
				case State.Block:
					segments.Add (new TemplateSegment (SegmentType.Block, tokeniser.Value, tokeniser));
					break;
				case State.Content:
					segments.Add (new TemplateSegment (SegmentType.Content, tokeniser.Value, tokeniser));
					break;
				case State.Expression:
					segments.Add (new TemplateSegment (SegmentType.Expression, tokeniser.Value, tokeniser));
					break;
				case State.Helper:
					segments.Add (new TemplateSegment (SegmentType.Helper, tokeniser.Value, tokeniser));
					break;
				case State.Directive:
					Directive directive = null;
					string attName = null;
					while (!skip && tokeniser.Advance ()) {
						switch (tokeniser.State) {
						case State.DirectiveName:
							if (directive == null) {
								directive = new Directive (tokeniser.Value.ToLower (), tokeniser);
								if (directive.Name != "include")
									Directives.Add (directive);
							} else
								attName = tokeniser.Value;
							break;
						case State.DirectiveValue:
							if (attName != null && directive != null)
								directive.Attributes[attName.ToLower ()] = tokeniser.Value;
							else
								LogError ("Directive value without name", tokeniser);
							attName = null;
							break;
						case State.Directive:
							break;
						default:
							skip = true;
							break;
						}
					}
					if (directive.Name == "include")
						Import (host, directive);
					break;
				}
			}
		}
		
		void Import (ITextTemplatingEngineHost host, Directive includeDirective)
		{
			string fileName;
			if (includeDirective.Attributes.Count > 1 || !includeDirective.Attributes.TryGetValue ("file", out fileName)) {
				LogError ("Unexpected attributes in include directive", includeDirective);
				return;
			}
			if (!File.Exists (fileName)) {
				LogError ("Included file '" + fileName + "' does not exist.", includeDirective);
				return;
			}
			
			string content, resolvedName;
			if (host.LoadIncludeText (fileName, out content, out resolvedName))
				Parse (host, new Tokeniser (resolvedName, content));
			else
				LogError ("Could not resolve include file '" + fileName + "'.", includeDirective);
		}
		
		public CompilerError LogError (string message)
		{
			return LogError (message, null);
		}
		
		public CompilerError LogError (string message, ILocation location)
		{
			CompilerError err = new CompilerError ();
			err.ErrorText = message;
			if (location != null) {
				err.Line = location.Line;
				err.FileName = location.FileName;
			} else {
				err.FileName = rootFileName;
			}
			errors.Add (err);
			return err;
		}
		
		public bool HasErrorsNotWarnings ()
		{
			foreach (CompilerError err in errors)
				if (!err.IsWarning)
					return true;
			return false;
		}
	}
	
	class TemplateSegment : ILocation
	{
		public TemplateSegment (SegmentType type, string text, ILocation location)
		{
			this.Type = type;
			this.Line = location.Line;
			this.Text = text;
			this.FileName = location.FileName;
		}
		
		public SegmentType Type { get; set; }
		public int Line { get; set; }
		public string Text { get; set; }
		public string FileName { get; set; }
	}
	
	class Directive : ILocation
	{
		public Directive (string name, ILocation location)
		{
			this.Name = name;
			Attributes = new Dictionary<string, string> ();
			this.Line = location.Line;
			this.FileName = location.FileName;
		}
		
		public string Name { get; private set; }
		public Dictionary<string,string> Attributes { get; private set; }
		public string FileName { get; set; }
		public int Line { get; set; }
		
		public string Extract (string key)
		{
			string value;
			if (!Attributes.TryGetValue (key, out value))
				return null;
			Attributes.Remove (key);
			return value;
		}
	}
	
	enum SegmentType
	{
		Block,
		Expression,
		Content,
		Helper
	}
	
	interface ILocation
	{
		int Line { get; }
		string FileName { get; }
	}
}
