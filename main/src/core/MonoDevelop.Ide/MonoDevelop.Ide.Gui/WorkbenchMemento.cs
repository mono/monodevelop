// 
// WorkbenchMemento.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
//       Mike Krüeger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.



using System;
using System.Collections.Generic;
using System.Drawing;
using System.Xml;

using MonoDevelop.Core;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Components.DockToolbars;

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
				return properties.Get ("windowState", Gdk.WindowState.Maximized);
			}
			set {
				 properties.Set ("windowState", value);
			}
		}
		
		public Rectangle Bounds {
			get {
				var geo = Gdk.Screen.Default.GetMonitorGeometry (0);
				return properties.Get ("bounds", new Rectangle(geo.X + 50, geo.Y + 50, geo.Width - 100, geo.Height - 150));
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
		
		public DockToolbarFrameStatus ToolbarStatus {
			get {
				return properties.Get ("toolbarStatus", new DockToolbarFrameStatus ());
			}
			set {
				properties.Set ("toolbarStatus", value);
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
	
	class DocumentUserPrefsFilenameComparer : IEqualityComparer<DocumentUserPrefs>
	{
		public bool Equals (DocumentUserPrefs x, DocumentUserPrefs y)
		{
			if (x == null)
				return y == null;
			if (y == null)
				return false;
			return x.FileName == y.FileName;
		}

		public int GetHashCode (DocumentUserPrefs obj)
		{
			return obj == null || obj.FileName == null ? 0 : obj.FileName.GetHashCode ();
		}
	}
	
	[DataItem ("File")]
	class DocumentUserPrefs
	{
		[ItemProperty]
		public string FileName;
		
		[ItemProperty (DefaultValue = 0)]
		public int Line;
		
		[ItemProperty (DefaultValue = 0)]
		public int Column;
	}
	
	[DataItem ("Pad")]
	class PadUserPrefs
	{
		[ItemProperty]
		public string Id;
		
		[ItemProperty]
		public XmlElement State;
	}
	
	[DataItem ("Workbench")]
	class WorkbenchUserPrefs
	{
		[ItemProperty]
		public string ActiveConfiguration;
		
		[ItemProperty]
		public string ActiveDocument;
		
		[ItemProperty]
		public List<DocumentUserPrefs> Files = new List<DocumentUserPrefs> ();
		
		[ItemProperty]
		public List<PadUserPrefs> Pads = new List<PadUserPrefs> ();
	}
}
