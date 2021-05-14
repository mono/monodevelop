// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core;

namespace NuGet.PackageManagement.UI
{
	internal class LicenseFileText : IText, INotifyPropertyChanged
	{
		string _text;
		string _licenseText;
		string _licenseHeader;
		readonly string _licenseFileLocation;
		private Func<string, string> _loadFileFromPackage;

		int _initialized;

		internal LicenseFileText (string text, string licenseFileHeader, Func<string, string> loadFileFromPackage, string licenseFileLocation)
		{
			_text = text;
			_licenseHeader = licenseFileHeader;
			_licenseText = GettextCatalog.GetString ("Loading license file…");
			_loadFileFromPackage = loadFileFromPackage;
			_licenseFileLocation = licenseFileLocation;
		}

		internal void LoadLicenseFile ()
		{
			if (Interlocked.CompareExchange (ref _initialized, 1, 0) == 0) {
				if (_loadFileFromPackage != null) {
					Task.Run (async () => {
						string content = _loadFileFromPackage (_licenseFileLocation);
						await Runtime.RunInMainThread (() => {
							LicenseText = content;
						});
					}).Ignore ();
				}
			}
		}

		public string LicenseHeader {
			get => _licenseHeader;
			set {
				_licenseHeader = value;
				OnPropertyChanged ("LicenseHeader");
			}
		}

		public string Text {
			get => _text;
			set {
				_text = value;
				OnPropertyChanged ("Text");
			}
		}

		public string LicenseFileLocation => _licenseFileLocation;

		public string LicenseText {
			get => _licenseText;
			set {
				_licenseText = value;
				OnPropertyChanged ("LicenseText");
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged (string name)
		{
			PropertyChanged?.Invoke (this, new System.ComponentModel.PropertyChangedEventArgs (name));
		}
	}
}