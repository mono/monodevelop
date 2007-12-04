//
// CSharpMSBuildProjectExtension.cs
//
// Author:
//   Ankit Jain <jankit@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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

using MonoDevelop.Projects;
using MonoDevelop.Core;

using CSharpBinding;

using System;
using System.IO;
using System.Xml;
using System.Xml.XPath;

namespace MonoDevelop.Prj2Make
{
	public class CSharpMSBuildProjectExtension : MSBuildProjectExtension
	{
		const string myguid = "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}";

		public override string TypeGuid {
			get { return myguid; }
		}

		public override string Name {
			get { return "C#";}
		}

		public override bool Supports (string type_guid, string filename, string type_guids)
		{
			if (String.IsNullOrEmpty (type_guid)) {
				if (Path.GetExtension (filename) == ".csproj")
					return true;
			} else {
				if (String.Compare (type_guid, myguid, true) == 0)
					return true;
			}

			return false;
			//FIXME: else look at the type_guids string, like "asp_guid;c#_guid"
		}

		public override DotNetProject CreateProject (string type_guid, string filename, string type_guids)
		{
			if (!Supports (type_guid, filename, type_guids))
				throw new InvalidOperationException (String.Format ("Project of type guid = {0} not supported by this extension.", type_guid));
			return new DotNetProject ("C#");
		}

		public override void ReadConfig (DotNetProject project, DotNetProjectConfiguration config, XPathNavigator nav, string basePath, IProgressMonitor monitor)
		{
			base.ReadConfig (project, config, nav, basePath, monitor);

			CSharpCompilerParameters csparams = (CSharpCompilerParameters) config.CompilationParameters;

			bool bool_tmp = false;
			string str_tmp = String.Empty;
			int int_tmp = 0;

			if (Utils.ReadAsBool (nav, "AllowUnsafeBlocks", ref bool_tmp))
				csparams.UnsafeCode = bool_tmp;

			if (Utils.ReadAsBool (nav, "Optimize", ref bool_tmp))
				csparams.Optimize = bool_tmp;

			if (Utils.ReadAsBool (nav, "CheckForOverflowUnderflow", ref bool_tmp))
				csparams.GenerateOverflowChecks = bool_tmp;

			if (Utils.ReadAsString (nav, "DefineConstants", ref str_tmp, true))
				csparams.DefineSymbols = str_tmp;

			if (Utils.ReadAsInt (nav, "WarningLevel", ref int_tmp))
				csparams.WarningLevel = int_tmp;

			if (Utils.ReadAsString (nav, "ApplicationIcon", ref str_tmp, false)) {
				string resolvedPath = Utils.MapAndResolvePath (basePath, str_tmp);
				if (resolvedPath != null)
					csparams.Win32Icon = Utils.Unescape (resolvedPath);
			}

			if (Utils.ReadAsString (nav, "Win32Resource", ref str_tmp, false)) {
				string resolvedPath = Utils.MapAndResolvePath (basePath, str_tmp);
				if (resolvedPath != null)
					csparams.Win32Resource = Utils.Unescape (resolvedPath);
			}
		}

		public override void WriteConfig (DotNetProject project, DotNetProjectConfiguration config, XmlElement configElement, IProgressMonitor monitor)
		{
			base.WriteConfig (project, config, configElement, monitor);

			if (project.LanguageName != "C#")
				// FIXME: extension list must be wrong, error!
				return;

			CSharpCompilerParameters csparams =
				(CSharpCompilerParameters) config.CompilationParameters;

			Utils.EnsureChildValue (configElement, "RootNamespace", project.DefaultNamespace);
			Utils.EnsureChildValue (configElement, "AllowUnsafeBlocks", csparams.UnsafeCode);
			Utils.EnsureChildValue (configElement, "Optimize", csparams.Optimize);
			Utils.EnsureChildValue (configElement, "CheckForOverflowUnderflow", csparams.GenerateOverflowChecks);
			Utils.EnsureChildValue (configElement, "DefineConstants", csparams.DefineSymbols);
			Utils.EnsureChildValue (configElement, "WarningLevel", csparams.WarningLevel);
			if (csparams.Win32Icon != null && csparams.Win32Icon.Length > 0)
				Utils.EnsureChildValue (configElement, "ApplicationIcon",
					Utils.CanonicalizePath (FileService.AbsoluteToRelativePath (
						project.BaseDirectory, csparams.Win32Icon)));

			if (csparams.Win32Resource != null && csparams.Win32Resource.Length > 0)
				Utils.EnsureChildValue (configElement, "Win32Resource",
					Utils.CanonicalizePath (FileService.AbsoluteToRelativePath (
						project.BaseDirectory, csparams.Win32Resource)));
		}

		public override void OnFinishWrite (MSBuildData data, DotNetProject project)
		{
			base.OnFinishWrite (data, project);
			if (Utils.GetMSBuildData (project) != null)
				// existing project file
				return;

			XmlElement elem = data.Document.CreateElement ("Import", Utils.ns);
			data.Document.DocumentElement.InsertAfter (elem, data.Document.DocumentElement.LastChild);
			elem.SetAttribute ("Project", @"$(MSBuildBinPath)\Microsoft.CSharp.Targets");
		}

		public override string GetGuidChain (DotNetProject project)
		{
			if (project.GetType () != typeof (DotNetProject) || project.LanguageName != "C#")
				return null;

			return myguid;
		}

	}
}
