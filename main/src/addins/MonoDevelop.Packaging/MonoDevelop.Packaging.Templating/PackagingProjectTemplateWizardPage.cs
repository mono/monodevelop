//
// PackagingProjectTemplateWizardPage.cs
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

using MonoDevelop.Core;
using MonoDevelop.Ide.Projects;
using MonoDevelop.Ide.Templates;
using MonoDevelop.Packaging.Gui;
using MonoDevelop.Projects;
using NuGet.Packaging;
using NuGet.Versioning;

namespace MonoDevelop.Packaging.Templating
{
	class PackagingProjectTemplateWizardPage : WizardPage
	{
		readonly string title = GettextCatalog.GetString ("Configure your NuGet package");
		readonly PackagingProjectTemplateWizard wizard;
		GtkPackagingProjectTemplateWizardPageWidget view;
		string id = string.Empty;
		string version = string.Empty;
		string description = string.Empty;
		string authors = string.Empty;
		bool validId;
		bool validVersion;

		public PackagingProjectTemplateWizardPage (PackagingProjectTemplateWizard wizard)
		{
			this.wizard = wizard;

			Authors = AuthorInformation.Default.Name;
			Version = "1.0.0";
		}

		public override string Title {
			get { return title; }
		}

		protected override object CreateNativeWidget<T> ()
		{
			if (view == null)
				view = new GtkPackagingProjectTemplateWizardPageWidget (this);

			return view;
		}

		protected override void Dispose (bool disposing)
		{
			if (view != null) {
				view.Dispose ();
				view = null;
			}
		}

		void UpdateCanMoveNext ()
		{
			CanMoveToNextPage = validId &&
				validVersion &&
				!string.IsNullOrEmpty (description) &&
				!string.IsNullOrEmpty (authors);
		}

		bool IsValidId ()
		{
			IdError = null;

			if (string.IsNullOrEmpty (id)) {
				return false;
			} else if (id.Length > NuGetPackageMetadata.MaxPackageIdLength) {
				IdError = GettextCatalog.GetString ("The package id must not exceed 100 characters.");
				return false;
			} else if (!PackageIdValidator.IsValidPackageId (id)) {
				IdError = GettextCatalog.GetString ("The package id contains invalid characters. Examples of valid package ids include 'MyPackage' and 'MyPackage.Sample'.");
				return false;
			}

			return true;
		}

		bool IsValidVersion ()
		{
			VersionError = null;

			if (string.IsNullOrEmpty (version)) {
				return false;
			} else {
				NuGetVersion nugetVersion = null;
				if (!NuGetVersion.TryParse (version, out nugetVersion)) {
					VersionError = GettextCatalog.GetString ("The package version contains invalid characters. Examples of valid version include '1.0.0' and '1.2.3-beta1'.");
					return false;
				}
			}

			return true;
		}

		public string Id {
			get { return id; }
			set {
				id = value.Trim ();
				wizard.Parameters ["PackageId"] = id;
				wizard.Parameters ["ProjectName"] = NewProjectConfiguration.GenerateValidProjectName (id);
				validId = IsValidId ();
				UpdateCanMoveNext ();
			}
		}

		public string IdError { get; private set; }

		public bool HasIdError ()
		{
			return !string.IsNullOrEmpty (IdError);
		}

		public string Version {
			get { return version; }
			set {
				version = value.Trim ();
				wizard.Parameters ["PackageVersion"] = version;
				validVersion = IsValidVersion ();
				UpdateCanMoveNext ();
			}
		}

		public string VersionError { get; private set; }

		public bool HasVersionError ()
		{
			return !string.IsNullOrEmpty (VersionError);
		}

		public string Description {
			get { return description; }
			set {
				description = value.Trim ();
				wizard.Parameters ["PackageDescription"] = description;
				UpdateCanMoveNext ();
			}
		}

		public string Authors {
			get { return authors; }
			set {
				authors = value.Trim ();
				wizard.Parameters ["PackageAuthors"] = authors;
				UpdateCanMoveNext ();
			}
		}
	}
}
