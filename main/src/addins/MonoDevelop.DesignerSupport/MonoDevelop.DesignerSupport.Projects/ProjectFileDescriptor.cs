// ProjectFileDescriptor.cs
//
//Author:
//  Lluis Sanchez Gual
//
//Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Ide;
using System.Linq;

namespace MonoDevelop.DesignerSupport
{
	partial class ProjectFileDescriptor : CustomDescriptor, IDisposable
	{
		ProjectFile file;
		Project project;

		public ProjectFileDescriptor (ProjectFile file)
		{
			this.file = file;
			project = file.Project;
			if (project != null) {
				project.FilePropertyChangedInProject += OnFilePropertyChangedInProject;
			}
		}

		void OnFilePropertyChangedInProject (object sender, ProjectFileEventArgs args)
		{
			var pad = IdeApp.Workbench.GetPad <IPropertyPad> ();
			if (pad == null)
				return;

			var propertyPad = (IPropertyPad)pad.Content;
			if (propertyPad.IsGridEditing)
				return;

			if (args.Any (arg => arg.ProjectFile == file))
				propertyPad.PopulateGrid (saveEditSession: false);
		}

		void IDisposable.Dispose ()
		{
			if (project != null) {
				project.FilePropertyChangedInProject -= OnFilePropertyChangedInProject;
				project = null;
			}
		}

		[LocalizedCategory ("Misc")]
		[LocalizedDisplayName ("Name")]
		[LocalizedDescription ("Name of the file.")]
		public string Name {
			get { return System.IO.Path.GetFileName (file.Name); }
		}

		[LocalizedCategory ("Misc")]
		[LocalizedDisplayName ("Path")]
		[LocalizedDescription ("Full path of the file.")]
		public string Path {
			get { return file.FilePath; }
		}

		[LocalizedCategory ("Misc")]
		[LocalizedDisplayName ("Type")]
		[LocalizedDescription ("Type of the file.")]
		public string FileType {
			get {
				string type = IdeServices.DesktopService.GetMimeTypeForUri (file.Name);
				return IdeServices.DesktopService.GetMimeTypeDescription (type);
			}
		}

		[LocalizedCategory ("Build")]
		[LocalizedDisplayName ("Build action")]
		[LocalizedDescription ("Action to perform when building this file.")]
		[TypeConverter (typeof (BuildActionStringsConverter))]
		[RefreshProperties(RefreshProperties.All)]
		public string BuildAction {
			get { return file.BuildAction; }
			set { file.BuildAction = value; }
		}

		[LocalizedCategory ("Build")]
		[LocalizedDisplayName ("Resource ID")]
		[LocalizedDescription ("Identifier of the embedded resource.")]
		public string ResourceId {
			get { return file.ResourceId; }
			set { file.ResourceId = value; }
		}

		[LocalizedCategory ("Build")]
		[LocalizedDisplayName ("Copy to output directory")]
		[LocalizedDescription ("Whether to copy the file to the project's output directory when the project is built.")]
		public FileCopyMode CopyToOutputDirectory {
			get { return file.CopyToOutputDirectory; }
			set { file.CopyToOutputDirectory = value; }
		}

		[LocalizedCategory ("Build")]
		[LocalizedDisplayName ("Custom Tool")]
		[LocalizedDescription ("The ID of a custom code generator.")]
		[TypeConverter (typeof (CustomToolStringsConverter))]
		public string Generator {
			get { return file.Generator; }
			set { file.Generator = value; }
		}

		[LocalizedCategory ("Build")]
		[LocalizedDisplayName ("Custom Tool Namespace")]
		[LocalizedDescription ("Overrides the namespace in which the custom code generator should generate code.")]
		public string CustomToolNamespace {
			get { return file.CustomToolNamespace; }
			set { file.CustomToolNamespace = value; }
		}

		protected override bool IsReadOnly (string propertyName)
		{
			return false;
		}
	}
}
