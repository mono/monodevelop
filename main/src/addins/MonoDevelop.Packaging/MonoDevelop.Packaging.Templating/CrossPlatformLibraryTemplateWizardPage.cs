//
// CrossPlatformLibraryTemplateWizardPage.cs
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

namespace MonoDevelop.Packaging.Templating
{
	class CrossPlatformLibraryTemplateWizardPage : WizardPage
	{
		readonly string title = GettextCatalog.GetString ("Configure your Multiplatform Library");
		readonly CrossPlatformLibraryTemplateWizard wizard;
		GtkCrossPlatformLibraryProjectTemplateWizardPageWidget view;
		string libraryName = string.Empty;
		string description = string.Empty;
		bool isAndroidChecked;
		bool isIOSChecked;
		bool isSharedProjectSelected;
		bool isPortableClassLibrarySelected;
		bool createNuGetProject;

		public CrossPlatformLibraryTemplateWizardPage (CrossPlatformLibraryTemplateWizard wizard)
		{
			this.wizard = wizard;

			wizard.Parameters["PackageAuthors"] = AuthorInformation.Default.Name;
			wizard.Parameters["PackageVersion"] = "1.0.0";

			IsAndroidEnabled = Services.ProjectService.CanCreateProject ("C#", "MonoDroid");
			IsIOSEnabled = Services.ProjectService.CanCreateProject ("C#", "XamarinIOS");

			IsAndroidChecked = true;
			IsIOSChecked = true;
			IsSharedProjectSelected = true;
			IsPortableClassLibrarySelected = false;
			CreateNuGetProject = true;
		}

		public override string Title {
			get { return title; }
		}

		protected override object CreateNativeWidget<T> ()
		{
			if (view == null)
				view = new GtkCrossPlatformLibraryProjectTemplateWizardPageWidget (this);

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
			CanMoveToNextPage = IsValidLibraryName () &&
				!string.IsNullOrEmpty (description) &&
				ValidPlatformsSelected ();
		}

		bool IsValidLibraryName ()
		{
			LibraryNameError = null;

			if (string.IsNullOrEmpty (libraryName)) {
				return false;
			} else if (libraryName.Length > NuGetPackageMetadata.MaxPackageIdLength) {
				LibraryNameError = GettextCatalog.GetString ("Library name must not exceed 100 characters.");
				return false;
			} else if (!PackageIdValidator.IsValidPackageId (libraryName)) {
				LibraryNameError = GettextCatalog.GetString ("The library name contains invalid characters. Examples of valid library names include 'MyPackage' and 'MyPackage.Sample'.");
				return false;
			}

			return true;
		}

		bool ValidPlatformsSelected ()
		{
			if (isSharedProjectSelected) {
				return isAndroidChecked || isIOSChecked;
			}

			return true;
		}

		public string LibraryName {
			get { return libraryName; }
			set {
				libraryName = value.Trim ();
				wizard.Parameters ["PackageId"] = libraryName;
				wizard.Parameters ["ProjectName"] = NewProjectConfiguration.GenerateValidProjectName (libraryName);
				UpdateCanMoveNext ();
			}
		}

		public string LibraryNameError { get; private set; }

		public bool HasLibraryNameError ()
		{
			return !string.IsNullOrEmpty (LibraryNameError);
		}

		public string Description {
			get { return description; }
			set {
				description = value.Trim ();
				wizard.Parameters ["PackageDescription"] = description;
				UpdateCanMoveNext ();
			}
		}

		public bool IsIOSEnabled { get; private set; }

		public bool IsIOSChecked {
			get { return isIOSChecked; }
			set {
				isIOSChecked = value && IsIOSEnabled;
				UpdateCreateIOSProjectParameter ();
				UpdateCanMoveNext ();
			}
		}

		void UpdateCreateIOSProjectParameter ()
		{
			wizard.Parameters ["CreateIOSProject"] = (isIOSChecked && !isPortableClassLibrarySelected).ToString ();
		}

		public bool IsAndroidEnabled { get; private set; }

		public bool IsAndroidChecked {
			get { return isAndroidChecked; }
			set {
				isAndroidChecked = value && IsAndroidEnabled;
				UpdateCreateAndroidProjectParameter ();
				UpdateCanMoveNext ();
			}
		}

		void UpdateCreateAndroidProjectParameter ()
		{
			wizard.Parameters ["CreateAndroidProject"] = (isAndroidChecked && !isPortableClassLibrarySelected).ToString ();
		}

		public bool IsPortableClassLibrarySelected {
			get { return isPortableClassLibrarySelected; }
			set {
				isPortableClassLibrarySelected = value;
				CreateNuGetProject = !isPortableClassLibrarySelected;
				isSharedProjectSelected = !isPortableClassLibrarySelected;
				UpdateTemplateParameters ();
				UpdateCanMoveNext ();
			}
		}

		public bool IsSharedProjectSelected {
			get { return isSharedProjectSelected; }
			set {
				isSharedProjectSelected = value;
				isPortableClassLibrarySelected = !isSharedProjectSelected;
				UpdateTemplateParameters ();
				UpdateCanMoveNext ();
			}
		}

		void UpdateTemplateParameters ()
		{
			wizard.Parameters ["CreateSharedProject"] = isSharedProjectSelected.ToString ();
			wizard.Parameters ["CreatePortableProject"] = isPortableClassLibrarySelected.ToString ();

			UpdateCreateAndroidProjectParameter ();
			UpdateCreateIOSProjectParameter ();
		}

		bool CreateNuGetProject {
			get { return createNuGetProject; }
			set {
				createNuGetProject = value;
				wizard.Parameters ["CreateNuGetProject"] = createNuGetProject.ToString ();
			}
		}
	}
}
