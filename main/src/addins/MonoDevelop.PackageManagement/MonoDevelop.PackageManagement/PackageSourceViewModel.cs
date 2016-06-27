// 
// PackageSourceViewModel.cs
// 
// Author:
//   Matt Ward <ward.matt@gmail.com>
// 
// Copyright (C) 2013 Matthew Ward
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using NuGet.Configuration;

namespace MonoDevelop.PackageManagement
{
	internal class PackageSourceViewModel : ViewModelBase<PackageSourceViewModel>
	{
		PackageSource packageSource;

		public PackageSourceViewModel ()
			: this (new PackageSource (""))
		{
		}
		
		public PackageSourceViewModel(PackageSource packageSource)
		{
			this.packageSource = packageSource.Clone ();

			Name = packageSource.Name;
			Password = packageSource.Password;
			IsValid = true;
			ValidationFailureMessage = "";
		}
		
		public PackageSource GetPackageSource()
		{
			// HACK: Workaround a NuGet 3.4.3 bug where it double encrypts the password when 
			// it saves the NuGet.Config file. The PasswordText should hold the encrypted string 
			// but instead we use the plain text password so it is only encrypted once.
			// https://github.com/NuGet/Home/issues/2647

			return new PackageSource (Source, Name, IsEnabled) {
				UserName = UserName,
				PasswordText = Password,
				ProtocolVersion = packageSource.ProtocolVersion
			};
		}

		public NuGet.PackageSource GetNuGet2PackageSource ()
		{
			return new NuGet.PackageSource (Source, Name, IsEnabled) {
				UserName = UserName,
				Password = Password
			};
		}
		
		public string Name { get; set; }
		
		public string Source {
			get { return packageSource.Source; }
			set { packageSource.Source = value; }
		}
		
		public bool IsEnabled {
			get { return packageSource.IsEnabled; }
			set { packageSource.IsEnabled = value; }
		}

		public string UserName {
			get { return packageSource.UserName; }
			set { packageSource.UserName = value; }
		}

		public string Password { get; set; }

		public bool HasPassword ()
		{
			return !String.IsNullOrEmpty (Password);
		}

		public bool IsValid { get; set; }
		public string ValidationFailureMessage { get; set; }
	}
}
