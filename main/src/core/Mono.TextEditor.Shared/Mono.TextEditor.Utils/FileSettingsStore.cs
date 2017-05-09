//
// FileSettingsStore.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
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

namespace Mono.TextEditor.Utils
{
	static class FileSettingsStore
	{
		
		internal class Settings
		{
			public int CaretOffset { get; set; }

			public double vAdjustment { get; set; }

			public double hAdjustment { get; set; }

			public Dictionary<int, bool> FoldingStates = new Dictionary<int, bool> ();

			public override string ToString ()
			{
				return string.Format ("[Settings: CaretOffset={0}, vAdjustment={1}, hAdjustment={2}]", CaretOffset, vAdjustment, hAdjustment);
			}
		}

		static Dictionary<string, Settings> settingStore = new Dictionary<string, Settings> ();

		public static bool TryGetValue (string contentName, out Settings settings)
		{
			if (contentName == null)
				throw new ArgumentNullException ("contentName");
			return settingStore.TryGetValue (contentName, out settings);
		}

		public static void Store (string contentName, Settings settings)
		{
			if (contentName == null)
				throw new ArgumentNullException ("contentName");
			if (settings == null)
				throw new ArgumentNullException ("settings");
			settingStore [contentName] = settings;
		}

		public static void Remove (string fileName)
		{
			settingStore.Remove (fileName);
		}
	}
}

