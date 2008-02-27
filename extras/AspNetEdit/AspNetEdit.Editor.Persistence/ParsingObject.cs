/* 
* ParsingObject.cs - Similar to ControlBuilder. Builds document and controls for a parsed tag
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
using System.Web.UI.Design;
using System.Collections;
using System.ComponentModel.Design;

using MonoDevelop.AspNet.Parser.Internal;
using MonoDevelop.AspNet.Parser.Dom;

namespace AspNetEdit.Editor.Persistence
{
	internal abstract class ParsingObject
	{
		private ParsingObject parent;
		private string tagid;
		private bool closed = false;

		protected ParsingObject (string tagid, ParsingObject parent)
		{
			if (parent == null && ! (this is RootParsingObject) )
				throw new ArgumentNullException ("parent", "All ParsingObjects except RootParsingObjects must have parents");
			this.tagid = tagid;
			this.parent = parent;
			
		}

		protected virtual IWebFormReferenceManager WebFormReferenceManager
		{
			get { return parent.WebFormReferenceManager; }
		}

		protected virtual IDesignerHost DesignerHost
		{
			get { return parent.DesignerHost; }
		}

		/// <summary>
		/// Adds control to collection. Default implementation passes it up the tree towards RootParsingObject.
		/// </summary>
		protected virtual void AddControl (object control)
		{
			CheckOpen();
			parent.AddControl (control);
		}

		public string TagID
		{
			get { return tagid; }
		}

		public virtual bool InServerForm
		{
			get { return (parent == null) ? false : parent.InServerForm; }
		}

		/// <summary>
		/// Adds text into document. Default implementation passes it up the tree.
		/// </summary>
		public virtual void AddText (string text)
		{
			CheckOpen ();
			parent.AddText (text);
		}

		public virtual bool AllowWhitespace ()
		{
			return true;
		}

		protected void CheckOpen ()
		{
			if (closed)
				throw new Exception ("The ParsingObject has been closed and no changes can be made to it");
		}

		/// <returns>The parent ParsingObject</returns>
		public virtual ParsingObject CloseObject (string closingTagText)
		{
			return parent;
		}

		/// <summary>
		/// Creates a ParsingObject as a child of this one, and returns it.
		/// </summary>
		public virtual ParsingObject CreateChildParsingObject (ILocation location, string tagid, TagAttributes attributes)
		{
			string[] str = tagid.Split(':');

			//html tags
			//TODO: check for valid tags?
			if (str.Length == 1)
			{
				if (attributes.IsRunAtServer () && (0 == string.Compare ("form", tagid)))
					return new ServerFormParsingObject (location.PlainText, tagid, this);
				return new HtmlParsingObject (location.PlainText, tagid, this);
			}

			//fall through to server tags
			if (str.Length != 2)
				throw new ParseException (location, "Server tag name is not of form prefix:name");

			Type tagType = WebFormReferenceManager.GetObjectType(str[0], str[1]);
			if (tagType == null)
				throw new ParseException(location, "The tag " + tagid + "has not been registered");

			return new ServerObjectParsingObject (tagType, attributes.GetDictionary(null), tagid, this);
		}
	}
}
