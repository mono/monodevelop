/* 
* RootParsingObject.cs - a root-level ParsingObject
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
using System.Web.UI;
using System.Collections;
using System.ComponentModel.Design;

namespace AspNetEdit.Editor.Persistence
{
	internal class RootParsingObject : HtmlParsingObject
	{
		private StringBuilder stringBuilder = new StringBuilder ();
		private ArrayList controls = new ArrayList ();
		private IWebFormReferenceManager refMan;
		private IDesignerHost host;

		public RootParsingObject (IDesignerHost host)
			: base ("", "", null)
		{
			this.host = host;
			refMan = host.GetService(typeof(IWebFormReferenceManager)) as IWebFormReferenceManager;
			if (refMan == null)
				throw new Exception ("Could not get IWebFormReferenceManager from host");
		}

		public override void AddText (string text)
		{
			stringBuilder.Append (text);
		}

		protected override void AddControl (object control)
		{
			controls.Add (control);
		}

		public void GetParsedContent (out Control[] controls, out string documentText)
		{
			controls = (Control[]) this.controls.ToArray (typeof(System.Web.UI.Control));
			documentText = stringBuilder.ToString ();
		}
		
		protected override IWebFormReferenceManager WebFormReferenceManager
		{
			get { return refMan; }
		}

		protected override IDesignerHost DesignerHost
		{
			get { return host; }
		}
	}
}
