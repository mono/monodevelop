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

namespace MonoDevelop.AspNet.Gui
{
	
	
	public partial class XspOptionsPanelWidget : Gtk.Bin
	{
		
		public XspOptionsPanelWidget (AspNetAppProject project)
		{
			this.Build();
			
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
			
			//set to valid port range
			portNumber.SetRange (0, UInt16.MaxValue);
			
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
		
		public void Store (AspNetAppProject project)
		{
			XspParameters xPar = project.XspParameters;
			
			xPar.Address = ipAddress.Text;
			xPar.Port = System.Convert.ToUInt16 (portNumber.Value);
			xPar.Verbose = verboseCheck.Active;
			xPar.SslMode = (XspSslMode) sslMode.Active;
			xPar.SslProtocol = (XspSslProtocol) sslProtocol.Active;
			xPar.KeyType = (XspKeyType) keyType.Active;
			xPar.PrivateKeyFile = keyLocation.Path;
			xPar.CertificateFile = certLocation.Path;
			xPar.PasswordOptions = (XspPasswordOptions) passwordOptions.Active;
			xPar.PrivateKeyPassword = passwordEntry.Text;
		}
		
		void updateSensitivity (object sender, EventArgs e)
		{
			bool sslEnabled = ((XspSslMode) sslMode.Active) != XspSslMode.None;
			sslProtocol.Sensitive = sslEnabled;
			keyType.Sensitive = sslEnabled;
			
			bool keyEnabled = (sslEnabled)? (keyType.Active != 0) : false;
			keyLocation.Sensitive = keyEnabled;
			passwordOptions.Sensitive = keyEnabled;
			
			bool certEnabled = (keyEnabled)? (keyType.Active == 2) : false;
			certLocation.Sensitive = certEnabled;
			
			passwordEntry.Sensitive = (keyEnabled)? (passwordOptions.Active == 2) : false;
			if (!passwordEntry.Sensitive)
				passwordEntry.Text = "";
		}
	}
}
