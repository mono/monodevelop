//
// EmptyClass.cs
//
// Author:
//       josemiguel <jostor@microsoft.com>
//
// Copyright (c) 2019 
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
using System.IO;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using Xwt;

namespace MonoDevelop.DotNetCore.Commands
{
	class SelectSDKCommandHandler : CommandHandler
	{
		protected override void Run ()
		{
			using (var selectSDKDialog = new SelectSDKDialog ()) {
				var parent = Toolkit.CurrentEngine.WrapWindow (IdeApp.Workbench.RootWindow);
				var result = selectSDKDialog.Run (parent);
				selectSDKDialog.Close ();
			}

			base.Run ();
		}
	}

	class SelectSDKDialog : Dialog
	{
		VBox mainVBox;
		Label label;
		DialogButton acceptButton;
		DialogButton cancelButton;
		DialogButton showButton;
		ComboBox comboBox;
		DotNetCoreSdkPaths dotNetCoreSdkPaths;
		HBox messageBox;
		Label messageLabel;
		ImageView messageIcon;

		string SolutionBaseDirectory => IdeApp.ProjectOperations.CurrentSelectedSolutionItem.BaseDirectory;

		public SelectSDKDialog ()
		{
			dotNetCoreSdkPaths = new DotNetCoreSdkPaths ();
			dotNetCoreSdkPaths.GlobalJsonPath = dotNetCoreSdkPaths.LookUpGlobalJson (SolutionBaseDirectory);
			Initialize ();
		}

		void Initialize ()
		{
			Title = GettextCatalog.GetString (".NET Core SDK");
			Resizable = false;
			mainVBox = new VBox {
				Name = "mainVBox",
				Spacing = 6
			};

			label = new Label {
				Name = "label",
				Text = GettextCatalog.GetString ("Select .NET Core SDK in global.json:")
			};
			mainVBox.PackStart (label);

			messageBox = new HBox ();
			messageBox.Hide ();
			messageLabel = new Label ();
			messageIcon = new ImageView {
				Image = ImageService.GetIcon (Gtk.Stock.DialogWarning, Gtk.IconSize.Menu)
			};

			comboBox = new ComboBox ();
			comboBox.SelectionChanged += ComboBox_SelectionChanged;
			for (var i = 0; i < DotNetCoreSdk.Versions.Length; i++)
				comboBox.Items.Add (DotNetCoreSdk.Versions [i].OriginalString);

			//if globalJson exists, select it
			var currentSdk = dotNetCoreSdkPaths.ReadGlobalJson ();
			var acceptButtonCaption = "Create";
			if (!string.IsNullOrEmpty (currentSdk)) {
				if (!comboBox.Items.Contains (currentSdk)) {
					currentSdk = $"{currentSdk} (not installed)";
					comboBox.Items.Add (currentSdk);
					messageLabel.Text = GettextCatalog.GetString ("{0} will be used instead.", ResolveSDK (currentSdk));
					messageBox.Show ();
				} 

				comboBox.SelectedText = currentSdk;
				acceptButtonCaption = "Update";
			}

			messageBox.PackStart (messageIcon);
			messageBox.PackStart (messageLabel);
			mainVBox.PackStart (comboBox);
			mainVBox.PackEnd (messageBox);


			acceptButton = new DialogButton (GettextCatalog.GetString (acceptButtonCaption), Xwt.Command.Ok);
			cancelButton = new DialogButton (GettextCatalog.GetString ("Cancel"), Xwt.Command.Close);
			showButton = new DialogButton (GettextCatalog.GetString ("Reveal in Finder"), Xwt.Command.Clear);

			acceptButton.Clicked += AcceptButton_Clicked;
			showButton.Clicked += ShowButton_Clicked;

			Content = mainVBox;
			if (File.Exists (dotNetCoreSdkPaths.GlobalJsonPath))
				Buttons.Add (showButton);
			Buttons.Add (cancelButton);
			Buttons.Add (acceptButton);
			this.DefaultCommand = acceptButton.Command;

			Width = 400;
			Height = 120;
			Name = "MainWindow";
			FullScreen = false;
			Resizable = false;
		}

		void ComboBox_SelectionChanged (object sender, EventArgs e)
		{
			if (!comboBox.SelectedText.Contains ("(not installed)"))
				messageBox.Hide ();
		}

		string ResolveSDK (string sdkVersion)
		{
			var fakeFolder = Path.Combine (Path.GetTempPath (), "_globaljson");
			Directory.CreateDirectory (fakeFolder);
			var selectedSDK = sdkVersion.Replace ("(not installed)", string.Empty).Trim ();
			CreateGlobalJson (fakeFolder, selectedSDK);
			var resolver = new DotNetCoreSdkPaths ();
			resolver.ResolveSDK (fakeFolder, forceLookUpGlobalJson: true);
			Directory.Delete (fakeFolder, recursive: true);
			return resolver.MSBuildSDKsPath;
		}

		void AcceptButton_Clicked (object sender, EventArgs e)
		{
			var selectedSDK = comboBox.SelectedText.Replace ("(not installed)", string.Empty).Trim ();
			var path = CreateGlobalJson (SolutionBaseDirectory, selectedSDK);

			if (string.IsNullOrEmpty (path)) {
				MessageDialog.ShowError (GettextCatalog.GetString ("Global.json couldn`t be created."));
			}

			this.Close ();
		}

		void ShowButton_Clicked (object sender, EventArgs e)
		{
			if (string.IsNullOrEmpty (dotNetCoreSdkPaths.GlobalJsonPath))
				return;

			IdeServices.DesktopService.OpenFolder (new FileInfo (dotNetCoreSdkPaths.GlobalJsonPath).Directory.FullName);
		}

		string CreateGlobalJson (string workingDirectory, string version)
		{
			try {
				var GlobalJsonContent = $"\n\t{{\n\t\t\"sdk\": {{\n\t\t\t\"version\": \"{version}\"\n\t\t}}\n\t}}";
				var GlobalJsonLocation = Path.Combine (workingDirectory, "global.json");

				File.WriteAllText (GlobalJsonLocation, GlobalJsonContent);

				return GlobalJsonLocation;
			} catch (Exception) {
				return string.Empty;
			}
		}
	}
}
