//
// FileNestingService.cs
//
// Author:
//       Rodrigo Moya <rodrigo.moya@xamarin.com>
//
// Copyright (c) 2019 Microsoft, Inc. (http://microsoft.com)
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
using System.Collections.Immutable;
using Mono.Addins;
using MonoDevelop.Core;

namespace MonoDevelop.Projects.FileNesting
{
	internal static class FileNestingService
	{
		static ImmutableList<NestingRulesProvider> rulesProviders = ImmutableList<NestingRulesProvider>.Empty;

		static FileNestingService ()
		{
			AddinManager.AddExtensionNodeHandler (typeof (NestingRulesProvider), HandleRulesProviderExtension);
		}

		static void HandleRulesProviderExtension (object sender, ExtensionNodeEventArgs args)
		{
			var pr = args.ExtensionObject as NestingRulesProvider;
			if (pr != null) {
				if (args.Change == ExtensionChange.Add) {
					rulesProviders = rulesProviders.Add (pr);
				} else {
					rulesProviders = rulesProviders.Remove (pr);
				}
			}
		}

		public static FilePath GetParentFile (FilePath inputFile)
		{
			foreach (var rp in rulesProviders) {
				var parentFile = rp.GetParentFile (inputFile);
				if (!string.IsNullOrEmpty (parentFile)) {
					return parentFile;
				}
			}

			return null;
		}
	}
}
