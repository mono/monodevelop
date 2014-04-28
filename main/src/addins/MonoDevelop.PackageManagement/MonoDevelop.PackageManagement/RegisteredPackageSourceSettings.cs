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

using NuGet;

namespace ICSharpCode.PackageManagement
{
	public class RegisteredPackageSourceSettings
	{
		public static readonly string ActivePackageSourceSectionName = "activePackageSource";

		public static readonly PackageSource AggregatePackageSource = 
			new PackageSource("(Aggregate source)", "All");

		ISettings settings;
		IPackageSourceProvider packageSourceProvider;
		PackageSource defaultPackageSource;
		RegisteredPackageSources packageSources;
		PackageSource activePackageSource;
		
		public RegisteredPackageSourceSettings(ISettings settings)
			: this(
				settings,
				new PackageSourceProvider(settings, new [] { RegisteredPackageSources.DefaultPackageSource }),
				RegisteredPackageSources.DefaultPackageSource)
		{
		}
		
		public RegisteredPackageSourceSettings(
			ISettings settings,
			IPackageSourceProvider packageSourceProvider,
			PackageSource defaultPackageSource)
		{
			this.settings = settings;
			this.packageSourceProvider = packageSourceProvider;
			this.defaultPackageSource = defaultPackageSource;
			ReadActivePackageSource();
		}

		void ReadActivePackageSource()
		{
			IList<KeyValuePair<string, string>> packageSources = settings.GetValues(ActivePackageSourceSectionName);
			activePackageSource = PackageSourceConverter.ConvertFromFirstKeyValuePair(packageSources);
		}

		public RegisteredPackageSources PackageSources {
			get {
				if (packageSources == null) {
					ReadPackageSources();
				}
				return packageSources;
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
	}
}
