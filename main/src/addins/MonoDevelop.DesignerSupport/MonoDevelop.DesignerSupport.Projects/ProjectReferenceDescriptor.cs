// ProjectReferenceDescriptor.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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
//
//


using System;
using System.ComponentModel;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using System.Reflection;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.DesignerSupport.Projects
{
	class ProjectReferenceDescriptor: CustomDescriptor
	{
		ProjectReference pref;
		
		public ProjectReferenceDescriptor (ProjectReference pref)
		{
			this.pref = pref;
		}
		
		[LocalizedCategory ("Reference")]
		[LocalizedDisplayName ("Type")]
		[LocalizedDescription ("Type of the reference.")]
		public string Type {
			get {
				switch (pref.ReferenceType) {
				case ReferenceType.Assembly:
					return GettextCatalog.GetString ("Assembly");
				case ReferenceType.Gac:
					return GettextCatalog.GetString ("Package");
				case ReferenceType.Project:
					return GettextCatalog.GetString ("Project");
				case ReferenceType.Custom:
					return GettextCatalog.GetString ("Custom");
				}
				return string.Empty;
			}
		}
		
		[LocalizedCategory ("Reference")]
		[LocalizedDisplayName ("Project")]
		[LocalizedDescription ("Referenced project, when the reference is of type 'Project'.")]
		public string ProjectName {
			get {
				if (pref.ReferenceType == ReferenceType.Project)
					return pref.Reference;
				else
					return string.Empty;
			}
		}
		
		[LocalizedCategory ("Reference")]
		[LocalizedDisplayName ("Assembly Name")]
		[LocalizedDescription ("Name of the assembly.")]
		public string FullName {
			get {
				if (Path.Length > 0) {
					try {
						return AssemblyName.GetAssemblyName (Path).Name;
					} catch {
					}
				}
				return string.Empty;
			}
		}
		
		[LocalizedCategory ("Reference")]
		[LocalizedDisplayName ("Assembly Version")]
		[LocalizedDescription ("Version of the assembly.")]
		public string Version {
			get {
				if (Path.Length > 0) {
					try {
						return AssemblyName.GetAssemblyName (Path).Version.ToString ();
					} catch {
					}
				}
				return string.Empty;
			}
		}
		
		[LocalizedCategory ("Reference")]
		[LocalizedDisplayName ("Path")]
		[LocalizedDescription ("Path to the assembly.")]
		public string Path {
			get {
				string[] files = pref.GetReferencedFileNames (IdeApp.Workspace.ActiveConfiguration);
				if (files.Length > 0)
					return files [0];
				else
					return string.Empty;
			}
		}
		
		[LocalizedCategory ("Build")]
		[LocalizedDisplayName ("Local Copy")]
		[LocalizedDescription ("Copy the referenced assembly to the output directory.")]
		public bool LocalCopy {
			get { return pref.LocalCopy; }
			set { pref.LocalCopy = value; }
		}
		
		[LocalizedCategory ("Build")]
		[LocalizedDisplayName ("Specific Version")]
		[LocalizedDescription ("Require a specific version of the assembly. A warning will be issued if the specific version is not found in the system.")]
		public bool SpecificVersion {
			get { return pref.SpecificVersion; }
			set { pref.SpecificVersion = value; }
		}
		
		[LocalizedCategory ("Reference")]
		[LocalizedDisplayName ("Package")]
		[LocalizedDescription ("Package that provides this reference.")]
		public string Package {
			get {
				if (pref.ReferenceType == ReferenceType.Gac && pref.Package != null)
					return pref.Package.Name + " "  + pref.Package.Version;
				else
					return string.Empty;
			}
		}
		
		protected override bool IsReadOnly (string propertyName)
		{
			if (propertyName == "SpecificVersion" && (pref.ReferenceType == ReferenceType.Project || pref.ReferenceType == ReferenceType.Custom))
				return true;
			return false;
		}
	}
}
