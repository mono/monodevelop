using System;
using System.Runtime.InteropServices;

namespace MonoDevelop.Gui.HtmlControl
{
	[Guid(@"eab22ac2-30c1-11cf-a7eb-0000c05bae0b"), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
	public interface IWebBrowserEvents
	{
		[DispId(100)]
		void RaiseBeforeNavigate(string url, int flags, string targetFrameName, ref object postData, string headers, ref bool cancel);
		
		[DispId(101)]
		void RaiseNavigateComplete(string url);
	}
}
