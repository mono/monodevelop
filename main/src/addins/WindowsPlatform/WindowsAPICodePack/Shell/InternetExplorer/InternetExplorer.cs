//
// InternetExplorer.cs
//
// Author:
//       Vsevolod Kukol <sevoku@microsoft.com>
//
// Copyright (c) 2016 (c) Vsevolod Kukol
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
using Microsoft.Win32;
using System.IO;

namespace Microsoft.WindowsAPICodePack.InternetExplorer
{
	/// <summary>Internet Explorer Emulation Modes</summary>
	/// <remarks>https://msdn.microsoft.com/de-de/library/ee330730.aspx#browser_emulation</remarks>
	public enum IEEmulationMode
	{
		IE7 = 0x00001b58,
		IE8 = 0x00001f40,
		IE8Force = 0x000022b8,
		IE9 = 0x00002328,
		IE9Force = 0x0000270f,
		IE10 = 0x00002710,
		IE10Force = 0x00002711,
		IE11 = 0x00002af8,
		IE11Force = 0x00002af9,
		Default = IE7,
	}

	public static class InternetExplorer
	{
		static readonly string browserEmulationKeyPath = @"Software\\Microsoft\\Internet Explorer\\Main\\FeatureControl\\FEATURE_BROWSER_EMULATION";

		/// <summary>
		/// Gets or sets the embedded IE emulation mode.
		/// </summary>
		/// <value>The emulation mode.</value>
		public static IEEmulationMode EmulationMode {
			get {
				var regKey = Registry.CurrentUser.OpenSubKey (browserEmulationKeyPath, false);
				if (regKey == null)
					return IEEmulationMode.Default;

				var myProgramName = Path.GetFileName (System.Reflection.Assembly.GetEntryAssembly ().Location);
				var currentValue = regKey.GetValue (myProgramName);
				return currentValue is int ? (IEEmulationMode)currentValue : IEEmulationMode.Default;
			}
			set {
				if (EmulationMode == value) // load current value and update it only if necessary
					return;
				var regKey = Registry.CurrentUser.CreateSubKey (browserEmulationKeyPath, RegistryKeyPermissionCheck.ReadWriteSubTree);

				var executableName = Path.GetFileName (System.Reflection.Assembly.GetEntryAssembly ().Location);
				var currentValue = regKey.GetValue (executableName);
				if (currentValue == null || !(currentValue is int) || (int)currentValue != (int)value)
					regKey.SetValue (executableName, (int)value, RegistryValueKind.DWord);
			}
		}
	}
}
