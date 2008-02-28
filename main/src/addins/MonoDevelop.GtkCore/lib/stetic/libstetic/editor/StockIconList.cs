
using System;
using System.Reflection;
using System.Collections;
using Gtk;

namespace Stetic.Editor
{
	class StockIconList : IconList 
	{
		public StockIconList ()
		{
			foreach (PropertyInfo info in typeof (Gtk.Stock).GetProperties (BindingFlags.Public | BindingFlags.Static)) {
				if (info.CanRead && info.PropertyType == typeof (string)) {
					string name = (string) info.GetValue (null, null);
					AddIcon (name, WidgetUtils.LoadIcon (name, Gtk.IconSize.Menu), name);
				}
			}
			foreach (PropertyInfo info in typeof (Gnome.Stock).GetProperties (BindingFlags.Public | BindingFlags.Static)) {
				if (info.CanRead && info.PropertyType == typeof (string)) {
					string name = (string) info.GetValue (null, null);
					AddIcon (name, WidgetUtils.LoadIcon (name, Gtk.IconSize.Menu), name);
				}
			}
		}
	}
}
