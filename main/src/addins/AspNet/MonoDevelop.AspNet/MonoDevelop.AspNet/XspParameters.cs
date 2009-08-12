//
// XspParameters.cs: stores and builds parameters for launching XSP server
//
// Authors:
//   Michael Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (C) 2006 Michael Hutchinson
//
//
// This source code is licenced under The MIT License:
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
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core.Serialization;

namespace MonoDevelop.AspNet
{
	[DataItem ("XspParameters")]
	public class XspParameters : ICloneable
	{
		[ItemProperty ("Port")]
		ushort port = 8080;
		
		[ItemProperty ("Address")]
		string address = "127.0.0.1";
		
		[ItemProperty ("SslMode")]
		XspSslMode sslMode = XspSslMode.None;
		
		[ItemProperty ("SslProtocol")]
		XspSslProtocol sslProtocol = XspSslProtocol.Default;
		
		[ItemProperty ("KeyType")]
		XspKeyType keyType = XspKeyType.None;
		
		[ItemProperty ("CertFile")]
		string certFile = "";
		
		[ItemProperty ("KeyFile")]
		string pkFile = "";
		
		[ItemProperty ("PasswordOptions")]
		XspPasswordOptions passwordOptions = XspPasswordOptions.None;
		
		[ItemProperty ("Password")]
		string password = "";
		
		[ItemProperty ("Verbose")]
		bool verbose = true;
		
		public object Clone ()
		{
			return MemberwiseClone ();
		}
		
		//UInt16 is not CLS-compliant
		public int Port {
			get { return (int) port; }
			set {
				try {
					port = System.Convert.ToUInt16 (value);
				} catch (InvalidCastException) {
					throw new ArgumentException ("The port value is outside the permitted range");
				}
			}
		}
		
		public string Address {
			get { return address; }
			set {
				try {
					System.Net.IPAddress tempAdd = System.Net.IPAddress.Parse (value);
					address = tempAdd.ToString ();
				} catch(FormatException) {
					throw new ArgumentException ("The IP address is invalid.");
				}
			}
		}
		
		public XspSslMode SslMode {
			get { return sslMode; }
			set { sslMode = value; }
		}
		
		public XspKeyType KeyType {
			get { return keyType; }
			set { keyType = value; }
		}
		
		public string CertificateFile {
			get { return certFile; }
			set { certFile = value; }
		}
		
		public string PrivateKeyFile {
			get { return pkFile; }
			set { pkFile = value; }
		}
		
		public XspPasswordOptions PasswordOptions {
			get { return passwordOptions; }
			set { passwordOptions = value; }
		}
		
		public string PrivateKeyPassword {
			get { return password; }
			set { password = value; }
		}
		
		public XspSslProtocol SslProtocol {
			get { return sslProtocol; }
			set { sslProtocol = value; }
		}
		
		public bool Verbose {
			get { return verbose; }
			set { verbose = value; }
		}
		
		public string GetXspParameters ()
		{
			System.Text.StringBuilder opt = new System.Text.StringBuilder ();
			
			opt.AppendFormat (" --port {0}", port);
			opt.AppendFormat (" --address {0}", address);
			
			switch (sslMode) {
				case XspSslMode.Enable:
					opt.Append (" --https");
					break;
				case XspSslMode.ClientAccept:
					opt.Append (" --https-client-accept");
					break;
				case XspSslMode.ClientRequire:
					opt.Append (" --https-client-require");
					break;
			}
			
			if (sslMode != XspSslMode.None)
				opt.AppendFormat (" --protocols {0}", System.Enum.GetName (typeof (XspSslProtocol), sslProtocol));
			
			switch (keyType)
			{
				case XspKeyType.None:
					break;
				case XspKeyType.PVK:
					if (!string.IsNullOrEmpty (certFile))
						opt.AppendFormat (" --cert {0}", certFile);
					if (!string.IsNullOrEmpty (pkFile))
						opt.AppendFormat (" --pkfile {0}", pkFile);
					break;
				case XspKeyType.Pkcs12:
					if (!string.IsNullOrEmpty (pkFile))
						opt.AppendFormat (" --p12file {0}", pkFile);
					break;
			}
			
			//password for keyfile
			if ((keyType != XspKeyType.None) && (passwordOptions != XspPasswordOptions.None)) {
				string pwtemp = "";
				
				if (passwordOptions == XspPasswordOptions.Store)
					pwtemp = password;
				else
					//TODO: hide password chars (will need custom dialogue)
					pwtemp = MonoDevelop.Core.Gui.MessageService.GetPassword (
					      "Please enter the password for your private key for the XSP Web Server",
					      "XSP Private Key Password");
				
				if (!string.IsNullOrEmpty (pwtemp))
					opt.AppendFormat (" --pkpwd {0}", pwtemp);
			}		
			
			/*
			opt.AppendFormat (" --appconfigfile {0}", "FILENAME");
			opt.AppendFormat (" --appconfigdir {0}", "DIR");
			opt.AppendFormat (" --applications {0}", "APPS");
			*/
			
			//required because we don't necessarily have a terminal
			opt.Append (" --nonstop");
			
			if (verbose)
				opt.Append (" --verbose");
			
			return opt.ToString ();
		}
	}
	
	public enum XspSslProtocol {
		Default,
		Tls,
		Ssl2,
		Ssl3
	}
	
	public enum XspSslMode {
		None,
		Enable,
		ClientAccept,
		ClientRequire
	}
	
	public enum XspKeyType {
		None,
		Pkcs12,
		PVK
	}
	
	public enum XspPasswordOptions {
		None,
		Ask,
		Store
	}
}
