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
		
		[DisplayName ("Name")]
		[Description ("Name of the file.")]
		public string Name {
			get { return System.IO.Path.GetFileName (file.Name); }
		}
		
		[DisplayName ("Path")]
		[Description ("Full path of the file.")]
		public string Path {
			get { return file.FilePath; }
		}
		
		[DisplayName ("Type")]
		[Description ("Type of the file.")]
		public string FileType {
			get {
				string type = MonoDevelop.Core.Gui.Services.PlatformService.GetMimeTypeForUri (file.Name);
				return MonoDevelop.Core.Gui.Services.PlatformService.GetMimeTypeDescription (type); 
			}
		}
		
		[Category ("Build")]
		[DisplayName ("Build action")]
		[Description ("Action to perform when building this file.")]
		public BuildAction BuildAction {
			get { return file.BuildAction; }
			set { file.BuildAction = value; }
		}
		
		[Category ("Build")]
		[DisplayName ("Resource ID")]
		[Description ("Identifier of the embedded resource.")]
		public string ResourceId {
			get { return file.ResourceId; }
			set { file.ResourceId = value; }
		}
		
		protected override bool IsReadOnly (string propertyName)
		{
			if (propertyName == "ResourceId" && file.BuildAction != BuildAction.EmbedAsResource)
				return true;
			return false;
		}
	}
}
