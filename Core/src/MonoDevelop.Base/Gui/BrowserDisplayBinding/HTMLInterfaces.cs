// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

namespace MonoDevelop.BrowserDisplayBinding
{
	[System.Runtime.InteropServices.GuidAttribute("626FC520-A41E-11CF-A731-00A0C9082637")]
	[System.Runtime.InteropServices.ComVisibleAttribute(true)]
	[System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsDual)]
	public interface IHTMLDocument
	{
		object GetScript();
	}
	
	[System.Runtime.InteropServices.ComVisibleAttribute(true)]
	[System.Runtime.InteropServices.GuidAttribute("3050F2E3-98B5-11CF-BB82-00AA00BDCE0B")]
	[System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsDual)]
	public interface IHTMLStyleSheet
	{
		object GetRules();
		string GetCssText();
		void SetCssText(string p);
		string GetMedia();
		void SetMedia(string p);
		void RemoveRule(int lIndex);
		void RemoveImport(int lIndex);
		int AddRule(string bstrSelector, string bstrStyle, int lIndex);
		int AddImport(string bstrURL, int lIndex);
		string GetId();
		string GetStyleSheetType();
		string GetHref();
		void SetHref(string p);
		object GetImports();
		bool GetReadOnly();
		bool GetDisabled();
		void SetDisabled(bool p);
		IHTMLElement GetOwningElement();
		IHTMLStyleSheet GetParentStyleSheet();
		string GetTitle();
		void SetTitle(string p);
	}
	
	[System.Runtime.InteropServices.ComVisibleAttribute(true)]
	[System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsDual)]
	[System.Runtime.InteropServices.GuidAttribute("3050F25E-98B5-11CF-BB82-00AA00BDCE0B")]
	public interface IHTMLStyle
	{
		bool RemoveAttribute(string strAttributeName, int lFlags);
		object GetAttribute(string strAttributeName, int lFlags);
		void SetAttribute(string strAttributeName, object AttributeValue, int lFlags);
		string GetFilter();
		void SetFilter(string p);
		string GetClip();
		void SetClip(string p);
		string GetCursor();
		void SetCursor(string p);
		float GetPosHeight();
		void SetPosHeight(float p);
		float GetPosWidth();
		void SetPosWidth(float p);
		float GetPosLeft();
		void SetPosLeft(float p);
		float GetPosTop();
		void SetPosTop(float p);
		int GetPixelHeight();
		void SetPixelHeight(int p);
		int GetPixelWidth();
		void SetPixelWidth(int p);
		int GetPixelLeft();
		void SetPixelLeft(int p);
		int GetPixelTop();
		void SetPixelTop(int p);
		string GetCssText();
		void SetCssText(string p);
		string GetPageBreakAfter();
		void SetPageBreakAfter(string p);
		string GetPageBreakBefore();
		void SetPageBreakBefore(string p);
		string GetOverflow();
		void SetOverflow(string p);
		object GetZIndex();
		void SetZIndex(object p);
		string GetPosition();
		object GetLeft();
		void SetLeft(object p);
		object GetTop();
		void SetTop(object p);
		string GetWhiteSpace();
		void SetWhiteSpace(string p);
		string GetListStyle();
		void SetListStyle(string p);
		string GetListStyleImage();
		void SetListStyleImage(string p);
		string GetListStylePosition();
		void SetListStylePosition(string p);
		string GetListStyleType();
		void SetListStyleType(string p);
		string GetVisibility();
		void SetVisibility(string p);
		string GetDisplay();
		void SetDisplay(string p);
		string GetClear();
		void SetClear(string p);
		string GetStyleFloat();
		void SetStyleFloat(string p);
		object GetHeight();
		void SetHeight(object p);
		object GetWidth();
		void SetWidth(object p);
		string GetBorderLeftStyle();
		void SetBorderLeftStyle(string p);
		string GetBorderBottomStyle();
		void SetBorderBottomStyle(string p);
		string GetBorderRightStyle();
		void SetBorderRightStyle(string p);
		string GetBorderTopStyle();
		void SetBorderTopStyle(string p);
		string GetBorderStyle();
		void SetBorderStyle(string p);
		object GetBorderLeftWidth();
		void SetBorderLeftWidth(object p);
		object GetBorderBottomWidth();
		void SetBorderBottomWidth(object p);
		object GetBorderRightWidth();
		void SetBorderRightWidth(object p);
		object GetBorderTopWidth();
		void SetBorderTopWidth(object p);
		string GetBorderWidth();
		void SetBorderWidth(string p);
		object GetBorderLeftColor();
		void SetBorderLeftColor(object p);
		object GetBorderBottomColor();
		void SetBorderBottomColor(object p);
		object GetBorderRightColor();
		void SetBorderRightColor(object p);
		object GetBorderTopColor();
		void SetBorderTopColor(object p);
		string GetBorderColor();
		void SetBorderColor(string p);
		string GetBorderLeft();
		void SetBorderLeft(string p);
		string GetBorderBottom();
		void SetBorderBottom(string p);
		string GetBorderRight();
		void SetBorderRight(string p);
		string GetBorderTop();
		void SetBorderTop(string p);
		string GetBorder();
		void SetBorder(string p);
		string GetPadding();
		void SetPadding(string p);
		object GetPaddingLeft();
		void SetPaddingLeft(object p);
		object GetPaddingBottom();
		void SetPaddingBottom(object p);
		object GetPaddingRight();
		void SetPaddingRight(object p);
		object GetPaddingTop();
		void SetPaddingTop(object p);
		string GetMargin();
		void SetMargin(string p);
		object GetMarginLeft();
		void SetMarginLeft(object p);
		object GetMarginBottom();
		void SetMarginBottom(object p);
		object GetMarginRight();
		void SetMarginRight(object p);
		object GetMarginTop();
		void SetMarginTop(object p);
		object GetLineHeight();
		void SetLineHeight(object p);
		object GetTextIndent();
		void SetTextIndent(object p);
		string GetTextAlign();
		void SetTextAlign(string p);
		string GetTextTransform();
		void SetTextTransform(string p);
		object GetVerticalAlign();
		void SetVerticalAlign(object p);
		bool GetTextDecorationBlink();
		void SetTextDecorationBlink(bool p);
		bool GetTextDecorationLineThrough();
		void SetTextDecorationLineThrough(bool p);
		bool GetTextDecorationOverline();
		void SetTextDecorationOverline(bool p);
		bool GetTextDecorationUnderline();
		void SetTextDecorationUnderline(bool p);
		bool GetTextDecorationNone();
		void SetTextDecorationNone(bool p);
		string GetTextDecoration();
		void SetTextDecoration(string p);
		object GetLetterSpacing();
		void SetLetterSpacing(object p);
		object GetWordSpacing();
		void SetWordSpacing(object p);
		object GetBackgroundPositionY();
		void SetBackgroundPositionY(object p);
		object GetBackgroundPositionX();
		void SetBackgroundPositionX(object p);
		string GetBackgroundPosition();
		void SetBackgroundPosition(string p);
		string GetBackgroundAttachment();
		void SetBackgroundAttachment(string p);
		string GetBackgroundRepeat();
		void SetBackgroundRepeat(string p);
		string GetBackgroundImage();
		void SetBackgroundImage(string p);
		object GetBackgroundColor();
		void SetBackgroundColor(object p);
		string GetBackground();
		void SetBackground(string p);
		object GetColor();
		void SetColor(object p);
		string GetFont();
		void SetFont(string p);
		object GetFontSize();
		void SetFontSize(object p);
		string GetFontWeight();
		void SetFontWeight(string p);
		string GetFontObject();
		void SetFontObject(string p);
		string GetFontStyle();
		void SetFontStyle(string p);
		string GetFontFamily();
		void SetFontFamily(string p);
	}
	
	[System.Runtime.InteropServices.ComVisibleAttribute(true)]
	[System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsDual)]
	[System.Runtime.InteropServices.GuidAttribute("332C4425-26CB-11D0-B483-00C04FD90119")]
	public interface IHTMLDocument2
	{
		IHTMLStyleSheet CreateStyleSheet(string bstrHref, int lIndex);
		string toString();
		object GetOnerrorupdate();
		void SetOnerrorupdate(object p);
		object GetOnbeforeupdate();
		void SetOnbeforeupdate(object p);
		object GetStyleSheets();
		object GetParentWindow();
		IHTMLElement ElementFromPoint(int x, int y);
		object GetOnselectstart();
		void SetOnselectstart(object p);
		object GetOndragstart();
		void SetOndragstart(object p);
		object GetOnrowenter();
		void SetOnrowenter(object p);
		object GetOnrowexit();
		void SetOnrowexit(object p);
		object GetOnafterupdate();
		void SetOnafterupdate(object p);
		object GetOnreadystatechange();
		void SetOnreadystatechange(object p);
		object GetOnmouseover();
		void SetOnmouseover(object p);
		object GetOnmouseout();
		void SetOnmouseout(object p);
		object GetOnmousemove();
		void SetOnmousemove(object p);
		object GetOnmousedown();
		void SetOnmousedown(object p);
		object GetOnmouseup();
		void SetOnmouseup(object p);
		object GetOnkeypress();
		void SetOnkeypress(object p);
		object GetOnkeydown();
		void SetOnkeydown(object p);
		object GetOnkeyup();
		void SetOnkeyup(object p);
		object GetOndblclick();
		void SetOndblclick(object p);
		object GetOnclick();
		void SetOnclick(object p);
		object GetOnhelp();
		void SetOnhelp(object p);
		IHTMLElement CreateElement(string eTag);
		bool ExecCommandShowHelp(string cmdID);
		bool ExecCommand(string cmdID, bool showUI, object value);
		object QueryCommandValue(string cmdID);
		string QueryCommandText(string cmdID);
		bool QueryCommandIndeterm(string cmdID);
		bool QueryCommandState(string cmdID);
		bool QueryCommandEnabled(string cmdID);
		bool QueryCommandSupported(string cmdID);
		void Clear();
		void Close();
		object Open(string URL, object name, object features, object replace);
		void DummyWriteln(int psarray);
		void DummyWrite(int psarray);
		string GetNameProp();
		string GetProtocol();
		string GetSecurity();
		string GetFileUpdatedDate();
		string GetFileModifiedDate();
		string GetFileCreatedDate();
		string GetFileSize();
		string GetMimeType();
		string GetDefaultCharset();
		void SetDefaultCharset(string p);
		string GetCharset();
		void SetCharset(string p);
		bool GetExpando();
		void SetExpando(bool p);
		string GetCookie();
		void SetCookie(string p);
		string GetDomain();
		void SetDomain(string p);
		string GetURL();
		void SetURL(string p);
		string GetLastModified();
		object GetLocation();
		string GetReferrer();
		object GetVlinkColor();
		void SetVlinkColor(object p);
		object GetLinkColor();
		void SetLinkColor(object p);
		object GetFgColor();
		void SetFgColor(object p);
		object GetBgColor();
		void SetBgColor(object p);
		object GetAlinkColor();
		void SetAlinkColor(object p);
		IHTMLElementCollection GetPlugins();
		IHTMLElementCollection GetEmbeds();
		object GetFrames();
		string GetReadyState();
		object GetSelection();
		string GetDesignMode();
		void SetDesignMode(string p);
		IHTMLElementCollection GetScripts();
		string GetTitle();
		void SetTitle(string p);
		IHTMLElementCollection GetAnchors();
		IHTMLElementCollection GetForms();
		IHTMLElementCollection GetLinks();
		IHTMLElementCollection GetApplets();
		IHTMLElementCollection GetImages();
		IHTMLElement GetActiveElement();
		IHTMLElement GetBody();
		IHTMLElementCollection GetAll();
		object GetScript();
	}
	
	
	[System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsDual)]
	[System.Runtime.InteropServices.ComVisibleAttribute(true)]
	[System.Runtime.InteropServices.GuidAttribute("3050F1FF-98B5-11CF-BB82-00AA00BDCE0B")]
	public interface IHTMLElement
	{
		object GetAll();
		object GetChildren();
		object GetOnfilterchange();
		void SetOnfilterchange(object p);
		object GetOndatasetcomplete();
		void SetOndatasetcomplete(object p);
		object GetOndataavailable();
		void SetOndataavailable(object p);
		object GetOndatasetchanged();
		void SetOndatasetchanged(object p);
		object GetOnrowenter();
		void SetOnrowenter(object p);
		object GetOnrowexit();
		void SetOnrowexit(object p);
		object GetOnerrorupdate();
		void SetOnerrorupdate(object p);
		object GetOnafterupdate();
		void SetOnafterupdate(object p);
		object GetOnbeforeupdate();
		void SetOnbeforeupdate(object p);
		string toString();
		object GetOndragstart();
		void SetOndragstart(object p);
		object GetFilters();
		void Click();
		bool GetIsTextEdit();
		IHTMLElement GetParentTextEdit();
		void InsertAdjacentText(string where, string text);
		void InsertAdjacentHTML(string where, string html);
		string GetOuterText();
		void SetOuterText(string p);
		string GetOuterHTML();
		void SetOuterHTML(string p);
		string GetInnerText();
		void SetInnerText(string p);
		string GetInnerHTML();
		void SetInnerHTML(string p);
		IHTMLElement GetOffsetParent();
		int GetOffsetHeight();
		int GetOffsetWidth();
		int GetOffsetTop();
		int GetOffsetLeft();
		string GetLang();
		void SetLang(string p);
		object GetRecordNumber();
		int GetSourceIndex();
		bool Contains(IHTMLElement pChild);
		void ScrollIntoView(object varargStart);
		object GetOnselectstart();
		void SetOnselectstart(object p);
		string GetLanguage();
		void SetLanguage(string p);
		string GetTitle();
		void SetTitle(string p);
		object GetDocument();
		object GetOnmouseup();
		void SetOnmouseup(object p);
		object GetOnmousedown();
		void SetOnmousedown(object p);
		object GetOnmousemove();
		void SetOnmousemove(object p);
		object GetOnmouseover();
		void SetOnmouseover(object p);
		object GetOnmouseout();
		void SetOnmouseout(object p);
		object GetOnkeypress();
		void SetOnkeypress(object p);
		object GetOnkeyup();
		void SetOnkeyup(object p);
		object GetOnkeydown();
		void SetOnkeydown(object p);
		object GetOndblclick();
		void SetOndblclick(object p);
		object GetOnclick();
		void SetOnclick(object p);
		object GetOnhelp();
		void SetOnhelp(object p);
		IHTMLStyle GetStyle();
		IHTMLElement GetParentElement();
		string GetTagName();
		string GetId();
		void SetId(string p);
		string GetClassName();
		void SetClassName(string p);
		bool RemoveAttribute(string strAttributeName, int lFlags);
		void GetAttribute(string strAttributeName, int lFlags, object[] pvars);
		void SetAttribute(string strAttributeName, object AttributeValue, int lFlags);
	}
	
	[System.Runtime.InteropServices.ComVisibleAttribute(true)]
	[System.Runtime.InteropServices.InterfaceTypeAttribute(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsDual)]
	[System.Runtime.InteropServices.GuidAttribute("3050F21F-98B5-11CF-BB82-00AA00BDCE0B")]
	public interface IHTMLElementCollection
	{
		object Tags(object tagName);
		object Item(object name, object index);
		object Get_newEnum();
		int GetLength();
		void SetLength(int p);
		string toString();
	}
}
