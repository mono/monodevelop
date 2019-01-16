
//
// GtkNuGetPackageMetadataOptionsPanelWidget.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
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
using System.Globalization;
using System.Linq;
using Gtk;
using MonoDevelop.Components.AtkCocoaHelper;
using MonoDevelop.Core;
using MonoDevelop.Projects;

namespace MonoDevelop.Packaging.Gui
{
	[System.ComponentModel.ToolboxItem (true)]
	public partial class GtkNuGetPackageMetadataOptionsPanelWidget : Gtk.Bin
	{
		NuGetPackageMetadata metadata;
		bool projectOriginallyHadMetadata;
		bool hasPackageId;
		List<CultureInfo> languages;
		ListStore languagesListStore;

		public GtkNuGetPackageMetadataOptionsPanelWidget ()
		{
			this.Build ();

			PopulateLanguages ();

			SetupAccessibility ();
		}

		void SetupAccessibility ()
		{
			packageDescriptionPaddingLabel.Accessible.Role = Atk.Role.Filler;
			packageReleaseNotesPaddingLabel.Accessible.Role = Atk.Role.Filler;

			packageIdTextBox.SetCommonAccessibilityAttributes ("NuGetMetadata.ID",
				packageIdLabel,
				GettextCatalog.GetString ("Enter the ID of the NuGet package"));

			packageVersionTextBox.SetCommonAccessibilityAttributes ("NuGetMetadata.Version",
				packageVersionLabel,
				GettextCatalog.GetString ("Enter the version of the NuGet package"));

			packageAuthorsTextBox.SetCommonAccessibilityAttributes ("NuGetMetadata.Authors",
				packageAuthorsLabel,
				GettextCatalog.GetString ("Enter the authors of the NuGet package"));

			packageDescriptionTextView.SetCommonAccessibilityAttributes ("NuGetMetadata.Description",
				packageDescriptionLabel,
				GettextCatalog.GetString ("Enter the description of the NuGet package"));

			packageOwnersTextBox.SetCommonAccessibilityAttributes ("NuGetMetadata.Owners",
				packageOwnersLabel,
				GettextCatalog.GetString ("Enter the owners of the NuGet package"));

			packageCopyrightTextBox.SetCommonAccessibilityAttributes ("NuGetMetadata.Copyright",
				packageCopyrightLabel,
				GettextCatalog.GetString ("Enter the copyright statement for the NuGet package"));

			packageTitleTextBox.SetCommonAccessibilityAttributes ("NuGetMetadata.Title",
				packageTitleLabel,
				GettextCatalog.GetString ("Enter the title of the NuGet package"));

			packageSummaryTextBox.SetCommonAccessibilityAttributes ("NuGetMetadata.Summary",
				packageSummaryLabel,
				GettextCatalog.GetString ("Enter the summary for the NuGet package"));

			packageProjectUrlTextBox.SetCommonAccessibilityAttributes ("NuGetMetadata.URL",
				packageProjectUrlLabel,
				GettextCatalog.GetString ("Enter the project URL for the NuGet package"));

			packageIconUrlTextBox.SetCommonAccessibilityAttributes ("NuGetMetadata.Icon",
				packageIconUrlLabel,
				GettextCatalog.GetString ("Enter the URL for the NuGet package's icon"));

			packageLicenseUrlTextBox.SetCommonAccessibilityAttributes ("NuGetMetadata.licence",
				packageLicenseUrlLabel,
				GettextCatalog.GetString ("Enter the URL for the NuGet package's license"));

			packageRequireLicenseAcceptanceCheckBox.SetCommonAccessibilityAttributes ("NuGetMetadata.Acceptance",
				packageRequireLicenseAcceptanceLabel,
				GettextCatalog.GetString ("Check to require the user to accept the NuGet package's license"));

			packageDevelopmentDependencyCheckBox.SetCommonAccessibilityAttributes ("NuGetMetadata.Development",
				packageDevelopmentDependencyLabel,
				GettextCatalog.GetString ("Check to indicate that this is a development dependency"));

			packageTagsTextBox.SetCommonAccessibilityAttributes ("NuGetMetadata.Tags",
				packageTagsLabel,
				GettextCatalog.GetString ("Enter the tags for this NuGet package"));

			packageLanguageComboBox.SetCommonAccessibilityAttributes ("NuGetMetadata.Language",
				packageLanguageLabel,
				GettextCatalog.GetString ("Select the language for this NuGet package"));

			packageReleaseNotesTextView.SetCommonAccessibilityAttributes ("NuGetMetadata.ReleaseNotes",
				packageReleaseNotesLabel,
				GettextCatalog.GetString ("Enter the release notes for this NuGet package"));
		}

		internal static System.Action<bool> OnProjectHasMetadataChanged;

		internal void Load (PackagingProject project)
		{
			metadata = project.GetPackageMetadata ();
			LoadMetadata ();
		}

		internal void Load (DotNetProject project)
		{
			metadata = new NuGetPackageMetadata ();
			metadata.Load (project);
			LoadMetadata ();

			projectOriginallyHadMetadata = ProjectHasMetadata ();
			hasPackageId = projectOriginallyHadMetadata;
			packageIdTextBox.Changed += PackageIdTextBoxChanged;
		}

		void LoadMetadata ()
		{
			packageIdTextBox.Text = GetTextBoxText (metadata.Id);
			packageVersionTextBox.Text = GetTextBoxText (metadata.Version);
			packageAuthorsTextBox.Text = GetTextBoxText (metadata.Authors);
			packageDescriptionTextView.Buffer.Text = GetTextBoxText (metadata.Description);

			packageCopyrightTextBox.Text = GetTextBoxText (metadata.Copyright);
			packageDevelopmentDependencyCheckBox.Active = metadata.DevelopmentDependency;
			packageIconUrlTextBox.Text = GetTextBoxText (metadata.IconUrl);
			LoadLanguage (metadata.Language);
			packageLicenseUrlTextBox.Text = GetTextBoxText (metadata.LicenseUrl);
			packageOwnersTextBox.Text = GetTextBoxText (metadata.Owners);
			packageProjectUrlTextBox.Text = GetTextBoxText (metadata.ProjectUrl);
			packageReleaseNotesTextView.Buffer.Text = GetTextBoxText (metadata.ReleaseNotes);
			packageRequireLicenseAcceptanceCheckBox.Active = metadata.RequireLicenseAcceptance;
			packageSummaryTextBox.Text = GetTextBoxText (metadata.Summary);
			packageTagsTextBox.Text = GetTextBoxText (metadata.Tags);
			packageTitleTextBox.Text = GetTextBoxText (metadata.Title);
		}

		static string GetTextBoxText (string text)
		{
			return text ?? string.Empty;
		}

		void LoadLanguage (string language)
		{
			if (string.IsNullOrEmpty (language)) {
				packageLanguageComboBox.Active = 0;
				return;
			}

			int index = GetLanguageIndex (language);
			if (index >= 0) {
				packageLanguageComboBox.Active = index + 1;
				return;
			}

			// Language does not match so we need to add it to the combo box.
			TreeIter iter = languagesListStore.AppendValues (language);
			packageLanguageComboBox.SetActiveIter (iter);
		}

		int GetLanguageIndex (string language)
		{
			for (int i = 0; i < languages.Count; ++i) {
				CultureInfo culture = languages [i];
				if (string.Equals (culture.Name, language, StringComparison.OrdinalIgnoreCase)) {
					return i;
				}
			}
			return -1;
		}

		internal void Save (PackagingProject project)
		{
			UpdateMetadata ();
			project.UpdatePackageMetadata (metadata);
		}

		internal void Save (DotNetProject project)
		{
			UpdateMetadata ();
			metadata.UpdateProject (project);

			if (!projectOriginallyHadMetadata && ProjectHasMetadata ()) {
				project.ReloadProjectBuilder ();

				EnsureBuildPackagingNuGetPackageIsInstalled (project);
			}
		}

		void EnsureBuildPackagingNuGetPackageIsInstalled (DotNetProject project)
		{
			if (!project.IsBuildPackagingNuGetPackageInstalled ()) {
				var extension = project.GetFlavor<DotNetProjectPackagingExtension> ();
				extension.InstallBuildPackagingNuGetAfterWrite = true;
			}
		}

		void UpdateMetadata ()
		{
			metadata.Id = packageIdTextBox.Text;
			metadata.Version = packageVersionTextBox.Text;
			metadata.Authors = packageAuthorsTextBox.Text;
			metadata.Description = packageDescriptionTextView.Buffer.Text;

			metadata.Copyright = packageCopyrightTextBox.Text;
			metadata.DevelopmentDependency = packageDevelopmentDependencyCheckBox.Active;
			metadata.IconUrl = packageIconUrlTextBox.Text;
			metadata.Language = GetSelectedLanguage ();
			metadata.LicenseUrl = packageLicenseUrlTextBox.Text;
			metadata.Owners = packageOwnersTextBox.Text;
			metadata.ProjectUrl = packageProjectUrlTextBox.Text;
			metadata.ReleaseNotes = packageReleaseNotesTextView.Buffer.Text;
			metadata.RequireLicenseAcceptance = packageRequireLicenseAcceptanceCheckBox.Active;
			metadata.Summary = packageSummaryTextBox.Text;
			metadata.Tags = packageTagsTextBox.Text;
			metadata.Title = packageTitleTextBox.Text;
		}

		string GetSelectedLanguage ()
		{
			if (packageLanguageComboBox.Active == 0) {
				// 'None' selected.
				return string.Empty;
			}

			int languageIndex = packageLanguageComboBox.Active - 1;
			if (languageIndex < languages.Count) {
				return languages [languageIndex].Name;
			}

			// No match for language so just return the combo box text.
			return packageLanguageComboBox.ActiveText;
		}

		void PopulateLanguages ()
		{
			languagesListStore = new ListStore (typeof (string));
			packageLanguageComboBox.Model = languagesListStore;

			languages = new List<CultureInfo> ();

			foreach (CultureInfo culture in CultureInfo.GetCultures (CultureTypes.AllCultures)) {
				if (!string.IsNullOrEmpty (culture.Name)) {
					languages.Add (culture);
				}
			}

			languages.Sort (CompareLanguages);

			languagesListStore.AppendValues (GettextCatalog.GetString ("None"));

			foreach (CultureInfo language in languages) {
				languagesListStore.AppendValues (language.DisplayName);
			}

			packageLanguageComboBox.Active = 0;
		}

		static int CompareLanguages (CultureInfo x, CultureInfo y)
		{
			return string.Compare (x.DisplayName, y.DisplayName, StringComparison.CurrentCulture);
		}

		bool ProjectHasMetadata ()
		{
			return !string.IsNullOrEmpty (metadata.Id);
		}

		void PackageIdTextBoxChanged (object sender, EventArgs e)
		{
			bool anyPackageIdText = !string.IsNullOrEmpty (packageIdTextBox.Text);

			if (anyPackageIdText != hasPackageId) {
				hasPackageId = anyPackageIdText;
				OnProjectHasMetadataChanged?.Invoke (hasPackageId);
			}
		}
	}
}

