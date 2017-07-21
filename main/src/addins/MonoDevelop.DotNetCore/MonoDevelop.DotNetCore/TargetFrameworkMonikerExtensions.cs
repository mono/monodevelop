//
// TargetFrameworkMonikerExtensions.cs
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

using MonoDevelop.Core.Assemblies;

namespace MonoDevelop.DotNetCore
{
	static class TargetFrameworkMonikerExtensions
	{
		public static string GetShortFrameworkName (this TargetFrameworkMoniker framework)
		{
			if (framework.IsNetFramework ())
				return GetShortNetFrameworkName (framework);

			string identifier = GetShortFrameworkIdentifier (framework);
			return identifier + framework.Version;
		}

		public static string GetShortFrameworkIdentifier (this TargetFrameworkMoniker framework)
		{
			if (string.IsNullOrEmpty (framework.Identifier))
				return string.Empty;

			string shortFrameworkIdentifier = framework.Identifier;

			if (shortFrameworkIdentifier[0] == '.')
				shortFrameworkIdentifier = shortFrameworkIdentifier.Substring (1);

			return shortFrameworkIdentifier.ToLower ();
		}

		static string GetShortNetFrameworkName (TargetFrameworkMoniker framework)
		{
			return "net" + framework.Version.Replace (".", string.Empty);
		}

		public static bool IsNetFramework (this TargetFrameworkMoniker framework)
		{
			return framework.Identifier == ".NETFramework";
		}

		public static bool IsNetCoreApp (this TargetFrameworkMoniker framework)
		{
			return framework.Identifier == ".NETCoreApp";
		}

		public static bool IsNetStandard (this TargetFrameworkMoniker framework)
		{
			return framework.Identifier == ".NETStandard";
		}

		public static bool IsNetStandardOrNetCoreApp (this TargetFrameworkMoniker framework)
		{
			return framework.IsNetStandard () || framework.IsNetCoreApp ();
		}
	}
}
