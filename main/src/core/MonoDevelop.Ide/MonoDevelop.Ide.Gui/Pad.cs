//
// Pad.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using System.Drawing;
using MonoDevelop.Ide.Codons;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Gui
{
	public class Pad
	{
		IPadWindow window;
		PadCodon content;
		DefaultWorkbench workbench;
		string[] categories;
		
		internal Pad (DefaultWorkbench workbench, PadCodon content)
		{
			this.window    = workbench.GetPadWindow (content);
			this.window.PadHidden += delegate {
				IsOpenedAutomatically = false;
			};
			this.content   = content;
			this.workbench = workbench;
		}

		internal PadCodon InternalContent {
			get { return content; }
		}
		
		public object Content {
			get { return window.Content; }
		}
		
		public string Title {
			get { return window.Title; }
		}
		
		public IconId Icon {
			get { return window.Icon; }
		}
		
		public string Id {
			get { return window.Id; }
		}
		
		public bool IsOpenedAutomatically {
			get;
			set;
		}
		
		public string[] Categories {
			get {
				if (categories == null) {
					CategoryNode cat = content.Parent as CategoryNode;
					if (cat == null)
						categories = new string[] { GettextCatalog.GetString ("Pads") };
					else {
						List<string> list = new List<string> ();
						while (cat != null) {
							list.Insert (0, cat.Name);
							cat = cat.Parent as CategoryNode;
						}
						categories = list.ToArray ();
					}
				}
				return categories;
			}
		}
		
		public void BringToFront ()
		{
			BringToFront (false);
		}
		
		public void BringToFront (bool grabFocus)
		{
			workbench.BringToFront (content);
			window.Activate (grabFocus);
		}
		
		public bool AutoHide {
			get { return window.AutoHide; }
			set { window.AutoHide = value; }
		}
		
		public bool Visible {
			get {
				return window.Visible;
			}
			set {
				window.Visible = value;
			}
		}

		public bool Sticky {
			get {
				return window.Sticky;
			}
			set {
				window.Sticky = value;
			}
		}
		
		internal IPadWindow Window {
			get { return window; }
		}
		
		internal IMementoCapable GetMementoCapable ()
		{
			PadWindow pw = (PadWindow) window;
			return pw.GetMementoCapable ();
		}
		
		public void Destroy ()
		{
			Visible = false;
			((DefaultWorkbench)workbench).RemovePad (content);
		}
	}
}
