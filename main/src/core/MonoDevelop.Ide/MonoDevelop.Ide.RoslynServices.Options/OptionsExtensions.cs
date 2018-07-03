//
// OptionsExtensions.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2018 Microsoft Inc.
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
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Options;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Projects.Policies;

namespace MonoDevelop.Ide.RoslynServices.Options
{
	static class OptionsExtensions
	{
		public static IEnumerable<string> GetPropertyNames (this OptionKey optionKey)
		{
			// Prevent NRE being thrown on iteration.
			if (optionKey.Option.StorageLocations.IsDefaultOrEmpty)
				yield break;

			foreach (var storageLocation in optionKey.Option.StorageLocations) {
				if (storageLocation is RoamingProfileStorageLocation roamingLocation)
					yield return roamingLocation.GetKeyNameForLanguage (optionKey.Language);
				if (storageLocation is LocalUserProfileStorageLocation userLocation)
					yield return userLocation.KeyName;
			}
		}

		public static string GetPropertyName (this OptionKey optionKey) => GetPropertyNames (optionKey).FirstOrDefault ();

		public static TextStylePolicy GetTextStylePolicy (this OptionKey optionKey)
		{
			var mimeChain = DesktopService.GetMimeTypeInheritanceChainForRoslynLanguage (optionKey.Language);
			if (mimeChain == null) {
				throw new Exception ($"Unknown Roslyn language {optionKey.Language}");
			}
			return PolicyService.GetDefaultPolicy<TextStylePolicy> (mimeChain);
		}
	}
}
