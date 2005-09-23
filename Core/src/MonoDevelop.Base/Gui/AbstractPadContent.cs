// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Drawing;

using MonoDevelop.Core.Services;

namespace MonoDevelop.Gui
{
	public abstract class AbstractPadContent : IPadContent
	{
		string title;
		string icon;
		string id;
		string defaultPosition = "left";
		
		public abstract Gtk.Widget Control {
			get;
		}
		
		public virtual string Title {
			get {
				return title;
			}
		}
		
		public virtual string Icon {
			get {
				return icon;
			}
		}

		public string Id {
			get { return id; }
			set { id = value; }
		}
		
		public string DefaultPlacement {
			get { return defaultPosition; }
			set { defaultPosition = value; }
		}
		
		public AbstractPadContent(string title) : this(title, null)
		{
			id = GetType ().FullName;
		}
		
		public AbstractPadContent(string title, string iconResoureName)
		{
			this.title = title;
			this.icon  = iconResoureName;
			id = GetType ().FullName;
		}
		
		public virtual void RedrawContent()
		{
		}
		
		public virtual void Dispose()
		{
		}
		
		protected virtual void OnTitleChanged(EventArgs e)
		{
			if (TitleChanged != null) {
				TitleChanged(this, e);
			}
		}
		
		protected virtual void OnIconChanged(EventArgs e)
		{
			if (IconChanged != null) {
				IconChanged(this, e);
			}
		}
		
		public event EventHandler TitleChanged;
		public event EventHandler IconChanged;
	}
}
