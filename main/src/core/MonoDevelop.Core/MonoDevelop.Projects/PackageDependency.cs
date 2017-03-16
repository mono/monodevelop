//
// PackageDependency.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
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

using System.Collections.Generic;
using MonoDevelop.Projects.MSBuild;

namespace MonoDevelop.Projects
{
	public sealed class PackageDependency
	{
		string[] dependencies;

		PackageDependency (string type, MSBuildEvaluatedItem item)
		{
			Type = type;
			Init (item);
		}

		public string Type { get; private set; }
		public string Name { get; private set; }
		public string Version { get; private set; }
		public bool Resolved { get; private set; }

		public IEnumerable<string> Dependencies {
			get { return dependencies; }
		}

		public bool IsTargetFramework { get; private set; }
		public bool IsPackage { get; private set; }

		internal static PackageDependency Create (MSBuildEvaluatedItem item)
		{
			string type = GetMetadataValue ("Type", item);
			return Create (type, item);
		}

		static string GetMetadataValue (string name, MSBuildEvaluatedItem item)
		{
			string result;
			if (item.Metadata.TryGetValue (name, out result))
				return result;

			return string.Empty;
		}

		static bool GetMetadataBoolValue (string name, MSBuildEvaluatedItem item, bool defaultValue = false)
		{
			string value = GetMetadataValue (name, item);
			if (string.IsNullOrEmpty (value))
				return defaultValue;

			bool result;
			if (bool.TryParse (value, out result))
				return result;

			return defaultValue;
		}

		static PackageDependency Create (string type, MSBuildEvaluatedItem item)
		{
			switch (type) {
				case "Target":
				case "Package":
				return new PackageDependency (type, item);

				default:
				return null;
			}
		}

		void Init (MSBuildEvaluatedItem item)
		{
			if (Type == "Package") {
				Name = GetMetadataValue ("Name", item);
				Version = GetMetadataValue ("Version", item);
				Resolved = GetMetadataBoolValue ("Resolved", item);
				IsPackage = true;
			} else if (Type == "Target") {
				Name = GetMetadataValue ("FrameworkName", item);
				Version = GetMetadataValue ("FrameworkVersion", item);
				Resolved = true;
				IsTargetFramework = true;
			}

			dependencies = GetDependencies (item);
		}

		string[] GetDependencies (MSBuildEvaluatedItem item)
		{
			string value = GetMetadataValue ("Dependencies", item);
			if (string.IsNullOrEmpty (value))
				return new string[0];

			return value.Split (';');
		}
	}
}
