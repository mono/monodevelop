//
// ProjectExtensions.cs
//
// Author:
//       Rodrigo Moya <rodrigo.moya@xamarin.com>
//
// Copyright (c) 2019 
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
using MonoDevelop.Projects;
using MonoDevelop.Ide;

namespace MonoDevelop.DotNetCore
{
	static class ProjectExtensions
	{
		const string RunArgumentsProperty = "RunArguments";

		public static string GetRunArguments (this DotNetProject project, ConfigurationSelector configSel, string outputFileName, string appArgs)
		{
			var currentConfig = project.GetConfiguration (configSel) as ProjectConfiguration;
			var runArguments = currentConfig?.Properties?.GetValue (RunArgumentsProperty)?.Replace ('\\', '/');

			return string.IsNullOrEmpty (runArguments) ? $"\"{outputFileName}\" {appArgs}" : $"{runArguments} {appArgs}";
		}

		public static string GetWorkingDirectory (this DotNetProject project, ConfigurationSelector configSel, string defaultDir)
		{
			return defaultDir ?? project.GetOutputFileName (configSel).ParentDirectory;
		}
	}
}
