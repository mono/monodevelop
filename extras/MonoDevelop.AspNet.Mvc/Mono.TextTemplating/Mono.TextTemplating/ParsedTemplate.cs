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
using Microsoft.VisualStudio.TextTemplating;

namespace Mono.TextTemplating
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
				template.LogError (ex.Message, ex.Location);
			}
			return template;
		}
		
		void Parse (ITextTemplatingEngineHost host, Tokeniser tokeniser)
		{
			bool skip = false;
			while ((skip || tokeniser.Advance ()) && tokeniser.State != State.EOF) {
				skip = false;
				switch (tokeniser.State) {
				case State.Block:
					if (!String.IsNullOrEmpty (tokeniser.Value))
						segments.Add (new TemplateSegment (SegmentType.Block, tokeniser.Value, tokeniser.Location));
					break;
				case State.Content:
					if (!String.IsNullOrEmpty (tokeniser.Value))
						segments.Add (new TemplateSegment (SegmentType.Content, tokeniser.Value, tokeniser.Location));
					break;
				case State.Expression:
					if (!String.IsNullOrEmpty (tokeniser.Value))
						segments.Add (new TemplateSegment (SegmentType.Expression, tokeniser.Value, tokeniser.Location));
					break;
				case State.Helper:
					if (!String.IsNullOrEmpty (tokeniser.Value))
						segments.Add (new TemplateSegment (SegmentType.Helper, tokeniser.Value, tokeniser.Location));
					break;
				case State.Directive:
					Directive directive = null;
					string attName = null;
					while (!skip && tokeniser.Advance ()) {
						switch (tokeniser.State) {
						case State.DirectiveName:
							if (directive == null) {
								directive = new Directive (tokeniser.Value.ToLower (), tokeniser.Location);
								if (directive.Name != "include")
									Directives.Add (directive);
							} else
								attName = tokeniser.Value;
							break;
						case State.DirectiveValue:
							if (attName != null && directive != null)
								directive.Attributes[attName.ToLower ()] = tokeniser.Value;
							else
								LogError ("Directive value without name", tokeniser.Location);
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
				default:
					throw new InvalidOperationException ();
				}
			}
		}
		
		void Import (ITextTemplatingEngineHost host, Directive includeDirective)
		{
			string fileName;
			if (includeDirective.Attributes.Count > 1 || !includeDirective.Attributes.TryGetValue ("file", out fileName)) {
				LogError ("Unexpected attributes in include directive", includeDirective.Location);
				return;
			}
			if (!File.Exists (fileName)) {
				LogError ("Included file '" + fileName + "' does not exist.", includeDirective.Location);
				return;
			}
			
			string content, resolvedName;
			if (host.LoadIncludeText (fileName, out content, out resolvedName))
				Parse (host, new Tokeniser (resolvedName, content));
			else
				LogError ("Could not resolve include file '" + fileName + "'.", includeDirective.Location);
		}
		
		void LogError (string message, Location location, bool isWarning)
		{
			CompilerError err = new CompilerError ();
			err.ErrorText = message;
			if (location.FileName != null) {
				err.Line = location.Line;
				err.Column = location.Column;
				err.FileName = location.FileName;
			} else {
				err.FileName = rootFileName;
			}
			err.IsWarning = isWarning;
			errors.Add (err);
		}
		
		public void LogError (string message)
		{
			LogError (message, Location.Empty, false);
		}
		
		public void LogWarning (string message)
		{
			LogError (message, Location.Empty, true);
		}
		
		public void LogError (string message, Location location)
		{
			LogError (message, Location.Empty, false);
		}
		
		public void LogWarning (string message, Location location)
		{
			LogError (message, location, true);
		}
	}
	
	class TemplateSegment
	{
		public TemplateSegment (SegmentType type, string text, Location location)
		{
			this.Type = type;
			this.Location = location;
			this.Text = text;
		}
		
		public SegmentType Type { get; set; }
		public string Text { get; set; }
		public Location Location { get; set; }
	}
	
	class Directive
	{
		public Directive (string name, Location location)
		{
			this.Name = name;
			Attributes = new Dictionary<string, string> ();
			this.Location = location;
		}
		
		public string Name { get; private set; }
		public Dictionary<string,string> Attributes { get; private set; }
		public Location Location { get; set; }
		
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
	
	struct Location
	{
		public Location (string fileName, int line, int column)
		{
			this.FileName = fileName;
			this.Column = column;
			this.Line = line;
		}
		
		public int Line { get; private set; }
		public int Column { get; private set; }
		public string FileName { get; private set; }
		
		public static Location Empty {
			get { return new Location (null, -1, -1); }
		}
		
		public Location AddLine ()
		{
			return new Location (this.FileName, this.Line + 1, 1);
		}
		
		public Location AddCol ()
		{
			return AddCols (1);
		}
		
		public Location AddCols (int number)
		{
			return new Location (this.FileName, this.Line, this.Column + number);
		}
		
		public override string ToString ()
		{
			return string.Format("[{0} ({1},{2})]", FileName, Line, Column);
		}
	}
}
