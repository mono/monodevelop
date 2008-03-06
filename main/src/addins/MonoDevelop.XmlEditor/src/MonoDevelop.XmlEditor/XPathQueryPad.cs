//
// MonoDevelop XML Editor
//
// Copyright (C) 2006 Matthew Ward
//

using Gtk;
using MonoDevelop.Core;
using MonoDevelop.Core.Properties;
using MonoDevelop.Ide.Gui;
using System;

namespace MonoDevelop.XmlEditor
{
	public class XPathQueryPad : IPadContent, IXmlEditorViewContentProvider
	{		
		XPathQueryWidget xpathQueryWidget;
		bool disposed;
		
		public XPathQueryPad()
		{
			xpathQueryWidget = new XPathQueryWidget(this);
			xpathQueryWidget.ShowAll();
			
			IdeApp.Workbench.ActiveDocumentChanged += ActiveDocumentChanged;
			LoadProperties();
		}
		
		/// <summary>
		/// Gets the active XmlEditorViewContent.
		/// </summary>
		public XmlEditorViewContent View {
			get {
				return XmlEditorService.GetActiveView();
			}
		}
		
		public void JumpTo(string fileName, int line, int column)
		{
			IdeApp.Workbench.OpenDocument(fileName, Math.Max(1, line), Math.Max(1, column), true);
		}
		
		void IPadContent.Initialize(IPadWindow window)
		{
			window.Title = "XPath Query";
			window.Icon = "MonoDevelop.XmlEditor.XPathQueryPad";
		}
		
		public string Id {
			get { 
				return "MonoDevelop.XmlEditor.XPathQueryPad";
			}
		}
		
		public string DefaultPlacement {
			get { 
				return "Bottom";
			}
		}
			
		public void RedrawContent()
		{
		}
		
		public Widget Control {
			get {
				return xpathQueryWidget;
			}
		}
		
		public void Dispose()
		{
			if (!disposed) {
				disposed = true;
				IdeApp.Workbench.ActiveDocumentChanged -= ActiveDocumentChanged;
				SaveProperties();
			}
		}
		
		public void RemoveXPathHighlighting()
		{
			//xpathQueryWidget.RemoveXPathNodeTextMarkers();
		}
		
		void ActiveDocumentChanged(object source, EventArgs e)
		{
			xpathQueryWidget.UpdateQueryButtonState();
		}
		
		/// <summary>
		/// Reads the xpath query pad properties and updates
		/// XPath Query widget.
		/// </summary>
		void LoadProperties()
		{
			xpathQueryWidget.Query = XPathQueryPadOptions.LastXPathQuery;
		}
		
		/// <summary>
		/// Updates the xpath query pad properties from the
		/// XPath Query widget.
		void SaveProperties()
		{
			XPathQueryPadOptions.LastXPathQuery = xpathQueryWidget.Query;
		}
	}
}
