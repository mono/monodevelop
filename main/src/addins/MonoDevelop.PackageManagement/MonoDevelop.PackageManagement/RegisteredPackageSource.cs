// 
// RegisteredPackageSource.cs
// 
// Author:
//   Matt Ward <ward.matt@gmail.com>
// 
// Copyright (C) 2012 Matthew Ward
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
using NuGet;

namespace ICSharpCode.PackageManagement
{
	public class RegisteredPackageSource
	{
		public string Source { get; set; }
		public string Name { get; set; }
		public bool IsEnabled { get; set; }
		public string UserName { get; set; }
		public string Password { get; set; }
		
		public RegisteredPackageSource()
		{
		}
		
		public RegisteredPackageSource(PackageSource packageSource)
		{
			Source = packageSource.Source;
			Name = packageSource.Name;
			IsEnabled = packageSource.IsEnabled;
			UserName = packageSource.UserName;
			Password = packageSource.Password;
		}
		
		public PackageSource ToPackageSource()
		{
			return new PackageSource (Source, Name, IsEnabled) {
				UserName = UserName,
				Password = Password
			};
		}
	}
}
