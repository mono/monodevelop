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
using System.Collections.Generic;

using MonoDevelop.AspNet.Parser;
using MonoDevelop.AspNet.Parser.Internal;

namespace MonoDevelop.AspNet.Parser.Dom
{
	public class RootNode : ParentNode
	{
		string fileName;
		List<ParseException> errors = new List<ParseException> ();
		
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
		
		internal ICollection<ParseException> ParseErrors {
			get { return errors; }
		}
		
		public override void AcceptVisit (Visitor visitor)
		{
			visitor.Visit (this);
			foreach (Node n in children) {
				if (visitor.QuickExit)
					break;
				n.AcceptVisit (visitor);
			}
			visitor.Leave (this);
		}
		
		public override string ToString ()
		{
			return string.Format ("[RootNode Filename='{0}']", fileName);
		}
		
		public override int ContainsPosition (int line, int col)
		{
			return 0;
		}
		
		#region Parsing code
		
		Node currentNode;
		static string[] implicitSelfClosing = { "hr", "br", "img" };
		static string[] implicitCloseOnBlock = { "p" };
		static string[] blockLevel = { "p", "div", "hr", "img", "blockquote", "html", "body", "form" };
		
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
				TagNode tn = currentNode as TagNode;
				if (tn == null) {
					errors.Add (new ParseException (location, "Closing tag '" + tagId +"' does not match an opening tag."));
				} else {
					if (tn.TagName == tagId) {
						tn.EndLocation = location;
						currentNode = currentNode.Parent;
						tn.Close ();
					} else {
						errors.Add (new ParseException (location, "Closing tag '" + tagId +"' does not match opening tag '" + tn.TagName + "'."));
						currentNode = currentNode.Parent;
						TagParsed (location, TagType.Close, tagId, null);
					}
				}
				break;
				
			case TagType.CodeRender:
			case TagType.CodeRenderExpression:
			case TagType.DataBinding:
				try {
					AddtoCurrent (location, new ExpressionNode (location, tagId));
				} catch (ParseException ex) {
					errors.Add (ex);
				}
				break;
				
			case TagType.Directive:
				try {
					AddtoCurrent (location, new DirectiveNode (location, tagId, attributes));
				} catch (ParseException ex) {
					errors.Add (ex);
				}	
				break;
				
			case TagType.Include:
				throw new NotImplementedException ("Server-side includes have not yet been implemented: " + location.PlainText);
				
			case TagType.ServerComment:
				//FIXME: the parser doesn't actually return these
				throw new NotImplementedException ("Server comments have not yet been implemented: " + location.PlainText);
				
			case TagType.SelfClosing:
				try {
					tn = new TagNode (location, tagId, attributes);
					AddtoCurrent (location, tn);
					tn.Close ();
				} catch (ParseException ex) {
					errors.Add (ex);
				}
				break;
				
			case TagType.Tag:
				try {
					//HACK: implicit close on block level in HTML4
					TagNode prevTag = currentNode as TagNode;
					if (prevTag != null) {
						if (Array.IndexOf (implicitCloseOnBlock, prevTag.TagName.ToLowerInvariant ()) > -1
						    && Array.IndexOf (blockLevel, tagId.ToLowerInvariant ()) > -1) {
							errors.Add (new ParseException (location, "Unclosed " + prevTag.TagName + " tag. Assuming implicitly closed by block level tag."));
							prevTag.Close ();
							currentNode = currentNode.Parent;
						}
					}
					
					//create and add the new tag
					TagNode child = new TagNode (location, tagId, attributes);
					AddtoCurrent (location, child);
					
					//HACK: implicitly closing tags in HTML4
					if (Array.IndexOf (implicitSelfClosing, tagId.ToLowerInvariant ()) > -1) {
						errors.Add (new ParseException (location, "Unclosed " + tagId + " tag. Assuming implicitly closed."));
						child.Close ();
					} else {
						currentNode = child;
					}
					
				} catch (ParseException ex) {
					errors.Add (ex);
				}
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
			errors.Add (new ParseException (location, message));
		}
		
		#endregion
	}
}
