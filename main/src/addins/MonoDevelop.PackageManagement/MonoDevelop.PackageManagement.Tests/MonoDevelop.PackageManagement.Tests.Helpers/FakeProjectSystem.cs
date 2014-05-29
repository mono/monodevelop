﻿//
// FakeProjectSystem.cs
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
using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;

using NuGet;

namespace MonoDevelop.PackageManagement.Tests.Helpers
{
	public class FakeProjectSystem : FakeFileSystem, IProjectSystem
	{
		public FrameworkName TargetFramework {
			get { return new FrameworkName(".NETFramework, Version=v4.0"); }
		}

		public string ProjectName {
			get { return String.Empty; }
		}

		public dynamic GetPropertyValue(string propertyName)
		{
			throw new NotImplementedException();
		}

		public void AddReference(string referencePath, Stream stream)
		{
			throw new NotImplementedException();
		}

		public bool ReferenceExists(string name)
		{
			throw new NotImplementedException();
		}

		public void RemoveReference(string name)
		{
			throw new NotImplementedException();
		}

		public bool IsSupportedFile(string path)
		{
			throw new NotImplementedException();
		}

		public void AddFrameworkReference(string name)
		{
			throw new NotImplementedException();
		}

		public string ResolvePath(string path)
		{
			throw new NotImplementedException();
		}

		public bool IsBindingRedirectSupported { get; set; }

		public void AddImport(string targetPath, ProjectImportLocation location)
		{
			throw new NotImplementedException();
		}

		public void RemoveImport(string targetPath)
		{
			throw new NotImplementedException();
		}

		public bool FileExistsInProject(string path)
		{
			throw new NotImplementedException();
		}
	}
}

