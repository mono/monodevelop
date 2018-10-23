﻿using System;
using System.IO;
using MonoDevelop.AspNetCore.Commands;
using MonoDevelop.Core;
using MonoDevelop.DotNetCore;
using MonoDevelop.Ide;
using Xwt;

namespace MonoDevelop.AspNetCore.Dialogs
{
	class PublishToFolderDialog : Dialog
	{
		VBox mainVBox;
		Label publishYourAppLabel;
		VBox browseVBox;
		Label chooseLabel;
		HBox browseEntryHBox;
		TextEntry pathEntry;
		Button browseButton;
		DialogButton publishButton;
		DialogButton cancelButton;
		HBox messageBox;
		Label messageLabel;
		ImageView messageIcon;
		Uri BinBaseUri => new Uri (Path.Combine (publishCommandItem.Project.BaseDirectory, "bin"));

		readonly PublishCommandItem publishCommandItem;

		public event EventHandler<PublishCommandItem> PublishToFolderRequested;

		public PublishToFolderDialog (PublishCommandItem publishCommandItem)
		{
			this.publishCommandItem = publishCommandItem;
			Initialize ();
		}

		protected void Initialize ()
		{
			Width = 400;
			Height = 120;
			Name = "MainWindow";
			Title = GettextCatalog.GetString ("Publish to Folder");
			Resizable = false;
			mainVBox = new VBox {
				Name = "mainVBox",
				Spacing = 6
			};

			publishYourAppLabel = new Label {
				Name = "publishYourAppLabel",
				Text = GettextCatalog.GetString ("Publish your app to a folder or a file share")
			};
			mainVBox.PackStart (publishYourAppLabel);
			browseVBox = new VBox {
				Name = "browseVBox",
				Spacing = 6
			};
			browseVBox.MarginTop = 20;
			chooseLabel = new Label {
				Name = "chooseLabel",
				Text = GettextCatalog.GetString ("Choose a folder:")
			};
			browseVBox.PackStart (chooseLabel);
			browseEntryHBox = new HBox {
				Name = "browseEntryHBox",
				Spacing = 4
			};
			var activeConfiguration = publishCommandItem.Project.GetActiveConfiguration ();
			var defaultDirectory = activeConfiguration == null
				? BinBaseUri.ToString ()
				: Path.Combine (BinBaseUri.ToString (),
								publishCommandItem.Project.TargetFramework.Id.GetShortFrameworkName (),
								publishCommandItem.Project.GetActiveConfiguration ());
			//make it relative by default
			defaultDirectory = BinBaseUri.MakeRelativeUri (new Uri (defaultDirectory)).ToString ();
			pathEntry = new TextEntry {
				Name = "pathEntry",
				Text = defaultDirectory
			};
			pathEntry.Changed += pathEntry_Changed;
			pathEntry.LostFocus += (sender, e) => {
				publishButton.Sensitive = !string.IsNullOrEmpty (pathEntry.Text) && !messageBox.Visible;
			};
			browseEntryHBox.PackStart (pathEntry, expand: true);
			browseButton = new Button {
				Name = "browseButton",
				Label = GettextCatalog.GetString ("Browse")
			};
			browseButton.Clicked += browseButton_Clicked;
			browseEntryHBox.PackEnd (browseButton);
			browseVBox.PackStart (browseEntryHBox);

			messageBox = new HBox ();
			messageBox.Hide ();
			messageLabel = new Label ();
			messageIcon = new ImageView ();
			messageLabel.Text = GettextCatalog.GetString ("The path provided is not a valid folder path.");
			messageIcon.Image = ImageService.GetIcon (Gtk.Stock.Cancel, Gtk.IconSize.Menu);//.WithStyles (sensitive ? "" : "disabled");
			messageBox.PackStart (messageIcon);
			messageBox.PackStart (messageLabel);
			mainVBox.PackStart (browseVBox);
			mainVBox.PackEnd (messageBox);

			publishButton = new DialogButton (GettextCatalog.GetString ("Publish"), Command.Ok);
			cancelButton = new DialogButton (GettextCatalog.GetString ("Cancel"), Command.Close);
			Content = mainVBox;
			Buttons.Add (publishButton);
			Buttons.Add (cancelButton);
		}

		void pathEntry_Changed (object sender, EventArgs e)
		{
			if (Uri.IsWellFormedUriString (pathEntry.Text, UriKind.RelativeOrAbsolute))
				messageBox.Hide ();
			else
				messageBox.Show ();
		}


		protected override void OnCommandActivated (Command cmd)
		{
			if (cmd == Command.Ok) {
				publishCommandItem.Profile = new ProjectPublishProfile {
					PublishUrl = pathEntry.Text,
					TargetFramework = publishCommandItem.Project.TargetFramework.Id.GetShortFrameworkName (),
					LastUsedBuildConfiguration = publishCommandItem.Project.GetActiveConfiguration (),
					LastUsedPlatform = publishCommandItem.Project.GetActivePlatform ()
				};

				PublishToFolderRequested?.Invoke (this, publishCommandItem);
				publishButton.Sensitive = false;
				return;
			}

			base.OnCommandActivated (cmd);
		}

		void browseButton_Clicked (object sender, EventArgs e)
		{
			var fileDialog = new Components.SelectFolderDialog (GettextCatalog.GetString ("Publish to Folder"), Components.FileChooserAction.SelectFolder) {
				SelectMultiple = false,
				CurrentFolder = pathEntry.Text
			};
			fileDialog.Run ();
			pathEntry.Text = BinBaseUri.MakeRelativeUri (new Uri (fileDialog.SelectedFile)).ToString ();
		}

		protected override void Dispose (bool disposing)
		{
			browseButton.Clicked -= browseButton_Clicked;

			base.Dispose (disposing);
		}
	}
}