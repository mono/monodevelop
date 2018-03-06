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
using MonoDevelop.Projects.MSBuild;

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

		// Profile 111 is ~equivalent to netstandard 1.1
		TargetFrameworkMoniker defaultPclTfm = new TargetFrameworkMoniker (TargetFrameworkMoniker.ID_PORTABLE, "v4.5", "Profile111");

		string [] KnownNetStandardVersions = new [] {
			"netstandard1.0",
			"netstandard1.1",
			"netstandard1.2",
			"netstandard1.3",
			"netstandard1.4",
			"netstandard1.5",
			//FIXME: XS' version of NuGet doesn't support 1.6
			//"netstandard1.6",
		};

		const string NetStandardPackageName = "NETStandard.Library";
		const string NetStandardPackageVersion = "1.6.0";
		const string NetStandardDefaultFramework = "netstandard1.3";
		const string NetStandardPclCompatPackageName = "Microsoft.NETCore.Portable.Compatibility";
		const string NetStandardPclCompatPackageVersion = "1.0.1";

		ComboBox netStandardCombo;
		Entry targetFrameworkEntry;
		RadioButton netstandardRadio;
		RadioButton pclRadio;
		Button frameworkPickerButton;

		//TODO: better error handling
		public PortableRuntimeOptionsPanelWidget (DotNetProject project, IEnumerable<ItemConfiguration> configurations)
		{
			Build ();

			this.project = project;

			TargetFramework = project.TargetFramework;

			string netstandardVersion = null;
			try {
				netstandardVersion = GetProjectJsonFrameworks (project)?.FirstOrDefault ();
				if (netstandardVersion != null && !netstandardVersion.StartsWith ("netstandard", StringComparison.Ordinal)) {
					netstandardVersion = null;
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Error reading project.json file", ex);
			}

			NetStandardVersion = netstandardVersion;

			if (netstandardVersion != null) {
				netstandardRadio.Active = true;
				// Even though netstandard really uses PCL5 in the project file, that PCL is not really useful by itself.
				// Within this dialog, replace it with a better value for the user to get if they switch to PCL.
				// When saving , if it's netstandard we'll write PCL5 to the project regardless.
				TargetFramework = Runtime.SystemAssemblyService.GetTargetFramework (defaultPclTfm);
			} else {
				pclRadio.Active = true;
			}

			UpdateSensitivity ();
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
			pclPickerHbox.PackStart (targetFrameworkEntry = new Entry { IsEditable = false, WidthChars = 20, Name = "targetFrameworkEntry" }, false, false, 0);
			frameworkPickerButton = new Button (GettextCatalog.GetString ("Change..."));
			pclPickerHbox.PackStart (frameworkPickerButton, false, false, 0);

			var pclDesc = new Label { Markup = GettextCatalog.GetString ("Your library will be compatible with the frameworks supported by the selected <a href='{0}'>PCL profile</a>.", pcldDocsUrl), Xalign = 0f };
			GtkWorkarounds.SetLinkHandler (pclDesc, HandleLink);
			radioBox.PackStart (new Alignment (0f, 0f, 1f, 1f) { Child = pclDesc, LeftPadding = 24 });

			frameworkPickerButton.Clicked += PickFramework;

			// both toggle when we switch between them, only need to subscribe to one event
			netstandardRadio.Toggled += RadioToggled;
			
			netStandardCombo.Name = "netStandardCombo";

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

		//TODO error handling
		public void Store ()
		{
			bool needsRestore = false;

			//get the new framework and netstandard version
			var isNetStandard = netstandardRadio.Active;
			var nsVersion = isNetStandard ? NetStandardVersion : null;
			var fx = TargetFramework;


			//netstandard always uses PCL5 framework
			if (isNetStandard) {
				fx = Runtime.SystemAssemblyService.GetTargetFramework (pcl5Tfm);
			}

			//netstandard always uses project.json, ensure it exists
			var projectJsonFile = project.GetProjectFile (project.BaseDirectory.Combine ("project.json"));
			if (isNetStandard && projectJsonFile == null) {
				projectJsonFile = MigrateToProjectJson (project);
				needsRestore = true;
			}

			//if project.json exists, update it
			if (projectJsonFile != null) {
				var nugetFx = nsVersion ?? GetPclProfileFullName (fx.Id) ?? NetStandardDefaultFramework;
				bool projectJsonChanged;
				SetProjectJsonValues (projectJsonFile.FilePath, nugetFx, out projectJsonChanged);
				needsRestore = projectJsonChanged;
			}

			//if the framework has changed, update it
			if (fx != null && fx != project.TargetFramework) {
				project.TargetFramework = fx;
			}

			if (needsRestore) {
				FileService.NotifyFileChanged (projectJsonFile.FilePath);
			}
		}

		static IEnumerable<string> GetProjectJsonFrameworks (DotNetProject project)
		{
			var packagesJsonFile = project.GetProjectFile (project.BaseDirectory.Combine ("project.json"));
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

		static void SetProjectJsonValues (string filename, string framework, out bool changed)
		{
			changed = false;

			bool isOpen;
			var file = TextFileProvider.Instance.GetTextEditorData (filename, out isOpen);

			JObject json;

			using (var tr = file.CreateReader ())
			using (var jr = new Newtonsoft.Json.JsonTextReader (tr)) {
				json = (JObject)JToken.Load (jr);
			}

			var deps = (json ["dependencies"] as JObject);

			if (framework.StartsWith ("netstandard", StringComparison.Ordinal)) {
				if (deps == null) {
					deps = new JObject ();
					json ["dependencies"] = deps;
				}

				//make sure NETStandard.Library has the version we need
				if (EnsurePackageHasVersion (deps, NetStandardPackageName, NetStandardPackageVersion)) {
					//if we had to fix that, also add to optional Microsoft.NETCore.Portable.Compatibility
					EnsurePackageHasVersion (deps, NetStandardPclCompatPackageName, NetStandardPclCompatPackageVersion);
					changed = true;
				}
			} else {
				//not netstandard, remove the netstandard nuget package ref
				if (deps != null) {
					deps.Property (NetStandardPackageName)?.Remove ();
					deps.Property (NetStandardPclCompatPackageName)?.Remove ();
				}
			}

			string [] existingTargetFrameworks = null;
			var frameworks = (json ["frameworks"] as JObject);
			if (frameworks != null) {
				existingTargetFrameworks = frameworks.Properties ().Select (p => p.Name).ToArray ();
			}

			if (existingTargetFrameworks != null && !existingTargetFrameworks.Any(f => f == framework)) {
				var existingFxValue = ((JProperty) json ["frameworks"].First()).Value as JObject;
				json ["frameworks"] = new JObject (
					new JProperty (framework, existingFxValue ?? new JObject ())
				);
				changed = true;
			}

			if (changed) {
				file.ReplaceText (0, file.Length, json.ToString ());

				if (!isOpen) {
					file.Save ();
				}
			}
		}

		static bool EnsurePackageHasVersion (JObject dependencies, string packageName, string version)
		{
			var existingRefVersion = dependencies.Property (packageName)?.Value?.Value<string> ();
			string newRefVersion = EnsureMinimumVersion (version, existingRefVersion);
			if (existingRefVersion != newRefVersion) {
				dependencies [packageName] = newRefVersion;
				return true;
			}
			return false;
		}

		internal static ProjectFile MigrateToProjectJson (DotNetProject project)
		{
			var projectJsonName = project.BaseDirectory.Combine ("project.json");
			var projectJsonFile = new ProjectFile (projectJsonName, BuildAction.None);

			bool isOpen = false;
			JObject json;
			ITextDocument file;

			if (System.IO.File.Exists (projectJsonName)) {
				file = TextFileProvider.Instance.GetTextEditorData (projectJsonFile.FilePath.ToString (), out isOpen);
				using (var tr = file.CreateReader ())
				using (var jr = new Newtonsoft.Json.JsonTextReader (tr)) {
					json = (JObject)JToken.Load (jr);
				}
			} else {
				file = TextEditorFactory.CreateNewDocument ();
				file.FileName = projectJsonName;
				file.Encoding = Encoding.UTF8;
				json = new JObject (
					new JProperty ("dependencies", new JObject ()),
					new JProperty ("frameworks", new JObject())
				);
			}

			List<string> packages = null;
			var packagesConfigFile = project.GetProjectFile (project.BaseDirectory.Combine ("packages.config"));
			if (packagesConfigFile != null) {
				//NOTE: it might also be open and unsaved, but that's an unimportant edge case, ignore it
				var configDoc = System.Xml.Linq.XDocument.Load (packagesConfigFile.FilePath);
				if (configDoc.Root != null) {
					var deps = (json ["dependencies"] as JObject) ?? ((JObject)(json ["dependencies"] = new JObject ()));
					foreach (var packagelEl in configDoc.Root.Elements ("package")) {
						var packageId = (string)packagelEl.Attribute ("id");
						var packageVersion = (string)packagelEl.Attribute ("version");
						deps [packageId] = packageVersion;

						if (packages == null)
							packages = new List<string> ();
						packages.Add (packageId + "." + packageVersion);
					}
				}
			}

			var framework = GetPclProfileFullName (project.TargetFramework.Id) ?? NetStandardDefaultFramework;
			json ["frameworks"] = new JObject (
				new JProperty (framework, new JObject())
			);

			file.Text = json.ToString ();

			if (!isOpen) {
				file.Save ();
			}

			project.AddFile (projectJsonFile);
			if (packagesConfigFile != null) {
				project.Files.Remove (packagesConfigFile);

				//we have to delete the packages.config, or the NuGet addin will try to retarget its packages
				FileService.DeleteFile (packagesConfigFile.FilePath);

				//remove the package refs nuget put in the file, project.json doesn't use those
				project.References.RemoveRange (project.References.Where (IsFromPackage).ToArray ());

				// Remove any imports from NuGet packages. These will be added by NuGet into the generated
				// ProjectName.nuget.props and ProjectName.nuget.targets files when using project.json.
				if (packages != null) {
					foreach (var import in project.MSBuildProject.Imports.ToArray ()) {
						if (packages.Any (p => import.Project.IndexOf (p, StringComparison.OrdinalIgnoreCase) >= 0)) {
							import.ParentObject.ParentProject.Remove (import);
						}
					}
				}
			}

			return projectJsonFile;
		}

		//HACK: we don't have the info to do this properly, really the package management addin should handle this
		static bool IsFromPackage (ProjectReference r)
		{
			if (r.ReferenceType != ReferenceType.Assembly) {
				return false;
			}
			var packagesDir = r.Project.ParentSolution.BaseDirectory.Combine ("packages");
			return r.GetReferencedFileNames(null).Any (f => ((FilePath)f).IsChildPathOf (packagesDir));
		}

		static string GetPclProfileFullName (TargetFrameworkMoniker tfm)
		{
			if (tfm.Identifier != TargetFrameworkMoniker.ID_PORTABLE) {
				return null;
			}

			//nuget only accepts pcls with a profile number
			if (tfm.Profile == null || !tfm.Profile.StartsWith ("Profile", StringComparison.Ordinal))
			{
				return null;
			}

			return tfm.ToString ();
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
