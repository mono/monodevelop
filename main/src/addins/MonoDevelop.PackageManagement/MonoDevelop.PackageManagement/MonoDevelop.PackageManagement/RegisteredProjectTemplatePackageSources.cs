// 
// RegisteredProjectTemplatePackageSources.cs
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
using System.Collections.Generic;
using NuGet;

namespace ICSharpCode.PackageManagement
{
	public class RegisteredProjectTemplatePackageSources
	{
		RegisteredPackageSourceSettings registeredPackageSourceSettings;
		
		public RegisteredProjectTemplatePackageSources()
			: this(new PackageManagementPropertyService(), new SettingsFactory())
		{
		}
		
		public RegisteredProjectTemplatePackageSources(
			IPropertyService propertyService,
			ISettingsFactory settingsFactory)
		{
			GetRegisteredPackageSources(propertyService, settingsFactory);
		}
		
		void GetRegisteredPackageSources(IPropertyService propertyService, ISettingsFactory settingsFactory)
		{
			ISettings settings = CreateSettings(propertyService, settingsFactory);
			PackageSource defaultPackageSource = CreateDefaultPackageSource(propertyService);
			registeredPackageSourceSettings = new RegisteredPackageSourceSettings(settings, defaultPackageSource);
		}
		
		ISettings CreateSettings(IPropertyService propertyService, ISettingsFactory settingsFactory)
		{
			var settingsFileName = new ProjectTemplatePackagesSettingsFileName(propertyService);
			return settingsFactory.CreateSettings(settingsFileName.Directory);
		}
		
		PackageSource CreateDefaultPackageSource(IPropertyService propertyService)
		{
			var defaultPackageSource = new DefaultProjectTemplatePackageSource(propertyService);
			return defaultPackageSource.PackageSource;
		}
		
		public RegisteredPackageSources PackageSources {
			get { return registeredPackageSourceSettings.PackageSources; }
		}
	}
}
