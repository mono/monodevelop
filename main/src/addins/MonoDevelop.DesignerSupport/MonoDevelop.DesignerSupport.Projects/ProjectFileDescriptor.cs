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
	class ProjectFileDescriptor: CustomDescriptor, IDisposable
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
			var pad = IdeApp.Workbench.GetPad <PropertyPad> ();
			if (pad == null)
				return;

			var grid = ((PropertyPad)pad.Content).PropertyGrid;
			if (grid.IsEditing)
				return;

			if (args.Any (arg => arg.ProjectFile == file))
				grid.Populate (saveEditSession: false);
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
				string type = DesktopService.GetMimeTypeForUri (file.Name);
				return DesktopService.GetMimeTypeDescription (type); 
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
		
		[MonoDevelop.Components.PropertyGrid.PropertyEditors.StandardValuesSeparator ("--")]
		class BuildActionStringsConverter : StandardStringsConverter
		{
			public override System.Collections.ICollection GetStandardStrings (ITypeDescriptorContext context)
			{
				ProjectFileDescriptor descriptor = context != null?
					context.Instance as ProjectFileDescriptor : null;
				
				if (descriptor != null && descriptor.file != null && descriptor.file.Project != null) {
					return descriptor.file.Project.GetBuildActions (descriptor.file.FilePath);
				} else {
					return new string[] {"Content", "None", "Compile"};
				}
			}
			
			public override bool CanConvertTo (ITypeDescriptorContext context, Type destinationType)
			{
				return destinationType == typeof (string);
			}
			
			public override object ConvertTo (ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
			{
				return (string)value;
			}

			public override bool CanConvertFrom (ITypeDescriptorContext context, Type sourceType)
			{
				return sourceType == typeof (string);
			}
			
			public override object ConvertFrom (ITypeDescriptorContext context,
			                                    System.Globalization.CultureInfo culture, object value)
			{
				if (!IsValid (context, value))
					throw new FormatException ("Invalid build target name");
				
				return (string)value;
			}
			
			public override bool IsValid (ITypeDescriptorContext context, object value)
			{
				if (!(value is string))
					return false;
				
				string str = (string) value;
				if (string.IsNullOrEmpty (str) || !char.IsLetter (str[0]))
					return false;
				
				for (int i = 1; i < str.Length; i++) {
					char c = str[i];
					if (char.IsLetterOrDigit (c) || c == '_')
						continue;
					else
						return false;
				}
				
				return true;
			}
			public override bool GetStandardValuesExclusive (ITypeDescriptorContext context)
			{
				return false;
			}
		}
		
		abstract class StandardStringsConverter : TypeConverter
		{
			public override bool GetStandardValuesSupported (ITypeDescriptorContext context)
			{
				return true;
			}
		
			public override bool CanConvertFrom (ITypeDescriptorContext context, Type sourceType)
			{
				return sourceType == typeof (string) || base.CanConvertFrom (context, sourceType);
			}
		
			public override bool CanConvertTo (ITypeDescriptorContext context, Type destinationType)
			{
				return destinationType == typeof (string) || base.CanConvertTo (context, destinationType);
			}
		
			public override object ConvertFrom (ITypeDescriptorContext context, 
			                                    System.Globalization.CultureInfo culture, object value)
			{
				if (value != null && value is string)
					return value;
				else
					return base.ConvertFrom (context, culture, value);
			}
		
			public override object ConvertTo (ITypeDescriptorContext context, System.Globalization.CultureInfo culture,
			                                  object value, Type destinationType)
			{
				if (value != null && (destinationType == typeof (string)))
					return value;
				else
					return base.ConvertTo (context, culture, value, destinationType);
			}
		
			public override StandardValuesCollection GetStandardValues (ITypeDescriptorContext context)
			{
				return new StandardValuesCollection (GetStandardStrings (context));
			}
		
			public abstract System.Collections.ICollection GetStandardStrings (ITypeDescriptorContext context);
		}
	}
}
