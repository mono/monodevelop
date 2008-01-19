//
// AspNetMSBuildProjectExtension.cs
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

using System;
using System.Xml;

using MonoDevelop.Prj2Make;
using MonoDevelop.Projects;

namespace AspNetAddIn
{
	public class AspNetMSBuildProjectExtension : MSBuildProjectExtension
	{
		const string myguid = "{349C5851-65DF-11DA-9384-00065B846F21}";

		public override string TypeGuid {
			get { return myguid; }
		}

		public override string Name {
			get { return "AspNet";}
		}

		public override bool Supports (string type_guid, string filename, string type_guids)
		{
			return (String.Compare (type_guid, myguid, true) == 0);
		}

		public override DotNetProject CreateProject (string type_guid, string filename, string type_guids)
		{
			if (!Supports (type_guid, filename, type_guids))
				throw new InvalidOperationException (String.Format ("Project of type guid = {0} not supported by this extension.", type_guid));

			string lang = Utils.GetLanguage (filename);
			//if (lang == null)
			return new AspNetAppProject (lang);
		}

		public override void ReadFlavorProperties (MSBuildData data, DotNetProject project, XmlNode node, string guid)
		{
			if (String.Compare (guid, myguid, true) != 0) {
				base.ReadFlavorProperties (data, project, node, guid);
				return;
			}

			AspNetAppProject asp_project = (AspNetAppProject) project;

			//FlavorProperties for web project
			if (node == null)
				return;
			node = Utils.MoveToChild (node, "WebProjectProperties");
			if (node == null)
				return;

			//Read
			int port = -1;
			if (!Utils.ReadAsInt (node, "DevelopmentServerPort", ref port) || port < 0)
				return;

			asp_project.XspParameters.Port = port;
		}

		//Writing methods

		public override void OnFinishWrite (MSBuildData data, DotNetProject project)
		{
			base.OnFinishWrite (data, project);

			if (!project.ExtendedProperties.Contains (typeof (MSBuildFileFormat))) {
				//New project file, eg when converting from another project
				XmlElement elem = data.Document.CreateElement ("Import", Utils.ns);
				data.Document.DocumentElement.InsertAfter (elem, data.Document.DocumentElement.LastChild);
				elem.SetAttribute ("Project", @"$(MSBuildExtensionsPath)\Microsoft\VisualStudio\v8.0\WebApplications\Microsoft.WebApplication.targets");
			}

			// insert this at the end
			XmlElement flavor_properties_element = MSBuildFileFormat.GetFlavorPropertiesElement (data, myguid, true);
			XmlNode webproject_element = Utils.MoveToChild (flavor_properties_element, "WebProjectProperties");
			if (webproject_element == null) {
				webproject_element = data.Document.CreateElement ("WebProjectProperties", Utils.ns);
				flavor_properties_element.AppendChild (webproject_element);
			}

			AspNetAppProject asp_project = (AspNetAppProject) project;
			Utils.EnsureChildValue (webproject_element, "DevelopmentServerPort", asp_project.XspParameters.Port);
		}

		public override string GetGuidChain (DotNetProject project)
		{
			if (project.GetType () != typeof (AspNetAppProject))
				return null;

			if (!MSBuildFileFormat.LanguageTypeGuids.ContainsKey (project.LanguageName))
				return null;

			return String.Format ("{0};{1}", myguid, MSBuildFileFormat.LanguageTypeGuids [project.LanguageName]);
		}
	}

}
