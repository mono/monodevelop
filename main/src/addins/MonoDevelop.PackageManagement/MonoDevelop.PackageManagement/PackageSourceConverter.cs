// 
// PackageSourceConverter.cs
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
using System.Linq;
using NuGet;

namespace ICSharpCode.PackageManagement
{
	public static class PackageSourceConverter
	{
		public static IEnumerable<PackageSource> ConvertFromSettings(IEnumerable<SettingValue> packageSources)
		{
			if (HasAny(packageSources)) {
				foreach (SettingValue packageSource in packageSources) {
					yield return CreatePackageSourceFromSetting(packageSource);
				}
			}
		}
		
		static bool HasAny(IEnumerable<SettingValue> packageSources)
		{
			if (packageSources != null) {
				return packageSources.Any();
			}
			return false;
		}
		
		static PackageSource CreatePackageSourceFromSetting(SettingValue savedPackageSource)
		{
			string source = savedPackageSource.Value;
			string name = savedPackageSource.Key;
			return new PackageSource(source, name);
		}
		
		public static PackageSource ConvertFromFirstSetting(IEnumerable<SettingValue> packageSources)
		{
			if (HasAny(packageSources)) {
				return CreatePackageSourceFromSetting(packageSources.First());
			}
			return null;
		}
		
		public static IList<KeyValuePair<string, string>> ConvertToKeyValuePairList(IEnumerable<PackageSource> packageSources)
		{
			var convertedPackageSources = new List<KeyValuePair<string, string>>();
			foreach (PackageSource source in packageSources) {
				convertedPackageSources.Add(ConvertToKeyValuePair(source));
			}
			return convertedPackageSources;
		}
		
		public static KeyValuePair<string, string> ConvertToKeyValuePair(PackageSource source)
		{
			return new KeyValuePair<string, string>(source.Name, source.Source);
		}
	}
}
