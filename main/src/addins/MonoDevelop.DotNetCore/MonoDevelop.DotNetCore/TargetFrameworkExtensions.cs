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
		public static bool IsNetStandard (this TargetFramework framework)
		{
			return framework.Id.IsNetStandard ();
		}

		public static bool IsNetStandard20 (this TargetFramework framework)
		{
			return framework.IsNetStandard () &&
				framework.Id.Version == "2.0";
		}

		public static bool IsNetStandard1x (this TargetFramework framework)
		{
			return framework.IsNetStandard () &&
				framework.IsVersion1x ();
		}

		public static bool IsLowerThanNetStandard16 (this TargetFramework framework)
		{
			if (framework.IsNetStandard20 ())
				return false;

			return framework.IsNetStandard1x () &&
				framework.Id.Version != "1.6"; 
		}

		static bool IsVersion1x (this TargetFramework framework)
		{
			return framework.Id.Version.StartsWith ("1.", StringComparison.Ordinal);
		}

		public static bool IsNetCoreApp (this TargetFramework framework)
		{
			return framework.Id.IsNetCoreApp ();
		}

		public static bool IsNetCoreApp20 (this TargetFramework framework)
		{
			return framework.IsNetCoreApp () &&
				framework.Id.Version == "2.0";
		}

		public static bool IsNetCoreApp1x (this TargetFramework framework)
		{
			return framework.IsNetCoreApp () &&
				framework.IsVersion1x ();
		}

		public static bool IsNetFramework (this TargetFramework framework)
		{
			return framework.Id.IsNetFramework ();
		}

		public static bool IsNetStandard20OrNetCore20 (this TargetFramework framework)
		{
			return framework.IsNetStandard20 () || framework.IsNetCoreApp ();
		}

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
