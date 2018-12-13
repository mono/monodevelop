//
// TargetFrameworkExtensions.cs
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

using System;
using MonoDevelop.Core.Assemblies;

namespace MonoDevelop.DotNetCore
{
	static class TargetFrameworkExtensions
	{
		public static string GetParameterName (this TargetFramework framework)
		{
			var parameter = framework.Id.Version;

			//special case for 1.x
			if (framework.IsVersion1x ())
				parameter = "1x";

			parameter = parameter.Replace (".", string.Empty);

			if (framework.IsNetCoreApp ()) {
				return $"UseNetCore{parameter}";
			} 

			if (framework.IsNetStandard ())
				return $"UseNetStandard{parameter}";

			return string.Empty;
		}

		public static bool IsNetStandard (this TargetFramework framework) => framework.Id.IsNetStandard ();

		public static bool IsNetStandard (this TargetFramework framework, string version)
		{
			return framework.Id.IsNetStandard () && framework.Id.Version.IndexOf (version, StringComparison.InvariantCulture) == 0;
		}

		public static bool IsLowerThanNetStandard16 (this TargetFramework framework)
		{
			if (framework.IsNetStandard ("2.0"))
				return false;

			return framework.IsNetStandard1x () && framework.Id.Version != "1.6"; 
		}

		public static bool IsNetStandard1x (this TargetFramework framework) => framework.IsNetStandard() && framework.IsVersion1x();

		public static bool IsNetCoreApp1x (this TargetFramework framework) => framework.IsNetCoreApp () && framework.IsVersion1x ();

		static bool IsVersion1x (this TargetFramework framework) => framework.Id.Version.StartsWith ("1.", StringComparison.Ordinal);

		public static bool IsNetCoreApp (this TargetFramework framework) => framework.Id.IsNetCoreApp ();

		public static bool IsNetCoreApp (this TargetFramework framework, string version)
		{
			return framework.Id.IsNetCoreApp () && framework.Id.Version.IndexOf (version, StringComparison.InvariantCulture) == 0;
		}

		public static bool IsNetCoreAppOrHigher (this TargetFramework framework, DotNetCoreVersion version)
		{
			DotNetCoreVersion dotNetCoreVersion;
			DotNetCoreVersion.TryParse (framework.Id.Version, out dotNetCoreVersion);
			if (dotNetCoreVersion == null)
				return false;

			return framework.Id.IsNetCoreApp () && dotNetCoreVersion >= version;
		}

		public static bool IsNetFramework (this TargetFramework framework) => framework.Id.IsNetFramework ();

		public static bool IsNetStandard20OrNetCore20 (this TargetFramework framework) => framework.IsNetStandard ("2.0") || framework.IsNetCoreApp ("2.0");

		public static string GetDisplayName (this TargetFramework framework)
		{
			if (framework.IsNetCoreApp ())
				return string.Format (".NET Core {0}", framework.Id.Version);

			if (framework.IsNetStandard ())
				return string.Format (".NET Standard {0}", framework.Id.Version);

			return framework.Name;
		}
	}
}
