//
// GtkReferenceAssembliesOptionsPanelWidget.cs
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
using System.Linq;
using Gtk;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Core.Assemblies;

namespace MonoDevelop.Packaging.Gui
{
	partial class GtkReferenceAssembliesOptionsPanelWidget
	{
		List<PortableProfileViewModel> pclProfiles;

		public GtkReferenceAssembliesOptionsPanelWidget ()
		{
			Build ();
		}

		internal void Load (PackagingProject project)
		{
			AddPortableClassLibraryProfiles (project);
		}

		internal void Save (PackagingProject project)
		{
			var selectedProfiles = pclProfiles
				.Where (profile => profile.IsEnabled)
				.Select (profile => profile.Framework.Id);
			
			project.UpdateReferenceAssemblyFrameworks (selectedProfiles);
		}

		void AddPortableClassLibraryProfiles (PackagingProject project)
		{
			var targetFrameworks = GetPortableTargetFrameworks (project).ToList ();
			targetFrameworks.Sort (CompareFrameworks);

			var selectedTargetFrameworks = project.GetReferenceAssemblyFrameworks ().ToList ();
			pclProfiles = targetFrameworks.Select (fx => CreatePortableProfileViewModel (fx, selectedTargetFrameworks)).ToList ();

			foreach (PortableProfileViewModel profile in pclProfiles) {
				pclProfilesStore.AppendValues (profile.IsEnabled, profile.ProfileName, profile.GetProfileDescription (), profile);
			}
		}

		static IEnumerable<TargetFramework> GetPortableTargetFrameworks (PackagingProject project)
		{
			return Runtime.SystemAssemblyService.GetTargetFrameworks ().Where (fx =>
				fx.Id.Identifier == ".NETPortable" &&
				!string.IsNullOrEmpty (fx.Id.Profile) &&
				project.TargetRuntime.IsInstalled (fx));
		}

		PortableProfileViewModel CreatePortableProfileViewModel (
			TargetFramework fx,
			IEnumerable<TargetFrameworkMoniker> selectedTargetFrameworks)
		{
			bool enabled = selectedTargetFrameworks.Contains (fx.Id);
			return new PortableProfileViewModel (fx, enabled);
		}

		static int CompareFrameworks (TargetFramework x, TargetFramework y)
		{
			var p = CompareProfiles (x.Id.Profile, y.Id.Profile);
			if (p != 0)
				return p;
			return string.Compare (x.Id.Version, y.Id.Version, StringComparison.Ordinal);
		}

		static int CompareProfiles (string x, string y)
		{
			int xn, yn;
			if (TryParseProfileID (x, out xn)) {
				if (TryParseProfileID (y, out yn))
					return xn.CompareTo (yn);
				return 1;
			}
			if (TryParseProfileID (y, out yn))
				return -1;
			return string.Compare (x, y, StringComparison.Ordinal);
		}

		static bool TryParseProfileID (string profile, out int id)
		{
			if (profile.StartsWith ("Profile", StringComparison.Ordinal))
				return int.TryParse (profile.Substring ("Profile".Length), out id);
			id = -1;
			return false;
		}

		void PortableProfileCheckBoxToggled (object o, ToggledArgs args)
		{
			TreeIter iter;
			pclProfilesStore.GetIterFromString (out iter, args.Path);
			var viewModel = pclProfilesStore.GetValue (iter, 3) as PortableProfileViewModel;
			viewModel.IsEnabled = !viewModel.IsEnabled;
			pclProfilesStore.SetValue (iter, IsEnabledCheckBoxColumn, viewModel.IsEnabled);
		}

		class PortableProfileViewModel
		{
			public PortableProfileViewModel (TargetFramework framework, bool enabled)
			{
				Framework = framework;
				IsEnabled = enabled;
			}

			public bool IsEnabled { get; set; }
			public TargetFramework Framework { get; set; }

			public string ProfileName {
				get { return Framework.Id.Profile; }
			}

			public string GetProfileDescription ()
			{
				return GetSupportedFrameworksDisplayName ();
			}

			string GetSupportedFrameworksDisplayName ()
			{
				int openingBracket = Framework.Name.IndexOf ('(');
				int closingBracket = Framework.Name.LastIndexOf (')');
				if (openingBracket != -1 && closingBracket != -1 && openingBracket < closingBracket) {
					return Framework.Name.Substring (openingBracket + 1, closingBracket - openingBracket - 1);
				}

				return Framework.Name;
			}
		}
	}
}
