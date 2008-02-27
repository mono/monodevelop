//
// RootNode.cs: Root node in ASP.NET document tree; can populate itself from 
//     a file stream.
//
// Authors:
//   Michael Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (C) 2006 Michael Hutchinson
//
//
// This source code is licenced under The MIT License:
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
using System.IO;
using AspNetAddIn.Parser;
using AspNetAddIn.Parser.Internal;

namespace AspNetAddIn.Parser.Tree
{
	public class RootNode : ParentNode
	{
		string fileName;
		
		public RootNode (string fileName, StreamReader textStream)
			: base (null)
		{
			Parse (fileName, textStream);
			this.fileName = fileName;
		}
		
		public RootNode ()
			: base (null)
		{
		}
		
		public override void AcceptVisit (Visitor visitor)
		{
			visitor.Visit (this);
			foreach (Node n in children)
				n.AcceptVisit (visitor);
			visitor.Leave (this);
		}
		
		public override string ToString ()
		{
			return string.Format ("[RootNode Filename='{0}']", fileName);
		}

		
		#region Parsing code
		
		Node currentNode;
		string currentTagId;
		
		public void Parse (string fileName, StreamReader textStream)
		{
			AspParser parser = new AspParser (fileName, textStream);
			
			parser.Error += new ParseErrorHandler (ParseError);
			parser.TagParsed += new TagParsedHandler (TagParsed);
			parser.TextParsed += new TextParsedHandler (TextParsed);
			
			currentNode = this;
			
			parser.Parse ();
		}
		
		void TagParsed (ILocation location, TagType tagtype, string tagId, TagAttributes attributes)
		{
			switch (tagtype)
			{
			case TagType.Close:
				if ( !(currentTagId != tagId))
					throw new ParseException (location, "Closing tag does not match opening tag " + tagId + ".");
				TagNode tn = currentNode as TagNode;
				if (tn != null)
					tn.EndTagLocation = location;
				currentNode = currentNode.Parent;
				break;
				
			case TagType.CodeRender:
			case TagType.CodeRenderExpression:
			case TagType.DataBinding:
				AddtoCurrent (location, new ExpressionNode (location, tagId));
				break;
				
			case TagType.Directive:
				AddtoCurrent (location, new DirectiveNode (location, tagId, attributes));
				break;
				
			case TagType.Include:
				throw new NotImplementedException ("Server-side includes have not yet been implemented: " + location.PlainText);
				
			case TagType.ServerComment:
				//FIXME: the parser doesn't actually return these
				throw new NotImplementedException ("Server comments have not yet been implemented: " + location.PlainText);
				
			case TagType.SelfClosing:
				AddtoCurrent (location, new TagNode (location, tagId, attributes));
				break;
				
			case TagType.Tag:
				Node child = new TagNode (location, tagId, attributes);
				AddtoCurrent (location, child);
				currentNode = child;
				break;
				
			case TagType.Text:
				//FIXME: the parser doesn't actually return these
				throw new NotImplementedException("Text tagtypes have not yet been implemented: " + location.PlainText);
			}
		}
		
		void AddtoCurrent (ILocation location, Node n)
		{
			ParentNode pn = currentNode as ParentNode;
			if (pn == null)
				throw new ParseException (location, "Nodes of type " + n.GetType () + " must be inside other tags");
			pn.AddChild (n);
		}
		
		void TextParsed (ILocation location, string text)
		{
			currentNode.AddText (location, text);
		}
		
		void ParseError (ILocation location, string message)
		{
			throw new ParseException (location, message);
		}
		
		#endregion
	}
}
