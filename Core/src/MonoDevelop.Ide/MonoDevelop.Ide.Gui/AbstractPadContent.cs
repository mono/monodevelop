// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Drawing;

using MonoDevelop.Core;

namespace MonoDevelop.Ide.Gui
{
	public abstract class AbstractPadContent : IPadContent
	{
		string id;
		string defaultPosition = "left";
		IPadWindow window;
		string title;
		string icon;
		
		public AbstractPadContent (string title) : this(title, null)
		{
			id = GetType ().FullName;
		}
		
		public AbstractPadContent (string title, string iconResoureName)
		{
			this.title = title;
			this.icon  = iconResoureName;
			id = GetType ().FullName;
		}
		
		public virtual void Initialize (IPadWindow window)
		{
			this.window = window;
			if (title != null) window.Title = title;
			if (icon != null) window.Icon  = icon;
		}
		
		public IPadWindow Window {
			get { return window; }
		}
		
		public abstract Gtk.Widget Control {
			get;
		}
		
		public string Id {
			get { return id; }
			set { id = value; }
		}
		
		public string DefaultPlacement {
			get { return defaultPosition; }
			set { defaultPosition = value; }
		}
		
		public virtual void RedrawContent()
		{
		}
		
		public virtual void Dispose()
		{
		}
	}
}
