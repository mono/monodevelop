// 
// WelcomePageRecentProjectsList.cs
//  
// Author:
//       Michael Hutchinson <mhutch@xamarin.com>
// 
// Copyright (c) 2011 Xamarin Inc.
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

using System;
using System.Linq;
using Gtk;
using MonoDevelop.Core;
using System.Collections.Generic;
using MonoDevelop.Ide.Desktop;
using System.Xml.Linq;

namespace MonoDevelop.Ide.WelcomePage
{
	public class WelcomePageRecentProjectsList : WelcomePageSection
	{
		bool destroyed;
		readonly VBox box;
		int itemCount = 10;
		readonly Xwt.Drawing.Image openProjectIcon;
		readonly Xwt.Drawing.Image newProjectIcon;

		public WelcomePageRecentProjectsList (string title = null, int count = 10): base (title)
		{
			openProjectIcon = Xwt.Drawing.Image.FromResource ("welcome-open-solution-16.png");
			newProjectIcon = Xwt.Drawing.Image.FromResource ("welcome-new-solution-16.png");
			box = new VBox ();

			itemCount = count;
			
			DesktopService.RecentFiles.Changed += RecentFilesChanged;
			RecentFilesChanged (null, null);

			SetContent (box);
			TitleAlignment.BottomPadding = Styles.WelcomeScreen.Pad.Solutions.LargeTitleMarginBottom;
			ContentAlignment.LeftPadding = 0;
			ContentAlignment.RightPadding = 0;
		}
		
		protected override void OnDestroyed ()
		{
			destroyed = true;
			base.OnDestroyed ();
			DesktopService.RecentFiles.Changed -= RecentFilesChanged;
		}

		void RecentFilesChanged (object sender, EventArgs e)
		{
			//this can get called by async dispatch after the widget is destroyed
			if (destroyed)
				return;
			
			foreach (var c in box.Children) {
				box.Remove (c);
				c.Destroy ();
			}

			Gtk.HBox hbox = new HBox ();
			var btn = new WelcomePageListButton (GettextCatalog.GetString ("New..."), null, newProjectIcon, "monodevelop://MonoDevelop.Ide.Commands.FileCommands.NewProject");
			btn.WidthRequest = (int) (Styles.WelcomeScreen.Pad.Solutions.SolutionTile.Width / 2.3);
			btn.BorderPadding = 6;
			btn.LeftTextPadding = 24;
			hbox.PackStart (btn, false, false, 0);

			btn = new WelcomePageListButton (GettextCatalog.GetString ("Open..."), null, openProjectIcon, "monodevelop://MonoDevelop.Ide.Commands.FileCommands.OpenFile");
			btn.WidthRequest = (int) (Styles.WelcomeScreen.Pad.Solutions.SolutionTile.Width / 2.3);
			btn.BorderPadding = 6;
			btn.LeftTextPadding = 24;
			hbox.PackStart (btn, false, false, 0);

			box.PackStart (hbox, false, false, 0);
			
			//TODO: pinned files
			foreach (var recent in DesktopService.RecentFiles.GetProjects ().Take (itemCount)) {
				var filename = recent.FileName;

				var accessed = recent.TimeStamp;
				var pixbuf = ImageService.GetIcon (GetIcon (filename), IconSize.Dnd);
				var button = new WelcomePageListButton (recent.DisplayName, System.IO.Path.GetDirectoryName (filename), pixbuf, "project://" + filename);
				button.BorderPadding = 2;
				button.AllowPinning = true;
				button.Pinned = recent.IsFavorite;
				//FIXME: update times as needed. currently QueryTooltip causes crashes on Windows
				//button.QueryTooltip += delegate (object o, QueryTooltipArgs args) {
				//	args.Tooltip.Text = filename + "\n" + TimeSinceEdited (accessed);
				//	args.RetVal = true;
				//};
				//button.HasTooltip = true;
				button.TooltipText = filename + "\n" + TimeSinceEdited (accessed);
				box.PackStart (button, false, false, 0);
				var pinClickHandler = new PinClickHandler (filename);
				pinClickHandler.Register (button);
			}

			this.ShowAll ();
		}

		class PinClickHandler : IDisposable
		{
			WelcomePageListButton button;
			string filename;

			public PinClickHandler (string filename)
			{
				this.filename = filename;
			}

			public void Dispose ()
			{
				if (button == null)
					return;
				button.PinClicked -= Button_PinClicked;
				button.Destroyed -= Button_Destroyed;
				button = null;
			}

			internal void Register (WelcomePageListButton button)
			{
				this.button = button;
				button.PinClicked += Button_PinClicked;
				button.Destroyed += Button_Destroyed;
			}

			void Button_PinClicked (object sender, EventArgs e)
			{
				DesktopService.RecentFiles.SetFavoriteFile (filename, button.Pinned);
			}

			void Button_Destroyed (object sender, EventArgs e)
			{
				Dispose ();
			}
		}


		static string TimeSinceEdited (DateTime prjtime)
		{
			TimeSpan sincelast = DateTime.UtcNow - prjtime;

			if (sincelast.Days >= 1)
				return GettextCatalog.GetPluralString ("Last opened {0} day ago", "Last opened {0} days ago", sincelast.Days, sincelast.Days);
			if (sincelast.Hours >= 1)
				return GettextCatalog.GetPluralString ("Last opened {0} hour ago", "Last opened {0} hours ago", sincelast.Hours, sincelast.Hours);
			if (sincelast.Minutes > 0)
				return GettextCatalog.GetPluralString ("Last opened {0} minute ago", "Last opened {0} minutes ago", sincelast.Minutes, sincelast.Minutes);
			
			return GettextCatalog.GetString ("Last opened less than a minute ago");
		}
		
		static string GetIcon (string fileName)
		{
			//string icon;
			//getting the icon requires probing the file, so handle IO errors
			try {
/* delay project service creation. 
				icon = IdeApp.Services.ProjectService.FileFormats.GetFileFormats
						(fileName, typeof(Solution)).Length > 0
							? "md-solution"
							: "md-workspace"; */
				
				return System.IO.Path.GetExtension (fileName) != ".mdw"
							? "md-solution"
							: "md-workspace";
			} catch (System.IO.IOException ex) {
				LoggingService.LogWarning ("Error building recent solutions list", ex);
				return null;
			}
		}
	}
}