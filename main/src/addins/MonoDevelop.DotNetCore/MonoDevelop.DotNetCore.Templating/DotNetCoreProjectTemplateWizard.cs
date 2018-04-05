//
// DotNetCoreProjectTemplateWizard.cs
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

using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Ide.Templates;

namespace MonoDevelop.DotNetCore.Templating
{
	class DotNetCoreProjectTemplateWizard : TemplateWizard
	{
		List<TargetFramework> targetFrameworks;

		public override WizardPage GetPage (int pageNumber)
		{
			var page = new DotNetCoreProjectTemplateWizardPage (this, targetFrameworks);
			targetFrameworks = null;
			return page;
		}

		public override int TotalPages {
			get { return GetTotalPages (); }
		}

		public override string Id {
			get { return "MonoDevelop.DotNetCore.ProjectTemplateWizard"; }
		}

		internal IList<TargetFramework> TargetFrameworks {
			get { return targetFrameworks; }
		}

		/// <summary>
		/// When only .NET Core 2.0 is installed there is only one option in the drop down
		/// list for the target framework for .NET Core projects so there is no point in displaying
		/// the wizard since nothing can be changed. If .NET Core 1.0 is installed then there is at
		/// least two options available. If the .NET Standard project template is selected then there
		/// are multiple options available. So here a check is made to see if more than one target
		/// framework is available. If not then the wizard will not be displayed.
		/// </summary>
		int GetTotalPages ()
		{
			GetTargetFrameworks ();
			if (targetFrameworks.Count > 1)
				return 1;

			ConfigureDefaultParameters ();

			return 0;
		}

		void GetTargetFrameworks ()
		{
			if (IsSupportedParameter ("NetStandard")) {
				targetFrameworks = DotNetCoreProjectSupportedTargetFrameworks.GetNetStandardTargetFrameworks ().ToList ();

				// Use 1.x target frameworks by default if none are available from the .NET Core sdk.
				if (!targetFrameworks.Any ())
					targetFrameworks = DotNetCoreProjectSupportedTargetFrameworks.GetDefaultNetStandard1xTargetFrameworks ().ToList ();

				if (IsSupportedParameter ("FSharpNetStandard")) {
					RemoveUnsupportedNetStandardTargetFrameworksForFSharp (targetFrameworks);
				}
			} else {
				targetFrameworks = DotNetCoreProjectSupportedTargetFrameworks.GetNetCoreAppTargetFrameworks ().ToList ();

				if (!SupportsNetCore1x ()) {
					RemoveUnsupportedNetCoreApp1xTargetFrameworks (targetFrameworks);
				}
			}
		}

		/// <summary>
		/// The F# project templates do not support .NET Standard below 1.6 so do not allow them to
		/// be selected.
		/// </summary>
		static void RemoveUnsupportedNetStandardTargetFrameworksForFSharp (List<TargetFramework> targetFrameworks)
		{
			targetFrameworks.RemoveAll (framework => framework.IsLowerThanNetStandard16 ());
		}

		/// <summary>
		/// FSharp class library project template and the Razor Pages project template do not support
		/// targeting 1.x versions so remove these frameworks.
		/// </summary>
		static void RemoveUnsupportedNetCoreApp1xTargetFrameworks (List<TargetFramework> targetFrameworks)
		{
			targetFrameworks.RemoveAll (framework => framework.IsNetCoreApp1x ());
		}

		/// <summary>
		/// Set default parameter values if no wizard will be displayed.
		/// </summary>
		void ConfigureDefaultParameters ()
		{
			if (IsSupportedParameter ("NetStandard")) {
				var highestFramework = DotNetCoreProjectSupportedTargetFrameworks.GetNetStandardTargetFrameworks ().FirstOrDefault ();

				if (highestFramework != null && highestFramework.IsNetStandard20 ()) {
					Parameters ["UseNetStandard20"] = "true";
				} else {
					Parameters ["UseNetStandard1x"] = "true";
				}
			} else {
				if (!SupportsNetCore1x ()) {
					var highestFramework = DotNetCoreProjectSupportedTargetFrameworks.GetNetCoreAppTargetFrameworks ().FirstOrDefault ();
					if (highestFramework != null && highestFramework.IsNetCoreApp21 ()) {
						Parameters ["UseNetCore21"] = "true";
					} else {
						Parameters ["UseNetCore20"] = "true";
					}
				} else {
					var highestFramework = DotNetCoreProjectSupportedTargetFrameworks.GetNetCoreAppTargetFrameworks ().FirstOrDefault ();
					if (highestFramework != null) {
						if (highestFramework.IsNetCoreApp21 ()) {
							Parameters ["UseNetCore21"] = "true";
						} else if (highestFramework.IsNetCoreApp20 ()) {
							Parameters ["UseNetCore20"] = "true";
						} else {
							Parameters ["UseNetCore1x"] = "true";
						}
					} else {
						Parameters ["UseNetCore1x"] = "true";
					}
				}
				ConfigureDefaultNetCoreAppFramework ();
			}
		}

		/// <summary>
		/// Framework needs to be specified for .NET Core library projects if only one runtime/sdk
		/// is available. Otherwise .NETStandard will be used for the target framework of the project.
		/// </summary>
		void ConfigureDefaultNetCoreAppFramework ()
		{
			if (!IsSupportedParameter ("NetCoreLibrary"))
				return;

			var highestFramework = DotNetCoreProjectSupportedTargetFrameworks.GetNetCoreAppTargetFrameworks ().FirstOrDefault ();
			if (highestFramework != null) {
				Parameters ["framework"] = highestFramework.Id.GetShortFrameworkName ();
			} else {
				Parameters ["framework"] = "netcoreapp1.1";
			}
		}

		bool SupportsNetCore1x ()
		{
			bool supportsNetCore20Only = IsSupportedParameter ("FSharpNetCoreLibrary") ||
				IsSupportedParameter ("RazorPages") ||
				IsSupportedParameter ("FSharpWebApi");

			return !supportsNetCore20Only;
		}
	}
}
