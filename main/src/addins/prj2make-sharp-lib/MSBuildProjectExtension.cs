//
// MSBuildProjectExtension.cs
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
using System.Xml.XPath;

namespace MonoDevelop.Prj2Make
{
	public abstract class MSBuildProjectExtension
	{
		internal MSBuildProjectExtension Next = null;

		public abstract string TypeGuid {
			get;
		}

		public virtual string Name {
			get { return "MSBuildProjectExtension"; }
		}

		public virtual bool IsLanguage {
			get { return false; }
		}

		// Must match the Id used by MonoDevelop for the language
		public virtual string LanguageId {
			get { return null; }
		}

		public virtual bool Supports (string guid, string filename, string type_guids)
		{
			return Next.Supports (guid, filename, type_guids);
		}

		public virtual DotNetProject CreateProject (string type_guid, string filename, string type_guids)
		{
			return Next.CreateProject (type_guid, filename, type_guids);
		}

		public virtual void ReadConfig (DotNetProject project, DotNetProjectConfiguration config, XPathNavigator nav, string basePath, IProgressMonitor monitor)
		{
			Next.ReadConfig (project, config, nav, basePath, monitor);
		}

		public virtual void ReadItemGroups (MSBuildData data, DotNetProject project, DotNetProjectConfiguration globalConfig, string basePath, IProgressMonitor monitor)
		{
			Next.ReadItemGroups (data, project, globalConfig, basePath, monitor);
		}

		public virtual void ReadItemGroup (MSBuildData data, DotNetProject project, DotNetProjectConfiguration globalConfig, string include, string basePath, XmlNode node, IProgressMonitor monitor)
		{
			Next.ReadItemGroup (data, project, globalConfig, include, basePath, node, monitor);
		}

		public virtual void ReadFlavorProperties (MSBuildData data, DotNetProject project, XmlNode node, string guid)
		{
			Next.ReadFlavorProperties (data, project, node, guid);
		}

		public virtual void OnFinishRead (MSBuildData data, DotNetProject project)
		{
			Next.OnFinishRead (data, project);
		}

		//Writing methods

		public virtual void WriteConfig (DotNetProject project, DotNetProjectConfiguration config, XmlElement configElement, IProgressMonitor monitor)
		{
			Next.WriteConfig (project, config, configElement, monitor);
		}

		public virtual XmlElement FileToXmlElement (MSBuildData data, Project project, ProjectFile projectFile)
		{
			return Next.FileToXmlElement (data, project, projectFile);
		}

		public virtual XmlElement ReferenceToXmlElement (MSBuildData data, Project project, ProjectReference projectRef)
		{
			return Next.ReferenceToXmlElement (data, project, projectRef);
		}

		public virtual void OnFinishWrite (MSBuildData data, DotNetProject project)
		{
			Next.OnFinishWrite (data, project);
		}

		public virtual string GetGuidChain (DotNetProject project)
		{
			return null;
		}

		public override string ToString ()
		{
			return Name + (Next != null ? " -> " + Next.ToString () : String.Empty);
		}
	}
}
