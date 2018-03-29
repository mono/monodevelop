//
// DotNetCoreRuntime.cs
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

namespace MonoDevelop.DotNetCore
{
	public static class DotNetCoreRuntime
	{
		static DotNetCoreRuntime ()
		{
			var path = new DotNetCorePath ();
			IsInstalled = !path.IsMissing;
			FileName = path.FileName;

			Versions = DotNetCoreRuntimeVersions.GetInstalledVersions (path)
				.OrderByDescending (version => version)
				.ToArray ();

			if (!IsInstalled)
				LoggingService.LogInfo (".NET Core runtime not found.");
		}

		public static string FileName { get; private set; }

		public static bool IsInstalled { get; private set; }

		public static bool IsMissing {
			get { return !IsInstalled; }
		}

		internal static DotNetCoreVersion[] Versions { get; private set; }

		internal static bool IsNetCore1xInstalled ()
		{
			return Versions.Any (version => version.Major == 1);
		}

		internal static bool IsNetCore20Installed ()
		{
			return Versions.Any (version => version.Major == 2 && version.Minor == 0);
		}

		internal static bool IsNetCore2xInstalled ()
		{
			return Versions.Any (version => version.Major == 2);
		}

		/// <summary>
		/// Used by unit tests to fake having different .NET Core sdks installed.
		/// </summary>
		internal static void SetVersions (IEnumerable<DotNetCoreVersion> versions)
		{
			Versions = versions.ToArray ();
		}

		/// <summary>
		/// Used by unit tests to fake having the sdk installed.
		/// </summary>
		internal static void SetInstalled (bool installed)
		{
			IsInstalled = installed;
		}
	}
}
