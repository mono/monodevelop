//
// SilverlightMSBuildProjectExtension.cs
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

using System;
using System.Xml;

namespace MonoDevelop.Prj2Make
{

	public class SilverlightMSBuildProjectExtension : MSBuildProjectExtension
	{
		const string myguid = "{A1591282-1198-4647-A2B1-27E5FF5F6F3B}";

		public override string TypeGuid {
			get { return myguid; }
		}

		public override string Name {
			get { return "Silverlight";}
		}

		public override bool Supports (string type_guid, string filename, string type_guids)
		{
			return (String.Compare (type_guid, myguid, true) == 0);
		}

		public override DotNetProject CreateProject (string type_guid, string filename, string type_guids)
		{
			if (!Supports (type_guid, filename, type_guids))
				throw new InvalidOperationException (String.Format ("Project of type guid = {0} not supported by this extension.", type_guid));

			int semicolon = type_guids.IndexOf (';');
			if (semicolon < 0 || semicolon == type_guids.Length)
				throw new Exception (String.Format ("Invalid type guid. Missing super-type guid"));

			//FIXME: this shouldn't be required
			type_guids = type_guids.Substring (semicolon + 1);
			type_guid = type_guids.Split (';') [0];
			return base.CreateProject (type_guid, filename, type_guids);
		}

		public override void ReadItemGroup (MSBuildData data, DotNetProject project, DotNetProjectConfiguration globalConfig, string include, string basePath, XmlNode node, IProgressMonitor monitor)
		{
			if (node.LocalName == "SilverlightPage") {
				//FIXME: this should be available only for 
				//<TargetFrameworkVersion>v3.5</TargetFrameworkVersion>
				//
				//This tag also has a
				//	<Generator>MSBuild:Compile</Generator>
				string path = Utils.GetValidPath (monitor, basePath, include);
				if (path == null)
					return;

				ProjectFile pf = project.AddFile (path, BuildAction.EmbeddedResource);
				pf.ExtendedProperties ["MonoDevelop.MSBuildFileFormat.SilverlightPage"] = String.Empty;
				data.ProjectFileElements [pf] = (XmlElement) node;

				// Add the corresponding %.g.cs to the project, we'll skip this
				// when saving the project file
				pf = project.AddFile (path + ".g.cs", BuildAction.Compile);
				pf.ExtendedProperties ["MonoDevelop.MSBuildFileFormat.SilverlightGeneratedFile"] = String.Empty;
				data.ProjectFileElements [pf] = (XmlElement) node;
			} else {
				base.ReadItemGroup (data, project, globalConfig, include, basePath, node, monitor);
			}
		}

		public override XmlElement FileToXmlElement (MSBuildData d, Project project, ProjectFile projectFile)
		{
			if (projectFile.ExtendedProperties ["MonoDevelop.MSBuildFileFormat.SilverlightGeneratedFile"] != null)
				//Ignore the generated %.xaml.g.cs files
				return null;

			return base.FileToXmlElement (d, project, projectFile);
		}

		public override string GetGuidChain (DotNetProject project)
		{
			return null;
		}

	}

}
