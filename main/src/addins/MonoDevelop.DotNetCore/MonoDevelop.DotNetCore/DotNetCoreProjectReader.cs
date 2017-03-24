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

		public static string GetDotNetCoreSdk (FilePath file)
		{
			try {
				using (var tr = new XmlTextReader (new StreamReader (file))) {
					if (tr.MoveToContent () == XmlNodeType.Element) {
						if (tr.LocalName != "Project")
							return null;

						string sdk = tr.GetAttribute ("Sdk");
						if (string.IsNullOrEmpty (sdk))
							return null;

						return sdk;
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
					return MSBuildProjectService.LoadItem (monitor, fileName, MSBuildFileFormat.VS2017, typeGuid, itemGuid, ctx);
				}

				throw new NotSupportedException ();
			});
		}
	}
}
