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
using System;
using System.Drawing;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.Web;
using MonoDevelop.Ide;

namespace MonoDevelop.Platform.Windows
{
	public class WindowsProxyCredentialProvider : ICredentialProvider
	{
		public ICredentials GetCredentials (Uri uri, IWebProxy proxy, CredentialType credentialType, ICredentials existingCredential, bool retrying)
		{
			if (uri == null)
				throw new ArgumentNullException ("uri");

			NetworkCredential currentCredentials = null;
			if (existingCredential != null)
				currentCredentials = Utility.GetCredentialsForUriFromICredentials (uri, existingCredential);

			var form = new PlaceholderForm (credentialType, uri, currentCredentials);
			var result = GdkWin32.RunModalWin32Form (form, IdeApp.Workbench.RootWindow);
			return result ? new NetworkCredential (form.Username, form.Password, form.Domain) : null;
		}
	}

	//Thar be dragons below here...

	class PlaceholderForm : GdkWin32.SpecialForm
	{
		internal string Username, Password, Domain;

		readonly Uri uri;
		readonly CredentialType type;
		readonly NetworkCredential current;

		internal PlaceholderForm (CredentialType type, Uri uri, NetworkCredential currentCredential)
		{
			this.uri = uri;
			this.type = type;
			current = currentCredential;
			Size = new Size (0, 0);
			Visible = false;
		}

		public override DialogResult ShowMagicDialog ()
		{
			var credUiInfo = new Native.CredentialUiInfo {
				MessageText = GettextCatalog.GetString ("{1} needs {2} credentials to access {0}.", uri.Host, BrandingService.ApplicationName, type == CredentialType.ProxyCredentials ? "proxy" : "request"),
				CaptionText = GettextCatalog.GetString ("{0} needs {1} credentials", BrandingService.ApplicationName, type == CredentialType.ProxyCredentials ? "proxy" : "request"),
				StructureSize = Marshal.SizeOf (typeof (Native.CredentialUiInfo)),
				ParentWindow = GdkWin32.HgdiobjGet (IdeApp.Workbench.RootWindow.GdkWindow)
			};

			var save = false;

			StringBuilder username = new StringBuilder (current != null ? current.UserName : string.Empty, 100), 
			              password = new StringBuilder (current != null ? current.Password : string.Empty, 100),
			              domain = new StringBuilder (100);
			int maxUsername = 100, maxPassword = 100, maxDomain = 100;

			var windowsVersion = Environment.OSVersion.Version;

			// Vista or higher = 6.x+, XP = 5.x
			if (windowsVersion.Major >= 6) {
				int outputSize, authPackage = 0;
				IntPtr output;

				var pinnedArray = new GCHandle ();
				uint packedAuthBufferLength = 0;

				if (current != null) {
					// Have creds? Pack them into the buffer and predisplay them.
					const int credPackGenericCredentials = 4;
					Native.CredPackAuthenticationBuffer (credPackGenericCredentials, current.UserName, current.Password,
						IntPtr.Zero, ref packedAuthBufferLength);
					// Now we know the size of the buffer, allocate a byte[] and pin it
					var packedAuthBufferBytes = new byte[packedAuthBufferLength];
					// Free the dummy handle from before
					if (pinnedArray.IsAllocated)
						pinnedArray.Free ();
					pinnedArray = GCHandle.Alloc (packedAuthBufferBytes, GCHandleType.Pinned);
					Native.CredPackAuthenticationBuffer (credPackGenericCredentials, current.UserName, current.Password, pinnedArray.AddrOfPinnedObject (), ref packedAuthBufferLength);
				}

				var authBuffer = current == null ? IntPtr.Zero : pinnedArray.AddrOfPinnedObject (); ;
				var returnCode = Native.CredUIPromptForWindowsCredentials (ref credUiInfo, 0, ref authPackage, authBuffer, packedAuthBufferLength,
					out output, out outputSize, ref save, Native.CredentialsUiWindowsFlags.Generic);

				// Unpin the array if we held it before
				if (authBuffer != IntPtr.Zero)
					pinnedArray.Free ();

				if (returnCode != Native.WindowsCredentialPromptReturnCode.NoError)
					return DialogResult.Cancel;

				if (!Native.CredUnPackAuthenticationBuffer (0, output, outputSize, username, ref maxUsername, domain, ref maxDomain, password, ref maxPassword))
					return DialogResult.Cancel;

				Native.CoTaskMemFree (output);

				Username = username.ToString ();
				Password = password.ToString ();
				Domain = domain.ToString ();

				return DialogResult.OK;
			} else {
				const Native.CredentialsUiFlags flags = Native.CredentialsUiFlags.AlwaysShowUi | Native.CredentialsUiFlags.GenericCredentials;
				var returnCode = Native.CredUIPromptForCredentials (ref credUiInfo, BrandingService.ApplicationName, IntPtr.Zero, 0,
					username, maxUsername, password, maxPassword, ref save, flags);
				Username = username.ToString ();
				Password = password.ToString ();
				Domain = string.Empty;

				return returnCode == Native.CredUiReturnCodes.NoError ? DialogResult.OK : DialogResult.Cancel;
			}
		}
	}

	static class Native
	{
		const string OLE32 = "ole32.dll";
		const string CREDUI = "credui.dll";

		[DllImport (OLE32)]
		internal static extern void CoTaskMemFree (IntPtr ptr);

		[DllImport (CREDUI)]
		internal static extern CredUiReturnCodes CredUIPromptForCredentials (ref CredentialUiInfo uiInfo, string targetName,
			IntPtr reserved1, int iError, StringBuilder userName, int maxUserName, StringBuilder password, int maxPassword,
			[MarshalAs (UnmanagedType.Bool)] ref bool pfSave, CredentialsUiFlags windowsFlags);

		[DllImport (CREDUI, CharSet = CharSet.Unicode)]
		internal static extern WindowsCredentialPromptReturnCode CredUIPromptForWindowsCredentials (ref CredentialUiInfo uiInfo,
			int authError, ref int authPackage, IntPtr inAuthBuffer, uint inAuthBufferSize,
			out IntPtr refOutAuthBuffer, out int refOutAuthBufferSize, ref bool fSave,
			CredentialsUiWindowsFlags uiWindowsFlags);

		[DllImport (CREDUI, CharSet = CharSet.Auto)]
		internal static extern bool CredUnPackAuthenticationBuffer (int dwFlags, IntPtr pAuthBuffer,
			int cbAuthBuffer, StringBuilder pszUserName, ref int pcchMaxUserName,
			StringBuilder pszDomainName, ref int pcchMaxDomainame, StringBuilder pszPassword,
			ref int pcchMaxPassword);

		[DllImport (CREDUI, CharSet = CharSet.Auto)]
		internal static extern bool CredPackAuthenticationBuffer (int dwFlags, string pszUserName, string pszPassword,
																  IntPtr packedCredentials, ref uint packedCredentialsLength);

		[StructLayout (LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		internal struct CredentialUiInfo
		{
			public int StructureSize;
			public IntPtr ParentWindow;
			public string MessageText;
			public string CaptionText;
			public IntPtr BannerBitmap;
		}

		[Flags]
		internal enum CredentialsUiFlags
		{
			IncorrectPassword = 0x1,
			DoNotPersist = 0x2,
			RequestAdministrator = 0x4,
			ExcludeCertificates = 0x8,
			RequireCertificate = 0x10,
			ShowSaveCheckBox = 0x40,
			AlwaysShowUi = 0x80,
			RequireSmartcard = 0x100,
			PasswordOnlyOk = 0x200,
			ValidateUsername = 0x400,
			CompleteUsername = 0x800,
			PERSIST = 0x1000,
			ServerCredential = 0x4000,
			ExpectConfirmation = 0x20000,
			GenericCredentials = 0x40000,
			UsernameTargetCredentials = 0x80000,
			KeepUsername = 0x100000,
		}

		internal enum CredUiReturnCodes
		{
			NoError = 0,
			ErrorCancelled = 1223,
			ErrorNoSuchLogonSession = 1312,
			ErrorNotFound = 1168,
			ErrorInvalidAccountName = 1315,
			ErrorInsufficientBuffer = 122,
			ErrorInvalidParameter = 87,
			ErrorInvalidFlags = 1004,
		}

		internal enum CredentialsUiWindowsFlags
		{
			/// <summary>
			/// The caller is requesting that the credential provider return the user name and password in plain text.
			/// This value cannot be combined with SECURE_PROMPT.
			/// </summary>
			Generic = 0x1,
			/// <summary>
			/// The Save check box is displayed in the dialog box.
			/// </summary>
			Checkbox = 0x2,
			/// <summary>
			/// Only credential providers that support the authentication package specified by the authPackage parameter should be enumerated.
			/// This value cannot be combined with InputCredentialsOnly.
			/// </summary>
			AuthenticationPackageOnly = 0x10,
			/// <summary>
			/// Only the credentials specified by the InAuthBuffer parameter for the authentication package specified by the authPackage parameter should be enumerated.
			/// If this flag is set, and the InAuthBuffer parameter is NULL, the function fails.
			/// This value cannot be combined with AuthenticationPackageOnly.
			/// </summary>
			InputCredentialsOnly = 0x20,
			/// <summary>
			/// Credential providers should enumerate only administrators. This value is intended for User Account Control (UAC) purposes only. We recommend that external callers not set this flag.
			/// </summary>
			EnumerateOnlyAdministrators = 0x100,
			/// <summary>
			/// Only the incoming credentials for the authentication package specified by the authPackage parameter should be enumerated.
			/// </summary>
			EnumerateOnlyCurrentUser = 0x200,
			/// <summary>
			/// The credential dialog box should be displayed on the secure desktop. This value cannot be combined with Generic.
			/// Windows Vista: This value is not supported until Windows Vista with SP1.
			/// </summary>
			ShowOnSecureDesktop = 0x1000,
			/// <summary>
			/// The credential provider should align the credential BLOB pointed to by the refOutAuthBuffer parameter to a 32-bit boundary, even if the provider is running on a 64-bit system.
			/// </summary>
			ShouldPackTo32BitBoundary = 0x10000000,
		}

		internal enum WindowsCredentialPromptReturnCode
		{
			NoError = 0,
			ErrorCancelled = 1223,
			ErrorNoSuchLogonSession = 1312,
			ErrorNotFound = 1168,
			ErrorInvalidAccountName = 1315,
			ErrorInsufficientBuffer = 122,
			ErrorInvalidParameter = 87,
			ErrorInvalidFlags = 1004,
		}
	}
}

