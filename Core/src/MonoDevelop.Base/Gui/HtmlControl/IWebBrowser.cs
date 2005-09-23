using System;
using System.Runtime.InteropServices;

namespace MonoDevelop.Gui.HtmlControl
{
	[Guid(@"eab22ac1-30c1-11cf-a7eb-0000c05bae0b")]
	public interface IWebBrowser
	{
		void GoBack();
		
		void GoForward();
		
		void GoHome();
		
		void GoSearch();
		
		void Navigate(string Url, ref object Flags, ref object targetFrame, ref object postData, ref object headers);
		
		void Refresh();
		
		void Refresh2();
		
		void Stop();
		
		void GetApplication();
		
		void GetParent();
		
		void GetContainer();
		
		[return: MarshalAs(UnmanagedType.Interface)]
		IHTMLDocument2 GetDocument();
	}
}
