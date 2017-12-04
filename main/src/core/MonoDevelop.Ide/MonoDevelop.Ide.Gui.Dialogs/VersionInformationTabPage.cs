// VersionInformationTabPage.cs
//
// Author:
//   Viktoria Dudka (viktoriad@remobjects.com)
//   Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2009 RemObjects Software
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
using Gtk;
using MonoDevelop.Core;
using System.Reflection;
using System.Text;
using System.IO;
using MonoDevelop.Ide.Fonts;
using Mono.Addins;
using System.Collections.Generic;
using System.Linq;


namespace MonoDevelop.Ide.Gui.Dialogs
{
	internal class VersionInformationTabPage: VBox
	{
		bool destroyed;
		
		public VersionInformationTabPage ()
		{
			BorderWidth = 6;
			SetLabel (GettextCatalog.GetString ("Loading..."));
			
			new System.Threading.Thread (() => {
				try {
					var info = SystemInformation.GetDescription ().ToArray ();
					Gtk.Application.Invoke ((o, args) => {
						if (destroyed)
							return;
						SetText (info);
					});
				} catch (Exception ex) {
					LoggingService.LogError ("Failed to load version information", ex);
					Gtk.Application.Invoke ((o, args) => {
						if (destroyed)
							return;
						SetLabel (GettextCatalog.GetString ("Failed to load version information."));
					});
				}
			}).Start ();
		}
		
		void Clear ()
		{
			foreach (var c in this.Children) {
				this.Remove (c);
			}
		}
		
		void SetLabel (string text)
		{
			Clear ();
			var label = new Gtk.Label (text);
			PackStart (label, true, true, 0);
			ShowAll ();
		}

		void SetText (IEnumerable<ISystemInformationProvider> text)
		{
			Clear ();

			var buf = new Gtk.Label ();
			buf.Selectable = true;
			buf.Xalign = 0;

			StringBuilder sb = new StringBuilder ();

			foreach (var info in text) {
				sb.Append ("<b>").Append (GLib.Markup.EscapeText (info.Title)).Append ("</b>\n");
				sb.Append (GLib.Markup.EscapeText (info.Description.Trim ())).Append ("\n\n");
			}

			buf.Markup = sb.ToString ().Trim () + "\n";

			var contentBox = new VBox ();
			contentBox.BorderWidth = 4;
			contentBox.PackStart (buf, false, false, 0);

			var asmButton = new Gtk.Button ("Show loaded assemblies");
			asmButton.Clicked += (sender, e) => {
				asmButton.Hide ();
				contentBox.PackStart (CreateAssembliesTable (), false, false, 0);
			};
			var hb = new Gtk.HBox ();
			hb.PackStart (asmButton, false, false, 0);
			contentBox.PackStart (hb, false, false, 0);

			var sw = new MonoDevelop.Components.CompactScrolledWindow () {
				ShowBorderLine = true,
				BorderWidth = 2
			};
			sw.AddWithViewport (contentBox);
			sw.ShadowType = ShadowType.None;
			((Gtk.Viewport)sw.Child).ShadowType = ShadowType.None;

			PackStart (sw, true, true, 0);
			ShowAll ();
		}

		Gtk.Widget CreateAssembliesTable ()
		{
			var box = new Gtk.VBox ();
			box.PackStart (new Gtk.Label () {
				Markup = "<b>LoadedAssemblies</b>",
				Xalign = 0
			});
			var table = new Gtk.Table (0, 0, false);
			table.ColumnSpacing = 3;
			uint line = 0;
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies ().Where (a => !a.IsDynamic).OrderBy (a => a.FullName)) {
				try {
					var assemblyName = assembly.GetName ();
					table.Attach (new Gtk.Label (assemblyName.Name) { Xalign = 0 }, 0, 1, line, line + 1);
					table.Attach (new Gtk.Label (assemblyName.Version.ToString ()) { Xalign = 0 }, 1, 2, line, line + 1);
					table.Attach (new Gtk.Label (System.IO.Path.GetFullPath (assembly.Location)) { Xalign = 0 }, 2, 3, line, line + 1);
				} catch {
				}
				line++;
			}
			box.PackStart (table, false, false, 0);
			box.ShowAll ();
			return box;
		}

		protected override void OnDestroyed ()
		{
			destroyed = true;
			base.OnDestroyed ();
		}
	}
}
