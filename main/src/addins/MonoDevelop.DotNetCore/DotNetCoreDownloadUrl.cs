//
// DotNetCoreDownloadUrl.cs
//
// Author:
//       Rodrigo Moya <rodrigo.moya@xamarin.com>
//
// Copyright (c) 2019 
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
namespace MonoDevelop.DotNetCore
{
	static class DotNetCoreDownloadUrl
	{
		static readonly string BaseDotNetCoreDownloadUrl = "https://aka.ms/vs/mac/install-netcore{0}";

		public static string GetDotNetCoreDownloadUrl (string version = "")
		{
			if (string.IsNullOrEmpty (version))
				return string.Format (BaseDotNetCoreDownloadUrl, string.Empty);

			if (DotNetCoreVersion.TryParse (version, out var dotNetCoreVersion)) {
				return GetDotNetCoreDownloadUrl (dotNetCoreVersion);
			}

			return "https://dotnet.microsoft.com/download";
		}

		public static string GetDotNetCoreDownloadUrl (DotNetCoreVersion version)
		{
			//special case for 2.0, 3.0, ..
			if (version.Minor == 0)
				return string.Format (BaseDotNetCoreDownloadUrl, version.Major);

			return string.Format (BaseDotNetCoreDownloadUrl, $"{version.Major}{version.Minor}");
		}
	}
}
