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
	class WelcomePageRecentProjectsList : VBox
	{
		bool destroyed;
		EventHandler recentChangesHandler;
		
		int itemCount = 10;
		
		public WelcomePageRecentProjectsList (XElement el)
		{
			var countAtt = el.Attribute ("count");
			if (countAtt != null)
				itemCount = (int) countAtt;
			
			recentChangesHandler = DispatchService.GuiDispatch (new EventHandler (RecentFilesChanged));
			DesktopService.RecentFiles.Changed += recentChangesHandler;
			RecentFilesChanged (null, null);
		}
		
		public override void Destroy ()
		{
			destroyed = true;
			base.Destroy ();
			DesktopService.RecentFiles.Changed -= recentChangesHandler;
		}

		void RecentFilesChanged (object sender, EventArgs e)
		{
			//this can get called by async dispatch after the widget is destroyed
			if (destroyed)
				return;
			
			foreach (var c in Children) {
				this.Remove (c);
				c.Destroy ();
			}
			
			//TODO: pinned files
			foreach (var recent in DesktopService.RecentFiles.GetProjects ().Take (itemCount)) {
				var filename = recent.FileName;
				var accessed = recent.TimeStamp;
				var button = new WelcomePageLinkButton (recent.DisplayName, "project://" + filename);
				button.Ellipsize = Pango.EllipsizeMode.Middle;
				//FIXME: update times as needed. currently QueryTooltip causes crashes on Windows
				//button.QueryTooltip += delegate (object o, QueryTooltipArgs args) {
				//	args.Tooltip.Text = filename + "\n" + TimeSinceEdited (accessed);
				//	args.RetVal = true;
				//};
				//button.HasTooltip = true;
				button.TooltipText = filename + "\n" + TimeSinceEdited (accessed);
				button.Icon = GetIcon (filename);
				this.PackStart (button, false, false, 0);
			}
			
			this.ShowAll ();
		}
		
		static string TimeSinceEdited (DateTime prjtime)
		{
			TimeSpan sincelast = DateTime.UtcNow - prjtime;

			if (sincelast.Days >= 1)
				return GettextCatalog.GetPluralString ("Last opened {0} days ago", "Last opened {0} days ago", sincelast.Days, sincelast.Days);
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
				if (!System.IO.File.Exists (fileName))
					return null;
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