//
// MonoRuntimeInfoExtensions.cs
//
// Author:
//       Rodrigo Moya <rodrigo.moya@xamarin.com>
//
// Copyright (c) 2018, Microsoft, Inc (http://www.microsoft.com)
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
	static class MonoRuntimeInfoExtensions
	{
		static readonly Version MonoVersion5_4 = new Version (5, 4, 0);
		static readonly Version DotNetCore2_1 = new Version (2, 1);

		internal static Version CurrentRuntimeVersion { get; set; } = MonoRuntimeInfo.FromCurrentRuntime ().RuntimeVersion;

		public static bool SupportsNetStandard20 (this Version monoVersion)
		{
			return monoVersion >= MonoVersion5_4;
		}

		public static bool SupportsNetStandard21 (this Version monoVersion)
		{
			//FIXME: update this: which Mono version will support .NET Standadrd 2.1
			return monoVersion >= MonoVersion5_4;
		}

		public static bool SupportsNetCore (this Version monoVersion, string netCoreVersion)
		{
			return monoVersion >= MonoVersion5_4 && Version.Parse (netCoreVersion) <= DotNetCore2_1;
		}
	}
}
