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
using MonoDevelop.Projects.MSBuild;
using MonoDevelop.Ide.Fonts;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Highlighting;
using System.Linq;

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
		}

		public readonly ConfigurationProperty<bool> EnableInstrumentation = Runtime.Preferences.EnableInstrumentation;
		public readonly ConfigurationProperty<bool> EnableAutomatedTesting = Runtime.Preferences.EnableAutomatedTesting;

		public readonly ConfigurationProperty<string> ProjectsDefaultPath = ConfigurationProperty.Create ("MonoDevelop.Core.Gui.Dialogs.NewProjectDialog.DefaultPath", System.IO.Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.Personal), "Projects"));

		public readonly ConfigurationProperty<bool> BuildBeforeExecuting = ConfigurationProperty.Create ("MonoDevelop.Ide.BuildBeforeExecuting", true);
		public readonly ConfigurationProperty<bool> BuildBeforeRunningTests = ConfigurationProperty.Create ("BuildBeforeRunningTests", true);
		public readonly ConfigurationProperty<BeforeCompileAction> BeforeBuildSaveAction = ConfigurationProperty.Create ("MonoDevelop.Ide.BeforeCompileAction", BeforeCompileAction.SaveAllFiles);
		public readonly ConfigurationProperty<bool> RunWithWarnings = ConfigurationProperty.Create ("MonoDevelop.Ide.RunWithWarnings", true);
		public readonly ConfigurationProperty<MSBuildVerbosity> MSBuildVerbosity = Runtime.Preferences.MSBuildVerbosity;
		public readonly ConfigurationProperty<BuildResultStates> ShowOutputPadAfterBuild = ConfigurationProperty.Create ("MonoDevelop.Ide.ShowOutputPadAfterBuild", BuildResultStates.Never);
		public readonly ConfigurationProperty<BuildResultStates> ShowErrorPadAfterBuild = ConfigurationProperty.Create ("MonoDevelop.Ide.NewShowErrorPadAfterBuild", BuildResultStates.Never);
		public readonly ConfigurationProperty<JumpToFirst> JumpToFirstErrorOrWarning = ConfigurationProperty.Create ("MonoDevelop.Ide.NewJumpToFirstErrorOrWarning", JumpToFirst.Error);
		public readonly ConfigurationProperty<bool> DefaultHideMessageBubbles = ConfigurationProperty.Create ("MonoDevelop.Ide.DefaultHideMessageBubbles", false);
		public readonly ConfigurationProperty<ShowMessageBubbles> ShowMessageBubbles = ConfigurationProperty.Create ("MonoDevelop.Ide.NewShowMessageBubbles", MonoDevelop.Ide.ShowMessageBubbles.ForErrorsAndWarnings);

		public readonly ConfigurationProperty<TargetRuntime> DefaultTargetRuntime = new DefaultTargetRuntimeProperty ();
		class DefaultTargetRuntimeProperty: ConfigurationProperty<TargetRuntime>
		{
			ConfigurationProperty<string> defaultTargetRuntimeText = ConfigurationProperty.Create ("MonoDevelop.Ide.DefaultTargetRuntime", "__current");

			public DefaultTargetRuntimeProperty ()
			{
				defaultTargetRuntimeText.Changed += (s,e) => OnChanged ();
			}

			protected override TargetRuntime OnGetValue ()
			{
				string id = defaultTargetRuntimeText.Value;
				if (id == "__current")
					return Runtime.SystemAssemblyService.CurrentRuntime;
				TargetRuntime tr = Runtime.SystemAssemblyService.GetTargetRuntime (id);
				return tr ?? Runtime.SystemAssemblyService.CurrentRuntime;
			}

			protected override bool OnSetValue (TargetRuntime value)
			{
				defaultTargetRuntimeText.Value = value.IsRunning ? "__current" : value.Id;
				return true;
			}
		}

		public readonly ConfigurationProperty<string> UserInterfaceLanguage = Runtime.Preferences.UserInterfaceLanguage;
		public readonly ConfigurationProperty<string> UserInterfaceThemeName = ConfigurationProperty.Create ("MonoDevelop.Ide.UserInterfaceTheme", Platform.IsLinux ? "" : "Light");
		public readonly ConfigurationProperty<WorkbenchCompactness> WorkbenchCompactness = ConfigurationProperty.Create ("MonoDevelop.Ide.WorkbenchCompactness", MonoDevelop.Ide.WorkbenchCompactness.Normal);
		public readonly ConfigurationProperty<bool> LoadPrevSolutionOnStartup = ConfigurationProperty.Create ("SharpDevelop.LoadPrevProjectOnStartup", false);
		public readonly ConfigurationProperty<bool> CreateFileBackupCopies = ConfigurationProperty.Create ("SharpDevelop.CreateBackupCopy", false);
		public readonly ConfigurationProperty<bool> LoadDocumentUserProperties = ConfigurationProperty.Create ("SharpDevelop.LoadDocumentProperties", true);
		public readonly ConfigurationProperty<bool> EnableDocumentSwitchDialog = ConfigurationProperty.Create ("MonoDevelop.Core.Gui.EnableDocumentSwitchDialog", true);
		public readonly ConfigurationProperty<bool> ShowTipsAtStartup = ConfigurationProperty.Create ("MonoDevelop.Core.Gui.Dialog.TipOfTheDayView.ShowTipsAtStartup", false);

		internal readonly ConfigurationProperty<Properties> WorkbenchMemento = ConfigurationProperty.Create ("SharpDevelop.Workbench.WorkbenchMemento", new Properties ());

		/// <summary>
		/// Font to use for treeview pads. Returns null if no custom font is set.
		/// </summary>
		public readonly ConfigurationProperty<Pango.FontDescription> CustomPadFont = FontService.GetFontProperty ("Pad");

		/// <summary>
		/// Font to use for output pads. Returns null if no custom font is set.
		/// </summary>
		public readonly ConfigurationProperty<Pango.FontDescription> CustomOutputPadFont = FontService.GetFontProperty ("OutputPad");

		public readonly ConfigurationProperty<bool> EnableCompletionCategoryMode = ConfigurationProperty.Create ("EnableCompletionCategoryMode", false);
		public readonly ConfigurationProperty<bool> ForceSuggestionMode = ConfigurationProperty.Create ("ForceCompletionSuggestionMode", false);
		public readonly ConfigurationProperty<bool> EnableAutoCodeCompletion = ConfigurationProperty.Create ("EnableAutoCodeCompletion", true);
		public readonly ConfigurationProperty<bool> AddImportedItemsToCompletionList = ConfigurationProperty.Create ("AddImportedItemsToCompletionList", false);
		public readonly ConfigurationProperty<bool> IncludeKeywordsInCompletionList = ConfigurationProperty.Create ("IncludeKeywordsInCompletionList", true);
		public readonly ConfigurationProperty<bool> IncludeCodeSnippetsInCompletionList = ConfigurationProperty.Create ("IncludeCodeSnippetsInCompletionList", true);

		[Obsolete ("Unused use CompletionOptionsHideAdvancedMembers")]
		public readonly ConfigurationProperty<bool> FilterCompletionListByEditorBrowsable = ConfigurationProperty.Create ("FilterCompletionListByEditorBrowsable", true);
		[Obsolete ("Unused use CompletionOptionsHideAdvancedMembers")]
		public readonly ConfigurationProperty<bool> IncludeEditorBrowsableAdvancedMembers = ConfigurationProperty.Create ("IncludeEditorBrowsableAdvancedMembers", true);

		public readonly ConfigurationProperty<bool> CompletionOptionsHideAdvancedMembers = ConfigurationProperty.Create ("CompletionOptionsHideAdvancedMembers", false);

		public Theme UserInterfaceTheme {
			get { return MonoDevelop.Components.IdeTheme.UserInterfaceTheme; }
		}

		internal static readonly string DefaultLightColorScheme = "Light";
		internal static readonly string DefaultDarkColorScheme = "Dark";

		public readonly ConfigurationProperty<bool> EnableSourceAnalysis = ConfigurationProperty.Create ("MonoDevelop.AnalysisCore.AnalysisEnabled_V2", true);
		public readonly ConfigurationProperty<bool> EnableUnitTestEditorIntegration = ConfigurationProperty.Create ("Testing.EnableUnitTestEditorIntegration", false);

		public readonly SchemeConfigurationProperty ColorScheme = new SchemeConfigurationProperty ("ColorScheme", DefaultLightColorScheme, DefaultDarkColorScheme);

		public readonly ThemeConfigurationProperty<string> UserTasksHighPrioColor = new ThemeConfigurationProperty<string> ("Monodevelop.UserTasksHighPrioColor", "", "rgb:ffff/ffff/ffff");
		public readonly ThemeConfigurationProperty<string> UserTasksNormalPrioColor = new ThemeConfigurationProperty<string> ("Monodevelop.UserTasksNormalPrioColor", "", "rgb:ffff/ffff/ffff");
		public readonly ThemeConfigurationProperty<string> UserTasksLowPrioColor = new ThemeConfigurationProperty<string> ("Monodevelop.UserTasksLowPrioColor", "", "rgb:ffff/ffff/ffff");

		public class ThemeConfigurationProperty<T>: ConfigurationProperty<T>
		{
			readonly ConfigurationProperty<T> lightConfiguration;
			readonly ConfigurationProperty<T> darkConfiguration;

			public ThemeConfigurationProperty (string propertyName, T defaultLightValue, T defaultDarkValue, string oldName = null)
			{
				lightConfiguration = ConfigurationProperty.Create<T> (propertyName, defaultLightValue, oldName);
				darkConfiguration = ConfigurationProperty.Create<T> (propertyName + "-Dark", defaultDarkValue, oldName + "-Dark");

				lightConfiguration.Changed += (s,e) => OnChanged ();
				darkConfiguration.Changed += (s,e) => OnChanged ();
				MonoDevelop.Ide.Gui.Styles.Changed += (sender, e) => OnChanged ();
			}

			public T ValueForTheme (Theme theme)
			{
				switch (theme) {
					case Theme.Light:
						return lightConfiguration.Value;
					case Theme.Dark:
						return darkConfiguration.Value;
					default:
						throw new InvalidOperationException ();
				}
			}

			protected override T OnGetValue ()
			{
				if (IdeApp.Preferences.UserInterfaceTheme == Theme.Light)
					return lightConfiguration;
				else
					return darkConfiguration;
			}

			protected override bool OnSetValue (T value)
			{
				if (IdeApp.Preferences.UserInterfaceTheme == Theme.Light)
					return lightConfiguration.Set (value);
				else
					return darkConfiguration.Set (value);
			}
		}

		public class SchemeConfigurationProperty: ThemeConfigurationProperty<string>
		{
			public SchemeConfigurationProperty (string propertyName, string defaultLightValue, string defaultDarkValue, string oldName = null)
				: base (propertyName, defaultLightValue, defaultDarkValue, oldName)
			{
			}

			protected override string OnGetValue ()
			{
				var style = base.OnGetValue ();
				if (SyntaxHighlightingService.ContainsStyle (style))
					return style;

				var defaultStyle = SyntaxHighlightingService.GetDefaultColorStyleName ();
				LoggingService.LogWarning ("Highlighting Theme \"{0}\" not found, using default \"{1}\" instead", style, defaultStyle);
				Value = defaultStyle;
				return SyntaxHighlightingService.GetDefaultColorStyleName ();
			}
		}
	}
	
	public enum BeforeCompileAction {
		Nothing,
		SaveAllFiles,
		PromptForSave,
	}

	public enum Theme
	{
		Light,
		Dark
	}
	
}
