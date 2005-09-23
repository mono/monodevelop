// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Drawing;
using System.Xml;

using MonoDevelop.Core.Properties;

namespace MonoDevelop.Gui
{
	/// <summary>
	/// This class contains the state of the <code>MdiWorkspace</code>, it is used to 
	/// make the <code>MdiWorkspace</code> state persistent.
	/// </summary>
	public class WorkbenchMemento : IXmlConvertable
	{
		Gdk.WindowState windowstate        = 0;
		//FormWindowState defaultwindowstate = FormWindowState.Normal;
		Rectangle bounds = new Rectangle(0, 0, 640, 480);
		bool fullscreen = false;
		IXmlConvertable layoutMemento;
		
		/*public FormWindowState DefaultWindowState {
			get {
				return defaultwindowstate;
			}
			set {
				defaultwindowstate = value;
			}
		}*/
		
		public Gdk.WindowState WindowState {
			get {
				return windowstate;
			}
			set {
				windowstate = value;
			}
		}
		
		public Rectangle Bounds {
			get {
				return bounds;
			}
			set {
				bounds = value;
			}
		}
		
		public bool FullScreen {
			get {
				return fullscreen;
			}
			set {
				fullscreen = value;
			}
		}
		
		public IXmlConvertable LayoutMemento {
			get {
				return layoutMemento;
			}
			set {
				layoutMemento = value;
			}
		}
		
		/// <summary>
		/// Creates a new instance of the <code>MdiWorkspaceMemento</code>.
		/// </summary>
		public WorkbenchMemento()
		{
			windowstate = 0;
			bounds      = new Rectangle(0, 0, 640, 480);
			fullscreen  = false;
		}
		
		WorkbenchMemento(XmlElement element, IXmlConvertable defaultLayoutMemento) : base ()
		{
			try {
				string[] boundstr = element.Attributes["bounds"].InnerText.Split(new char [] { ',' });
				
				bounds = new Rectangle(Int32.Parse(boundstr[0]), Int32.Parse(boundstr[1]), 
									   Int32.Parse(boundstr[2]), Int32.Parse(boundstr[3]));
			} catch {
			}
			
			try {
				windowstate = (Gdk.WindowState)Enum.Parse(typeof(Gdk.WindowState), element.Attributes["formwindowstate"].InnerText);
			} catch {
			}
			
			/*if (element.Attributes["defaultformwindowstate"] != null) {
				defaultwindowstate = (FormWindowState)Enum.Parse(typeof(FormWindowState), element.Attributes["defaultformwindowstate"].InnerText);
			}*/

			try {
				fullscreen  = Boolean.Parse(element.Attributes["fullscreen"].InnerText);
			} catch {
			}
			
			if (element.FirstChild is XmlElement && defaultLayoutMemento != null) {
				XmlElement e = (XmlElement) element.FirstChild;
				this.layoutMemento = (IXmlConvertable) defaultLayoutMemento.FromXmlElement (e);
			} else {
				this.layoutMemento = defaultLayoutMemento;
			}
		}

		public object FromXmlElement(XmlElement element)
		{
			return new WorkbenchMemento(element, layoutMemento);
		}
		
		public XmlElement ToXmlElement(XmlDocument doc)
		{
			XmlElement element = doc.CreateElement("WindowState");
			XmlAttribute attr;
			
			attr = doc.CreateAttribute("bounds");
			attr.InnerText = bounds.X + "," + bounds.Y + "," + bounds.Width + "," + bounds.Height;
			element.Attributes.Append(attr);
			
			attr = doc.CreateAttribute ("formwindowstate");
			attr.InnerText = windowstate.ToString();
			element.Attributes.Append(attr);
			
			/*attr = doc.CreateAttribute("defaultformwindowstate");
			attr.InnerText = defaultwindowstate.ToString();
			element.Attributes.Append(attr);*/
			
			attr = doc.CreateAttribute("fullscreen");
			attr.InnerText = fullscreen.ToString();
			element.Attributes.Append(attr);
			
			if (LayoutMemento != null) {
				XmlElement elayout = LayoutMemento.ToXmlElement (doc);
				element.AppendChild (elayout);
			}
			
			return element;
		}
	}
}
