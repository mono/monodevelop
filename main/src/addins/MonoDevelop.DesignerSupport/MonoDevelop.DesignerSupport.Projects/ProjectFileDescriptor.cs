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
using System.Reflection;
using System.IO;

namespace MonoDevelop.DesignerSupport
{
	class ProjectFileDescriptor: CustomDescriptor
	{
		ProjectFile file;
		
		public ProjectFileDescriptor (ProjectFile file)
		{
			this.file = file;
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
				string type = MonoDevelop.Core.Gui.Services.PlatformService.GetMimeTypeForUri (file.Name);
				return MonoDevelop.Core.Gui.Services.PlatformService.GetMimeTypeDescription (type); 
			}
		}
		
		[LocalizedCategory ("Build")]
		[LocalizedDisplayName ("Build action")]
		[LocalizedDescription ("Action to perform when building this file.")]
		[TypeConverter (typeof (BuildActionStringsConverter))]
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
		
		protected override bool IsReadOnly (string propertyName)
		{
			if (propertyName == "ResourceId" && file.BuildAction != MonoDevelop.Projects.BuildAction.EmbeddedResource)
				return true;
			return false;
		}
		
		[MonoDevelop.DesignerSupport.PropertyGrid.PropertyEditors.StandardValuesSeparator ("--")]
		class BuildActionStringsConverter : StandardStringsConverter
		{
			public override System.Collections.ICollection GetStandardStrings (ITypeDescriptorContext context)
			{
				ProjectFileDescriptor descriptor = context != null?
					context.Instance as ProjectFileDescriptor : null;
				
				if (descriptor != null && descriptor.file != null && descriptor.file.Project != null) {
					return descriptor.file.Project.GetBuildActions ();
				} else {
					return new string[] {"Content", "None", "Compile"};
				}
			}
			
			public override bool CanConvertTo (System.ComponentModel.ITypeDescriptorContext context, System.Type destinationType)
			{
				return destinationType == typeof (string);
			}
			
			public override object ConvertTo (System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, System.Type destinationType)
			{
				return MonoDevelop.Projects.BuildAction.Translate ((string)value);
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
				
				return MonoDevelop.Projects.BuildAction.ReTranslate ((string)value);
			}
			
			public override bool IsValid (ITypeDescriptorContext context, object value)
			{
				if (!(value is string))
					return false;
				
				string str = MonoDevelop.Projects.BuildAction.ReTranslate ((string) value);
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
				//only make the list exclusive if we managed to get a list from the parent project
				ProjectFileDescriptor descriptor = context != null?
					context.Instance as ProjectFileDescriptor : null;
				
				return (descriptor != null && descriptor.file != null && descriptor.file.Project != null);
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
