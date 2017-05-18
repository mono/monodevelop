
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

		public GtkNuGetPackageMetadataOptionsPanelWidget ()
		{
			this.Build ();

			PopulateLanguages ();

			SetupAccessibility ();
		}

		void SetupAccessibility ()
		{
			packageIdTextBox.SetCommonAccessibilityAttributes ("NuGetMetadata.ID",
			                                                   GettextCatalog.GetString ("ID"),
			                                                   GettextCatalog.GetString ("Enter the ID of the NuGet package"));
			packageIdTextBox.SetAccessibilityLabelRelationship (packageIdLabel);

			packageVersionTextBox.SetCommonAccessibilityAttributes ("NuGetMetadata.Version",
			                                                        GettextCatalog.GetString ("Version"),
			                                                        GettextCatalog.GetString ("Enter the version of the NuGet package"));
			packageVersionTextBox.SetAccessibilityLabelRelationship (packageVersionLabel);

			packageAuthorsTextBox.SetCommonAccessibilityAttributes ("NuGetMetadata.Authors",
			                                                        GettextCatalog.GetString ("Authors"),
			                                                        GettextCatalog.GetString ("Enter the authors of the NuGet package"));
			packageAuthorsTextBox.SetAccessibilityLabelRelationship (packageAuthorsLabel);

			packageDescriptionTextView.SetCommonAccessibilityAttributes ("NuGetMetadata.Description",
			                                                             GettextCatalog.GetString ("Description"),
			                                                             GettextCatalog.GetString ("Enter the description of the NuGet package"));
			packageDescriptionTextView.SetAccessibilityLabelRelationship (packageDescriptionLabel);

			packageOwnersTextBox.SetCommonAccessibilityAttributes ("NuGetMetadata.Owners",
			                                                       GettextCatalog.GetString ("Owners"),
			                                                       GettextCatalog.GetString ("Enter the owners of the NuGet package"));
			packageOwnersTextBox.SetAccessibilityLabelRelationship (packageOwnersLabel);

			packageCopyrightTextBox.SetCommonAccessibilityAttributes ("NuGetMetadata.Copyright",
			                                                          GettextCatalog.GetString ("Copyright"),
			                                                          GettextCatalog.GetString ("Enter the copyright statement for the NuGet package"));
			packageCopyrightTextBox.SetAccessibilityLabelRelationship (packageCopyrightLabel);

			packageTitleTextBox.SetCommonAccessibilityAttributes ("NuGetMetadata.Title",
			                                                      GettextCatalog.GetString ("Title"),
			                                                      GettextCatalog.GetString ("Enter the title of the NuGet package"));
			packageTitleTextBox.SetAccessibilityLabelRelationship (packageTitleLabel);

			packageSummaryTextBox.SetCommonAccessibilityAttributes ("NuGetMetadata.Summary",
			                                                        GettextCatalog.GetString ("Summary"),
			                                                        GettextCatalog.GetString ("Enter the summary for the NuGet package"));
			packageSummaryTextBox.SetAccessibilityLabelRelationship (packageSummaryLabel);

			packageProjectUrlTextBox.SetCommonAccessibilityAttributes ("NuGetMetadata.URL",
			                                                           GettextCatalog.GetString ("Project URL"),
			                                                           GettextCatalog.GetString ("Enter the project URL for the NuGet package"));
			packageProjectUrlTextBox.SetAccessibilityLabelRelationship (packageProjectUrlLabel);

			packageIconUrlTextBox.SetCommonAccessibilityAttributes ("NuGetMetadata.Icon",
			                                                        GettextCatalog.GetString ("Icon URL"),
			                                                        GettextCatalog.GetString ("Enter the URL for the NuGet package's icon"));
			packageIconUrlTextBox.SetAccessibilityLabelRelationship (packageIconUrlLabel);

			packageLicenseUrlTextBox.SetCommonAccessibilityAttributes ("NuGetMetadata.licence",
			                                                           GettextCatalog.GetString ("License URL"),
			                                                           GettextCatalog.GetString ("Enter the URL for the NuGet package's license"));
			packageLicenseUrlTextBox.SetAccessibilityLabelRelationship (packageLicenseUrlLabel);

			packageRequireLicenseAcceptanceCheckBox.SetCommonAccessibilityAttributes ("NuGetMetadata.Acceptance",
			                                                                          GettextCatalog.GetString ("Require License Acceptance"),
			                                                                          GettextCatalog.GetString ("Check to require the user to accept the NuGet package's license"));
			packageRequireLicenseAcceptanceCheckBox.SetAccessibilityLabelRelationship (packageRequireLicenseAcceptanceLabel);

			packageDevelopmentDependencyCheckBox.SetCommonAccessibilityAttributes ("NuGetMetadata.Development",
			                                                                       GettextCatalog.GetString ("Development Dependency"),
			                                                                       GettextCatalog.GetString ("Check to indicate that this is a development dependency"));
			packageDevelopmentDependencyCheckBox.SetAccessibilityLabelRelationship (packageDevelopmentDependencyLabel);

			packageTagsTextBox.SetCommonAccessibilityAttributes ("NuGetMetadata.Tags",
			                                                     GettextCatalog.GetString ("Tags"),
			                                                     GettextCatalog.GetString ("Enter the tags for this NuGet package"));
			packageTagsTextBox.SetAccessibilityLabelRelationship (packageTagsLabel);

			packageLanguageComboBox.SetCommonAccessibilityAttributes ("NuGetMetadata.Language",
			                                                          GettextCatalog.GetString ("Language"),
			                                                          GettextCatalog.GetString ("Select the language for this NuGet package"));
			packageLanguageComboBox.SetAccessibilityLabelRelationship (packageLanguageLabel);

			packageReleaseNotesTextView.SetCommonAccessibilityAttributes ("NuGetMetadata.ReleaseNotes",
			                                                              GettextCatalog.GetString ("Release Notes"),
			                                                              GettextCatalog.GetString ("Enter the release notes for this NuGet package"));
			packageReleaseNotesTextView.SetAccessibilityLabelRelationship (packageReleaseNotesLabel);
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
			packageLanguageComboBox.Entry.Text = GetTextBoxText (metadata.Language);
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
			metadata.Language = packageLanguageComboBox.Entry.Text;
			metadata.LicenseUrl = packageLicenseUrlTextBox.Text;
			metadata.Owners = packageOwnersTextBox.Text;
			metadata.ProjectUrl = packageProjectUrlTextBox.Text;
			metadata.ReleaseNotes = packageReleaseNotesTextView.Buffer.Text;
			metadata.RequireLicenseAcceptance = packageRequireLicenseAcceptanceCheckBox.Active;
			metadata.Summary = packageSummaryTextBox.Text;
			metadata.Tags = packageTagsTextBox.Text;
			metadata.Title = packageTitleTextBox.Text;
		}

		void PopulateLanguages ()
		{
			var languagesListStore = new ListStore (typeof (string));
			packageLanguageComboBox.Model = languagesListStore;

			List<string> languages = CultureInfo.GetCultures(CultureTypes.AllCultures)
				.Select (c => c.Name)
				.ToList ();

			languages.Sort ();

			foreach (string language in languages) {
				languagesListStore.AppendValues (language);
			}
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

