/* 
* DesignTimeParser.cs - Parses an ASP.NET page at design-time
* 
* Authors: 
*  Michael Hutchinson <m.j.hutchinson@gmail.com>
*  
* Copyright (C) 2005 Michael Hutchinson
*
* This sourcecode is licenced under The MIT License:
* 
* Permission is hereby granted, free of charge, to any person obtaining
* a copy of this software and associated documentation files (the
* "Software"), to deal in the Software without restriction, including
* without limitation the rights to use, copy, modify, merge, publish,
* distribute, sublicense, and/or sell copies of the Software, and to permit
* persons to whom the Software is furnished to do so, subject to the
* following conditions:
* 
* The above copyright notice and this permission notice shall be included in
* all copies or substantial portions of the Software.
*
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
* OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
* MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN
* NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
* DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
* OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
* USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Text;
using System.ComponentModel.Design;
using System.IO;
using System.Collections;
using System.Web.UI.Design;
using System.Web.UI;
using System.ComponentModel;
using AspNetEdit.Editor.ComponentModel;

using AspNetAddIn.Parser.Internal;
using AspNetAddIn.Parser.Tree;

namespace AspNetEdit.Editor.Persistence
{
	public class DesignTimeParser
	{
		DesignerHost host;
		IWebFormReferenceManager refMan;

		RootParsingObject rootParsingObject = null;
		ParsingObject openObject = null;
		Document document = null;

		public DesignTimeParser (DesignerHost host, Document document)
		{
			this.host = host;
			this.document = document;
			refMan = host.GetService(typeof(IWebFormReferenceManager)) as IWebFormReferenceManager;
			if (refMan == null)
				throw new Exception ("Could not get IWebFormReferenceManager from host");
		}

		/// <summary>
		/// Parses a document fragment. Processes all controls and directives and adds them to host. 
		/// </summary>
		/// <param name="fragment">The document fragment to parse</param>
		/// <returns>The document with all controls, directives and script blocks replaced by placeholders</returns>
		public void ProcessFragment (string fragment, out Control[] controls, out string substText)
		{
			
			AspParser parser = InitialiseParser (fragment);
			
			rootParsingObject = new RootParsingObject(host);
			openObject = rootParsingObject;
			
			parser.Parse ();
			
			if (openObject != rootParsingObject) {
				throw new Exception ("The tag " +  openObject.TagID + " was left unclosed");
			}
			
			rootParsingObject.GetParsedContent (out controls, out substText);
		}

		private AspParser InitialiseParser (string parseText)
		{
			AspParser parser = null;
			using (StringReader reader = new StringReader (parseText)) {
				parser = new AspParser (null, reader);
			}

			parser.Error += new ParseErrorHandler (ParseError);
			parser.TagParsed += new TagParsedHandler (TagParsed);
			parser.TextParsed += new TextParsedHandler (TextParsed);

			return parser;
		}

		void ParseError (ILocation location, string message)
		{
			throw new ParseException (location, message);
		}

		void TagParsed (ILocation location, TagType tagtype, string tagid, TagAttributes attributes)
		{
			switch (tagtype)
			{
				case TagType.Close:
					if (openObject == null)
						throw new ParseException (location, "There are more closing tags than opening tags");

					if (0 != string.Compare (openObject.TagID, tagid))
						throw new ParseException (location, "Closing tag " + tagid + " does not match opening tag " + openObject.TagID);
					openObject = openObject.CloseObject (location.PlainText);
					break;
				case TagType.CodeRender:
					throw new NotImplementedException ("Code render expressions have not yet been implemented: " + location.PlainText);
					//break;
				case TagType.CodeRenderExpression:
					throw new NotImplementedException ("Code render expressions have not yet been implemented: " + location.PlainText);
					//break;
				case TagType.DataBinding:
					throw new NotImplementedException("Data binding expressions have not yet been implemented: " + location.PlainText);
					//break;
				case TagType.Directive:
					ProcessDirective (tagid, attributes);
					break;
				case TagType.Include:
					throw new NotImplementedException ("Server-side includes have not yet been implemented: " + location.PlainText);
					//break;
				case TagType.ServerComment:
					throw new NotImplementedException ("Server comments have not yet been implemented: " + location.PlainText);
					//break;
				case TagType.Tag:
					//TODO: don't do this for XHTML
					if ((string.Compare (tagid, "br", true) == 0)
						|| (string.Compare (tagid, "hr", true) == 0))
						goto case TagType.SelfClosing;
					
					openObject = openObject.CreateChildParsingObject(location, tagid, attributes);
					break;
				case TagType.SelfClosing:
					if (openObject == null)
						throw new Exception ("Root tag cannot be self-closing");
				
					openObject = openObject.CreateChildParsingObject(location, tagid, attributes);
					openObject = openObject.CloseObject(string.Empty);
					break;
				case TagType.Text:
					throw new NotImplementedException("Text tagtypes have not yet been implemented: " + location.PlainText);
					//break;
			}
		}


		/*TODO: if only we could get the controlbuilder to build the control like in .NET 2's builder.BuildObject ()
		ControlBuilder builder = ControlBuilder.CreateBuilderFromType (null, b, tagType, str[1], (string) attributes["ID"], attributes.GetDictionary (null), currentLocation.BeginLine, currentLocation.Filename);

		if (builder == null)
			throw new ParseException (currentLocation, "Could not create builder for type " + tagType);
		builder.SetServiceProvider (host);
		Control c = builder.BuildObject ();
		*/
		
		void ProcessDirective (string tagid, TagAttributes attributes)
		{
			string placeholder = document.AddDirective (tagid, attributes.GetDictionary (null));
			openObject.AddText (placeholder);
		}

		void TextParsed (ILocation location, string text)
		{
			openObject.AddText (text);
		}
	}








}
