using System;
using System.IO;
using MonoDevelop.AspNetCore.Commands;
using MonoDevelop.Core;
using MonoDevelop.DotNetCore;
using MonoDevelop.Ide;
using MonoDevelop.Projects;
using Xwt;

namespace MonoDevelop.AspNetCore.Dialogs
{
	internal class DefaultFolderResolver
	{
		internal const string DefaultConfiguration = "Release";
		DotNetProject project;

		public Uri BinBaseUri => new Uri (Path.Combine (project.BaseDirectory, "bin"));
		public string Configuration { get; private set; } = DefaultConfiguration;
		public DefaultFolderResolver (DotNetProject project) => this.project = project;

		// The default folder is: "bin/Release/<netcore-version>/publish"
		// as long as Release exists. If it does not, then we take the active one.
		public string GetDefaultFolder (UriKind uriKind)
		{
			//check if there is a Release configuration
			bool releaseFound = false;
			for (int i = 0; i < project.Configurations.Count; i++) {
				if (project.Configurations [i].Name == DefaultConfiguration) {
					releaseFound = true;
					break;
				}
			}
				
			if (!releaseFound) //if there is no Release config, then we take the active one
				Configuration = project.GetActiveConfiguration ();

			var defaultDirectory = Path.Combine (BinBaseUri.ToString (), Configuration,
								project.TargetFramework.Id.GetShortFrameworkName (),
								"publish");

			if (uriKind == UriKind.Relative)
				return BinBaseUri.MakeRelativeUri (new Uri (defaultDirectory)).ToString ();

			return defaultDirectory;
		}
	}

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
		DefaultFolderResolver defaultDirectoryResolver;

		readonly PublishCommandItem publishCommandItem;

		public event EventHandler<PublishCommandItem> PublishToFolderRequested;

		public PublishToFolderDialog (PublishCommandItem publishCommandItem)
		{
			this.publishCommandItem = publishCommandItem;
			Initialize ();
		}

		protected void Initialize ()
		{
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
			defaultDirectoryResolver = new DefaultFolderResolver (publishCommandItem.Project);
			//make it relative by default
			var defaultDirectory = defaultDirectoryResolver.GetDefaultFolder (UriKind.Relative);
			pathEntry = new TextEntry {
				Name = "pathEntry",
				Text = defaultDirectory
			};
			pathEntry.Changed += pathEntry_Changed;
			pathEntry.LostFocus += PathEntry_LostFocus;
			browseEntryHBox.PackStart (pathEntry, expand: true);
			browseButton = new Button {
				Name = "browseButton",
				Label = GettextCatalog.GetString ("Browse...")
			};
			browseButton.Clicked += browseButton_Clicked;
			browseEntryHBox.PackEnd (browseButton);
			browseVBox.PackStart (browseEntryHBox);

			messageBox = new HBox ();
			messageBox.Hide ();
			messageLabel = new Label ();
			messageIcon = new ImageView ();
			messageLabel.Text = GettextCatalog.GetString ("The path provided is not a valid folder path.");
			messageIcon.Image = ImageService.GetIcon (Gtk.Stock.Cancel, Gtk.IconSize.Menu);
			messageBox.PackStart (messageIcon);
			messageBox.PackStart (messageLabel);
			mainVBox.PackStart (browseVBox);
			mainVBox.PackEnd (messageBox);

			publishButton = new DialogButton (GettextCatalog.GetString ("Publish"), Command.Ok);
			cancelButton = new DialogButton (GettextCatalog.GetString ("Cancel"), Command.Close);

			Content = mainVBox;
			Buttons.Add (cancelButton);
			Buttons.Add (publishButton);
			this.DefaultCommand = publishButton.Command;

			Width = 400;
			Height = 120;
			Name = "MainWindow";
			FullScreen = false;
			Resizable = false;
		}

		void pathEntry_Changed (object sender, EventArgs e)
		{
			if (Uri.IsWellFormedUriString (pathEntry.Text, UriKind.RelativeOrAbsolute) && !string.IsNullOrEmpty (pathEntry.Text))
				messageBox.Hide ();
			else
				messageBox.Show ();
		}

		void PathEntry_LostFocus (object sender, EventArgs e)
		{
			publishButton.Sensitive = !messageBox.Visible;
		}

		protected override void OnCommandActivated (Command cmd)
		{
			if (cmd == Command.Ok) {
				publishCommandItem.Profile = new ProjectPublishProfile {
					PublishUrl = pathEntry.Text,
					TargetFramework = publishCommandItem.Project.TargetFramework.Id.GetShortFrameworkName (),
					LastUsedBuildConfiguration = defaultDirectoryResolver.Configuration,
					LastUsedPlatform = publishCommandItem.Project.GetActivePlatform ()
				};

				PublishToFolderRequested?.Invoke (this, publishCommandItem);
				publishButton.Sensitive = false;
			}

			base.OnCommandActivated (cmd);
		}

		void browseButton_Clicked (object sender, EventArgs e)
		{
			var fileDialog = new Components.SelectFolderDialog (GettextCatalog.GetString ("Publish to Folder"), Components.FileChooserAction.CreateFolder) {
				SelectMultiple = false,
				CurrentFolder = pathEntry.Text
			};
			fileDialog.Run ();
			pathEntry.Text = defaultDirectoryResolver.BinBaseUri.MakeRelativeUri (new Uri (fileDialog.SelectedFile)).ToString ();
		}

		protected override void Dispose (bool disposing)
		{
			browseButton.Clicked -= browseButton_Clicked;
			pathEntry.Changed -= pathEntry_Changed;
			pathEntry.LostFocus -= PathEntry_LostFocus;

			base.Dispose (disposing);
		}
	}
}
