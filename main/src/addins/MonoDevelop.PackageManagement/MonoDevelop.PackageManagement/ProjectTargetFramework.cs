// 
// ProjectTargetFramework.cs
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
using System.Runtime.Versioning;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.PackageManagement;
using MonoDevelop.Projects;

namespace ICSharpCode.PackageManagement
{
	public class ProjectTargetFramework
	{
		IDotNetProject project;
		FrameworkName targetFramework;

		public ProjectTargetFramework (IDotNetProject project)
		{
			this.project = project;
			GetTargetFramework();
		}
		
		void GetTargetFramework()
		{
			string identifier = GetTargetFrameworkIdentifier();
			string version = GetTargetFrameworkVersion();
			string profile = GetTargetFrameworkProfile();
			
			GetTargetFramework(identifier, version, profile);
		}
		
		void GetTargetFramework(string identifier, string version, string profile)
		{
			string name = String.Format("{0}, Version={1}, Profile={2}", identifier, version, profile);
			targetFramework = new FrameworkName(name);
		}
		
		string GetTargetFrameworkIdentifier()
		{
			return UseDefaultIfNullOrEmpty(TargetFrameworkMoniker.Identifier, ".NETFramework");
		}
		
		TargetFrameworkMoniker TargetFrameworkMoniker {
			get { return project.TargetFrameworkMoniker; }
		}
		
		string UseDefaultIfNullOrEmpty(string value, string defaultValue)
		{
			if (String.IsNullOrEmpty(value)) {
				return defaultValue;
			}
			return value;
		}
		
		string GetTargetFrameworkVersion()
		{
			return TargetFrameworkMoniker.Version;
		}
		
		string GetTargetFrameworkProfile()
		{
			return UseDefaultIfNullOrEmpty(TargetFrameworkMoniker.Profile, String.Empty);
		}
		
		public FrameworkName TargetFrameworkName {
			get { return targetFramework; }
		}
	}
}
