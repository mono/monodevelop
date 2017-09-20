//
// ProjectBuilder.Shared.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
//       Michael Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (c) 2009-2011 Novell, Inc (http://www.novell.com)
// Copyright (c) 2011-2015 Xamarin Inc. (http://www.xamarin.com)
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

using Microsoft.Build.Framework;
using System.Xml;
using System.IO;
using System;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

namespace MonoDevelop.Projects.MSBuild
{
	partial class ProjectBuilder
	{
		public void Dispose ()
		{
			buildEngine.UnloadProject (file);
		}

		public void Refresh ()
		{
			buildEngine.UnloadProject (file);
		}

		public void RefreshWithContent (string projectContent)
		{
			buildEngine.UnloadProject (file);
			buildEngine.SetUnsavedProjectContent (file, projectContent);
		}


		public static LoggerVerbosity GetVerbosity (MSBuildVerbosity verbosity)
		{
			switch (verbosity) {
			case MSBuildVerbosity.Quiet:
				return LoggerVerbosity.Quiet;
			case MSBuildVerbosity.Minimal:
				return LoggerVerbosity.Minimal;
			default:
				return LoggerVerbosity.Normal;
			case MSBuildVerbosity.Detailed:
				return LoggerVerbosity.Detailed;
			case MSBuildVerbosity.Diagnostic:
				return LoggerVerbosity.Diagnostic;
			}
		}

		//from MSBuildProjectService
		static string UnescapeString (string str)
		{
			int i = str.IndexOf ('%');
			while (i != -1 && i < str.Length - 2) {
				int c;
				if (int.TryParse (str.Substring (i+1, 2), System.Globalization.NumberStyles.HexNumber, null, out c))
					str = str.Substring (0, i) + (char) c + str.Substring (i + 3);
				i = str.IndexOf ('%', i + 1);
			}
			return str;
		}

		internal static string GenerateSolutionConfigurationContents (ProjectConfigurationInfo[] configurations)
		{
			// can't use XDocument because of the 2.0 builder
			// and don't just build a string because things may need escaping

			var doc = new XmlDocument ();
			var root = doc.CreateElement ("SolutionConfiguration");
			doc.AppendChild (root);
			foreach (var config in configurations) {
				var el = doc.CreateElement ("ProjectConfiguration");
				root.AppendChild (el);
				el.SetAttribute ("Project", config.ProjectGuid);
				el.SetAttribute ("AbsolutePath", config.ProjectFile);
				el.SetAttribute ("BuildProjectInSolution", config.Enabled ? "True" : "False");
				el.InnerText = string.Format (config.Configuration + "|" + config.Platform);
			}

			//match MSBuild formatting
			var options = new XmlWriterSettings {
				Indent = true,
				IndentChars = "",
				OmitXmlDeclaration = true,
			};
			using (var sw = new StringWriter ())
			using (var xw = XmlWriter.Create (sw, options)) {
				doc.WriteTo (xw);
				xw.Flush ();
				return sw.ToString ();
			}
		}
	}
}

