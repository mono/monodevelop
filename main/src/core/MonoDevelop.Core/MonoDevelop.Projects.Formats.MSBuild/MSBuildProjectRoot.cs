//
// MSBuildProjectRoot.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc (http://www.xamarin.com)
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

namespace MonoDevelop.Projects.Formats.MSBuild
{
	partial class MSBuildProject
	{
		class MSBuildProjectRoot : MSBuildObject
		{
			MSBuildProject project;

			static readonly string [] knownAttributes = { "DefaultTargets", "ToolsVersion", "xmlns" };

			public MSBuildProjectRoot (MSBuildProject project)
			{
				this.project = project;
			}

			internal override string GetElementName ()
			{
				return "Project";
			}

			internal override string [] GetKnownAttributes ()
			{
				return knownAttributes;
			}

			internal override void ReadAttribute (string name, string value)
			{
				switch (name) {
					case "DefaultTargets": project.defaultTargets = value; return;
					case "ToolsVersion": project.toolsVersion = value; return;
				}
				base.ReadAttribute (name, value);
			}

			internal override string WriteAttribute (string name)
			{
				switch (name) {
					case "DefaultTargets": return project.defaultTargets;
					case "ToolsVersion": return project.toolsVersion;
					case "xmlns": return MSBuildProject.Schema;
				}
				return base.WriteAttribute (name);
			}

			internal override void ReadChildElement (MSBuildXmlReader reader)
			{
				MSBuildObject ob = null;
				switch (reader.LocalName) {
					case "ItemGroup": ob = new MSBuildItemGroup (); break;
					case "PropertyGroup": ob = new MSBuildPropertyGroup (); break;
					case "ImportGroup": ob = new MSBuildImportGroup (); break;
					case "Import": ob = new MSBuildImport (); break;
					case "Target": ob = new MSBuildTarget (); break;
					case "Choose": ob = new MSBuildChoose (); break;
				}
				if (ob != null) {
					ob.ParentProject = project;
					ob.Read (reader);
					project.objects.Add (ob);
				} else
					base.ReadChildElement (reader);
			}

			internal override System.Collections.Generic.IEnumerable<MSBuildObject> GetChildren ()
			{
				return project.objects;
			}
		}
	}
}

