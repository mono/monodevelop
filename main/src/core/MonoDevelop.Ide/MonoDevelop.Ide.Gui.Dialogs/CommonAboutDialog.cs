//  CommonAboutDialog.cs
//
// Author:
//   Todd Berman  <tberman@sevenl.net>
//   John Luke  <jluke@cfl.rr.com>
//   Lluis Sanchez Gual  <lluis@novell.com>
//   Viktoria Dudka  <viktoriad@remobjects.com>
//   Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2004 Todd Berman
// Copyright (c) 2004 John Luke
// Copyright (C) 2008 Novell, Inc.
// Copyright (c) 2011 Xamarin Inc. (http://xamarin.com)
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
//
//
using System;
using System.Text;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;

using Gdk;
using Gtk;
using GLib;
using Pango;
using System.IO;
using Mono.Addins;
using System.Collections.Generic;

namespace MonoDevelop.Ide.Gui.Dialogs
{
	internal class CommonAboutDialog : Dialog
	{
		public CommonAboutDialog ()
		{
			Title = string.Format (GettextCatalog.GetString ("About {0}"), BrandingService.ApplicationName);
			TransientFor = IdeApp.Workbench.RootWindow;
			AllowGrow = false;
			HasSeparator = false;
			
			var notebook = new Notebook ();
			notebook.BorderWidth = 0;
			notebook.AppendPage (new AboutMonoDevelopTabPage (), new Label (Title));
			notebook.AppendPage (new VersionInformationTabPage (), new Label (GettextCatalog.GetString ("Version Information")));
			notebook.AppendPage (new LoadedAssembliesTabPage (), new Label (GettextCatalog.GetString ("Loaded Assemblies")));
			VBox.PackStart (notebook, true, true, 0);
			
			AddButton (Gtk.Stock.Close, (int)ResponseType.Close);
			
			ShowAll ();
		}

		void ChangeColor (Gtk.Widget w)
		{
			w.ModifyBg (Gtk.StateType.Normal, new Gdk.Color (69, 69, 94));
			w.ModifyBg (Gtk.StateType.Active, new Gdk.Color (69, 69, 94));
			w.ModifyFg (Gtk.StateType.Normal, new Gdk.Color (255, 255, 255));
			w.ModifyFg (Gtk.StateType.Active, new Gdk.Color (255, 255, 255));
			w.ModifyFg (Gtk.StateType.Prelight, new Gdk.Color (255, 255, 255));
			Gtk.Container c = w as Gtk.Container;
			if (c != null) {
				foreach (Widget cw in c.Children)
					ChangeColor (cw);
			}
		}
		
		static CommonAboutDialog instance;
		
		public static void ShowAboutDialog ()
		{
			if (Platform.IsMac) {
				if (instance == null) {
					instance = new CommonAboutDialog ();
					MessageService.PlaceDialog (instance, IdeApp.Workbench.RootWindow);
					instance.Response += delegate {
						instance.Destroy ();
						instance = null;
					};
				}
				instance.Present ();
				return;
			}
			
			MessageService.ShowCustomDialog (new CommonAboutDialog ());
		}

		internal class LoadedAssembliesTabPage: VBox
		{
			public LoadedAssembliesTabPage ()
			{
				var buf = new TextBuffer (null);
				buf.Text = SystemInformation.GetLoadedAssemblies ();

				var sw = new MonoDevelop.Components.CompactScrolledWindow () {
					ShowBorderLine = true,
					BorderWidth = 2,
					Child = new TextView (buf) {
						Editable = false,
						LeftMargin = 4,
						RightMargin = 4,
						PixelsAboveLines = 4,
						PixelsBelowLines = 4
					}
				};
				
				sw.Child.ModifyFont (Pango.FontDescription.FromString (DesktopService.DefaultMonospaceFont));
				PackStart (sw, true, true, 0);
				ShowAll ();
			}
		}
	}
}