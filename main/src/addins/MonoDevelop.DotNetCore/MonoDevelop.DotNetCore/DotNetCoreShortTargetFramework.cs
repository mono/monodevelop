//
// DotNetCoreShortTargetFramework.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2018 Microsoft
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
	class DotNetCoreShortTargetFramework
	{
		DotNetCoreShortTargetFramework ()
		{
		}

		public string Identifier { get; private set; }
		public string Version { get; private set; }
		public string OriginalString { get; private set; }

		public override string ToString ()
		{
			return $"{Identifier}{Version}";
		}

		/// <summary>
		/// Parse the specified short target framework. Logic is based on:
		/// https://github.com/dotnet/sdk/blob/cfe8ff3c4e51c473ae75ca32d1c7a62043e96990/src/Tasks/Microsoft.NET.Build.Tasks/targets/Microsoft.NET.TargetFrameworkInference.targets#L50
		/// </summary>
		public static DotNetCoreShortTargetFramework Parse (string input)
		{
			if (string.IsNullOrEmpty (input))
				throw new ArgumentException (".NET Core short target framework cannot be null or an empty string.", nameof (input));

			if (input.Contains (","))
				throw new ArgumentException (".NET Core short target framework cannot contain ','.", nameof (input));

			if (input.Contains ("+"))
				throw new ArgumentException (".NET Core short target framework cannot contain '+'.", nameof (input));

			string identifier = input.TrimEnd ('.', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
			string version = input.Substring (identifier.Length);

			return new DotNetCoreShortTargetFramework {
				Identifier = identifier,
				OriginalString = input,
				Version = version
			};
		}

		public static bool TryParse (string input, out DotNetCoreShortTargetFramework framework)
		{
			framework = null;

			if (string.IsNullOrEmpty (input))
				return false;

			if (input.Contains (",") || input.Contains ("+"))
				return false;

			framework = Parse (input);

			return true;
		}

		public void Update (TargetFrameworkMoniker framework)
		{
			UpdateVersion (framework.Version);
		}

		void UpdateVersion (string newVersion)
		{
			if (Version.Contains (".")) {
				Version = newVersion;
			} else {
				Version = newVersion.Replace (".", string.Empty);
			}
		}
	}
}
