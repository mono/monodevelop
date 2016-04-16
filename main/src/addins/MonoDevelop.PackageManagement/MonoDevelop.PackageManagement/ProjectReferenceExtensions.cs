//
// ProjectReferenceExtensions.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Core;
using MonoDevelop.Projects;
using System.IO;
using NuGet.PackageManagement;

namespace MonoDevelop.PackageManagement
{
	internal static class ProjectReferenceExtensions
	{
		public static bool IsReferenceFromPackage (this ProjectReference projectReference, FilePath packagesFolderPath)
		{
			if (!projectReference.IsAssemblyReference ())
				return false;

			var project = projectReference.OwnerProject as DotNetProject;
			if (project == null)
				return false;

			var assemblyFilePath = new FilePath (projectReference.GetFullAssemblyPath ());
			if (assemblyFilePath.IsNullOrEmpty)
				return false;

			return assemblyFilePath.IsChildPathOf (packagesFolderPath);
		}

		public static bool IsReferenceFromPackage (this ProjectReference projectReference)
		{
			if (!projectReference.IsAssemblyReference ())
				return false;

			var project = projectReference.OwnerProject as DotNetProject;
			if (project == null)
				return false;

			var assemblyFilePath = new FilePath (projectReference.GetFullAssemblyPath ());
			if (assemblyFilePath.IsNullOrEmpty)
				return false;

			FilePath packagesFolderPath = project.GetPackagesFolderPath ();

			return assemblyFilePath.IsChildPathOf (packagesFolderPath);
		}

		static bool IsAssemblyReference (this ProjectReference reference)
		{
			return (reference.ReferenceType == ReferenceType.Assembly)
				|| ((reference.ReferenceType == ReferenceType.Package) && !reference.IsValid);
		}

		static string GetFullAssemblyPath (this ProjectReference projectReference)
		{
			if (!String.IsNullOrEmpty (projectReference.HintPath)) {
				return Path.GetFullPath (projectReference.HintPath);
			}

			return null;
		}
	}
}

