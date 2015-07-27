//
// NuGetController.cs
//
// Author:
//       Manish Sinha <manish.sinha@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc.
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
using MonoDevelop.Components.AutoTest;
using UserInterfaceTests;
using MonoDevelop.Components.Commands;
using NUnit.Framework;

namespace UserInterfaceTests
{
	public class NuGetPackageOptions
	{
		public string PackageName { get; set;}

		public string Version { get; set;}

		public bool IsPreRelease { get; set;}
	}

	public class NuGetController
	{
		static AutoTestClientSession Session {
			get { return TestService.Session; }
		}

		Action<string> takeScreenshot;

		bool isUpdate;

		readonly Func<AppQuery,AppQuery> nugetWindow;
		readonly Func<AppQuery,AppQuery> addPackageButton;
		readonly Func<AppQuery,AppQuery> updatePackageButton;
		readonly Func<AppQuery,AppQuery> resultList;
		readonly Func<AppQuery,AppQuery> includePreRelease;

		public static void AddPackage (NuGetPackageOptions packageOptions, Action<string> takeScreenshot = null)
		{
			AddUpdatePackage (packageOptions, takeScreenshot, false);
		}

		public static void UpdatePackage (NuGetPackageOptions packageOptions, Action<string> takeScreenshot = null)
		{
			AddUpdatePackage (packageOptions, takeScreenshot, true);
		}

		public static void UpdateAllNuGetPackages (Action<string> takeScreenshot = null)
		{
			Session.ExecuteCommand ("MonoDevelop.PackageManagement.Commands.UpdateAllPackagesInSolution");
			Ide.WaitForStatusMessage (new [] { "Updating packages in solution..." });
			Ide.WaitForPackageUpdate ();
			takeScreenshot ("All-NuGet-Packages-Updated");
		}

		static void AddUpdatePackage (NuGetPackageOptions packageOptions, Action<string> takeScreenshot, bool isUpdate)
		{
			var nuget = new NuGetController (takeScreenshot, isUpdate);
			nuget.Open ();
			nuget.EnterSearchText (packageOptions.PackageName, packageOptions.Version, packageOptions.IsPreRelease);
			nuget.SelectResultByPackageName (packageOptions.PackageName, packageOptions.Version);
			nuget.ClickAdd ();
			Ide.WaitForStatusMessage (new[] {
				string.Format ("{0} successfully {1}.", packageOptions.PackageName, isUpdate ? "updated": "added")
			});
			if (takeScreenshot != null)
				takeScreenshot ("NuGet-Operation-Finished");
		}

		public NuGetController (Action<string> takeScreenshot = null, bool isUpdate = false)
		{
			this.takeScreenshot = takeScreenshot ?? delegate { };
			this.isUpdate = isUpdate;

			nugetWindow = c => c.Window ().Marked ("Add Packages");
			addPackageButton = c => nugetWindow (c).Children ().Button ().Text ("Add Package");
			updatePackageButton = c => nugetWindow (c).Children ().Button ().Text ("Update Package");
			resultList = c => nugetWindow (c).Children ().TreeView ().Model ();
			includePreRelease = c => nugetWindow (c).Children ().CheckButton ().Text ("Show pre-release packages");
		}

		public void Open ()
		{
			Session.WaitForElement (IdeQuery.DefaultWorkbench);
			Session.ExecuteCommand ("MonoDevelop.PackageManagement.Commands.AddNuGetPackages", source: CommandSource.MainMenu);
			WaitForAddButton ();
			takeScreenshot ("NuGet-Dialog-Opened");
		}

		public void EnterSearchText (string packageName, string version = null, bool includePreReleasePackages = false)
		{
			TogglePreRelease (includePreReleasePackages);
			Assert.IsTrue ( Session.EnterText (c => c.Window ().Marked ("Add Packages").Children ().Textfield ().Marked ("search-entry"),
				packageName + (version != null ? " version: " + version.Trim () : string.Empty)));
			WaitForAddButton (true);
			takeScreenshot ("Search-term-entered");
		}

		public void TogglePreRelease (bool includePreReleasePackages)
		{
			Session.ToggleElement (includePreRelease, includePreReleasePackages);
		}

		public void SelectResultByIndex (int index)
		{
			Assert.IsTrue (Session.SelectElement (c => resultList (c).Children ().Index (index)));
		}

		public void SelectResultByPackageName (string packageName, string version = null)
		{
			for (int i = 0; i < Session.Query (c => resultList (c).Children ()).Length; i++) {
				Session.SelectElement (c => resultList (c).Children ().Index (i));
				takeScreenshot (string.Format ("Selected-Item-{0}", i));
				var found = Session.Query (c => nugetWindow (c).Children ().CheckType (typeof(Gtk.Label)).Text (packageName)).Length > 0;
				if (version != null) {
					found = found && (Session.Query (c => nugetWindow (c).Children ().CheckType (typeof(Gtk.Label)).Text (version)).Length > 0);
					if (found)
						return;
				}
			}
			takeScreenshot ("Package-Failed-To-Be-Found");
			Assert.Fail ("No package '{0}' with version: '{1}' found", packageName, version);
		}

		public void ClickAdd ()
		{
			WaitForAddButton (true);
			Assert.IsTrue (Session.ClickElement (isUpdate ? updatePackageButton : addPackageButton));
			Session.WaitForElement (IdeQuery.TextArea);
		}

		public void Close ()
		{
			Session.WaitForElement (nugetWindow);
			Assert.IsTrue (Session.ClickElement (c => nugetWindow (c).Children ().Button ().Text ("Close")));
		}

		void WaitForAddButton (bool? enabled = null)
		{
			if (enabled == null)
				Session.WaitForElement (addPackageButton);
			else
				Session.WaitForElement (c => (isUpdate? updatePackageButton(c) : addPackageButton (c)).Sensitivity (enabled.Value), 10000);
		}
	}
}

