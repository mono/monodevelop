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
using MonoDevelop.Projects.Formats.MSBuild;
using MonoDevelop.Ide.Fonts;

namespace MonoDevelop.Ide
{
	public enum JumpToFirst {
		Never,
		Error,
		ErrorOrWarning
	}
	
	public enum BuildResultStates {
		Never,
		Always,
		OnErrors,
		OnErrorsOrWarnings
	}
	
	public enum ShowMessageBubbles {
		Never,
		ForErrors,
		ForErrorsAndWarnings
	}
	
	public enum WorkbenchCompactness
	{
		VerySpacious,
		Spacious,
		Normal,
		Compact,
		VeryCompact
	}
	
	public class IdePreferences
	{
		internal IdePreferences ()
		{
			FontService.RegisterFontChangedCallback ("OutputPad", delegate {
				if (CustomOutputPadFontChanged != null)
					CustomOutputPadFontChanged (null, EventArgs.Empty);
			});
			FontService.RegisterFontChangedCallback ("Pad", delegate {
				if (CustomPadFontChanged != null)
					CustomPadFontChanged (null, EventArgs.Empty);
			});
		}
		
		public string DefaultProjectFileFormat {
			get { return PropertyService.Get ("MonoDevelop.DefaultFileFormat", MonoDevelop.Projects.Formats.MSBuild.MSBuildProjectService.DefaultFormat); }
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
		
		/*public BuildResultStates ShowOutputPadDuringBuild {
			get { return PropertyService.Get ("MonoDevelop.Ide.ShowOutputPadDuringBuild", BuildResultStates.Never); }
			set { PropertyService.Set ("MonoDevelop.Ide.ShowOutputPadDuringBuild", value); }
		}

		public event EventHandler<PropertyChangedEventArgs> ShowOutputPadShowOutputPadDuringBuildChanged {
			add { PropertyService.AddPropertyHandler ("MonoDevelop.Ide.ShowOutputPadDuringBuild", value); }
			remove { PropertyService.RemovePropertyHandler ("MonoDevelop.Ide.ShowOutputPadDuringBuild", value); }
		}*/
		
		public BuildResultStates ShowOutputPadAfterBuild {
			get { return PropertyService.Get ("MonoDevelop.Ide.ShowOutputPadAfterBuild", BuildResultStates.Never); }
			set { PropertyService.Set ("MonoDevelop.Ide.ShowOutputPadAfterBuild", value); }
		}

		public event EventHandler<PropertyChangedEventArgs> ShowOutputPadAfterBuildChanged {
			add { PropertyService.AddPropertyHandler ("MonoDevelop.Ide.ShowOutputPadAfterBuild", value); }
			remove { PropertyService.RemovePropertyHandler ("MonoDevelop.Ide.ShowOutputPadAfterBuild", value); }
		}
		/*
		public BuildResultStates ShowErrorPadDuringBuild {
			get { return PropertyService.Get ("MonoDevelop.Ide.ShowErrorPadDuringBuild", BuildResultStates.Never); }
			set { PropertyService.Set ("MonoDevelop.Ide.ShowErrorPadDuringBuild", value); }
		}

		public event EventHandler<PropertyChangedEventArgs> ShowErrorPadDuringBuildChanged {
			add { PropertyService.AddPropertyHandler ("MonoDevelop.Ide.ShowErrorPadDuringBuild", value); }
			remove { PropertyService.RemovePropertyHandler ("MonoDevelop.Ide.ShowErrorPadDuringBuild", value); }
		}*/
		
		public BuildResultStates ShowErrorPadAfterBuild {
			get { return PropertyService.Get ("MonoDevelop.Ide.NewShowErrorPadAfterBuild", BuildResultStates.Never); }
			set { PropertyService.Set ("MonoDevelop.Ide.NewShowErrorPadAfterBuild", value); }
		}

		public event EventHandler<PropertyChangedEventArgs> ShowErrorPadAfterBuildChanged {
			add { PropertyService.AddPropertyHandler ("MonoDevelop.Ide.NewShowErrorPadAfterBuild", value); }
			remove { PropertyService.RemovePropertyHandler ("MonoDevelop.Ide.NewShowErrorPadAfterBuild", value); }
		}
		
		public JumpToFirst JumpToFirstErrorOrWarning {
			get { return PropertyService.Get ("MonoDevelop.Ide.NewJumpToFirstErrorOrWarning", JumpToFirst.Error); }
			set { PropertyService.Set ("MonoDevelop.Ide.NewJumpToFirstErrorOrWarning", value); }
		}
		
		public event EventHandler<PropertyChangedEventArgs> AutoHideMessageBubblesChanged {
			add { PropertyService.AddPropertyHandler ("MonoDevelop.Ide.NewShowMessageBubbles", value); }
			remove { PropertyService.RemovePropertyHandler ("MonoDevelop.Ide.NewShowMessageBubbles", value); }
		}
		
		public bool DefaultHideMessageBubbles {
			get { return PropertyService.Get ("MonoDevelop.Ide.DefaultHideMessageBubbles", false); }
			set { PropertyService.Set ("MonoDevelop.Ide.DefaultHideMessageBubbles", value); }
		}
		
		public event EventHandler<PropertyChangedEventArgs> DefaultHideMessageBubblesChanged {
			add { PropertyService.AddPropertyHandler ("MonoDevelop.Ide.DefaultHideMessageBubbles", value); }
			remove { PropertyService.RemovePropertyHandler ("MonoDevelop.Ide.DefaultHideMessageBubbles", value); }
		}
		
		public ShowMessageBubbles ShowMessageBubbles {
			get { return PropertyService.Get ("MonoDevelop.Ide.NewShowMessageBubbles", ShowMessageBubbles.ForErrorsAndWarnings); }
			set { PropertyService.Set ("MonoDevelop.Ide.NewShowMessageBubbles", value); }
		}
		
		public event EventHandler<PropertyChangedEventArgs> ShowMessageBubblesChanged {
			add { PropertyService.AddPropertyHandler ("MonoDevelop.Ide.NewShowMessageBubbles", value); }
			remove { PropertyService.RemovePropertyHandler ("MonoDevelop.Ide.NewShowMessageBubbles", value); }
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
				string id = PropertyService.Get ("MonoDevelop.Ide.DefaultTargetRuntime", "__current"); 
				if (id == "__current")
					return Runtime.SystemAssemblyService.CurrentRuntime;
				TargetRuntime tr = Runtime.SystemAssemblyService.GetTargetRuntime (id);
				return tr ?? Runtime.SystemAssemblyService.CurrentRuntime;
			}
			set { PropertyService.Set ("MonoDevelop.Ide.DefaultTargetRuntime", value.IsRunning ? "__current" : value.Id); }
		}

		public event EventHandler<PropertyChangedEventArgs> DefaultTargetRuntimeChanged {
			add { PropertyService.AddPropertyHandler ("MonoDevelop.Ide.DefaultTargetRuntime", value); }
			remove { PropertyService.RemovePropertyHandler ("MonoDevelop.Ide.DefaultTargetRuntime", value); }
		}

		public bool BuildWithMSBuild {
			get { return PropertyService.Get ("MonoDevelop.Ide.BuildWithMSBuild", false); }
			set { PropertyService.Set ("MonoDevelop.Ide.BuildWithMSBuild", value); }
		}
		
		public MSBuildVerbosity MSBuildVerbosity {
			get { return PropertyService.Get ("MonoDevelop.Ide.MSBuildVerbosity", MSBuildVerbosity.Normal); }
			set { PropertyService.Set ("MonoDevelop.Ide.MSBuildVerbosity", value); }
		}

		public bool EnableInstrumentation {
			get { return PropertyService.Get ("MonoDevelop.EnableInstrumentation", false); }
			set { PropertyService.Set ("MonoDevelop.EnableInstrumentation", value); }
		}

		public event EventHandler<PropertyChangedEventArgs> EnableInstrumentationChanged {
			add { PropertyService.AddPropertyHandler ("MonoDevelop.EnableInstrumentation", value); }
			remove { PropertyService.RemovePropertyHandler ("MonoDevelop.EnableInstrumentation", value); }
		}
		
		public bool EnableAutomatedTesting {
			get { return PropertyService.Get ("MonoDevelop.EnableAutomatedTesting", false); }
			set { PropertyService.Set ("MonoDevelop.EnableAutomatedTesting", value); }
		}

		public event EventHandler<PropertyChangedEventArgs> EnableAutomatedTestingChanged {
			add { PropertyService.AddPropertyHandler ("MonoDevelop.EnableAutomatedTesting", value); }
			remove { PropertyService.RemovePropertyHandler ("MonoDevelop.EnableAutomatedTesting", value); }
		}
		
		/// <summary>
		/// Font to use for treeview pads. Returns null if no custom font is set.
		/// </summary>
		public Pango.FontDescription CustomPadFont {
			get { return FontService.GetFontDescription ("Pad", false); }
		}

		public event EventHandler<EventArgs> CustomPadFontChanged;

		/// <summary>
		/// Font to use for output pads. Returns null if no custom font is set.
		/// </summary>
		public Pango.FontDescription CustomOutputPadFont {
			get { return FontService.GetFontDescription ("OutputPad", false); }
		}

		public event EventHandler CustomOutputPadFontChanged;

		public string UserInterfaceLanguage {
			get { return PropertyService.Get ("MonoDevelop.Ide.UserInterfaceLanguage", ""); }
			set { PropertyService.Set ("MonoDevelop.Ide.UserInterfaceLanguage", value); }
		}
		
		public event EventHandler<PropertyChangedEventArgs> UserInterfaceLanguageChanged {
			add { PropertyService.AddPropertyHandler ("MonoDevelop.Ide.UserInterfaceLanguage", value); }
			remove { PropertyService.RemovePropertyHandler ("MonoDevelop.Ide.UserInterfaceLanguage", value); }
		}
		
		public string UserInterfaceTheme {
			get { return PropertyService.Get ("MonoDevelop.Ide.UserInterfaceTheme", ""); }
			set { PropertyService.Set ("MonoDevelop.Ide.UserInterfaceTheme", value); }
		}
		
		public event EventHandler<PropertyChangedEventArgs> UserInterfaceThemeChanged {
			add { PropertyService.AddPropertyHandler ("MonoDevelop.Ide.UserInterfaceTheme", value); }
			remove { PropertyService.RemovePropertyHandler ("MonoDevelop.Ide.UserInterfaceTheme", value); }
		}
		
		public WorkbenchCompactness WorkbenchCompactness {
			get { return PropertyService.Get ("MonoDevelop.Ide.WorkbenchCompactness", WorkbenchCompactness.Normal); }
			set { PropertyService.Set ("MonoDevelop.Ide.WorkbenchCompactness", value); }
		}
		
		public event EventHandler<PropertyChangedEventArgs> WorkbenchCompactnessChanged {
			add { PropertyService.AddPropertyHandler ("MonoDevelop.Ide.WorkbenchCompactness", value); }
			remove { PropertyService.RemovePropertyHandler ("MonoDevelop.Ide.WorkbenchCompactness", value); }
		}
		
		public string ColorScheme {
			get { return PropertyService.Get ("ColorScheme", "Default"); }
			set { PropertyService.Set ("ColorScheme", value); }
		}
		
		public event EventHandler<PropertyChangedEventArgs> ColorSchemeChanged {
			add { PropertyService.AddPropertyHandler ("ColorScheme", value); }
			remove { PropertyService.RemovePropertyHandler ("ColorScheme", value); }
		}
	}
	
	public enum BeforeCompileAction {
		Nothing,
		SaveAllFiles,
		PromptForSave,
	}
	
}
