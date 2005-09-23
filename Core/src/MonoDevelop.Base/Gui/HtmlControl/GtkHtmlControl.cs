//
// GtkHtmlControl - An Html widget that uses libgtkhtml3
//
// Author: John Luke  <jluke@cfl.rr.com>
//
// Copyright 2003 John Luke
//

using System;
using Gtk;
using GtkSharp;

namespace MonoDevelop.Gui.HtmlControl
{
	public class GtkHtmlControl : EmbedWidget, IWebBrowser
	{
		private static GLib.GType type;
		
		static GtkHtmlControl ()
		{
			type = RegisterGType (typeof (GtkHtmlControl));
		}
		
		public GtkHtmlControl () : base (type)
		{
		}
		
		public void GoHome ()
		{
		}
		
		public void GoSearch ()
		{
		}
		
		public void Navigate (string Url, ref object Flags, ref object targetFrame, ref object postData, ref object headers)
		{
		}
		
		public void Refresh ()
		{
		}
		
		public void Refresh2 ()
		{
		}
		
		public void Stop ()
		{
		}
		
		public void GetApplication ()
		{
		}
		
		public void GetParent ()
		{
		}
		
		public void GetContainer ()
		{
		}
		
		public IHTMLDocument2 GetDocument ()
		{
			return null;
		}
	}
}
