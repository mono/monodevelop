//
// NewProjectDialogController.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
//

using System.Collections.Generic;
using MonoDevelop.Ide.Templates;

namespace MonoDevelop.Ide.Projects
{
	/// <summary>
	/// To be renamed to NewProjectDialog
	/// </summary>
	public class NewProjectDialogController : INewProjectDialogController
	{
		List<TemplateCategory> templateCategories;

		public NewProjectDialogController ()
		{
			LoadTemplateCategories ();
		}

		public void Show ()
		{
			INewProjectDialogBackend dialog = CreateNewProjectDialog ();
			dialog.RegisterController (this);
			dialog.ShowDialog ();
		}

		INewProjectDialogBackend CreateNewProjectDialog ()
		{
			return new GtkNewProjectDialogBackend ();
		}

		public IEnumerable<TemplateCategory> TemplateCategories {
			get { return templateCategories; }
		}

		void LoadTemplateCategories ()
		{
			templateCategories = new List<TemplateCategory> ();

			var generalCategory = new TemplateCategory ("general", "General", null);
			var template = new SolutionTemplate ("blank-app-portable", "Blank App (Xamarin.Forms Portable)", "md-project") { // FIXME: VV: Retina
				Description = "Blank App (Xamarin.Forms Portable). More text and some more. Blah, blah, blah, blah, more text that should wrap. More and more. More and even more",
				LargeImageId = "template-default-background-light.png",
				Wizard = "Xamarin.Forms.Template.Wizard"
			};
			generalCategory.AddTemplate (template);
			template = new SolutionTemplate ("blank-app-shared", "Blank App (Xamarin.Forms Shared)", "md-project") { // FIXME: VV: Retina
				Description = "Blank App (Xamarin.Forms Shared)",
				LargeImageId = "template-default-background-light.png"
			};
			generalCategory.AddTemplate (template);

			var appCategory = new TemplateCategory ("app", "App", null);
			appCategory.AddCategory (generalCategory);

			var libraryCategory = new TemplateCategory ("library", "Library", null);
			var testsCategory = new TemplateCategory ("test", "Tests", null);

			var crossPlatformCategory = new TemplateCategory ("CrossPlat", "Cross-platform", "md-platform-cross-platform"); // FIXME: VV: Retina
			crossPlatformCategory.AddCategory (appCategory);
			crossPlatformCategory.AddCategory (libraryCategory);
			crossPlatformCategory.AddCategory (testsCategory);
			templateCategories.Add (crossPlatformCategory);

			var iosCategory = new TemplateCategory ("ios", "iOS", "md-platform-ios"); // FIXME: VV: Retina
			iosCategory.AddCategory (appCategory);
			iosCategory.AddCategory (libraryCategory);
			iosCategory.AddCategory (testsCategory);
			templateCategories.Add (iosCategory);

			var androidCategory = new TemplateCategory ("android", "Android", "md-platform-android"); // FIXME: VV: Retina
			androidCategory.AddCategory (appCategory);
			androidCategory.AddCategory (libraryCategory);
			androidCategory.AddCategory (testsCategory);
			templateCategories.Add (androidCategory);

			var osxCategory = new TemplateCategory ("osx", "OS X", "md-platform-mac"); // FIXME: VV: Retina
			osxCategory.AddCategory (appCategory);
			osxCategory.AddCategory (libraryCategory);
			templateCategories.Add (osxCategory);

			var aspNetCategory = new TemplateCategory ("aspnet", "ASP.NET", null);
			var miscCategory = new TemplateCategory ("misc", "Miscellaneous", null);
			var otherCategory = new TemplateCategory ("other", "Other", "md-platform-other"); // FIXME: VV: Retina
			otherCategory.AddCategory (aspNetCategory);
			otherCategory.AddCategory (miscCategory);
			templateCategories.Add (otherCategory);
		}

		public TemplateWizard CreateTemplateWizard (string id)
		{
			if (id == "Xamarin.Forms.Template.Wizard") {
			//	return new XamarinFormsTemplateWizard ();
			}
			return null;
		}
	}
}

