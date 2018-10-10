using System;
using System.IO;
using MonoDevelop.AspNetCore.Commands;
using MonoDevelop.Core;
using MonoDevelop.DotNetCore;
using Xwt;

namespace MonoDevelop.AspNetCore.Dialogs
{
	class PublishToFolderDialog : Dialog
	{
		VBox MainVBox;
		Label PublishYourAppLabel;
		VBox BrowseVBox;
		Label ChooseLabel;
		HBox BrowseEntryHBox;
		TextEntry PathEntry;
		Button BrowseButton;
		DialogButton PublishButton;
		DialogButton CancelButton;
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
			Height = 180;
			Name = "MainWindow";
			Title = GettextCatalog.GetString ("Publish to Folder");
			Resizable = false;
			MainVBox = new VBox {
				Name = "MainVBox",
				Spacing = 6
			};

			PublishYourAppLabel = new Label {
				Name = "publishYourAppLabel",
				Text = GettextCatalog.GetString ("Publish your app to a folder or a file share")
			};
			MainVBox.PackStart (PublishYourAppLabel);
			BrowseVBox = new VBox {
				WidthRequest = 50,
				HeightRequest = 70,
				Name = "BrowseVBox",
				Spacing = 6
			};
			ChooseLabel = new Label {
				Name = "ChooseLabel",
				Text = GettextCatalog.GetString ("Choose a folder:")
			};
			BrowseVBox.PackStart (ChooseLabel);
			BrowseEntryHBox = new HBox {
				Name = "BrowseEntryHBox",
				Spacing = 4
			};
			var defaultDirectory = publishCommandItem.Project.GetActiveConfiguration () == null
				? BinBaseUri.ToString ()
				: Path.Combine (BinBaseUri.ToString (),
								publishCommandItem.Project.TargetFramework.Id.GetShortFrameworkName (),
								publishCommandItem.Project.GetActiveConfiguration ());
			//make it relative by default
			defaultDirectory = BinBaseUri.MakeRelativeUri (new Uri (defaultDirectory)).ToString ();
			PathEntry = new TextEntry {
				Name = "PathEntry",
				Text = defaultDirectory
			};
			PathEntry.LostFocus += (sender, e) => {
				PublishButton.Sensitive = !string.IsNullOrEmpty (PathEntry.Text);
			};
			BrowseEntryHBox.PackStart (PathEntry, expand: true);
			BrowseButton = new Button {
				Name = "BrowseButton",
				Label = GettextCatalog.GetString ("Browse")
			};
			BrowseButton.Clicked += BrowseButton_Clicked;
			BrowseEntryHBox.PackEnd (BrowseButton);
			BrowseVBox.PackStart (BrowseEntryHBox);
			MainVBox.PackEnd (this.BrowseVBox);
			PublishButton = new DialogButton (GettextCatalog.GetString ("Publish"), Command.Ok);
			CancelButton = new DialogButton (GettextCatalog.GetString ("Cancel"), Command.Close);
			Content = MainVBox;
			Buttons.Add (PublishButton);
			Buttons.Add (CancelButton);
		}

		protected override void OnCommandActivated (Command cmd)
		{
			if (cmd == Command.Ok) {
				publishCommandItem.Profile = new ProjectPublishProfile ();
				publishCommandItem.Profile.PublishUrl = PathEntry.Text;
				publishCommandItem.Profile.TargetFramework = publishCommandItem.Project.TargetFramework.Id.GetShortFrameworkName ();
				publishCommandItem.Profile.LastUsedBuildConfiguration = publishCommandItem.Project.GetActiveConfiguration ();
				publishCommandItem.Profile.LastUsedPlatform = publishCommandItem.Project.GetActivePlatform ();

				PublishToFolderRequested?.Invoke (this, publishCommandItem);
				PublishButton.Sensitive = false;
				return;
			}

			base.OnCommandActivated (cmd);
		}

		void BrowseButton_Clicked (object sender, EventArgs e)
		{
			var fileDialog = new Components.SelectFolderDialog (GettextCatalog.GetString ("Publish to Folder"), Components.FileChooserAction.SelectFolder) {
				SelectMultiple = false,
				CurrentFolder = PathEntry.Text
			};
			fileDialog.Run ();
			PathEntry.Text = BinBaseUri.MakeRelativeUri (new Uri (fileDialog.SelectedFile)).ToString ();
		}

		protected override void Dispose (bool disposing)
		{
			BrowseButton.Clicked -= BrowseButton_Clicked;

			base.Dispose (disposing);
		}
	}
}