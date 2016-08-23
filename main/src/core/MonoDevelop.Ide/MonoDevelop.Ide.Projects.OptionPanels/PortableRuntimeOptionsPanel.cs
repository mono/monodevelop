//
// PortableRuntimeOptionsPanel.cs
//
// Author: Jeffrey Stedfast <jeff@xamarin.com>
//
// Copyright (c) 2012 Xamarin Inc.
// Copyright (c) Microsoft Inc.
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
using System.Text;
using System.Linq;
using System.Collections.Generic;

using MonoDevelop.Components;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Core.Assemblies;

using Gtk;
using MonoDevelop.Ide.Gui.Dialogs;
using Newtonsoft.Json.Linq;
using MonoDevelop.Ide.Editor;

namespace MonoDevelop.Ide.Projects.OptionPanels
{
	internal class PortableRuntimeOptionsPanel : ItemOptionsPanel
	{
		PortableRuntimeOptionsPanelWidget widget;

		public override Control CreatePanelWidget ()
		{
			widget = new PortableRuntimeOptionsPanelWidget ((DotNetProject)ConfiguredProject, ItemConfigurations);

			return widget;
		}

		public override void ApplyChanges ()
		{
			widget.Store ();
		}
	}

	class PortableRuntimeOptionsPanelWidget : Gtk.VBox
	{
		const string netstandardDocsUrl = "https://docs.microsoft.com/en-us/dotnet/articles/standard/library";
		const string pcldDocsUrl = "https://developer.xamarin.com/guides/cross-platform/application_fundamentals/pcl/introduction_to_portable_class_libraries/";

		DotNetProject project;
		TargetFramework target;

		TargetFrameworkMoniker pcl5Tfm = new TargetFrameworkMoniker (TargetFrameworkMoniker.ID_PORTABLE, "v5.0");

		string [] KnownNetStandardVersions = new [] {
			"netstandard1.0",
			"netstandard1.1",
			"netstandard1.2",
			"netstandard1.3",
			"netstandard1.4",
			"netstandard1.5",
			"netstandard1.6",
		};

		const string NetStandardPackageName = "NETStandard.Library";
		const string NetStandardPackageVersion = "1.6.0";

		ComboBox netStandardCombo;
		Entry targetFrameworkEntry;
		RadioButton netstandardRadio;
		RadioButton pclRadio;
		Button frameworkPickerButton;

		public PortableRuntimeOptionsPanelWidget (DotNetProject project, IEnumerable<ItemConfiguration> configurations)
		{
			Build ();

			this.project = project;

			TargetFramework = project.TargetFramework;

			//TODO: error handling
			NetStandardVersion = GetProjectJsonFrameworks (project)?.FirstOrDefault ();
		}

		void Build ()
		{
			Spacing = 6;

			PackStart (new Label { Markup = string.Format ("<b>{0}</b>", GettextCatalog.GetString ("Target Framework")), Xalign = 0f });

			var fxAlignment = new Alignment (0f, 0f, 1f, 1f) { LeftPadding = 12 };
			PackStart (fxAlignment);
			var radioBox = new VBox { Spacing = 10 };
			fxAlignment.Add (radioBox);

			var netstandardPickerHbox = new HBox { Spacing = 10 };
			radioBox.PackStart (netstandardPickerHbox);
			netstandardRadio = new RadioButton (GettextCatalog.GetString (".NET Standard Platform:"));
			netstandardPickerHbox.PackStart (netstandardRadio, false, false, 0);
			netstandardPickerHbox.PackStart (netStandardCombo = ComboBox.NewText (), false, false, 0);

			var netstandardDesc = new Label { Markup = GettextCatalog.GetString ("Your library will be compatible with all frameworks that support the selected <a href='{0}'>.NET Standard</a> version.", netstandardDocsUrl), Xalign = 0f };
			GtkWorkarounds.SetLinkHandler (netstandardDesc, HandleLink);
			radioBox.PackStart (new Alignment (0f, 0f, 1f, 1f) { Child = netstandardDesc, LeftPadding = 24 });

			var pclPickerHbox = new HBox { Spacing = 10 };
			radioBox.PackStart (pclPickerHbox);
			pclRadio = new RadioButton (netstandardRadio, GettextCatalog.GetString (".NET Portable:"));
			pclPickerHbox.PackStart (pclRadio, false, false, 0);
			pclPickerHbox.PackStart (targetFrameworkEntry = new Entry { IsEditable = false, WidthChars = 20 }, false, false, 0);
			frameworkPickerButton = new Button (GettextCatalog.GetString ("Change..."));
			pclPickerHbox.PackStart (frameworkPickerButton, false, false, 0);

			var pclDesc = new Label { Markup = GettextCatalog.GetString ("Your library will be compatible with the frameworks supported by the selected <a href='{0}'>PCL profile</a>.", pcldDocsUrl), Xalign = 0f };
			GtkWorkarounds.SetLinkHandler (pclDesc, HandleLink);
			radioBox.PackStart (new Alignment (0f, 0f, 1f, 1f) { Child = pclDesc, LeftPadding = 24 });

			frameworkPickerButton.Clicked += PickFramework;

			// both toggle when we switch between them, only need to subscribe to one event
			netstandardRadio.Toggled += RadioToggled;

			UpdateSensitivity ();

			ShowAll ();
		}

		string NetStandardVersion {
			get {
				return netStandardCombo.ActiveText;
			}
			set {
				((ListStore)netStandardCombo.Model).Clear ();

				int selected = -1;

				for (int i = 0; i < KnownNetStandardVersions.Length; i++) {
					var version = KnownNetStandardVersions [i];
					netStandardCombo.AppendText (version);
					if (version == value) {
						selected = i;
					}
				}

				if (value == null) {
					selected = KnownNetStandardVersions.Length - 1;
				} else if (selected < 0) {
					//project uses some version we don't know about, add it
					netStandardCombo.AppendText (value);
					selected = KnownNetStandardVersions.Length;
				}

				netStandardCombo.Active = selected;
			}
		}

		TargetFramework TargetFramework {
			get {
				return target;
			}
			set {
				target = value;
				targetFrameworkEntry.Text = PortableRuntimeSelectorDialog.GetPclShortDisplayName (target, false);
			}
		}

		void RadioToggled (object sender, EventArgs e)
		{
			UpdateSensitivity ();
		}

		void UpdateSensitivity ()
		{
			bool pcl = pclRadio.Active;

			netStandardCombo.Sensitive = !pcl;
			targetFrameworkEntry.Sensitive = pcl;
			frameworkPickerButton.Sensitive = pcl;
		}

		void HandleLink (string url)
		{
			DesktopService.ShowUrl (url);
		}

		void PickFramework (object sender, EventArgs e)
		{
			var dlg = new PortableRuntimeSelectorDialog (target);
			try {
				var result = MessageService.RunCustomDialog (dlg, (Gtk.Window)Toplevel);
				if (result == (int)Gtk.ResponseType.Ok) {
					TargetFramework = dlg.TargetFramework;
				}
			} finally {
				dlg.Destroy ();
			}
		}

		public void Store ()
		{
			//TODO error handling
			var fx = TargetFramework;
			var isNetStandard = netstandardRadio.Active;
			var nsVersion = NetStandardVersion;

			bool projectFileDirty = false;

			//netstandard always uses PCL5 framework
			if (isNetStandard) {
				fx = Runtime.SystemAssemblyService.GetTargetFramework (pcl5Tfm);
				SetProjectJsonValues (project, nsVersion, out projectFileDirty);
			} else {
				//TODO: set target to PCL ID and remove dep
			}

			if (fx != null && fx != project.TargetFramework) {
				projectFileDirty = true;
				project.TargetFramework = fx;
			}

			if (projectFileDirty) {
				IdeApp.ProjectOperations.SaveAsync (project);
			}
		}

		static IEnumerable<string> GetProjectJsonFrameworks (DotNetProject project)
		{
			var packagesJsonFile = project.Files.GetFileWithVirtualPath ("project.json");
			if (packagesJsonFile == null) {
				return null;
			}

			var file = TextFileProvider.Instance.GetEditableTextFile (packagesJsonFile.FilePath.ToString ());

			JObject json;

			using (var tr = file.CreateReader ())
			using (var jr = new Newtonsoft.Json.JsonTextReader (tr)) {
				json = (JObject)JToken.Load (jr);
			}

			var frameworks = json ["frameworks"] as JObject;
			if (frameworks == null)
				return null;

			return frameworks.Properties ().Select (p => p.Name);
		}

		static void SetProjectJsonValues (DotNetProject project, string framework, out bool projectFileDirty)
		{
			projectFileDirty = false;
			var packagesJsonFile = project.Files.GetFileWithVirtualPath ("project.json");

			if (packagesJsonFile == null) {
				packagesJsonFile = AddProjectJson (project);
				projectFileDirty = true;
			}

			bool isOpen;
			var file = TextFileProvider.Instance.GetTextEditorData (packagesJsonFile.FilePath.ToString (), out isOpen);

			JObject json;

			using (var tr = file.CreateReader ())
			using (var jr = new Newtonsoft.Json.JsonTextReader (tr)) {
				json = (JObject)JToken.Load (jr);
			}

			var deps = (json ["dependencies"] as JObject) ?? ((JObject) (json ["dependencies"] = new JObject ()));
			var existingRefVersion = deps.Property (NetStandardPackageName)?.Value?.Value<string> ();
			deps [NetStandardPackageName] = EnsureMinimumVersion (NetStandardPackageVersion, existingRefVersion);

			var existingFxValue = (json ["frameworks"] as JObject)?.Properties ()?.SingleOrDefault ();
			json ["frameworks"] = new JObject (
				new JProperty (framework, existingFxValue?.Value ?? new JObject ())
			);

			file.Text = json.ToString ();

			if (!isOpen) {
				file.Save ();
			}
		}

		static ProjectFile AddProjectJson (DotNetProject project)
		{
			//TODO: migrate packages.config
			ProjectFile packagesJsonFile = new ProjectFile (project.BaseDirectory.Combine ("project.json"), BuildAction.None);

			if (!System.IO.File.Exists (packagesJsonFile.FilePath)) {
				System.IO.File.WriteAllText (packagesJsonFile.FilePath,
@"{
  ""supports"": {},
  ""dependencies"": {},
  ""frameworks"": {}
}");
			}

			project.AddFile (packagesJsonFile);
			return packagesJsonFile;
		}

		static string EnsureMinimumVersion (string minimum, string existing)
		{
			if (existing == null) {
				return minimum;
			}

			var minimumSplit = minimum.Split (new char [] { '.', '-' });
			var existingSplit = existing.Split (new char [] { '.', '-' });

			for (int i = 0; i < minimumSplit.Length; i++) {
				var m = int.Parse (minimumSplit [i]);
				int e;
				if (existingSplit.Length <= i || !int.TryParse (existingSplit [i], out e)) {
					return minimum;
				}
				if (m > e) {
					return minimum;
				}
				if (e > m) {
					return existing;
				}
			}

			return minimum;
		}
	}
}