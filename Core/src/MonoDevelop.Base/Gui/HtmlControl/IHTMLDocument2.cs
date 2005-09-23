using System;
using System.Runtime.InteropServices;

namespace MonoDevelop.Gui.HtmlControl
{
	[
	InterfaceType(ComInterfaceType.InterfaceIsDual),
	ComVisible(true),
	Guid(@"332C4425-26CB-11D0-B483-00C04FD90119")
	]
	public interface IHTMLDocument2
	{
		[return: MarshalAs(UnmanagedType.Interface)]
		object GetScript();
		
		[return: MarshalAs(UnmanagedType.Interface)]
		object GetAll();
		
		[return: MarshalAs(UnmanagedType.Interface)]
		IHTMLElement GetBody();
		
		[return: MarshalAs(UnmanagedType.Interface)]
		object GetActiveElement();
		
		[return: MarshalAs(UnmanagedType.Interface)]
		object GetImages();
		
		[return: MarshalAs(UnmanagedType.Interface)]
		object GetApplets();
		
		[return: MarshalAs(UnmanagedType.Interface)]
		object GetLinks();
		
		[return: MarshalAs(UnmanagedType.Interface)]
		object GetForms();
		
		[return: MarshalAs(UnmanagedType.Interface)]
		object GetAnchors();
		
		void SetTitle(string p);
		
		[return: MarshalAs(UnmanagedType.BStr)]
		string GetTitle();
		
		[return: MarshalAs(UnmanagedType.Interface)]
		object GetScripts();
		
		void SetDesignMode(string p);
		
		[return: MarshalAs(UnmanagedType.BStr)]
		string GetDesignMode();
		
		[return: MarshalAs(UnmanagedType.Interface)]
		object GetSelection();
		
		[return: MarshalAs(UnmanagedType.BStr)]
		string GetReadyState();
		
		[return: MarshalAs(UnmanagedType.Interface)]
		object GetFrames();
		
		[return: MarshalAs(UnmanagedType.Interface)]
		object GetEmbeds();
		
		[return: MarshalAs(UnmanagedType.Interface)]
		object GetPlugins();
		
		void SetAlinkColor(object p);
		
		[return: MarshalAs(UnmanagedType.Struct)]
		object GetAlinkColor();
		
		void SetBgColor(object p);
		
		[return: MarshalAs(UnmanagedType.Struct)]
		object GetBgColor();
		
		void SetFgColor(object p);
		
		[return: MarshalAs(UnmanagedType.Struct)]
		object GetFgColor();
		
		void SetLinkColor(object p);
		
		[return: MarshalAs(UnmanagedType.Struct)]
		object GetLinkColor();
		
		void SetVlinkColor(object p);
		
		[return: MarshalAs(UnmanagedType.Struct)]
		object GetVlinkColor();
		
		[return: MarshalAs(UnmanagedType.BStr)]
		string GetReferrer();
		
		[return: MarshalAs(UnmanagedType.Interface)]
		object GetLocation();
		
		[return: MarshalAs(UnmanagedType.BStr)]
		string GetLastModified();
		
		void SetURL(string p);
		
		[return: MarshalAs(UnmanagedType.BStr)]
		string GetURL();
		
		void SetDomain(string p);
		
		[return: MarshalAs(UnmanagedType.BStr)]
		string GetDomain();
		
		void SetCookie(string p);
		
		[return: MarshalAs(UnmanagedType.BStr)]
		string GetCookie();
		
		void SetExpando(bool p);
		
		[return: MarshalAs(UnmanagedType.Bool)]
		bool GetExpando();
		
		void SetCharset(string p);
		
		[return: MarshalAs(UnmanagedType.BStr)]
		string GetCharset();
		
		void SetDefaultCharset(string p);
		
		[return: MarshalAs(UnmanagedType.BStr)]
		string GetDefaultCharset();
		
		[return: MarshalAs(UnmanagedType.BStr)]
		string GetMimeType();
		
		[return: MarshalAs(UnmanagedType.BStr)]
		string GetFileSize();
		
		[return: MarshalAs(UnmanagedType.BStr)]
		string GetFileCreatedDate();
		
		[return: MarshalAs(UnmanagedType.BStr)]
		string GetFileModifiedDate();
		
		[return: MarshalAs(UnmanagedType.BStr)]
		string GetFileUpdatedDate();
		
		[return: MarshalAs(UnmanagedType.BStr)]
		string GetSecurity();
		
		[return: MarshalAs(UnmanagedType.BStr)]
		string GetProtocol();
		
		[return: MarshalAs(UnmanagedType.BStr)]
		string GetNameProp();
		
		void DummyWrite(int psarray);
		
		void DummyWriteln(int psarray);
		
		[return: MarshalAs(UnmanagedType.Interface)]
		object Open(string URL, object name, object features, object replace);
		
		void Close();
		
		void Clear();
		
		[return: MarshalAs(UnmanagedType.Bool)]
		bool QueryCommandSupported(string cmdID);
		
		[return: MarshalAs(UnmanagedType.Bool)]
		bool QueryCommandEnabled(string cmdID);
		
		[return: MarshalAs(UnmanagedType.Bool)]
		bool QueryCommandState(string cmdID);
		
		[return: MarshalAs(UnmanagedType.Bool)]
		bool QueryCommandIndeterm(string cmdID);
		
		[return: MarshalAs(UnmanagedType.BStr)]
		string QueryCommandText(string cmdID);
		
		[return: MarshalAs(UnmanagedType.Struct)]
		object QueryCommandValue(string cmdID);
		
		[return: MarshalAs(UnmanagedType.Bool)]
		bool ExecCommand(string cmdID, bool showUI, object value);
		
		[return: MarshalAs(UnmanagedType.Bool)]
		bool ExecCommandShowHelp(string cmdID);
		
		[return: MarshalAs(UnmanagedType.Interface)]
		object CreateElement(string eTag);
		
		void SetOnhelp(object p);
		
		[return: MarshalAs(UnmanagedType.Struct)]
		object GetOnhelp();
		
		void SetOnclick(object p);
		
		[return: MarshalAs(UnmanagedType.Struct)]
		object GetOnclick();
		
		void SetOndblclick(object p);
		
		[return: MarshalAs(UnmanagedType.Struct)]
		object GetOndblclick();
		
		void SetOnkeyup(object p);
		
		[return: MarshalAs(UnmanagedType.Struct)]
		object GetOnkeyup();
		
		void SetOnkeydown(object p);
		
		[return: MarshalAs(UnmanagedType.Struct)]
		object GetOnkeydown();
		
		void SetOnkeypress(object p);
		
		[return: MarshalAs(UnmanagedType.Struct)]
		object GetOnkeypress();
		
		void SetOnmouseup(object p);
		
		[return: MarshalAs(UnmanagedType.Struct)]
		object GetOnmouseup();
		
		void SetOnmousedown(object p);
		
		[return: MarshalAs(UnmanagedType.Struct)]
		object GetOnmousedown();
		
		void SetOnmousemove(object p);
		
		[return: MarshalAs(UnmanagedType.Struct)]
		object GetOnmousemove();
		
		void SetOnmouseout(object p);
		
		[return: MarshalAs(UnmanagedType.Struct)]
		object GetOnmouseout();
		
		void SetOnmouseover(object p);
		
		[return: MarshalAs(UnmanagedType.Struct)]
		object GetOnmouseover();
		
		void SetOnreadystatechange(object p);
		
		[return: MarshalAs(UnmanagedType.Struct)]
		object GetOnreadystatechange();
		
		void SetOnafterupdate(object p);
		
		[return: MarshalAs(UnmanagedType.Struct)]
		object GetOnafterupdate();
		
		void SetOnrowexit(object p);
		
		[return: MarshalAs(UnmanagedType.Struct)]
		object GetOnrowexit();
		
		void SetOnrowenter(object p);
		
		[return: MarshalAs(UnmanagedType.Struct)]
		object GetOnrowenter();
		
		void SetOndragstart(object p);
		
		[return: MarshalAs(UnmanagedType.Struct)]
		object GetOndragstart();
		
		void SetOnselectstart(object p);
		
		[return: MarshalAs(UnmanagedType.Struct)]
		object GetOnselectstart();
		
		[return: MarshalAs(UnmanagedType.Interface)]
		object ElementFromPoint(int x, int y);
		
		[return: MarshalAs(UnmanagedType.Interface)]
		object GetParentWindow();
		
		[return: MarshalAs(UnmanagedType.Interface)]
		object GetStyleSheets();
		
		void SetOnbeforeupdate(object p);
		
		[return: MarshalAs(UnmanagedType.Struct)]
		object GetOnbeforeupdate();
		
		void SetOnerrorupdate(object p);
		
		[return: MarshalAs(UnmanagedType.Struct)]
		object GetOnerrorupdate();
		
		[return: MarshalAs(UnmanagedType.BStr)]
		string toString();
		
		[return: MarshalAs(UnmanagedType.Interface)]
		object CreateStyleSheet(string bstrHref, int lIndex);
	}
}
