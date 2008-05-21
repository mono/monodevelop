// IdePreferences.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Gui
{
	public class IdePreferences
	{
		public string DefaultProjectFileFormat {
			get { return PropertyService.Get ("MonoDevelop.DefaultFileFormat", "MD1"); }
			set { PropertyService.Set ("MonoDevelop.DefaultFileFormat", value); }
		}
		
		public event EventHandler<PropertyChangedEventArgs> DefaultProjectFileFormatChanged {
			add { PropertyService.AddPropertyHandler ("MonoDevelop.DefaultFileFormat", value); }
			remove { PropertyService.RemovePropertyHandler ("MonoDevelop.DefaultFileFormat", value); }
		}
		
		public bool LoadPrevSolutionOnStartup {
			get { return PropertyService.Get ("SharpDevelop.LoadPrevProjectOnStartup", false); }
			set { PropertyService.Set ("SharpDevelop.LoadPrevProjectOnStartup", value); }
		}
		
		public event EventHandler<PropertyChangedEventArgs> LoadPrevSolutionOnStartupChanged {
			add { PropertyService.AddPropertyHandler ("SharpDevelop.LoadPrevProjectOnStartup", value); }
			remove { PropertyService.RemovePropertyHandler ("SharpDevelop.LoadPrevProjectOnStartup", value); }
		}
		
		public bool CreateFileBackupCopies {
			get { return PropertyService.Get ("SharpDevelop.CreateBackupCopy", false); }
			set { PropertyService.Set ("SharpDevelop.CreateBackupCopy", value); }
		}
		
		public event EventHandler<PropertyChangedEventArgs> CreateFileBackupCopiesChanged {
			add { PropertyService.AddPropertyHandler ("SharpDevelop.CreateBackupCopy", value); }
			remove { PropertyService.RemovePropertyHandler ("SharpDevelop.CreateBackupCopy", value); }
		}
		
		public bool LoadDocumentUserProperties {
			get { return PropertyService.Get ("SharpDevelop.LoadDocumentProperties", true); }
			set { PropertyService.Set ("SharpDevelop.LoadDocumentProperties", value); }
		}
		
		public event EventHandler<PropertyChangedEventArgs> LoadDocumentUserPropertiesChanged {
			add { PropertyService.AddPropertyHandler ("SharpDevelop.LoadDocumentProperties", value); }
			remove { PropertyService.RemovePropertyHandler ("SharpDevelop.LoadDocumentProperties", value); }
		}
	}
}
