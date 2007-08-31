// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Drawing;
using System.Xml;

using MonoDevelop.Core;

namespace MonoDevelop.Ide.Gui
{
	/// <summary>
	/// This class contains the state of the <code>MdiWorkspace</code>, it is used to 
	/// make the <code>MdiWorkspace</code> state persistent.
	/// </summary>
	public class WorkbenchMemento 
	{
		Properties properties = new Properties ();

		public Properties ToProperties ()
		{
			return properties;
		}
		
		public Gdk.WindowState WindowState {
			get {
				return properties.Get ("windowState", (Gdk.WindowState)0);
			}
			set {
				 properties.Set ("windowState", value);
			}
		}
		
		public Rectangle Bounds {
			get {
				return properties.Get ("bounds", new Rectangle(0, 0, 640, 480));
			}
			set {
				properties.Set ("bounds", value);
			}
		}
		
		public bool FullScreen {
			get {
				return properties.Get ("fullscreen", false);
			}
			set {
				properties.Set ("fullscreen", value);
			}
		}
		
		public Properties LayoutMemento {
			get {
				return properties.Get ("layoutMemento", new Properties ());
			}
			set {
				properties.Set ("layoutMemento", value);
			}
		}
		
		/// <summary>
		/// Creates a new instance of the <code>MdiWorkspaceMemento</code>.
		/// </summary>
		public WorkbenchMemento (Properties properties)
		{
			this.properties = properties;
		}
	}
}
