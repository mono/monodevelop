// 
// RegisteredPackageSourceSettings.cs
// 
// Author:
//   Matt Ward <ward.matt@gmail.com>
// 
// Copyright (C) 2012 Matthew Ward
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

using MonoDevelop.Core;
using MonoDevelop.PackageManagement;
using NuGet;

namespace ICSharpCode.PackageManagement
{
	public class RegisteredPackageSourceSettings
	{
		public static readonly string PackageSourcesSectionName = "packageSources";
		public static readonly string ActivePackageSourceSectionName = "activePackageSource";
		public static readonly string DisabledPackageSourceSectionName = "disabledPackageSources";

		public static readonly PackageSource AggregatePackageSource = 
			new PackageSource("(Aggregate source)", "All");

		ISettings settings;
		ISettingsProvider settingsProvider;
		IPackageSourceProvider packageSourceProvider;
		PackageSource defaultPackageSource;
		RegisteredPackageSources packageSources;
		PackageSource activePackageSource;
		
		public RegisteredPackageSourceSettings (ISettingsProvider settingsProvider)
			: this(
				settingsProvider,
				RegisteredPackageSources.DefaultPackageSource)
		{
		}
		
		public RegisteredPackageSourceSettings (
			ISettingsProvider settingsProvider,
			PackageSource defaultPackageSource)
		{
			this.settingsProvider = settingsProvider;
			this.defaultPackageSource = defaultPackageSource;

			this.settings = settingsProvider.LoadSettings ();
			this.packageSourceProvider = CreatePackageSourceProvider (settings);

			ReadActivePackageSource();
			RegisterSolutionEvents ();
		}

		void RegisterSolutionEvents ()
		{
			settingsProvider.SettingsChanged += SettingsChanged;
		}

		IPackageSourceProvider CreatePackageSourceProvider (ISettings settings)
		{
			return new PackageSourceProvider (settings, new [] { RegisteredPackageSources.DefaultPackageSource });
		}

		void ReadActivePackageSource()
		{
			IList<SettingValue> packageSources = settings.GetValues(ActivePackageSourceSectionName, false);
			activePackageSource = PackageSourceConverter.ConvertFromFirstSetting(packageSources);
		}

		public RegisteredPackageSources PackageSources {
			get {
				if (packageSources == null) {
					TryReadPackageSources();
				}
				return packageSources;
			}
		}

		void TryReadPackageSources()
		{
			try {
				ReadPackageSources ();
			} catch (Exception ex) {
				LoggingService.LogError ("Unable to read NuGet.config file.", ex);

				// Fallback to using the default package source only (nuget.org)
				// and treat NuGet.config as read-only.
				packageSourceProvider = CreatePackageSourceProvider (NullSettings.Instance);
				ReadPackageSources ();
			}
		}
		
		void ReadPackageSources()
		{
			IEnumerable<PackageSource> savedPackageSources = packageSourceProvider.LoadPackageSources ();
			packageSources = new RegisteredPackageSources(savedPackageSources, defaultPackageSource);
			packageSources.CollectionChanged += PackageSourcesChanged;
			
			if (!savedPackageSources.Any()) {
				UpdatePackageSourceSettingsWithChanges();
			}
		}
		
		void PackageSourcesChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			UpdatePackageSourceSettingsWithChanges();
		}
		
		void UpdatePackageSourceSettingsWithChanges()
		{
			packageSourceProvider.SavePackageSources (packageSources);
		}

		public PackageSource ActivePackageSource {
			get {
				if (activePackageSource != null) {
					if (activePackageSource.IsAggregate()) {
						return activePackageSource;
					}
					if (PackageSources.Contains(activePackageSource)) {
						return activePackageSource;
					}
				}
				return null;
			}
			set {
				activePackageSource = value;

				if (settings is NullSettings) {
					// NuGet failed to load settings so do not try to update them since this will fail.
					return;
				}

				if (activePackageSource == null) {
					RemoveActivePackageSourceSetting();
				} else {
					UpdateActivePackageSourceSetting();
				}
			}
		}
		
		void RemoveActivePackageSourceSetting()
		{
			settings.DeleteSection(ActivePackageSourceSectionName);
		}
		
		void UpdateActivePackageSourceSetting()
		{
			RemoveActivePackageSourceSetting();
			
			KeyValuePair<string, string> activePackageSourceSetting = PackageSourceConverter.ConvertToKeyValuePair(activePackageSource);
			SaveActivePackageSourceSetting(activePackageSourceSetting);
		}
		
		void SaveActivePackageSourceSetting(KeyValuePair<string, string> activePackageSource)
		{
			settings.SetValue(ActivePackageSourceSectionName, activePackageSource.Key, activePackageSource.Value);
		}

		void SettingsChanged (object sender, EventArgs e)
		{
			settings = settingsProvider.LoadSettings ();
			packageSourceProvider = CreatePackageSourceProvider (settings);
			ReadActivePackageSource ();
			ResetPackageSources ();
		}

		void ResetPackageSources ()
		{
			if (packageSources != null) {
				packageSources.CollectionChanged -= PackageSourcesChanged;
				packageSources = null;
			}
		}

		public ISettings Settings {
			get { return settings; }
		}
	}
}
