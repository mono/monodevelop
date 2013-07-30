//
// WindowsProxyCredentialProvider.cs
//
// Author:
//       Bojan Rajkovic <bojan.rajkovic@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc.
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
using MonoDevelop.Core.Web;
using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide;
using System.Net;
using System.Drawing;

namespace MonoDevelop.Platform.Windows
{
	public class WindowsProxyCredentialProvider : ICredentialProvider
	{
		[DllImport("credui")]
		static extern CredUIReturnCodes CredUIPromptForCredentials (ref CredentialUIInfo creditUR, string targetName,
		                                                            IntPtr reserved1, int iError, StringBuilder userName,
		                                                            int maxUserName, StringBuilder password, int maxPassword,
		                                                            [MarshalAs (UnmanagedType.Bool)] ref bool pfSave,
		                                                            CredentialUIFlags flags);

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		private struct CredentialUIInfo
		{
			public int StructureSize;
			public IntPtr ParentWindow;
			public string MessageText;
			public string CaptionText;
			public IntPtr BannerBitmap;
		}

		[Flags]
		enum CredentialUIFlags
		{
			IncorrectPassword         = 0x1,
			DoNotPersist              = 0x2,
			RequestAdministrator      = 0x4,
			ExcludeCertificates       = 0x8,
			RequireCertificate        = 0x10,
			ShowSaveCheckBox          = 0x40,
			AlwaysShowUi              = 0x80,
			RequireSmartcard          = 0x100,
			PasswordOnlyOk            = 0x200,
			ValidateUsername          = 0x400,
			CompleteUsername          = 0x800,
			Persist                   = 0x1000,
			ServerCredential          = 0x4000,
			ExpectConfirmation        = 0x20000,
			GenericCredentials        = 0x40000,
			UsernameTargetCredentials = 0x80000,
			KeepUsername              = 0x100000,
		}

		public enum CredUIReturnCodes
		{
			NoError                 = 0,
			ErrorCancelled          = 1223,
			ErrorNoSuchLogonSession = 1312,
			ErrorNotFound           = 1168,
			ErrorInvalidAccountName = 1315,
			ErrorInsufficientBuffer = 122,
			ErrorInvalidParameter   = 87,
			ErrorInvalidFlags       = 1004,
		}

		// ICKY CODE BEGINS HERE

		internal class PlaceholderForm : GdkWin32.SpecialForm
		{
			internal string Username, Password;

			Uri uri;

			internal PlaceholderForm (Uri uri)
			{
				this.uri = uri;
				Size = new Size (0, 0);
				Visible = false;
			}

			public new DialogResult ShowDialog ()
			{
				StringBuilder password = new StringBuilder (), username = new StringBuilder ();
				var credUiInfo = new CredentialUIInfo ();
				credUiInfo.MessageText = string.Format ("Xamarin Studio needs proxy credentials to access {0}.", uri.Host);
				credUiInfo.StructureSize = Marshal.SizeOf (credUiInfo);
				credUiInfo.ParentWindow = Handle;
				var save = false;
				var flags = CredentialUIFlags.AlwaysShowUi | CredentialUIFlags.GenericCredentials;

				var returnCode = CredUIPromptForCredentials (ref credUiInfo, "Xamarin Studio", IntPtr.Zero, 0, username, 100, password, 100, ref save, flags);
				Username = username.ToString ();
				Password = password.ToString ();

				return returnCode == CredUIReturnCodes.NoError ? DialogResult.OK : DialogResult.Cancel;
			}
		}

		public ICredentials GetCredentials (Uri uri, IWebProxy proxy, CredentialType credentialType, bool retrying)
		{
			var form = new PlaceholderForm (uri);

			var result = GdkWin32.RunModalWin32Form (form, IdeApp.Workbench.RootWindow);
			string userName = form.Username, password = form.Password;

			return result ? new NetworkCredential (userName, password) : null;
		}
	}
}

