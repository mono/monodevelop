//
// IEControl - An Html widget that uses IE
//
// Author: John Luke  <jluke@cfl.rr.com>
//
// Copyright 2003 John Luke
//

using System;

namespace MonoDevelop.Gui.HtmlControl
{
	public class IEControl : AxHost, IWebBrowser
	{
		public IEControl () : base("8856f961-340a-11d0-a96b-00c04fd705a2")
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
