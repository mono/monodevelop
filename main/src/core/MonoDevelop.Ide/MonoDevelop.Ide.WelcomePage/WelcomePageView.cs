/*
Copyright (c) 2005 Scott Ellington

Permission is hereby granted, free of charge, to any person 
obtaining a copy of this software and associated documentation 
files (the "Software"), to deal in the Software without 
restriction, including without limitation the rights to use, 
copy, modify, merge, publish, distribute, sublicense, and/or sell 
copies of the Software, and to permit persons to whom the 
Software is furnished to do so, subject to the following 
conditions:

The above copyright notice and this permission notice shall be 
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, 
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES 
OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND 
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT 
HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR 
OTHER DEALINGS IN THE SOFTWARE. 
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Xml;
using Gtk;
using MonoDevelop.Core;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Desktop;
using MonoDevelop.Ide;

namespace MonoDevelop.Ide.WelcomePage
{	
	class WelcomePageView : AbstractViewContent
	{
		WelcomePageWidget widget;
		ScrolledWindow scroller;
		
		public override string StockIconId {
			get { return Gtk.Stock.Home; }
		}
		
		public override void Load (string fileName) 
		{
		}

		public override bool IsFile {
			get { return false; }
		}
		
		public override Widget Control {
			get { return scroller;  }
		}
		
		public WelcomePageView () : base ()
		{
			this.ContentName = GettextCatalog.GetString ("Welcome");
			this.IsViewOnly = true;
			
			scroller = new ScrolledWindow ();
			widget = new WelcomePageWidget ();
			scroller.AddWithViewport (widget);
			scroller.ShadowType = ShadowType.None;
			scroller.FocusChain = new Widget[] { widget };
			scroller.Show ();
		}
	}
}