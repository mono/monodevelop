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
using System.Linq;
using System.Collections.Generic;
using System.Text;

using Gtk;
using MonoDevelop.Core;

namespace MonoDevelop.Platform.Updater
{


	partial class UpdateDialog : Gtk.Dialog
	{
		const int PAGE_MESSAGE = 0;
		const int PAGE_UPDATES = 1;
		
		UpdateResult stableResult, unstableResult;

		public UpdateDialog ()
		{
			this.Build ();
			notebook1.ShowTabs = false;
			
			checkAutomaticallyCheck.Active = UpdateService.CheckAutomatically;
			checkAutomaticallyCheck.Toggled += delegate {
				UpdateService.CheckAutomatically = checkAutomaticallyCheck.Active;
			};
			
			checkIncludeUnstable.Active = UpdateService.CheckAutomatically;
			checkIncludeUnstable.Toggled += delegate {
				bool includeUnstable = checkIncludeUnstable.Active;
				UpdateService.IncludeUnstable = includeUnstable;
				
				SetMessage (GettextCatalog.GetString ("Checking for updates..."));
				
				var cachedResult = includeUnstable? unstableResult : stableResult;
				if (cachedResult != null && !cachedResult.HasError) {
					LoadUpdates (cachedResult.Updates);
				} else {
					UpdateService.QueryUpdateServer (UpdateService.DefaultUpdateInfos, includeUnstable, LoadResult);
				}
			};
			
			SetMessage (GettextCatalog.GetString ("Checking for updates..."));
		}
		
		public void LoadResult (UpdateResult result)
		{
			if (result.IncludesUnstable)
				unstableResult = result;
			else
				stableResult = result;
			
			if (result.HasError) {
				SetMessage (result.ErrorMessage);
			} else {
				LoadUpdates (result.Updates);
			}
		}
		
		void SetMessage (string message)
		{
			notebook1.CurrentPage = PAGE_MESSAGE;
			messageLabel.Text = message;
		}
		
		void LoadUpdates (List<Update> updates)
		{
			if (updates == null || updates.Count == 0) {
				SetMessage (GettextCatalog.GetString ("No updates available"));
				return;
			}
			
			foreach (var c in productBox.Children) {
				productBox.Remove (c);
				c.Destroy ();
			}
			
			productBox.Spacing = 0;
			
			bool includeUnstable = checkIncludeUnstable.Active;
			
			foreach (var update in updates) {
				if (!includeUnstable && update.IsUnstable)
					continue;
				var updateBox = new VBox () { Spacing = 2 };
				var labelBox = new HBox ();
				updateBox.PackStart (labelBox, false, false, 0);
				
				var updateExpander = new Expander ("");
				updateExpander.LabelWidget = new Label () {
					Markup = string.Format ("<b>{0}</b>\n{1} ({2:yyyy-MM-dd}){3}", update.Name, update.Version, update.Date,
					                        update.IsUnstable? "\n<b>UNSTABLE PREVIEW RELEASE</b>" : ""),
				};
				labelBox.PackStart (updateExpander, true, true, 0);
				
				var downloadButton = new Button () {
					Label = GettextCatalog.GetString ("Download")
				};
				
				//NOTE: grab the variable from the loop var so the closure captures it 
				var url = update.Url;
				downloadButton.Clicked += delegate {
					MonoDevelop.Core.Gui.DesktopService.ShowUrl (url);
				};
				labelBox.PackStart (downloadButton, false, false, 0);
				
				var sb = new StringBuilder ();
				for (int i = 0; i < update.Releases.Count; i++) {
					var release = update.Releases[i];
					if (i > 0) {
						if (i == 1) {
							sb.AppendLine ();
							sb.AppendLine ("This release also includes previous updates:");
						}
						sb.AppendLine ();
						sb.AppendFormat ("{0} ({1:yyyy-MM-dd})\n", release.Version, release.Date);
						sb.AppendLine ();
					}
					sb.Append (release.Notes.Trim ('\t', ' ', '\n', '\r'));
					sb.AppendLine ();
				}
				var buffer = new TextBuffer (null);
				buffer.Text = sb.ToString ();
				var textView = new TextView (buffer);
				textView.WrapMode = WrapMode.Word;
				textView.Editable = false;
				textView.LeftMargin = textView.RightMargin = 4;
				updateBox.PackStart (textView, false, false, 0);
				
				bool startsExpanded = false;
				updateExpander.Expanded = startsExpanded;
				updateExpander.Activated += delegate {
					textView.Visible = updateExpander.Expanded;
				};
				
				updateBox.BorderWidth = 4;
				
				productBox.PackStart (updateBox, false, false, 0);
				updateBox.ShowAll ();
				//this has to be set false after the ShowAll
				textView.Visible = startsExpanded;
				
				
				var sep = new HSeparator ();
				productBox.PackStart (sep, false, false, 0);
				sep.Show ();
			}
			
			notebook1.CurrentPage = PAGE_UPDATES;
		}
	}
}
