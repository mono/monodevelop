// 
// XmlParser.cs
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
using System.Diagnostics;

namespace MonoDevelop.Xml.StateEngine
{
	
	
	public class XmlParser : Parser2
	{
		public XmlParser () : base (false) {}
		public XmlParser (bool buildTree) : base (buildTree) {}
		
		protected XState CurrentXState {
			get {
				Debug.Assert (CurrentState < StateOffset && CurrentState > -1);
				return (XState) CurrentState;
			}
		}
		
		protected virtual int StateOffset {
			get { return (int) XState.ProcessingInstructionClosing + 1; }
		}
		
		protected override int Dispatch (char c)
		{
			switch (CurrentXState) {
			case XState.Free:
				switch (c) {
				case '<': return (int)XState.OpeningBracket;
//				case '&': return (int) XState.Entity;
				default: return (int)XState.Free;
				}

			case XState.Malformed:
				if (StateLength == 0) {
					if (BuildTree)
						ConnectAll ();
					EndAll (true);
				}
				switch (c) {
				case '>': return (int)XState.Free;
				case '<': return (int)XState.OpeningBracket;
				default: return (int)XState.Malformed;
				}

			case XState.OpeningBracket:
				if (StateLength == 0) {
					switch (c) {
					case '!': return (int)XState.OpeningBracket;
					case '?':
						Nodes.Push (new XProcessingInstruction (this.Position - 2));
						return (int)XState.ProcessingInstruction;
					
					case '/':
						Nodes.Push (new XClosingTag (this.Position - 2));
						return (int)XState.ClosingTagName;
					
					if (char.IsLetter (c) || c == '_') {
							Nodes.Push (new XElement (this.Position - 2));
							KeywordBuilder.Append (c);
							return (int) XState.TagName;
						}
					}
						
				} else if (StateLength == 1) {
					switch (c) {
					case '-':
						return (int)XState.CommentOpening;
					case '[':
						return (int)XState.CDataOpening;
					}
				}
				LogError ("Malformed tag start.");
				if (c == '>') return (int)XState.Free;
				else if (c == '<') return (int)XState.OpeningBracket;
				else return (int)XState.Malformed;

			case XState.TagName:
return (int)XState.TagName;		
/*
				if (char.IsLetterOrDigit (c) || c == '_') {
					KeywordBuilder.Append (c);
					return (int)XState.TagName;
				}
				
				if (c == ':') {
					XElement el = Nodes.Peek () as INamedXObject;
					if (namedObject.Name.Name != null || KeywordBuilder.Length == 0) {
						LogError ("Unexpected ':' in name.");
						return GetStateForCurrentObject ();
					} else {
						namedObject.Name = new XName (context.KeywordBuilder.ToString ());
						context.KeywordBuilder.Length = 0;
					}
				}
				
				else if (char.IsWhiteSpace (c)) return (int)XState.Tag;
				else if (c == '>') return (int)XState.Free;
				else if (c == '/') return (int)XState.SelfClosing;
				else break;

			case XState.Tag:
				if (char.IsWhiteSpace (c)) return (int)XState.Tag;
				else if (char.IsLetter (c)) return PushCurrent (XState.AttributeName);
				else if (c == '>') return (int)XState.Free;
				else if (c == '/') return (int)XState.SelfClosing;
				else break;

			case XState.SelfClosing:
				if (c == '>') return (int)XState.Free;
				else break;

			case XState.AttributeName:
				if (char.IsWhiteSpace (c))
					return (int)XState.AttributeNamed;
				else if (char.IsLetterOrDigit (c) | c == ':')
					return (int)XState.AttributeName;
				else if (c == '=')
					return (int)XState.AttributeValue;
				else return (int)XState.Malformed;

			case XState.AttributeNamed:
				if (c == '=') return (int)XState.AttributeValue;
				else if (char.IsWhiteSpace (c)) return (int)XState.AttributeNamed;
				else return (int)XState.Malformed;

			case XState.AttributeValue:
				if (c == '\'') return (int)XState.AttributeQuotes;
				else if (c == '"') return (int)XState.AttributeDoubleQuotes;
				else if (c == '<') break;
				else if (char.IsWhiteSpace (c)) return (int)XState.AttributeValue;
				else if (char.IsLetterOrDigit (c)) {
					LogWarning ("Unquoted attribute value.");
					return (int)XState.AttributeUnquoted;
				} else {
					LogWarning ("Incomplete attribute.");
					return PopState ();
				}

			case XState.AttributeQuotes:
				switch (c) {
				case '\'': return PopState ();
				case '<': return UnexpectedOpeningTag;
				case '&': return PushCurrent (XState.Entity);
				default: return (int)XState.AttributeQuotes;
				}

			case XState.AttributeDoubleQuotes:
				switch (c) {
				case '"': return PopState ();
				case '<': return UnexpectedOpeningTag;
				case '&': return PushCurrent (XState.Entity);
				default: return (int)XState.AttributeDoubleQuotes;
				}

			case XState.AttributeUnquoted:
				switch (c) {
				case '<': return UnexpectedOpeningTag;
				case '&': return PushCurrent (XState.Entity);
				case '.': case '_': case ':': case ';': return CurrentState;
				case '>': return (int)XState.Free;
				}
				if (char.IsLetterOrDigit (c)) return CurrentState;
				else if (char.IsWhiteSpace (c)) return PopState ();
				break;
*/
			case XState.CData:
				return (int)((c == ']')? XState.CDataClosing: XState.CData);

			case XState.CDataOpening:
				if (StateLength < "CDATA[".Length) {
					if ("CDATA["[StateLength] == c) return (int)XState.CDataOpening;
					else break;
				} else {
					Nodes.Push (new XCData (Position - "<![CDATA[".Length));
					return (int)XState.CData;
				}

			case XState.CDataClosing:
				if ("]>"[StateLength] == c) {
					if (StateLength == 1) {
						XCData n = (XCData) Nodes.Pop ();
						n.End (Position);
						((XContainer)Nodes.Peek ()).AddChildNode (n);
						return (int) XState.Free;
					} else {
						return (int)XState.CDataClosing;
					}
				} else return (int)XState.CData;
/*
			case XState.Entity:
				if ((StateLength == 0 && c == '#') || char.IsLetterOrDigit (c))
					return (int)XState.Entity;
				else if (c == ';') return PopState ();
				else LogWarning ("Malformed entity");
				break;
*/
			case XState.ProcessingInstruction:
				switch (c) {
				case '<': break;
				case '?': return (int)XState.ProcessingInstructionClosing;
				default: return (int)XState.ProcessingInstruction;
				}
				break;

			case XState.ProcessingInstructionClosing:
				switch (c) {
				case '<': break;
				case '?': return (int)XState.ProcessingInstructionClosing;
				case '>': {
					XProcessingInstruction n = (XProcessingInstruction) Nodes.Pop ();
					n.End (Position);
					if (BuildTree)
						((XContainer)Nodes.Peek ()).AddChildNode (n);
					return (int) XState.Free; }
				default: return (int)XState.ProcessingInstruction;
				}
				break;

			case XState.CommentOpening:
				if (c == '-') {
					Nodes.Push (new XComment (Position - "<!--".Length));
					return (int)XState.Comment;
				}
				else break;

			case XState.Comment:
				if (c == '-') return (int)XState.CommentClosing;
				else return (int)XState.Comment;

			case XState.CommentClosing:
				if (StateLength == 0) {
					if (c == '-') return (int)XState.CommentClosing;
					else return (int)XState.Comment;
				} else if (StateLength > 0) {
					if (c == '>') {
						XComment n = (XComment) Nodes.Pop ();
						n.End (Position);
						if (BuildTree)
							((XContainer)Nodes.Peek ()).AddChildNode (n);
					}
					else LogWarning ("The string '--' should not appear in comments.");
					if (c == '-') return (int)XState.CommentClosing;
					else return (int)XState.Comment;
				}
				else break;
			}
			
			if (c == '<')
				return UnexpectedOpeningTag;
			return (int)XState.Malformed;
		}
		
		protected override object CreateNew ()
		{
			return new XmlParser ();
		}
		
		protected virtual int UnexpectedOpeningTag {
			get {
				LogError ("Unexpected opening tag");
				return (int)XState.OpeningBracket;
			}
		}
		
		protected override XDocument CreateDocument ()
		{
			return new XDocument ();
		}
	}
}
