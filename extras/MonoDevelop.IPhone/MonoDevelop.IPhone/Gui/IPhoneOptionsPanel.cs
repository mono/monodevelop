// 
// IPhoneOptionsPanelWidget.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
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

using System;
using Gtk;
using MonoDevelop.Projects.Gui.Dialogs;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Gui;

namespace MonoDevelop.IPhone.Gui
{
	
	public class IPhoneOptionsPanel : ItemOptionsPanel
	{
		IPhoneOptionsWidget panel;
		
		public override Widget CreatePanelWidget ()
		{
			panel = new IPhoneOptionsWidget ();
			panel.Load ((IPhoneProject) ConfiguredProject);
			return panel;
		}
		
		public override void ApplyChanges ()
		{
			panel.Store ((IPhoneProject) ConfiguredProject);
		}
		
		public override bool IsVisible ()
		{
			return ConfiguredProject is IPhoneProject
				&& (((IPhoneProject)ConfiguredProject).CompileTarget == CompileTarget.Exe);
		}
	}

	internal partial class IPhoneOptionsWidget : Gtk.Bin
	{

		public IPhoneOptionsWidget ()
		{
			this.Build ();
			targetDevicesCombo.Changed += HandleTargetDevicesComboChanged;
		}

		void HandleTargetDevicesComboChanged (object sender, EventArgs e)
		{
			switch (SupportedDevices) {
			case TargetDevice.IPhoneAndIPad:
				iPadNibPicker.Sensitive = true;
				IPhoneIconSensitive = true;
				IPadIconSensitive = true;
				IPadSpotlightIconSensitive = true;
				SettingsIconSensitive = true;
				break;
			case TargetDevice.IPhone:
				iPadNibPicker.Sensitive = false;
				IPhoneIconSensitive = true;
				IPadIconSensitive = false;
				IPadSpotlightIconSensitive = false;
				SettingsIconSensitive = true;
				break;
			case TargetDevice.IPad:
				iPadNibPicker.Sensitive = false;
				IPhoneIconSensitive = false;
				IPadIconSensitive = true;
				IPadSpotlightIconSensitive = true;
				SettingsIconSensitive = true;
				break;
			}
		}
		
		bool IPhoneIconSensitive {
			set {
				iphoneIconLabel.Sensitive = iphoneIconSizeLabel.Sensitive = iphoneIconPicker.Sensitive = value;
			}
		}
		
		bool IPadIconSensitive {
			set {
				ipadIconLabel.Sensitive = ipadIconSizeLabel.Sensitive = ipadIconPicker.Sensitive = value;
			}
		}
		
		bool IPadSpotlightIconSensitive {
			set {
				ipadSpotlightIconLabel.Sensitive = ipadSpotlightIconSizeLabel.Sensitive
					= ipadSpotlightIconPicker.Sensitive = value;
			}
		}
		
		bool SettingsIconSensitive {
			set {
				settingsIconLabel.Sensitive = settingsIconSizeLabel.Sensitive = settingsIconPicker.Sensitive = value;
			}
		}
		
		TargetDevice SupportedDevices {
			get {
				switch (targetDevicesCombo.Active) {
				case 0:
					return TargetDevice.IPhoneAndIPad;
				case 1:
					return TargetDevice.IPhone;
				case 2:
					return TargetDevice.IPad;
				default:
					throw new InvalidOperationException ("targetDevicesCombo has unexpected value");
				}
			}
			set {
				switch (value) {
				case TargetDevice.IPhoneAndIPad:
					targetDevicesCombo.Active = 0;
					break;
				case TargetDevice.IPhone:
					targetDevicesCombo.Active = 1;
					break;
				case TargetDevice.IPad:
					targetDevicesCombo.Active = 2;
					break;
				default:
					LoggingService.LogWarning ("Unknown value '{0}' in SupportedDevices. Changing to default.");
					goto case TargetDevice.IPhone;
				}
			}
		}
		
		public void Load (IPhoneProject proj)
		{
			devRegionEntry.Text = proj.BundleDevelopmentRegion ?? "";
			bundleIdEntry.Text = proj.BundleIdentifier ?? "";
			bundleVersionEntry.Text = proj.BundleVersion ?? "";
			displayNameEntry.Text = proj.BundleDisplayName ?? "";
			
			mainNibPicker.Project         = iPadNibPicker.Project         = proj;
			mainNibPicker.EntryIsEditable = iPadNibPicker.EntryIsEditable = true;
			mainNibPicker.DefaultFilter   = iPadNibPicker.DefaultFilter   = "*.xib";
			
			mainNibPicker.DialogTitle = GettextCatalog.GetString ("Select main interface file...");
			mainNibPicker.SelectedFile = proj.MainNibFile.ToString () ?? "";
			
			iPadNibPicker.DialogTitle = GettextCatalog.GetString ("Select iPad interface file...");
			iPadNibPicker.SelectedFile = proj.MainNibFileIPad.ToString () ?? "";
			
			targetDevicesCombo.AppendText (GettextCatalog.GetString ("iPhone and iPad"));
			targetDevicesCombo.AppendText (GettextCatalog.GetString ("iPhone only"));
			targetDevicesCombo.AppendText (GettextCatalog.GetString ("iPad only"));
			
			SupportedDevices = proj.SupportedDevices;
			
			HandleTargetDevicesComboChanged (null, null);
			
			ProjectFileEntry [] pickers = { iphoneIconPicker, ipadIconPicker, settingsIconPicker, ipadSpotlightIconPicker };
			foreach (var p in pickers) {
				p.Project = proj;
				p.DefaultFilter = "*.png";
				p.DialogTitle = GettextCatalog.GetString ("Select icon...");
			}
			
			iphoneIconPicker.SelectedFile = proj.BundleIcon.ToString () ?? "";
			ipadIconPicker.SelectedFile = proj.BundleIconIPad.ToString () ?? "";
			settingsIconPicker.SelectedFile = proj.BundleIconSpotlight.ToString () ?? "";
			ipadSpotlightIconPicker.SelectedFile = proj.BundleIconIPadSpotlight.ToString () ?? "";
		}
		
		public void Store (IPhoneProject proj)
		{
			proj.BundleDevelopmentRegion = NullIfEmpty (devRegionEntry.Text);
			proj.BundleIdentifier = NullIfEmpty (bundleIdEntry.Text);
			proj.BundleVersion = NullIfEmpty (bundleVersionEntry.Text);
			proj.BundleDisplayName = NullIfEmpty (displayNameEntry.Text);
			proj.MainNibFile = NullIfEmpty (mainNibPicker.SelectedFile);
			proj.MainNibFileIPad = NullIfEmpty (iPadNibPicker.SelectedFile);
			
			proj.BundleIcon = NullIfEmpty (iphoneIconPicker.SelectedFile);
			proj.BundleIconIPad = NullIfEmpty (ipadIconPicker.SelectedFile);
			proj.BundleIconSpotlight = NullIfEmpty (settingsIconPicker.SelectedFile);
			proj.BundleIconIPadSpotlight = NullIfEmpty (ipadSpotlightIconPicker.SelectedFile);
			
			proj.SupportedDevices = SupportedDevices;
		}
		
		string NullIfEmpty (string s)
		{
			if (s == null || s.Length != 0)
				return s;
			return null;
		}
	}
}
