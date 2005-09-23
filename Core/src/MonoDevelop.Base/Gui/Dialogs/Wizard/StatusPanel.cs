// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike KrÃ¼ger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections;
using System.ComponentModel;
using System.Xml;

using MonoDevelop.Services;
using MonoDevelop.Core.Services;
using MonoDevelop.Core.AddIns;
using MonoDevelop.Core.Properties;
using MonoDevelop.Core.AddIns.Codons;

using Gtk;
using Gdk;

namespace MonoDevelop.Gui.Dialogs
{
	public class StatusPanel : Gtk.DrawingArea
	{
		WizardDialog wizard;
		Pixbuf bitmap = null;
		Gdk.GC gc;
		Pango.Layout ly;
		
		Pango.FontDescription smallFont;
//		Pango.FontDescription normalFont;
		Pango.FontDescription boldFont;
		
		public StatusPanel(WizardDialog wizard)
		{
			smallFont  = Style.FontDescription;
			smallFont.Size = (int) (smallFont.Size * 0.75);
//			normalFont = Style.FontDescription;
			boldFont   = Style.FontDescription;
			boldFont.Weight = Pango.Weight.Bold;
			
			this.wizard = wizard;
			SetSizeRequest (198, 400);

			bitmap = Runtime.Gui.Resources.GetBitmap ("GeneralWizardBackground");

			AddEvents ((int) (Gdk.EventMask.ExposureMask));
			ExposeEvent += new Gtk.ExposeEventHandler (OnPaint);
			Realized += new EventHandler (OnRealized);
		}

		protected void OnRealized (object o, EventArgs args)
		{
			gc = new Gdk.GC (GdkWindow);
			ly = new Pango.Layout(PangoContext);
		}
		
		protected void OnPaint(object o, Gtk.ExposeEventArgs e)
		{
			GdkWindow.BeginPaintRect (e.Event.Area);
				GdkWindow.DrawPixbuf (gc, bitmap, 0, 0, 0, 0, -1, -1, Gdk.RgbDither.None, 0, 0);
				smallFont.Weight = Pango.Weight.Normal;
				ly.FontDescription = smallFont;
				ly.SetText (GettextCatalog.GetString ("Steps"));
				int lyWidth, lyHeight;
				ly.GetSize (out lyWidth, out lyHeight);
				int smallFontHeight = (int)(lyHeight/1024.0f);
				GdkWindow.DrawLayout (gc, 10, 24 - smallFontHeight, ly);
				GdkWindow.DrawLine(gc, 10, 24, WidthRequest - 10, 24);
				
				int curNumber = 0;
				for (int i = 0; i < wizard.WizardPanels.Count; i = wizard.GetSuccessorNumber(i)) {
					IDialogPanelDescriptor descriptor = ((IDialogPanelDescriptor)wizard.WizardPanels[i]);
					ly.FontDescription = smallFont;
					if (wizard.ActivePanelNumber == i) {
						Pango.FontDescription tmpFont = smallFont.Copy ();
						tmpFont.Weight = Pango.Weight.Bold;
						ly.FontDescription = tmpFont;
						
					}
					ly.SetText ((1 + curNumber) + ". " + descriptor.Label);
					GdkWindow.DrawLayout(gc, 10, 40 + curNumber * smallFontHeight, ly);
					++curNumber;
				}
			GdkWindow.EndPaint ();
		}
		
	}
}
