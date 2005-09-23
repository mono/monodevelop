/*
 * Copyright (C) 2004 Todd Berman <tberman@off.net>
 * Copyright (C) 2004 Jeroen Zwartepoorte <jeroen@xs4all.nl>
 * Copyright (C) 2005 John Luke <john.luke@gmail.com>
 *
 * based on work by:
 * Copyright (C) 2002 Gustavo Giráldez <gustavo.giraldez@gmx.net>
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330,
 * Boston, MA 02111-1307, USA.
 */

using System;
using Gtk;

namespace Gdl
{
	public class Stock
	{
		static Gtk.IconFactory stock = new Gtk.IconFactory ();

		public static string Close {
			 get { return "gdl-close"; }
		}
		public static string MenuLeft {
			 get { return "gdl-menu-left"; }
		}
		public static string MenuRight {
			 get { return "gdl-menu-right"; }
		}
		
		static Stock ()
		{
			AddIcon ("gdl-close", "stock-close-12.png");
			AddIcon ("gdl-menu-left", "stock-menu-left-12.png");
			AddIcon ("gdl-menu-right", "stock-menu-right-12.png");
			
			stock.AddDefault ();
		}
		
		static void AddIcon (string stockid, string resource)
		{
			Gtk.IconSet iconset = stock.Lookup (stockid);
			
			if (iconset == null) {
				iconset = new Gtk.IconSet ();
				Gdk.Pixbuf img = Gdk.Pixbuf.LoadFromResource (resource);
				IconSource source = new IconSource ();
				source.Size = Gtk.IconSize.Menu;
				source.SizeWildcarded = false;
				source.Pixbuf = img;
				iconset.AddSource (source);
				stock.Add (stockid, iconset);
			}
		}
	}
}

