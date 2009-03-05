// 
// Tokeniser.cs
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
using System.IO;
using System.Diagnostics;

namespace Mono.TextTemplating
{
	
	
	class Tokeniser : ILocation
	{
		string content;
		int position = 0;
		string value;
		State nextState;
		int nextStateLine;
		
		public Tokeniser (string fileName, string content)
		{
			this.content = content;
			this.FileName = fileName;
		}
		
		public bool Advance ()
		{
			value = null;
			State = nextState;
			Line = nextStateLine;
			nextState = GetNextStateAndCurrentValue ();
			return State != State.EOF;
		}
		
		State GetNextStateAndCurrentValue ()
		{
			switch (State) {
			case State.Block:
			case State.Expression:
				return GetBlockEnd ();
			
			case State.Directive:
				return NextStateInDirective ();
				
			case State.Content:
				return NextStateInContent ();
				
			case State.DirectiveName:
				return GetDirectiveName ();
				
			case State.DirectiveValue:
				return GetDirectiveValue ();
				
			default:
				throw new InvalidOperationException ("Unexpected state '" + State.ToString () + "'");
			}
		}
		
		State GetBlockEnd ()
		{
			int start = position;
			for (; position < content.Length; position++) {
				char c = content[position];
				if (c == '\n') {
					nextStateLine++;
				} else if (c =='>' && content[position-1] == '#' && content[position-2] != '\\') {
					position++;
					value = content.Substring (start, position - start);
					return State.Content;
				}
			}
			return State.EOF;
		}
		
		State GetDirectiveName ()
		{
			int start = position;
			for (; position < content.Length; position++) {
				char c = content[position];
				if (!Char.IsLetterOrDigit (c)) {
					position--;
					value = content.Substring (start, position - 1);
				}
				return State.Directive;
			}
			return State.EOF;
		}
		
		State GetDirectiveValue ()
		{
			int start = position;
			int delimiter = '\0';
			for (; position < content.Length; position++) {
				char c = content[position];
				if (c == '\n')
					nextStateLine++;
				if (delimiter == '\0') {
					if (c == '\'' || c == '"') {
						start = position;
						delimiter = c;
					} else if (!Char.IsWhiteSpace (c)) {
						throw new ParserException ("Unexpected character '" + c + "'. Expecting attribute value.", this);
					}
					continue;
				}
				if (c == delimiter) {
					value = content.Substring (start, position - 1);
					return State.Directive;
				}
			}
			return State.EOF;
		}
		
		State NextStateInContent ()
		{
			int start = position;
			for (; position < content.Length; position++) {
				char c = content[position];
				if (c == '\n') {
					nextStateLine++;
				} else if (c =='<' && position + 2 > content.Length && content[position+1] == '#') {
					char type = content[position+2];
					if (type == '@') {
						position += 2;
						value = content.Substring (start, position - start);
						return State.Directive;
					} else if (type == '=') {
						position += 2;
						value = content.Substring (start, position - start);
						return State.Content;
					} else if (type == '+') {
						position += 2;
						value = content.Substring (start, position - start);
						return State.Helper;
					}  else {
						position++;
						value = content.Substring (start, position - start);
						return State.Block;
					}
				}
			}
			return State.EOF;
		}
		
		State NextStateInDirective ()
		{
			for (; position < content.Length; position++) {
				char c = content[position];
				if (c == '\n') {
					nextStateLine++;
				} else if (Char.IsLetter (c)) {
					position--;
					return State.DirectiveName;
				} else if (c == '=') {
					return State.DirectiveValue;	
				} else if (c == '#' && position + 1 < content.Length && content[position+1] == '>') {
					return State.Content;
				} else if (!Char.IsWhiteSpace (c)) {
					throw new ParserException ("Directive ended unexpectedly", this);
				}
			}
			return State.EOF;
		}
		
		public State State {
			get; private set;
		}
		
		public int Line {
			get; private set;
		}
		
		public int Position {
			get { return position; }
		}
		
		public string Content {
			get { return content; }
		}
		
		public string Value {
			get { return value; }
		}
		
		public string FileName {
			get; private set;
		}
	}
	
	enum State
	{
		Directive,
		Content,
		Expression,
		Block,
		Helper,
		DirectiveName,
		DirectiveValue,
		Name,
		EOF
	}
	
	class ParserException : Exception, ILocation
	{
		
		public ParserException (string message, Tokeniser parent) : base (message)
		{
			Line = parent.Line;
			FileName = parent.FileName;
		}
		
		public int Line { get; private set; }
		public string FileName { get; private set; }
	}
}
