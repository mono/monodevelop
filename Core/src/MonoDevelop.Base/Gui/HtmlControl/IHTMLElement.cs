using System;
using System.Runtime.InteropServices;

namespace MonoDevelop.Gui.HtmlControl
{
	[
	ComVisible(true),
	Guid(@"3050F1FF-98B5-11CF-BB82-00AA00BDCE0B"),
	InterfaceType(ComInterfaceType.InterfaceIsDual)
	]
	public interface IHTMLElement
	{
		void SetAttribute(string strAttributeName, object AttributeValue, int lFlags);
		
		void GetAttribute(string strAttributeName, int lFlags, object[] pvars);
		
		[return: MarshalAs(UnmanagedType.Bool)]
		bool RemoveAttribute(string strAttributeName, int lFlags);
		
		void SetClassName(string p);
		
		[return: MarshalAs(UnmanagedType.BStr)]
		string GetClassName();
		
		void SetId(string p);
		
		[return: MarshalAs(UnmanagedType.BStr)]
		string GetId();
		
		[return: MarshalAs(UnmanagedType.BStr)]
		string GetTagName();
		
		[return: MarshalAs(UnmanagedType.Interface)]
		IHTMLElement GetParentElement();
		
		[return: MarshalAs(UnmanagedType.Interface)]
		object GetStyle();
		
		void SetOnhelp(object p);
		
		[return: MarshalAs(UnmanagedType.Struct)]
		object GetOnhelp();
		
		void SetOnclick(object p);
		
		[return: MarshalAs(UnmanagedType.Struct)]
		object GetOnclick();
		
		void SetOndblclick(object p);
		
		[return: MarshalAs(UnmanagedType.Struct)]
		object GetOndblclick();
		
		void SetOnkeydown(object p);
		
		[return: MarshalAs(UnmanagedType.Struct)]
		object GetOnkeydown();
		
		void SetOnkeyup(object p);
		
		[return: MarshalAs(UnmanagedType.Struct)]
		object GetOnkeyup();
		
		void SetOnkeypress(object p);
		
		[return: MarshalAs(UnmanagedType.Struct)]
		object GetOnkeypress();
		
		void SetOnmouseout(object p);
		
		[return: MarshalAs(UnmanagedType.Struct)]
		object GetOnmouseout();
		
		void SetOnmouseover(object p);
		
		[return: MarshalAs(UnmanagedType.Struct)]
		object GetOnmouseover();
		
		void SetOnmousemove(object p);
		
		[return: MarshalAs(UnmanagedType.Struct)]
		object GetOnmousemove();
		
		void SetOnmousedown(object p);
		
		[return: MarshalAs(UnmanagedType.Struct)]
		object GetOnmousedown();
		
		void SetOnmouseup(object p);
		
		[return: MarshalAs(UnmanagedType.Struct)]
		object GetOnmouseup();
		
		[return: MarshalAs(UnmanagedType.Interface)]
		object GetDocument();
		
		void SetTitle(string p);
		
		[return: MarshalAs(UnmanagedType.BStr)]
		string GetTitle();
		
		void SetLanguage(string p);
		
		[return: MarshalAs(UnmanagedType.BStr)]
		string GetLanguage();
		
		void SetOnselectstart(object p);
		
		[return: MarshalAs(UnmanagedType.Struct)]
		object GetOnselectstart();
		
		void ScrollIntoView(object varargStart);
		
		[return: MarshalAs(UnmanagedType.Bool)]
		bool Contains(IHTMLElement pChild);
		
		[return: MarshalAs(UnmanagedType.I4)]
		int GetSourceIndex();
		
		[return: MarshalAs(UnmanagedType.Struct)]
		object GetRecordNumber();
		
		void SetLang(string p);
		
		[return: MarshalAs(UnmanagedType.BStr)]
		string GetLang();
		
		[return: MarshalAs(UnmanagedType.I4)]
		int GetOffsetLeft();
		
		[return: MarshalAs(UnmanagedType.I4)]
		int GetOffsetTop();
		
		[return: MarshalAs(UnmanagedType.I4)]
		int GetOffsetWidth();
		
		[return: MarshalAs(UnmanagedType.I4)]
		int GetOffsetHeight();
		
		[return: MarshalAs(UnmanagedType.Interface)]
		IHTMLElement GetOffsetParent();
		
		void SetInnerHTML(string p);
		
		[return: MarshalAs(UnmanagedType.BStr)]
		string GetInnerHTML();
		
		void SetInnerText(string p);
		
		[return: MarshalAs(UnmanagedType.BStr)]
		string GetInnerText();
		
		void SetOuterHTML(string p);
		
		[return: MarshalAs(UnmanagedType.BStr)]
		string GetOuterHTML();
		
		void SetOuterText(string p);
		
		[return: MarshalAs(UnmanagedType.BStr)]
		string GetOuterText();
		
		void InsertAdjacentHTML(string where, string html);
		
		void InsertAdjacentText(string where, string text);
		
		[return: MarshalAs(UnmanagedType.Interface)]
		IHTMLElement GetParentTextEdit();
		
		[return: MarshalAs(UnmanagedType.Bool)]
		bool GetIsTextEdit();
		
		void Click();
		
		[return: MarshalAs(UnmanagedType.Interface)]
		object GetFilters();
		
		void SetOndragstart(object p);
		
		[return: MarshalAs(UnmanagedType.Struct)]
		object GetOndragstart();
		
		[return: MarshalAs(UnmanagedType.BStr)]
		string toString();
		
		void SetOnbeforeupdate(object p);
		
		[return: MarshalAs(UnmanagedType.Struct)]
		object GetOnbeforeupdate();
		
		void SetOnafterupdate(object p);
		
		[return: MarshalAs(UnmanagedType.Struct)]
		object GetOnafterupdate();
		
		void SetOnerrorupdate(object p);
		
		[return: MarshalAs(UnmanagedType.Struct)]
		object GetOnerrorupdate();
		
		void SetOnrowexit(object p);
		
		[return: MarshalAs(UnmanagedType.Struct)]
		object GetOnrowexit();
		
		void SetOnrowenter(object p);
		
		[return: MarshalAs(UnmanagedType.Struct)]
		object GetOnrowenter();
		
		void SetOndatasetchanged(object p);
		
		[return: MarshalAs(UnmanagedType.Struct)]
		object GetOndatasetchanged();
		
		void SetOndataavailable(object p);
		
		[return: MarshalAs(UnmanagedType.Struct)]
		object GetOndataavailable();
		
		void SetOndatasetcomplete(object p);
		
		[return: MarshalAs(UnmanagedType.Struct)]
		object GetOndatasetcomplete();
		
		void SetOnfilterchange(object p);
		
		[return: MarshalAs(UnmanagedType.Struct)]
		object GetOnfilterchange();
		
		[return: MarshalAs(UnmanagedType.Interface)]
		object GetChildren();
		
		[return: MarshalAs(UnmanagedType.Interface)]
		object GetAll();
	}
}
