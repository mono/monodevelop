//
// DotNetCoreProjectTemplateStringTagProvider.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2018 Microsoft
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
using System.Linq;
using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Core.StringParsing;

namespace MonoDevelop.DotNetCore.Templating
{
	[Extension]
	class DotNetCoreProjectTemplateStringTagProvider : IStringTagProvider
	{
		string [] SupportedSDK = { "2.1", "2.2", "3.0" };

		public IEnumerable<StringTagDescription> GetTags (Type type)
		{
			for (var i = 0; i < SupportedSDK.Length; i++) {
				yield return new StringTagDescription (
					$"DotNetCoreSdk.{SupportedSDK[i]}.Templates.Common.ProjectTemplates.nupkg",
					GettextCatalog.GetString (string.Format (".NET Core SDK {0} Common Project Templates NuGet package path", SupportedSDK [i]))
				);

				yield return new StringTagDescription (
					$"DotNetCoreSdk.{SupportedSDK [i]}.Templates.Test.ProjectTemplates.nupkg",
					GettextCatalog.GetString (string.Format (".NET Core SDK {0} Test Project Templates NuGet package path", SupportedSDK[i]))
				);

				yield return new StringTagDescription (
					$"DotNetCoreSdk.{SupportedSDK [i]}.Templates.Web.ProjectTemplates.nupkg",
					GettextCatalog.GetString (string.Format (".NET Core SDK {0} Web Project Templates NuGet package path", SupportedSDK[i]))
				);
					
				if (i > 0 ) { //2.2 and before 
					yield return new StringTagDescription (
						$"DotNetCoreSdk.{SupportedSDK [i]}.Templates.NUnit3.DotNetNew.Template.nupkg",
						GettextCatalog.GetString (string.Format (".NET Core SDK {0} NUnit Project Templates NuGet package path", SupportedSDK [i]))
					);
				}
			}
		}

		public object GetTagValue (object instance, string tag)
		{
			string templateFileNamePrefix = GetTemplateFileNamePrefix (tag);
			if (string.IsNullOrEmpty (templateFileNamePrefix)) {
				return string.Empty;
			}

			string templatesDirectory = GetDotNetCoreSdkTemplatesDirectory (tag);
			if (string.IsNullOrEmpty (templatesDirectory)) {
				return string.Empty;
			}

			foreach (string fullPath in EnumerateFiles (templatesDirectory)) {
				if (IsMatch (fullPath, templateFileNamePrefix)) {
					return fullPath;
				}
			}

			return string.Empty;
		}

		bool IsMatch (FilePath fullPath, string fileNamePrefix)
		{
			return fullPath.HasExtension (".nupkg") &&
				fullPath.FileNameWithoutExtension.StartsWith (fileNamePrefix, StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Returns a project template file name prefix. For example:
		/// "microsoft.dotnet.common.projecttemplates."
		/// </summary>
		string GetTemplateFileNamePrefix (string tag)
		{
			const string templatesTagString = ".Templates.";
			int index = tag.IndexOf (templatesTagString, StringComparison.OrdinalIgnoreCase);
			if (index == -1) {
				return null;
			}

			string partialFileName = tag.Substring (index + templatesTagString.Length);

			const string nupkgTagString = ".nupkg";
			if (!partialFileName.EndsWith (nupkgTagString, StringComparison.OrdinalIgnoreCase)) {
				return null;
			}

			partialFileName = partialFileName.Substring (0, partialFileName.Length - nupkgTagString.Length + 1);

			return partialFileName.IndexOf ("NUnit3.", StringComparison.OrdinalIgnoreCase) == 0 ?
				partialFileName.ToLowerInvariant () :
				"microsoft.dotnet." + partialFileName.ToLowerInvariant ();
		}

		/// <summary>
		/// Only .NET Core SDKs 2.1/2.2/3.0 are supported.
		/// </summary>
		string GetDotNetCoreSdkTemplatesDirectory (string tag)
		{
			DotNetCoreVersion dotNetCoreSdk = null;

			if (tag.StartsWith ("DotNetCoreSdk.3.0", StringComparison.OrdinalIgnoreCase)) {
				dotNetCoreSdk = GetDotNetCoreSdk30Version ();
			} else if (tag.StartsWith ("DotNetCoreSdk.2.2", StringComparison.OrdinalIgnoreCase)) {
				dotNetCoreSdk = GetDotNetCoreSdk22Version ();
			} else if (tag.StartsWith ("DotNetCoreSdk.2.1", StringComparison.OrdinalIgnoreCase)) {
				dotNetCoreSdk = GetDotNetCoreSdk21Version ();
			}

			if (dotNetCoreSdk == null)
				return null;

			string templatesDirectory = Path.Combine (
				DotNetCoreSdk.SdkRootPath,
				dotNetCoreSdk.OriginalString,
				"Templates"
			);

			if (DirectoryExists (templatesDirectory)) {
				return templatesDirectory;
			}

			return string.Empty;
		}

		DotNetCoreVersion GetDotNetCoreSdk30Version ()
		{
			return DotNetCoreSdk.Versions
				.FirstOrDefault (v => v.Major == 3 && v.Minor == 0);
		}

		DotNetCoreVersion GetDotNetCoreSdk22Version ()
		{
			return DotNetCoreSdk.Versions
				.FirstOrDefault (v => v.Major == 2 && v.Minor == 2);
		}

		DotNetCoreVersion GetDotNetCoreSdk21Version ()
		{
			return DotNetCoreSdk.Versions
				.FirstOrDefault (v => v.Major == 2 && v.Minor == 1 && v.Patch >= 300);
		}

		/// <summary>
		/// Used by unit tests.
		/// </summary>
		internal Func<string, bool> DirectoryExists = Directory.Exists;

		/// <summary>
		/// Used by unit tests.
		/// </summary>
		internal Func<string, IEnumerable<string>> EnumerateFiles = Directory.EnumerateFiles;
	}
}
