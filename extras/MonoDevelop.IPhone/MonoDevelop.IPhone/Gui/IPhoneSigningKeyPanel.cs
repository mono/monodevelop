// 
// IPhoneSigningKeyPanelWidget.cs
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
using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.Projects.Gui.Dialogs;
using MonoDevelop.Projects;

namespace MonoDevelop.IPhone.Gui
{
	
	class IPhoneSigningKeyPanel : ItemOptionsPanel
	{
		IPhoneSigningKeyPanelWidget widget;
		IPhoneProject project;
		
		public override Gtk.Widget CreatePanelWidget ()
		{
			var info = project.UserProperties.GetValue<SigningKeyInformation> ("IPhoneSigningKeys");
			return widget = new IPhoneSigningKeyPanelWidget (info);
		}

		public override void ApplyChanges ()
		{
			if (project != null) {
				var info = widget.GetValue ();
				if (info != null)
					project.UserProperties.SetValue<SigningKeyInformation> ("IPhoneSigningKeys", info);
				else if (project.UserProperties.HasValue ("IPhoneSigningKeys"))
					project.UserProperties.RemoveValue ("IPhoneSigningKeys");
			}
		}

		public override void Initialize (MonoDevelop.Core.Gui.Dialogs.OptionsDialog dialog, object dataObject)
		{
			project = dataObject as IPhoneProject;
			base.Initialize (dialog, dataObject);
		}
		
		public override bool IsVisible ()
		{
			return project != null && base.IsVisible ();
		}
	}
	
	partial class IPhoneSigningKeyPanelWidget : Gtk.Bin
	{
		ListStore developerStore;
		ListStore distributionStore;
		IList<string> signingCerts;
		
		public IPhoneSigningKeyPanelWidget (SigningKeyInformation info)
		{
			this.Build ();
			
			var txtRenderer = new CellRendererText ();
			txtRenderer.Ellipsize = Pango.EllipsizeMode.End;
			
			developerCombo.Model = developerStore = new ListStore (typeof (String), typeof (String));
			developerCombo.PackStart (txtRenderer, true);
			developerCombo.AddAttribute (txtRenderer, "markup", 1);
			
			distributionCombo.Model = distributionStore = new ListStore (typeof (String), typeof (String));
			distributionCombo.PackStart (txtRenderer, true);
			distributionCombo.AddAttribute (txtRenderer, "markup", 1);
			
			signingCerts = Keychain.GetAllSigningIdentities ();
			
			string auto = GettextCatalog.GetString ("<b>Automatic</b>");
			developerStore.AppendValues (null, auto);
			distributionStore.AppendValues (null, auto);
			
			foreach (var cert in signingCerts) {
				if (cert.StartsWith (Keychain.DEV_CERT_PREFIX)) {
					developerStore.AppendValues (cert,
						GLib.Markup.EscapeText (cert.Substring (Keychain.DEV_CERT_PREFIX.Length).Trim ()));
				} else if (cert.StartsWith (Keychain.DIST_CERT_PREFIX)) {
					distributionStore.AppendValues (cert,
						GLib.Markup.EscapeText (cert.Substring (Keychain.DIST_CERT_PREFIX.Length).Trim ()));
				}
			}
			
			useSpecificCertCheck.Toggled += delegate {
				certBox.Sensitive = useSpecificCertCheck.Active;
			};
			
			SetValue (info);
		}
		
		internal SigningKeyInformation GetValue ()
		{
			if (!useSpecificCertCheck.Active)
				return null;
			
			string devKey = null, distKey = null;
			TreeIter iter;
			if (developerStore.GetIter (out iter, new TreePath (new int[] { developerCombo.Active })))
				devKey = (string) developerStore.GetValue (iter, 0); 
			if (distributionStore.GetIter (out iter, new TreePath (new int[] { distributionCombo.Active })))
				distKey = (string) distributionStore.GetValue (iter, 0);
			return new SigningKeyInformation (devKey, distKey);
		}
		
		void SetValue (SigningKeyInformation value)
		{
			distributionCombo.Active = developerCombo.Active = 0;
			useSpecificCertCheck.Active = certBox.Sensitive = value != null;
			if (value == null)
				return;
			
			TreeIter iter;
			if (distributionStore.GetIterFirst (out iter)) {
				int index = 0;
				do {
					if ((string)distributionStore.GetValue (iter, 0) == value.Distribution) {
						distributionCombo.Active = index;
						break;
					}
					index++;
				} while (distributionStore.IterNext (ref iter));
			}
			
			if (developerStore.GetIterFirst (out iter)) {
				int index = 0;
				do {
					if ((string)developerStore.GetValue (iter, 0) == value.Developer) {
						developerCombo.Active = index;
						break;
					}
					index++;
				} while (developerStore.IterNext (ref iter));
			}
		}
		
	}
}
