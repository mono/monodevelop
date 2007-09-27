//
// VBNetMSBuildProjectExtension.cs
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

using MonoDevelop.Core;
using MonoDevelop.Projects;
using VBBinding;

using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.XPath;

namespace MonoDevelop.Prj2Make
{

	public class VBNetMSBuildProjectExtension : MSBuildProjectExtension
	{
		const string myguid = "{F184B08F-C81C-45F6-A57F-5ABD9991F28F}";

		public override string TypeGuid {
			get { return myguid; }
		}

		public override string Name {
			get { return "VBNet";}
		}

		public override bool Supports (string type_guid, string filename, string type_guids)
		{
			if (String.IsNullOrEmpty (type_guid)) {
				if (Path.GetExtension (filename) == ".vbproj")
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

			return new DotNetProject ("VBNet");
		}

		public override void ReadConfig (DotNetProject project, DotNetProjectConfiguration config, XPathNavigator nav, string basePath, IProgressMonitor monitor)
		{
			base.ReadConfig (project, config, nav, basePath, monitor);

			if (project.LanguageName != "VBNet")
				// FIXME: extension list must be wrong, error!
				return;

			VBCompilerParameters vbparams = (VBCompilerParameters) config.CompilationParameters;

			bool bool_tmp = false;
			string str_tmp = String.Empty;
			int int_tmp = 0;

			if (Utils.ReadAsString (nav, "RootNamespace", ref str_tmp, false))
				vbparams.RootNamespace = str_tmp;

			if (Utils.ReadAsBool (nav, "AllowUnsafeBlocks", ref bool_tmp))
				vbparams.UnsafeCode = bool_tmp;

			if (Utils.ReadAsBool (nav, "Optimize", ref bool_tmp))
				vbparams.Optimize = bool_tmp;

			if (Utils.ReadAsBool (nav, "CheckForOverflowUnderflow", ref bool_tmp))
				vbparams.GenerateOverflowChecks = bool_tmp;

			if (Utils.ReadAsString (nav, "DefineConstants", ref str_tmp, true))
				vbparams.DefineSymbols = str_tmp;

			if (Utils.ReadAsInt (nav, "WarningLevel", ref int_tmp))
				vbparams.WarningLevel = int_tmp;

			if (Utils.ReadOffOnAsBool (nav, "OptionExplicit", ref bool_tmp))
				vbparams.OptionExplicit = bool_tmp;

			if (Utils.ReadOffOnAsBool (nav, "OptionStrict", ref bool_tmp))
				vbparams.OptionStrict = bool_tmp;

			if (Utils.ReadAsString (nav, "ApplicationIcon", ref str_tmp, false)) {
				string resolvedPath = Utils.MapAndResolvePath (basePath, str_tmp);
				if (resolvedPath != null)
					vbparams.Win32Icon = Utils.Unescape (resolvedPath);
			}

			if (Utils.ReadAsString (nav, "Win32Resource", ref str_tmp, false)) {
				string resolvedPath = Utils.MapAndResolvePath (basePath, str_tmp);
				if (resolvedPath != null)
					vbparams.Win32Resource = Utils.Unescape (resolvedPath);
			}
			//FIXME: OptionCompare, add support to VBnet binding, params etc
		}

		StringBuilder importsBuilder = null;
		public override void ReadItemGroups (MSBuildData data, DotNetProject project, DotNetProjectConfiguration globalConfig, string basePath, IProgressMonitor monitor)
		{
			importsBuilder = new StringBuilder ();
			base.ReadItemGroups (data, project, globalConfig, basePath, monitor);

			if (importsBuilder.Length > 0) {
				importsBuilder.Length --;
				VBCompilerParameters vbparams = (VBCompilerParameters) globalConfig.CompilationParameters;
				vbparams.Imports = importsBuilder.ToString ();
			}
			importsBuilder = null;
		}

		public override void ReadItemGroup (MSBuildData data, DotNetProject project, DotNetProjectConfiguration globalConfig, string include, string basePath, XmlNode node, IProgressMonitor monitor)
		{
			if (node.LocalName == "Import") {
				importsBuilder.AppendFormat ("{0},", include);
			} else {
				base.ReadItemGroup (data, project, globalConfig, include, basePath, node, monitor);
			}
		}

		public override void WriteConfig (DotNetProject project, DotNetProjectConfiguration config, XmlElement configElement, IProgressMonitor monitor)
		{
			base.WriteConfig (project, config, configElement, monitor);

			VBCompilerParameters vbparams =
				(VBCompilerParameters) config.CompilationParameters;

			Utils.EnsureChildValue (configElement, "RootNamespace", vbparams.RootNamespace);
			Utils.EnsureChildValue (configElement, "AllowUnsafeBlocks", vbparams.UnsafeCode);
			Utils.EnsureChildValue (configElement, "Optimize", vbparams.Optimize);
			Utils.EnsureChildValue (configElement, "CheckForOverflowUnderflow", vbparams.GenerateOverflowChecks);
			Utils.EnsureChildValue (configElement, "DefineConstants", vbparams.DefineSymbols);
			Utils.EnsureChildValue (configElement, "WarningLevel", vbparams.WarningLevel);
			Utils.EnsureChildValue (configElement, "OptionExplicit", vbparams.OptionExplicit ? "On" : "Off");
			Utils.EnsureChildValue (configElement, "OptionStrict", vbparams.OptionStrict ? "On" : "Off");
			if (vbparams.Win32Icon != null && vbparams.Win32Icon.Length > 0)
				Utils.EnsureChildValue (configElement, "ApplicationIcon",
					Utils.CanonicalizePath (Runtime.FileService.AbsoluteToRelativePath (
						project.BaseDirectory, vbparams.Win32Icon)));

			if (vbparams.Win32Resource != null && vbparams.Win32Resource.Length > 0)
				Utils.EnsureChildValue (configElement, "Win32Resource",
					Utils.CanonicalizePath (Runtime.FileService.AbsoluteToRelativePath (
						project.BaseDirectory, vbparams.Win32Resource)));

			//FIXME: VB.net Imports
		}

		public override void OnFinishWrite (MSBuildData data, DotNetProject project)
		{
			base.OnFinishWrite (data, project);
			if (Utils.GetMSBuildData (project) != null)
				// existing project file
				return;

			XmlElement elem = data.Document.CreateElement ("Import", Utils.ns);
			data.Document.DocumentElement.InsertAfter (elem, data.Document.DocumentElement.LastChild);
			elem.SetAttribute ("Project", @"$(MSBuildBinPath)\Microsoft.VisualBasic.targets");
		}

		public override string GetGuidChain (DotNetProject project)
		{
			if (project.GetType () != typeof (DotNetProject) || project.LanguageName != "VBNet")
				return null;

			return myguid;
		}

	}
}
