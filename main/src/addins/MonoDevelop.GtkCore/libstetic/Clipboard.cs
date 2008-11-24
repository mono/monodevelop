using Gtk;
using System;
using System.Xml;

namespace Stetic {

	internal static class Clipboard {

		const int SteticType = 0;
		const int TextType = 1;

		static Gtk.TargetEntry[] targets;
		static Gtk.TargetEntry[] Targets {
			get {
				if (targets == null) {
#if GTK_SHARP_2_6
					Gtk.TargetList list = new Gtk.TargetList ();
					list.Add ((string)WidgetUtils.ApplicationXSteticAtom, 0, SteticType);
					list.AddTextTargets (TextType);
					targets = (Gtk.TargetEntry[])list;
#else
					targets = new Gtk.TargetEntry[] {
						new Gtk.TargetEntry ((string)WidgetUtils.ApplicationXSteticAtom, 0, SteticType)
					};
#endif
				}
				return targets;
			}
		}

		static Gtk.Clipboard MainClipboard {
			get {
				return Gtk.Clipboard.Get (Gdk.Selection.Clipboard);
			}
		}

		static XmlElement selection;

		static void ClipboardGet (Gtk.Clipboard clipboard, Gtk.SelectionData seldata, uint info)
		{
			if (selection == null)
				return;

			if (info == TextType)
				seldata.Text = selection.OuterXml;
			else
				seldata.Set (WidgetUtils.ApplicationXSteticAtom, 8, System.Text.Encoding.UTF8.GetBytes (selection.OuterXml));
		}

		static void ClipboardClear (Gtk.Clipboard clipboard)
		{
			selection = null;
		}

		public static void Copy (Gtk.Widget widget)
		{
			MainClipboard.SetWithData (Targets, ClipboardGet, ClipboardClear);
			selection = widget != null ? WidgetUtils.ExportWidget (widget) : null;
		}

		public static void Cut (Gtk.Widget widget)
		{
			Copy (widget);
			Stetic.Wrapper.Widget wrapper = Stetic.Wrapper.Widget.Lookup (widget);
			if (wrapper != null)
				wrapper.Delete ();
		}

		static Placeholder target;

		static void ClipboardPaste (Gtk.Clipboard clipboard, Gtk.SelectionData seldata)
		{
			Stetic.Wrapper.Container parent = Stetic.Wrapper.Container.LookupParent (target);
			if (parent == null)
				return;

			Stetic.Wrapper.Widget wrapper = WidgetUtils.Paste (parent.Project, seldata);
			if (wrapper == null)
				return;

			parent.PasteChild (target, wrapper.Wrapped);
			target = null;
		}

		public static void Paste (Placeholder target)
		{
			Clipboard.target = target;
			MainClipboard.RequestContents (WidgetUtils.ApplicationXSteticAtom, ClipboardPaste);
		}
	}
}
