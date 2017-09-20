//
// IDEStyleOptionsPanel.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;

using System.Collections;
using System.Collections.Generic;

using MonoDevelop.Components;
using MonoDevelop.Components.AtkCocoaHelper;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Components.Commands;
using Mono.Addins;
using System.Linq;

namespace MonoDevelop.Ide.Gui.OptionPanels
{
	class IDEStyleOptionsPanel : OptionsPanel
	{
		IDEStyleOptionsPanelWidget widget;

		public override Control CreatePanelWidget ()
		{
			return widget = new IDEStyleOptionsPanelWidget ();
		}
		
		public override void ApplyChanges ()
		{
			widget.Store ();
		}
	}
	
	partial class IDEStyleOptionsPanelWidget : Gtk.Bin
	{
		string currentTheme;

		static Lazy<List<string>> themes = new Lazy<List<string>> (() => {
			var searchDirs = new List<string> ();

			string prefix = Environment.GetEnvironmentVariable ("MONO_INSTALL_PREFIX");
			FilePath homeDir = Environment.GetFolderPath (Environment.SpecialFolder.Personal);

			searchDirs.Add (homeDir.Combine (".themes"));
			searchDirs.Add (Gtk.Rc.ThemeDir);
			if (!string.IsNullOrEmpty (prefix))
				searchDirs.Add (new FilePath (prefix).Combine ("share").Combine ("themes"));
			

			var themes = FindThemes (searchDirs).ToList ();
			return themes;
		});

		public static IList<string> InstalledThemes {
			get {
				return themes.Value;
			}
		}
		

		public IDEStyleOptionsPanelWidget ()
		{
			this.Build();
			Load ();

			comboTheme.SetCommonAccessibilityAttributes ("IDEStyleOptionsPanel.Theme", labelTheme,
			                                             GettextCatalog.GetString ("Select the user interface theme"));

			comboLanguage.SetCommonAccessibilityAttributes ("IDEStyleOptionsPanel.Language", label2,
			                                                GettextCatalog.GetString ("Select the user interface language"));

			imageRestart.SetCommonAccessibilityAttributes ("IDEStyleOptionsPanel.RestartImage", labelRestart,
			                                               GettextCatalog.GetString ("A restart is required before these changes take effect"));
		}

		void Load ()
		{
			currentTheme = IdeApp.Preferences.UserInterfaceThemeName;

			foreach (var localeSet in LocalizationService.CurrentLocaleSet)
				comboLanguage.AppendText (localeSet.DisplayName);

			int i = LocalizationService.CurrentLocaleSet.FindIndex (ls => ls.Culture == IdeApp.Preferences.UserInterfaceLanguage);
			if (i == -1) i = 0;
			comboLanguage.Active = i;

			if (Platform.IsLinux)
				comboTheme.AppendText (GettextCatalog.GetString ("(Default)"));

			foreach (string t in InstalledThemes)
				comboTheme.AppendText (t);

			var sel = themes.Value.IndexOf (IdeApp.Preferences.UserInterfaceThemeName);
			if (sel == -1)
				sel = 0;
			else if (Platform.IsLinux)
				sel++;
			
			comboTheme.Active = sel;
			comboTheme.Changed += ComboThemeChanged;
			tableRestart.Visible = separatorRestart.Visible = false;

			labelRestart.LabelProp = GettextCatalog.GetString ("These preferences will take effect next time you start {0}", BrandingService.ApplicationName);
			btnRestart.Label = GettextCatalog.GetString ("Restart {0}", BrandingService.ApplicationName);

			comboLanguage.Changed += (sender, e) => UpdateRestartMessage();
			comboTheme.Changed += (sender, e) => UpdateRestartMessage ();
			UpdateRestartMessage ();
		}

		void RestartClicked (object sender, System.EventArgs e)
		{
			Store ();
			IdeApp.Restart (true).Ignore();
		}

		void UpdateRestartMessage ()
		{
			bool restartRequired = false;

			if (currentTheme != IdeApp.Preferences.UserInterfaceThemeName.Value ||
			    ((Platform.IsLinux && Gtk.Settings.Default.ThemeName != IdeApp.Preferences.UserInterfaceThemeName.Value) ||
			     IdeTheme.UserInterfaceTheme != (IdeApp.Preferences.UserInterfaceThemeName == "Dark" ? Theme.Dark : Theme.Light)))
				restartRequired = true;
			
			if (GettextCatalog.UILocale != IdeApp.Preferences.UserInterfaceLanguage ||
				LocalizationService.CurrentLocaleSet [comboLanguage.Active].Culture != IdeApp.Preferences.UserInterfaceLanguage)
				restartRequired = true;
			
			if (restartRequired) {
				tableRestart.Visible = separatorRestart.Visible = true;
			} else
				tableRestart.Visible = separatorRestart.Visible = false;
		}

		void ComboThemeChanged (object sender, EventArgs e)
		{
			SetTheme ();
		}

		void SetTheme ()
		{
			string theme;
			if (comboTheme.Active == 0 && Platform.IsLinux)
				theme = "";
			else
				theme = comboTheme.ActiveText;
			SetTheme (theme);
		}

		void SetTheme (string theme)
		{
			if (theme.Length == 0 && Platform.IsLinux) {
				currentTheme = "";
			} else {
				currentTheme = theme;
			}
		}

		// Code for getting the list of themes based on f-spot
		static ICollection<string> FindThemes (IEnumerable<string> themeDirs)
		{
			var themes = new HashSet<string> ();
			if (Platform.IsLinux) {
				string gtkrc = System.IO.Path.Combine ("gtk-2.0", "gtkrc");
				foreach (string themeDir in themeDirs) {
					if (string.IsNullOrEmpty (themeDir) || !System.IO.Directory.Exists (themeDir))
						continue;
					foreach (FilePath dir in System.IO.Directory.GetDirectories (themeDir)) {
						if (System.IO.File.Exists (dir.Combine (gtkrc))) {
							var themeName = dir.FileName;
							if (!IsBadGtkTheme (themeName))
								themes.Add (themeName);
						}
					}
				}
			} else {
				themes.Add ("Light");
				themes.Add ("Dark");
			}
			return themes;
		}

		internal static bool IsBadGtkTheme (string theme)
		{
			if (string.Equals ("QtCurve", theme, StringComparison.OrdinalIgnoreCase))
				return true;
			if (string.Equals ("oxygen-gtk", theme, StringComparison.OrdinalIgnoreCase))
				return Environment.GetEnvironmentVariable ("OXYGEN_DISABLE_INNER_SHADOWS_HACK") != "1";
			return false;
		}
		
		public void Store()
		{
			string lc = LocalizationService.CurrentLocaleSet [comboLanguage.Active].Culture;
			if (lc != IdeApp.Preferences.UserInterfaceLanguage)
				IdeApp.Preferences.UserInterfaceLanguage.Value = lc;

			if (currentTheme != IdeApp.Preferences.UserInterfaceThemeName.Value)
				IdeApp.Preferences.UserInterfaceThemeName.Value = currentTheme;
		}
	}
}
