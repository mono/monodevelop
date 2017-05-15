//
// DotNetCoreProjectTemplateWizardPage.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2017 Xamarin Inc. (http://xamarin.com)
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

using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Core;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Core.StringParsing;
using MonoDevelop.DotNetCore.Gui;
using MonoDevelop.Ide.Templates;

namespace MonoDevelop.DotNetCore.Templating
{
	class DotNetCoreProjectTemplateWizardPage : WizardPage
	{
		readonly DotNetCoreProjectTemplateWizard wizard;
		GtkDotNetCoreProjectTemplateWizardPageWidget view;
		List<TargetFramework> targetFrameworks;

		public DotNetCoreProjectTemplateWizardPage (DotNetCoreProjectTemplateWizard wizard)
		{
			this.wizard = wizard;
			GetTargetFrameworks ();
		}

		public override string Title {
			get {
				string templateName = wizard.Parameters ["TemplateName"];
				return GettextCatalog.GetString ("Configure your new {0}", templateName);
			}
		}

		protected override object CreateNativeWidget<T> ()
		{
			if (view == null)
				view = new GtkDotNetCoreProjectTemplateWizardPageWidget (this);

			return view;
		}

		protected override void Dispose (bool disposing)
		{
			if (view != null) {
				view.Dispose ();
				view = null;
			}
		}

		public bool ShowMultiplatformLibraryImage { get; private set; }

		void GetTargetFrameworks ()
		{
			if (wizard.IsSupportedParameter ("NetStandard")) {
				targetFrameworks = DotNetCoreProjectSupportedTargetFrameworks.GetNetStandardTargetFrameworks ().ToList ();

				// Use 1.x target frameworks by default if none are available from the .NET Core sdk.
				if (!targetFrameworks.Any ())
					targetFrameworks = DotNetCoreProjectSupportedTargetFrameworks.GetDefaultNetStandard1xTargetFrameworks ().ToList ();

				if (wizard.IsSupportedParameter ("FSharpNetStandard")) {
					RemoveUnsupportedNetStandardTargetFrameworksForFSharp (targetFrameworks);
				}
			} else {
				targetFrameworks = DotNetCoreProjectSupportedTargetFrameworks.GetNetCoreAppTargetFrameworks ().ToList ();

				if (wizard.IsSupportedParameter ("FSharpNetCoreLibrary")) {
					RemoveUnsupportedNetCoreAppTargetFrameworksForFSharpLibrary (targetFrameworks);
				}
			}
			SelectedTargetFrameworkIndex = 0;
		}

		/// <summary>
		/// The F# project templates do not .NET Standard below 1.6 so do not allow them to
		/// be selected.
		/// </summary>
		static void RemoveUnsupportedNetStandardTargetFrameworksForFSharp (List<TargetFramework> targetFrameworks)
		{
			targetFrameworks.RemoveAll (framework => framework.IsLowerThanNetStandard16 ());
		}

		/// <summary>
		/// FSharp class library project template does not support targeting 1.x versions so
		/// remove these.
		/// </summary>
		static void RemoveUnsupportedNetCoreAppTargetFrameworksForFSharpLibrary (List<TargetFramework> targetFrameworks)
		{
			targetFrameworks.RemoveAll (framework => framework.IsNetCoreApp1x ());
		}

		public IList<TargetFramework> TargetFrameworks {
			get { return targetFrameworks; }
		}

		int selectedTargetFrameworkIndex;

		public int SelectedTargetFrameworkIndex {
			get { return selectedTargetFrameworkIndex; }
			set {
				selectedTargetFrameworkIndex = value;
				UpdateTargetFrameworkParameters ();
			}
		}

		void UpdateTargetFrameworkParameters ()
		{
			TargetFramework framework = targetFrameworks [selectedTargetFrameworkIndex];
			wizard.Parameters ["Framework"] = framework.Id.GetShortFrameworkName ();

			wizard.Parameters ["UseNetCore20"] = framework.IsNetCoreApp20 ().ToString ();
			wizard.Parameters ["UseNetCore1x"] = framework.IsNetCoreApp1x ().ToString ();

			wizard.Parameters ["UseNetStandard20"] = framework.IsNetStandard20 ().ToString ();
			wizard.Parameters ["UseNetStandard1x"] = framework.IsNetStandard1x ().ToString ();
		}
	}
}
