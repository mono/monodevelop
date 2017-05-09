//
// GlobalOptionPersister.cs
//
// Author:
//       Mikayla Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (c) 2017 Microsoft Corp.
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
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;
using MonoDevelop.Projects.Policies;
using MonoDevelop.Ide.Gui.Content;

namespace MonoDevelop.Ide.TypeSystem
{
	[Export(typeof(IOptionPersister))]
	class GlobalOptionPersister : IOptionPersister
	{
		public bool TryFetch (OptionKey optionKey, out object value)
		{
			if (optionKey.Option == FormattingOptions.UseTabs) {
				value = !GetTextPolicyForLanguage (optionKey.Language).TabsToSpaces;
				return true;
			}

			if (optionKey.Option == FormattingOptions.TabSize) {
				value = GetTextPolicyForLanguage (optionKey.Language).TabWidth;
				return true;
			}

			if (optionKey.Option == FormattingOptions.IndentationSize) {
				value = GetTextPolicyForLanguage (optionKey.Language).IndentWidth;
				return true;
			}

			if (optionKey.Option == FormattingOptions.NewLine) {
				value = GetTextPolicyForLanguage (optionKey.Language).GetEolMarker ();
				return true;
			}

			//use this for checking for options we could be handling
			//PrintOptionKey(optionKey);

			value = null;
			return false;
		}

		TextStylePolicy GetTextPolicyForLanguage (string language)
		{
			var mimeChain = DesktopService.GetMimeTypeInheritanceChainForRoslynLanguage (language);
			if (mimeChain == null) {
				throw new Exception ($"Unknown Roslyn language {language}");
			}
			return PolicyService.GetDefaultPolicy<TextStylePolicy> (mimeChain);
		}

		public bool TryPersist (OptionKey optionKey, object value)
		{
			value = null;
			return false;
		}

		static void PrintOptionKey (OptionKey optionKey)
		{
			Console.WriteLine ($"Name '{optionKey.Option.Name}' Language '{optionKey.Language}' LanguageSpecific'{optionKey.Option.IsPerLanguage}'");

			var locations = optionKey.Option.StorageLocations;
			if(locations.IsDefault) {
				return;
			}

			foreach (var loc in locations) {
				switch (loc) {
				case RoamingProfileStorageLocation roaming:
					Console.WriteLine ($"    roaming: {roaming.GetKeyNameForLanguage (optionKey.Language)}");
					break;
				case LocalUserProfileStorageLocation local:
					Console.WriteLine ($"    local: {local.KeyName}");
					break;
				case EditorConfigStorageLocation edconf:
					Console.WriteLine ($"    editorconfig: {edconf.KeyName}");
					break;
				default:
					Console.WriteLine ($"    unknown: {loc.GetType()}");
					break;
				}
			}
		}

	}
}
