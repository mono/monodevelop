// XspOptionsPanelWidget.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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

using System;
using Gtk;

using MonoDevelop.Core;
using MonoDevelop.AspNet.Projects;
using MonoDevelop.Components;

namespace MonoDevelop.AspNet.Execution
{
	class XspOptionsPanelWidget : VBox
	{
		Entry ipAddress = new Entry { WidthChars = 39 }; // sized for IPv6
		SpinButton portNumber = new SpinButton (0, Int16.MaxValue, 1) { WidthChars = 5} ;
		CheckButton verboseCheck = new CheckButton ();
		readonly ComboBox sslMode = ComboBox.NewText ();
		readonly ComboBox sslProtocol = ComboBox.NewText ();
		readonly ComboBox keyType = ComboBox.NewText ();
		readonly ComboBox passwordOptions = ComboBox.NewText ();
		readonly FileEntry keyLocation = new FileEntry ();
		readonly FileEntry certLocation = new FileEntry ();
		readonly Entry passwordEntry = new Entry { InvisibleChar = '●' };
		
		public XspOptionsPanelWidget (AspNetAppProject project)
		{
			Build ();

			XspParameters xPar = project.XspParameters;
			
			//index should be equivalent to XspSslMode enum
			((ListStore) sslMode.Model).Clear ();
			sslMode.AppendText (GettextCatalog.GetString ("None"));
			sslMode.AppendText (GettextCatalog.GetString ("Enabled"));
			sslMode.AppendText (GettextCatalog.GetString ("Accept Client Certificates"));
			sslMode.AppendText (GettextCatalog.GetString ("Require Client Certificates"));
			
			//index should be equivalent to XspSslProtocol enum
			((ListStore) sslProtocol.Model).Clear ();
			sslProtocol.AppendText (GettextCatalog.GetString ("Default"));
			sslProtocol.AppendText ("TLS");
			sslProtocol.AppendText ("SSL 2");
			sslProtocol.AppendText ("SSL 3");
			
			((ListStore) keyType.Model).Clear ();
			keyType.AppendText (GettextCatalog.GetString ("None"));
			keyType.AppendText ("Pkcs12");
			keyType.AppendText ("PVK");
			
			((ListStore) passwordOptions.Model).Clear ();
			passwordOptions.AppendText (GettextCatalog.GetString ("None"));
			passwordOptions.AppendText (GettextCatalog.GetString ("Ask"));
			passwordOptions.AppendText (GettextCatalog.GetString ("Store (insecure)"));
			
			//load all options
			ipAddress.Text = xPar.Address;
			portNumber.Value = xPar.Port;
			verboseCheck.Active = xPar.Verbose;
			sslMode.Active = (int) xPar.SslMode;
			sslProtocol.Active = (int) xPar.SslProtocol;
			keyType.Active = (int) xPar.KeyType;
			keyLocation.Path = xPar.PrivateKeyFile;
			certLocation.Path = xPar.CertificateFile;
			passwordOptions.Active = (int) xPar.PasswordOptions;
			passwordEntry.Text = xPar.PrivateKeyPassword;
		}

		void Build ()
		{
			Spacing = 6;

			PackStart (new Label { Markup = "<b>" + GettextCatalog.GetString ("General Options") + "</b>", Xalign = 0 });

			var gpadLabel = new Label { WidthRequest = 12 };
			var ipLabel = new Label (GettextCatalog.GetString ("IP address:")) { Xalign = 0 };
			var ipHint = new Label (GettextCatalog.GetString ("(blank = localhost)")) { Xalign = 0 };
			var ipHbox = new HBox (false, 6);
			ipHbox.PackStart (ipAddress, false, false, 0);
			ipHbox.PackStart (ipHint, false, false, 0);
			var portLabel = new Label (GettextCatalog.GetString ("Port:")) { Xalign = 0 };
			var portHint = new Label (GettextCatalog.GetString ("(0 = random)")) { Xalign = 0 };
			var portHbox = new HBox (false, 6);
			portHbox.PackStart (portNumber, false, false, 0);
			portHbox.PackStart (portHint, false, false, 0);
			verboseCheck.Label = GettextCatalog.GetString ("Verbose console output");
			verboseCheck.Xalign = 0;

			var ipTable = new Table (3, 3, false) { ColumnSpacing = 6, RowSpacing = 6 };
			ipTable.Attach (gpadLabel, 0, 1, 0, 1, AttachOptions.Fill, 0, 0, 0);
			ipTable.Attach (ipLabel, 1, 2, 0, 1, AttachOptions.Fill, 0, 0, 0);
			ipTable.Attach (ipHbox, 2, 3, 0, 1, AttachOptions.Fill, 0, 0, 0);
			ipTable.Attach (portLabel, 1, 2, 1, 2, AttachOptions.Fill, 0, 0, 0);
			ipTable.Attach (portHbox, 2, 3, 1, 2, AttachOptions.Fill, 0, 0, 0);
			ipTable.Attach (verboseCheck, 1, 3, 2, 3, AttachOptions.Fill, 0, 0, 0);
			PackStart (ipTable);

			PackStart (new Label { Markup = "<b>" + GettextCatalog.GetString ("Security") + "</b>", Xalign = 0 });

			var sslPadLabel = new Label { WidthRequest = 12 };
			var sslModeLabel = new Label (GettextCatalog.GetString ("SSL mode:")) { Xalign = 0 };
			var sslModeAlign = new Alignment (0, 0, 0, 0) { Child = sslMode };
			var sslProtocolLabel = new Label (GettextCatalog.GetString ("SSL protocol:")) { Xalign = 0 };
			var sslProtocolAlign = new Alignment (0, 0, 0, 0) { Child = sslProtocol };

			var sslTable = new Table (2, 3, false) { ColumnSpacing = 6, RowSpacing = 6 };
			sslTable.Attach (sslPadLabel, 0, 1, 0, 1, AttachOptions.Fill, 0, 0, 0);
			sslTable.Attach (sslModeLabel, 1, 2, 0, 1, AttachOptions.Fill, 0, 0, 0);
			sslTable.Attach (sslModeAlign, 2, 3, 0, 1, AttachOptions.Fill, 0, 0, 0);
			sslTable.Attach (sslProtocolLabel, 1, 2, 1, 2, AttachOptions.Fill, 0, 0, 0);
			sslTable.Attach (sslProtocolAlign, 2, 3, 1, 2, AttachOptions.Fill, 0, 0, 0);
			PackStart (sslTable);

			PackStart (new Label { Markup = "<b>" + GettextCatalog.GetString ("SSL Key") + "</b>", Xalign = 0 });

			var keyPadLabel = new Label { WidthRequest = 12 };
			var keyTypeLabel = new Label (GettextCatalog.GetString ("Key type:")) { Xalign = 0 };
			var keyTypeAlign = new Alignment (0, 0, 0, 0) { Child = keyType };
			var keyFileLabel = new Label (GettextCatalog.GetString ("Key file:")) { Xalign = 0 };
			var certFileLabel = new Label (GettextCatalog.GetString ("Certificate file:")) { Xalign = 0 };
			var passwordLabel = new Label (GettextCatalog.GetString ("Password:")) { Xalign = 0 };
			var passwordHbox = new HBox (false, 6);
			passwordHbox.PackStart (passwordOptions, false, false, 0);
			passwordHbox.PackStart (passwordEntry, true, true, 0);

			var keyTable = new Table (4, 4, false) { ColumnSpacing = 6, RowSpacing = 6 };
			keyTable.Attach (keyPadLabel,     0, 1, 0, 1, AttachOptions.Fill, 0, 0, 0);
			keyTable.Attach (keyTypeLabel,    1, 2, 0, 1, AttachOptions.Fill, 0, 0, 0);
			keyTable.Attach (keyTypeAlign,    2, 3, 0, 1, AttachOptions.Fill, 0, 0, 0);
			keyTable.Attach (keyFileLabel,    1, 2, 1, 2, AttachOptions.Fill, 0, 0, 0);
			keyTable.Attach (keyLocation,     2, 4, 1, 2, AttachOptions.Expand | AttachOptions.Fill, 0, 0, 0);
			keyTable.Attach (certFileLabel,   1, 2, 2, 3, AttachOptions.Fill, 0, 0, 0);
			keyTable.Attach (certLocation,    2, 4, 2, 3, AttachOptions.Expand | AttachOptions.Fill, 0, 0, 0);
			keyTable.Attach (passwordLabel,   1, 2, 3, 4, AttachOptions.Fill, 0, 0, 0);
			keyTable.Attach (passwordHbox,    2, 4, 3, 4, AttachOptions.Expand | AttachOptions.Fill, 0, 0, 0);
			PackStart (keyTable);

			sslMode.Changed += UpdateSensitivity;
			keyType.Changed += UpdateSensitivity;
			passwordOptions.Changed += UpdateSensitivity;

			ShowAll ();
		}
		
		public void Store (AspNetAppProject project)
		{
			XspParameters xPar = project.XspParameters;
			
			xPar.Address = ipAddress.Text;
			xPar.Port = Convert.ToUInt16 (portNumber.Value);
			xPar.Verbose = verboseCheck.Active;
			xPar.SslMode = (XspSslMode) sslMode.Active;
			xPar.SslProtocol = (XspSslProtocol) sslProtocol.Active;
			xPar.KeyType = (XspKeyType) keyType.Active;
			xPar.PrivateKeyFile = keyLocation.Path;
			xPar.CertificateFile = certLocation.Path;
			xPar.PasswordOptions = (XspPasswordOptions) passwordOptions.Active;
			xPar.PrivateKeyPassword = passwordEntry.Text;
		}
		
		void UpdateSensitivity (object sender, EventArgs e)
		{
			bool sslEnabled = ((XspSslMode) sslMode.Active) != XspSslMode.None;
			sslProtocol.Sensitive = sslEnabled;
			keyType.Sensitive = sslEnabled;
			
			bool keyEnabled = (sslEnabled) && (keyType.Active != 0);
			keyLocation.Sensitive = keyEnabled;
			passwordOptions.Sensitive = keyEnabled;
			
			bool certEnabled = (keyEnabled) && (keyType.Active == 2);
			certLocation.Sensitive = certEnabled;
			
			passwordEntry.Sensitive = (keyEnabled) && (passwordOptions.Active == 2);
			if (!passwordEntry.Sensitive)
				passwordEntry.Text = "";
		}
	}
}
