
using System;
using Gtk;
using MonoDevelop.Ide.Gui;

namespace GladeAddIn.Gui
{
	public class GladePropertiesPad: AbstractPadContent
	{
		Gtk.Widget widget;
		Button addButton;
		
		public GladePropertiesPad (): base ("")
		{
			DefaultPlacement = "GladeAddIn.Gui.GladeWidgetTreePad/right; bottom";
			VBox box = new VBox ();
			CheckButton la = new CheckButton ("Map to code");
		//	box.PackStart (la, false, false, 6);
			box.PackStart (GladeService.App.Editor, true, true, 0);
			widget = box;
			
			Pango.FontDescription fd = widget.Style.FontDesc.Copy ();
			Console.WriteLine ("fd.Size:" + fd.Size);
			fd.Size /= 2;
			Console.WriteLine ("fd.Size:" + fd.Size);
			la.ModifyFont (Pango.FontDescription.FromString ("Arial 4pt"));
			la.ModifyBg (StateType.Normal, new Gdk.Color (255,0,0));
			
			HButtonBox bbox = GladeService.App.Editor.Children [1] as HButtonBox;
			Button rbut = bbox.Children [0] as Button;
			addButton = new Button ("Add to class");
			addButton.BorderWidth = rbut.BorderWidth;
			bbox.PackStart (addButton, false, true, 0);
			addButton.Clicked += new EventHandler (OnAdd);
			
			widget.ShowAll ();
		}
		
		public override Gtk.Widget Control {
			get { return widget; }
		}
		
		void OnAdd (object s, EventArgs a)
		{
			GladeService.AddCurrentWidgetToClass ();
		}
	}
}
