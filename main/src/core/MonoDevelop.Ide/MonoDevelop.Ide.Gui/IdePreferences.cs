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
using MonoDevelop.Core.Assemblies;

namespace MonoDevelop.Ide.Gui
{
	public class IdePreferences
	{
		internal IdePreferences ()
		{
		}
		
		public string DefaultProjectFileFormat {
			get { return PropertyService.Get ("MonoDevelop.DefaultFileFormat", "MSBuild05"); }
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

		public bool BuildBeforeExecuting {
			get { return PropertyService.Get ("MonoDevelop.Ide.BuildBeforeExecuting", true); }
			set { PropertyService.Set ("MonoDevelop.Ide.BuildBeforeExecuting", value); }
		}

		public event EventHandler<PropertyChangedEventArgs> BuildBeforeExecutingChanged {
			add { PropertyService.AddPropertyHandler ("MonoDevelop.Ide.BuildBeforeExecuting", value); }
			remove { PropertyService.RemovePropertyHandler ("MonoDevelop.Ide.BuildBeforeExecuting", value); }
		}

		public bool ShowOutputPadWhenBuildStarts {
			get { return PropertyService.Get ("MonoDevelop.Ide.ShowOutputWindowAtBuild", true); }
			set { PropertyService.Set ("MonoDevelop.Ide.ShowOutputWindowAtBuild", value); }
		}

		public event EventHandler<PropertyChangedEventArgs> ShowOutputPadWhenBuildStartsChanged {
			add { PropertyService.AddPropertyHandler ("MonoDevelop.Ide.ShowOutputWindowAtBuild", value); }
			remove { PropertyService.RemovePropertyHandler ("MonoDevelop.Ide.ShowOutputWindowAtBuild", value); }
		}

		public bool ShowErrorsPadAfterBuild {
			get { return PropertyService.Get ("MonoDevelop.Ide.ShowTaskListAfterBuild", true); }
			set { PropertyService.Set ("MonoDevelop.Ide.ShowTaskListAfterBuild", value); }
		}

		public event EventHandler<PropertyChangedEventArgs> ShowErrorsPadAfterBuildChanged {
			add { PropertyService.AddPropertyHandler ("MonoDevelop.Ide.ShowTaskListAfterBuild", value); }
			remove { PropertyService.RemovePropertyHandler ("MonoDevelop.Ide.ShowTaskListAfterBuild", value); }
		}

		public BeforeCompileAction BeforeBuildSaveAction {
			get { return PropertyService.Get ("MonoDevelop.Ide.BeforeCompileAction", BeforeCompileAction.SaveAllFiles); }
			set { PropertyService.Set ("MonoDevelop.Ide.BeforeCompileAction", value); }
		}

		public event EventHandler<PropertyChangedEventArgs> BeforeBuildSaveActionChanged {
			add { PropertyService.AddPropertyHandler ("MonoDevelop.Ide.BeforeCompileAction", value); }
			remove { PropertyService.RemovePropertyHandler ("MonoDevelop.Ide.BeforeCompileAction", value); }
		}

		public bool RunWithWarnings {
			get { return PropertyService.Get ("MonoDevelop.Ide.RunWithWarnings", true); }
			set { PropertyService.Set ("MonoDevelop.Ide.RunWithWarnings", value); }
		}

		public event EventHandler<PropertyChangedEventArgs> RunWithWarningsChanged {
			add { PropertyService.AddPropertyHandler ("MonoDevelop.Ide.RunWithWarnings", value); }
			remove { PropertyService.RemovePropertyHandler ("MonoDevelop.Ide.RunWithWarnings", value); }
		}

		public TargetRuntime DefaultTargetRuntime {
			get {
				string id = PropertyService.Get ("MonoDevelop.Ide.DefaultTargetRuntime", Runtime.SystemAssemblyService.CurrentRuntime.Id); 
				TargetRuntime tr = Runtime.SystemAssemblyService.GetTargetRuntime (id);
				return tr ?? Runtime.SystemAssemblyService.CurrentRuntime;
			}
			set { PropertyService.Set ("MonoDevelop.Ide.DefaultTargetRuntime", value.Id); }
		}

		public event EventHandler<PropertyChangedEventArgs> DefaultTargetRuntimeChanged {
			add { PropertyService.AddPropertyHandler ("MonoDevelop.Ide.DefaultTargetRuntime", value); }
			remove { PropertyService.RemovePropertyHandler ("MonoDevelop.Ide.DefaultTargetRuntime", value); }
		}
	}
	
	public enum BeforeCompileAction {
		Nothing,
		SaveAllFiles,
		PromptForSave,
	}
	
}
