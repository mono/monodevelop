//
// DotNetCoreProjectReader.cs
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

using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Projects.MSBuild;

namespace MonoDevelop.DotNetCore
{
	class DotNetCoreProjectReader : WorkspaceObjectReader
	{
		public override bool CanRead (FilePath file, Type expectedType)
		{
			if (!expectedType.IsAssignableFrom (typeof(SolutionItem)))
				return false;

			if (file.HasSupportedDotNetCoreProjectFileExtension ())
				return IsDotNetCoreProjectFile (file);

			return false;
		}

		/// <summary>
		/// Check the Project element contains an Sdk and ToolsVersion attribute.
		/// </summary>
		static bool IsDotNetCoreProjectFile (FilePath file)
		{
			return GetDotNetCoreSdk (file) != null;
		}

		/// <summary>
		/// Returns the first Sdk property defined by the project, if any exists
		/// </summary>
		public static string GetDotNetCoreSdk (FilePath file)
		{
			try {
				// A .NET Core project can define an sdk in any of the following ways
				// 1) An Sdk attribute on the Project node
				// 2) An Sdk node as a child of the Project node
				// 3) An Sdk attribute on any Import node
				var document = new XmlDocument ();
				document.Load (new StreamReader (file));
				XmlNode projectNode = document.SelectSingleNode ("/Project");
				if (projectNode != null) {
					XmlAttribute sdkAttr = projectNode.Attributes ["Sdk"];
					if (sdkAttr != null) {
						// Found an Sdk definition on the root Project node
						return sdkAttr.Value;
					}

					var childSdkNode = projectNode.SelectSingleNode ("Sdk");
					if (childSdkNode != null) {
						var name = childSdkNode.Attributes ["Name"];
						if (name != null) {
							// Found an Sdk definition on an Sdk node
							return name.Value;
						}
					}

					foreach (XmlNode importNode in projectNode.SelectNodes ("//Import")) {
						sdkAttr = importNode.Attributes ["Sdk"];
						if (sdkAttr != null) {
							return sdkAttr.Value;
						}
					}
				}
			} catch {
				// Ignore
			}
			return null;
		}

		public override Task<SolutionItem> LoadSolutionItem (ProgressMonitor monitor, SolutionLoadContext ctx, string fileName, MSBuildFileFormat expectedFormat, string typeGuid, string itemGuid)
		{
			return Task.Run (() => {
				if (CanRead (fileName, typeof(SolutionItem))) {
					DotNetCoreSdk.EnsureInitialized ();
					return MSBuildProjectService.LoadItem (monitor, fileName, MSBuildFileFormat.VS2012, typeGuid, itemGuid, ctx);
				}

				throw new NotSupportedException ();
			});
		}
	}
}
