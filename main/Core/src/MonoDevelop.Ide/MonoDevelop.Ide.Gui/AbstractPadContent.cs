//  AbstractPadContent.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using System.Drawing;

using MonoDevelop.Core;

namespace MonoDevelop.Ide.Gui
{
	public abstract class AbstractPadContent : IPadContent
	{
		string id;
		IPadWindow window;
		string title;
		string icon;
		
		protected AbstractPadContent () : this (null)
		{
		}
		
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
		
		public virtual void RedrawContent()
		{
		}
		
		public virtual void Dispose()
		{
		}
	}
}
