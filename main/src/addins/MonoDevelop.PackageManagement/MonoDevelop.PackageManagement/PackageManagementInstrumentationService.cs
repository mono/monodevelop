//
// PackageManagementInstrumentationService.cs
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
using MonoDevelop.Core;
using MonoDevelop.Core.Instrumentation;
using NuGet.PackageManagement;
using NuGet.Packaging.Core;

namespace MonoDevelop.PackageManagement
{
	internal class PackageManagementInstrumentationService
	{
		static Counter<PackageMetadata> InstallPackageCounter = InstrumentationService.CreateCounter<PackageMetadata> ("Package Installed", "Package Management", id: "PackageManagement.Package.Installed");
		static Counter<PackageMetadata> UninstallPackageCounter = InstrumentationService.CreateCounter<PackageMetadata> ("Package Uninstalled", "Package Management", id: "PackageManagement.Package.Uninstalled");

		public virtual void IncrementInstallPackageCounter (PackageMetadata metadata)
		{
			InstallPackageCounter.Inc (1, null, metadata);
		}

		public virtual void IncrementUninstallPackageCounter (PackageMetadata metadata)
		{
			UninstallPackageCounter.Inc (1, null, metadata);
		}

		public void InstrumentPackageAction (IPackageAction action) 
		{
			try {
				var provider = action as INuGetProjectActionsProvider;
				if (provider != null) {
					InstrumentPackageActions (provider.GetNuGetProjectActions ());
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Instrumentation Failure in PackageManagement", ex);
			}
		}

		void InstrumentPackageActions (IEnumerable<NuGetProjectAction> actions)
		{
			foreach (NuGetProjectAction action in actions) {
				var metadata = new PackageMetadata {
					PackageId = action.PackageIdentity.Id
				};

				if (action.PackageIdentity.HasVersion) {
					metadata.Package = GetFullPackageInfo (action.PackageIdentity);
				}

				switch (action.NuGetProjectActionType) {
					case NuGetProjectActionType.Install:
						IncrementInstallPackageCounter (metadata);
					break;
					case NuGetProjectActionType.Uninstall:
						IncrementUninstallPackageCounter (metadata);
					break;
				}
			}
		}

		static string GetFullPackageInfo (PackageIdentity packageIdentity)
		{
			return string.Format ("{0} v{1}", packageIdentity.Id, packageIdentity.Version);
		}
	}

	internal class PackageMetadata : CounterMetadata
	{
		public PackageMetadata ()
		{
		}

		public string PackageId {
			get => GetProperty<string> ();
			set => SetProperty (value);
		}

		public string Package {
			get => GetProperty<string> ();
			set => SetProperty (value);
		}

		public bool HasPackageVersion {
			get => ContainsProperty ("PackageVersion");
		}

		public bool HasPackage {
			get => ContainsProperty ("Package");
		}
	}
}

