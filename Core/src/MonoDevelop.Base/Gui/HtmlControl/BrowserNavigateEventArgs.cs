using System;
using System.ComponentModel;

namespace MonoDevelop.Gui.HtmlControl
{
	public delegate void BrowserNavigateEventHandler(object s, BrowserNavigateEventArgs e);
	
	public class BrowserNavigateEventArgs : CancelEventArgs
	{
		string url;
		
		public string Url {
			get {
				return url;
			}
		}
		
		public BrowserNavigateEventArgs(string url, bool cancel) : base(cancel)
		{
			this.url = url;
		}
		
	}
}

