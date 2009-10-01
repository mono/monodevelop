// 
// UpdateDialog.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
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
using System.Collections.Generic;
using System.Text;

using Gtk;
using MonoDevelop.Core;

namespace MonoDevelop.Platform
{


	partial class UpdateDialog : Gtk.Dialog
	{

		public UpdateDialog (List<MacUpdater.Update> updates)
		{
			this.Build ();
			checkAutomaticallyCheck.Active = MacUpdater.CheckAutomatically;
			checkAutomaticallyCheck.Toggled += delegate {
				MacUpdater.CheckAutomatically = checkAutomaticallyCheck.Active;
			};
			
			if (updates == null || updates.Count == 0) {
				((VBox)infoLabel.Parent).Remove (infoLabel);
				productBox.PackStart (new Alignment (0.5f, 0.5f, 0f, 0f) {
					Child = new Label (GettextCatalog.GetString ("No updates available"))
				}, true, true, 0);
				productBox.ShowAll ();
				return;
			}
			
			foreach (var update in updates) {
				var updateBox = new VBox () { Spacing = 2 };
				var labelBox = new HBox ();
				updateBox.PackStart (labelBox, false, false, 0);
				
				var updateExpander = new Expander ("");
				updateExpander.LabelWidget = new Label () {
					Markup = string.Format ("<b>{0}</b> {1}", update.Name, update.Version),
				};
				labelBox.PackStart (updateExpander, true, true, 0);
				
				var downloadButton = new Button () {
					Label = GettextCatalog.GetString ("Download")
				};
				downloadButton.Clicked += delegate {
					MonoDevelop.Core.Gui.DesktopService.ShowUrl (update.Url);
				};
				labelBox.PackStart (downloadButton, false, false, 0);
				
				var sb = new StringBuilder ();
				foreach (var release in update.Releases) {
					sb.AppendFormat ("{0} ({1:yyyy-MM-dd})\n", release.Version, release.Date);
					sb.AppendLine ();
					sb.Append (release.Notes);
					sb.AppendLine ();
				}
				var buffer = new TextBuffer (null);
				buffer.Text = sb.ToString ();
				var textView = new TextView (buffer);
				updateBox.PackStart (textView, false, false, 0);
				
				updateExpander.Activated += delegate {
					textView.Visible = updateExpander.Expanded;
				};
				
				productBox.PackStart (updateBox);
				updateBox.ShowAll ();
				
				textView.Visible = false;
			}
		}
	}
}
