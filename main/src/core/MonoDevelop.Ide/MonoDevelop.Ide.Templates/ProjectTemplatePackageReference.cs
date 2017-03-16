//
// ProjectTemplatePackageReference.cs
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
using System.Xml;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Templates
{
	public class ProjectTemplatePackageReference
	{
		public static ProjectTemplatePackageReference Create (XmlElement xmlElement)
		{
			return Create (xmlElement, FilePath.Null);
		}

		internal static ProjectTemplatePackageReference Create (XmlElement xmlElement, FilePath baseDirectory)
		{
			return new ProjectTemplatePackageReference {
				Id = GetAttribute (xmlElement, "id"),
				Version = GetAttribute (xmlElement, "version"),
				CreateCondition = GetAttribute (xmlElement, "if"),
				RequireLicenseAcceptance = GetLocalOrParentBoolAttribute (xmlElement, "requireLicenseAcceptance", true),
				IsLocalPackage = GetBoolAttribute (xmlElement, "local"),
				Directory = GetPath (xmlElement, "directory", baseDirectory)
			};
		}

		public string Id { get; private set; }
		public string Version { get; private set; }
		public string CreateCondition { get; private set; }

		internal bool IsLocalPackage { get; private set; }
		internal bool RequireLicenseAcceptance { get; private set; }
		internal FilePath Directory { get; private set; }

		static string GetAttribute (XmlElement xmlElement, string attributeName, string defaultValue = "")
		{
			foreach (XmlAttribute attribute in xmlElement.Attributes) {
				if (attributeName.Equals (attribute.Name, StringComparison.OrdinalIgnoreCase)) {
					return attribute.Value;
				}
			}
			return defaultValue;
		}

		static bool GetBoolAttribute (XmlElement xmlElement, string attributeName)
		{
			string attributeValue = GetAttribute (xmlElement, attributeName);
			return GetBoolValue (attributeValue);
		}

		static bool GetBoolValue (string value, bool defaultValue = false)
		{
			bool result = false;
			if (bool.TryParse (value, out result))
				return result;

			return defaultValue;
		}

		static bool GetLocalOrParentBoolAttribute (XmlElement xmlElement, string attributeName, bool defaultValue = false)
		{
			string attributeValue = GetLocalOrParentAttribute (xmlElement, attributeName);
			return GetBoolValue (attributeValue, defaultValue);
		}

		/// <summary>
		/// Local attribute value overrides parent attribute value.
		/// </summary>
		static string GetLocalOrParentAttribute (XmlElement xmlElement, string attributeName)
		{
			string attributeValue = GetAttribute (xmlElement, attributeName, null);
			if (attributeValue != null)
				return attributeValue;

			return GetAttribute ((XmlElement)xmlElement.ParentNode, attributeName);
		}

		static FilePath GetPath (XmlElement xmlElement, string attributeName, FilePath baseDirectory)
		{
			string directory = GetAttribute (xmlElement, attributeName, null);
			if (directory == null)
				return FilePath.Null;

			return baseDirectory.Combine (directory).FullPath;
		}
	}
}

