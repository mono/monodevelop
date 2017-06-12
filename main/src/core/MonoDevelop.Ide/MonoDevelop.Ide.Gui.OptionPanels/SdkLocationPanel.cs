// 
// SdkLocationPanel.cs
//  
// Author:
//       Michael Hutchinson <mhutch@xamarin.com>
// 
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

using System;
using Gtk;
using MonoDevelop.Components;
using MonoDevelop.Components.AtkCocoaHelper;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Dialogs;

namespace MonoDevelop.Ide.Gui.OptionPanels
{
	/// <summary>
	/// Panel that allows the user to specify the location of an SDK.
	/// </summary>
	public abstract class SdkLocationPanel : OptionsPanel
	{
		SdkLocationWidget w;
		
		public override Control CreatePanelWidget ()
		{
			return w = new SdkLocationWidget (this);
		}
		
		public override void ApplyChanges ()
		{
			w.ApplyChanges ();
		}
		
		/// <summary>
		/// The panel's header label.
		/// </summary>
		public abstract string Label { get; }

		/// <summary>
		/// The default SDK locations that will be searched if the value is blank.
		/// </summary>
		public virtual FilePath[] DefaultSdkLocations {
			get {
				return new FilePath[0];
			}
		}

		/// <summary>
		/// Checks whether or not a restart is needed when the SDK location is changed.
		/// </summary>
		/// <value><c>true</c> if a change requires a restart; otherwise, <c>false</c>.</value>
		public virtual bool RequiresRestart {
			get {
				return false;
			}
		}
		
		/// <summary>
		/// Check whether the SDK exists at a location.
		/// </summary>
		public abstract bool ValidateSdkLocation (FilePath location);
		
		/// <summary>
		/// Loads the SDK location setting. A null value means that the default should be used.
		/// </summary>
		public abstract FilePath LoadSdkLocationSetting ();
		
		/// <summary>
		/// Saves the SDK location setting. A null value means that the default should be used.
		/// </summary>
		public abstract void SaveSdkLocationSetting (FilePath location);
	}
	
	class SdkLocationWidget : VBox
	{
		FolderEntry locationEntry = new FolderEntry ();
		Label messageLabel = new Label ();
		ImageView messageIcon = new ImageView ();
		SdkLocationPanel panel;
		
		public SdkLocationWidget (SdkLocationPanel panel) : base (false, 12)
		{
			this.panel = panel;
			
			this.PackStart (new Label () {
				Markup = "<b>" + GLib.Markup.EscapeText (panel.Label) + "</b>",
					Xalign = 0f,
			});
			var alignment = new Alignment (0f, 0f, 1f, 1f) { LeftPadding = 24 };
			this.PackStart (alignment);
			var vbox = new VBox (false , 6);
			var locationBox = new HBox (false, 6);
			var messageBox = new HBox (false, 6);
			alignment.Add (vbox);
			vbox.PackStart (messageBox, false, false, 0);
			vbox.PackStart (locationBox, false, false, 0);
			locationBox.PackStart (new Label (GettextCatalog.GetString ("Location:")), false, false, 0);
			locationBox.PackStart (locationEntry, true, true, 0);
			messageBox.PackStart (messageIcon, false, false, 0);
			messageBox.PackStart (messageLabel, true, true, 0);
			messageLabel.Xalign = 0f;
			
			string location = panel.LoadSdkLocationSetting ();
			locationEntry.Path = location ?? "";
			
			locationEntry.PathChanged += delegate {
				Validate ();
			};
			Validate ();

			if (panel.RequiresRestart) {
				PackStart (new HSeparator (), false, false, 0);

				var tableRestart = new RestartPanel ();
				tableRestart.RestartRequested += (sender, e) => {
					ApplyChanges ();
					IdeApp.Restart (true);
				};

				PackStart (tableRestart, false, false, 0);
			}

			ShowAll ();
		}

		void UpdateIconAccessibility (bool found)
		{
			messageIcon.SetCommonAccessibilityAttributes ("LocationImage",
			                                              found ? GettextCatalog.GetString ("A Tick") : GettextCatalog.GetString ("A Cross"),
			                                              found ? GettextCatalog.GetString ("The SDK was found") : GettextCatalog.GetString ("The SDK was not found"));
		}

		void Validate ()
		{
			FilePath location = CleanPath (locationEntry.Path);
			if (!location.IsNullOrEmpty) {
				if (panel.ValidateSdkLocation (location)) {
					messageLabel.Text = GettextCatalog.GetString ("SDK found at specified location.");
					messageIcon.SetIcon (Gtk.Stock.Apply, IconSize.Menu);
					UpdateIconAccessibility (true);
					return;
				}
				messageLabel.Text = GettextCatalog.GetString ("No SDK found at specified location.");
				messageIcon.SetIcon (Gtk.Stock.Cancel, IconSize.Menu);
				UpdateIconAccessibility (false);
				return;
			}

			foreach (var loc in panel.DefaultSdkLocations) {
				if (panel.ValidateSdkLocation (loc)) {
					messageLabel.Text = GettextCatalog.GetString ("SDK found at default location.");
					messageIcon.SetIcon (Gtk.Stock.Apply, IconSize.Menu);
					UpdateIconAccessibility (true);
					return;
				}
			}

			messageLabel.Text = GettextCatalog.GetString ("No SDK found at default location.");
			messageIcon.SetIcon (Gtk.Stock.Cancel, IconSize.Menu);
			UpdateIconAccessibility (false);
		}
		
		FilePath CleanPath (FilePath path)
		{
			if (path.IsNullOrEmpty) {
				return null;
			}

			try {
				path = path.FullPath;
			} catch {
				return null;
			}

			//if it's a default path, blank it *unless* it overrides a higher priority default path
			bool overridesHigherPriorityDefault = false;
			foreach (var loc in panel.DefaultSdkLocations) {
				if (path == loc) {
					if (overridesHigherPriorityDefault) {
						break;
					}
					return null;
				}
				if (panel.ValidateSdkLocation (loc)) {
					overridesHigherPriorityDefault = true;
				}
			}

			return path;
		}
		
		public void ApplyChanges ()
		{
			panel.SaveSdkLocationSetting (CleanPath (locationEntry.Path));
		}
	}
}