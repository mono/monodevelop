//
// LocalizationService.cs
//
// Author:
//       Marius Ungureanu <marius.ungureanu@xamarin.com>
//
// Copyright (c) 2016 Xamarin, Inc (http://www.xamarin.com)
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

using Mono.Addins;
using MonoDevelop.Core;

namespace MonoDevelop.Ide
{
	public static class LocalizationService
	{
		const string path = "/MonoDevelop/Ide/LocaleSet";
		static readonly List<LocaleSet[]> locales = new List<LocaleSet[]> ();

		internal static void Initialize ()
		{
			AddinManager.AddExtensionNodeHandler (path, OnExtensionChanged);
			Array.Sort (defaultLocaleSet, (x, y) => x.DisplayName.CompareTo (y.DisplayName));
		}

		public static IReadOnlyList<LocaleSet> CurrentLocaleSet { get { return locales.FirstOrDefault () ?? defaultLocaleSet; } }

		static void OnExtensionChanged (object sender, ExtensionNodeEventArgs args)
		{
			var node = (TypeExtensionNode)args.ExtensionNode;
			var value = (LocaleSetProvider)node.GetInstance (typeof (LocaleSetProvider));
			if (args.Change == ExtensionChange.Add)
				locales.Add (value.LocaleSet);
			else
				locales.Remove (value.LocaleSet);
		}

		static LocaleSet [] defaultLocaleSet = {
			new LocaleSet ("", GettextCatalog.GetString ("(Default)")),
			new LocaleSet ("ca", GettextCatalog.GetString ("Catalan")),
			new LocaleSet ("zh_CN", GettextCatalog.GetString ("Chinese - China")),
			new LocaleSet ("zh_TW", GettextCatalog.GetString ("Chinese - Taiwan")),
			new LocaleSet ("cs", GettextCatalog.GetString ("Czech")),
			new LocaleSet ("da", GettextCatalog.GetString ("Danish")),
			new LocaleSet ("nl", GettextCatalog.GetString ("Dutch")),
			new LocaleSet ("fr", GettextCatalog.GetString ("French")),
			new LocaleSet ("gl", GettextCatalog.GetString ("Galician")),
			new LocaleSet ("de", GettextCatalog.GetString ("German")),
			new LocaleSet ("en", GettextCatalog.GetString ("English")),
			new LocaleSet ("hu", GettextCatalog.GetString ("Hungarian")),
			new LocaleSet ("id", GettextCatalog.GetString ("Indonesian")),
			new LocaleSet ("it", GettextCatalog.GetString ("Italian")),
			new LocaleSet ("ja", GettextCatalog.GetString ("Japanese")),
			new LocaleSet ("ko", GettextCatalog.GetString ("Korean")),
			new LocaleSet ("pl", GettextCatalog.GetString ("Polish")),
			new LocaleSet ("pt", GettextCatalog.GetString ("Portuguese")),
			new LocaleSet ("pt_BR", GettextCatalog.GetString ("Portuguese - Brazil")),
			new LocaleSet ("ru", GettextCatalog.GetString ("Russian")),
			new LocaleSet ("sl", GettextCatalog.GetString ("Slovenian")),
			new LocaleSet ("es", GettextCatalog.GetString ("Spanish")),
			new LocaleSet ("sv", GettextCatalog.GetString ("Swedish")),
			new LocaleSet ("tr", GettextCatalog.GetString ("Turkish")),
		};
	}

	public class LocaleSetProvider
	{
		public LocaleSet[] LocaleSet { get; protected set; }

		public LocaleSetProvider ()
		{
		}
	}

	public class LocaleSet
	{
		public string Culture { get; private set; }
		public string DisplayName { get; private set; }

		public LocaleSet (string culture, string displayName)
		{
			Culture = culture;
			DisplayName = displayName;
		}
	}
}

