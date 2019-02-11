//
// DotNetCoreSdkLocationWidget.UI.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2018 Microsoft
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
using System.Linq;
using MonoDevelop.Components.AtkCocoaHelper;
using MonoDevelop.Components.Extensions;
using MonoDevelop.Core;
using Xwt;

namespace MonoDevelop.DotNetCore.Gui
{
	partial class DotNetCoreSdkLocationWidget : Widget
	{
		CustomFileSelector locationFileSelector;
		Label commandLineFoundLabel;
		ImageView commandLineFoundIcon;
		Label sdkFoundLabel;
		ImageView sdkFoundIcon;
		ListBox sdkVersionsListBox;
		Label runtimeFoundLabel;
		ImageView runtimeFoundIcon;
		ListBox runtimeVersionsListBox;

		void Build ()
		{
			var mainVBox = new VBox ();
			mainVBox.Spacing = 12;

			// .NET Core command line section.
			var titleLabel = new Label ();
			titleLabel.Markup = GetBoldMarkup (GettextCatalog.GetString (".NET Core Command Line"));
			mainVBox.PackStart (titleLabel);

			var commandLineVBox = new VBox ();
			commandLineVBox.Spacing = 6;
			commandLineVBox.MarginLeft = 24;
			mainVBox.PackStart (commandLineVBox);

			var commandLineFoundHBox = new HBox ();
			commandLineFoundHBox.Spacing = 6;
			commandLineVBox.PackStart (commandLineFoundHBox, false, false);

			commandLineFoundIcon = new ImageView ();
			commandLineFoundHBox.PackStart (commandLineFoundIcon, false, false);

			commandLineFoundLabel = new Label ();
			commandLineFoundHBox.PackStart (commandLineFoundLabel, true, true);

			var locationBox = new HBox ();
			locationBox.Spacing = 6;
			commandLineVBox.PackStart (locationBox, false, false);

			var locationLabel = new Label ();
			locationLabel.Text = GettextCatalog.GetString ("Location:");
			locationBox.PackStart (locationLabel, false, false);

			locationFileSelector = new CustomFileSelector ();
			locationFileSelector.TextEntry.SetCommonAccessibilityAttributes (
				"DotNetCoreSdkLocationWidget.Location",
				locationLabel.Text,
				GettextCatalog.GetString ("Enter the .NET Core SDK location"));
			locationFileSelector.BrowseButton.SetCommonAccessibilityAttributes (
				"DotNetCoreSdkLocationWidget.Browse",
				GettextCatalog.GetString ("Browse"),
				GettextCatalog.GetString ("Select a folder for the .NET Core SDK location"));
			locationBox.PackStart (locationFileSelector, true, true);

			// .NET Core SDK section.
			var sdkVersionsTitleLabel = new Label ();
			sdkVersionsTitleLabel.Markup = GetBoldMarkup (GettextCatalog.GetString (".NET Core SDK"));
			mainVBox.PackStart (sdkVersionsTitleLabel);

			var sdkVersionsVBox = new VBox ();
			sdkVersionsVBox.Spacing = 6;
			sdkVersionsVBox.MarginLeft = 24;
			mainVBox.PackStart (sdkVersionsVBox, true, true);

			var sdkFoundHBox = new HBox ();
			sdkFoundHBox.Spacing = 6;
			sdkFoundHBox.MarginBottom = 6;
			sdkVersionsVBox.PackStart (sdkFoundHBox, false, false);

			sdkFoundIcon = new ImageView ();
			sdkFoundHBox.PackStart (sdkFoundIcon, false, false);

			sdkFoundLabel = new Label ();
			sdkFoundHBox.PackStart (sdkFoundLabel, false, false);

			sdkVersionsListBox = new ListBox ();
			sdkVersionsVBox.PackStart (sdkVersionsListBox, true, true);

			// .NET Core runtime section
			var runtimeVersionsTitleLabel = new Label ();
			runtimeVersionsTitleLabel.Markup = GetBoldMarkup (GettextCatalog.GetString (".NET Core Runtime"));
			mainVBox.PackStart (runtimeVersionsTitleLabel);

			var runtimeVersionsVBox = new VBox ();
			runtimeVersionsVBox.Spacing = 6;
			runtimeVersionsVBox.MarginLeft = 24;
			mainVBox.PackStart (runtimeVersionsVBox, true, true);

			var runtimeFoundHBox = new HBox ();
			runtimeFoundHBox.Spacing = 6;
			runtimeFoundHBox.MarginBottom = 6;
			runtimeVersionsVBox.PackStart (runtimeFoundHBox, false, false);

			runtimeFoundIcon = new ImageView ();
			runtimeFoundHBox.PackStart (runtimeFoundIcon, false, false);

			runtimeFoundLabel = new Label ();
			runtimeFoundHBox.PackStart (runtimeFoundLabel, false, false);

			runtimeVersionsListBox = new ListBox ();
			runtimeVersionsVBox.PackStart (runtimeVersionsListBox, true, true);

			Content = mainVBox;
		}

		static string GetBoldMarkup (string text)
		{
			return "<b>" + GLib.Markup.EscapeText (text) + "</b>";
		}

		void UpdateSdkIconAccessibility (bool found)
		{
			sdkFoundIcon.SetCommonAccessibilityAttributes (
				"DotNetCoreSdkFoundImage",
				found ? GettextCatalog.GetString ("A Tick") : GettextCatalog.GetString ("A Cross"),
				found ? GettextCatalog.GetString ("The .NET Core SDK was found") : GettextCatalog.GetString ("The .NET Core SDK was not found"));
		}

		void UpdateCommandLineIconAccessibility (bool found)
		{
			commandLineFoundIcon.SetCommonAccessibilityAttributes (
				"DotNetCoreCommandLineFoundImage",
				found ? GettextCatalog.GetString ("A Tick") : GettextCatalog.GetString ("A Cross"),
				found ? GettextCatalog.GetString ("The .NET Core command line was found") : GettextCatalog.GetString ("The .NET Core command line was not found"));
		}

		void UpdateRuntimeIconAccessibility (bool found)
		{
			runtimeFoundIcon.SetCommonAccessibilityAttributes (
				"DotNetCoreRuntimeFoundImage",
				found ? GettextCatalog.GetString ("A Tick") : GettextCatalog.GetString ("A Cross"),
				found ? GettextCatalog.GetString ("A .NET Core runtime was found") : GettextCatalog.GetString ("A .NET Core runtime was not found"));
		}

		/// <summary>
		/// This is slightly different from the standard FileSelector. When the select file
		/// dialog is cancelled the current directory is not remembered. This keeps the
		/// behaviour consistent with the other SDK pages that use the FileSelector from
		/// MonoDevelop.Components.
		/// </summary>
		class CustomFileSelector : Widget
		{
			TextEntry entry;
			Button browseButton;
			FileDialog dialog;
			string currentFolder;
			string title;

			public CustomFileSelector ()
			{
				var box = new HBox ();
				entry = new TextEntry ();
				entry.Changed += EntryChanged;
				box.PackStart (entry, true);

				browseButton = new Button ("…");
				box.PackStart (browseButton);
				browseButton.Clicked += BrowseButtonClicked;

				Content = box;
			}

			internal TextEntry TextEntry {
				get { return entry; }
			}

			internal Button BrowseButton {
				get { return browseButton; }
			}

			public string CurrentFolder {
				get {
					return dialog != null ? dialog.CurrentFolder : currentFolder;
				}
				set {
					if (dialog != null)
						dialog.CurrentFolder = value;

					currentFolder = value;
				}
			}

			public string FileName {
				get { return dialog != null ? dialog.FileName : entry.Text; }
				set { entry.Text = value; }
			}

			public string Title {
				get {
					return title;
				}

				set {
					title = value;
					if (dialog != null)
						dialog.Title = value;
				}
			}

			public event EventHandler FileChanged;

			void EntryChanged (object sender, EventArgs e)
			{
				FileChanged?.Invoke (sender, e);
			}

			void BrowseButtonClicked (object sender, EventArgs e)
			{
				dialog = new OpenFileDialog ();

				try {
					if (!string.IsNullOrEmpty (currentFolder))
						dialog.CurrentFolder = currentFolder;
					if (!string.IsNullOrEmpty (title))
						dialog.Title = title;
					if (dialog.Run (ParentWindow))
						FileName = dialog.FileName;
				} finally {
					dialog.Dispose ();
					dialog = null;
				}
			}
		}
	}
}
