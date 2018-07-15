// 
// Keychain.cs
//
// Authors: Michael Hutchinson <mhutchinson@novell.com>
//          Jeffrey Stedfast <jeff@xamarin.com>
//
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
// Copyright (c) 2013 Xamarin Inc. (http://www.xamarin.com)
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
using System.Text;
using System.Runtime.InteropServices;
using Security;

namespace MonoDevelop.MacInterop
{
	public static class Keychain
	{
		const string CoreFoundationLib = ObjCRuntime.Constants.CoreFoundationLibrary;
		const string SecurityLib = ObjCRuntime.Constants.SecurityLibrary;

		internal static IntPtr CurrentKeychain = IntPtr.Zero;

		[DllImport (CoreFoundationLib, EntryPoint="CFRelease")]
		static extern void CFReleaseInternal (IntPtr cfRef);

		static void CFRelease (IntPtr cfRef)
		{
			if (cfRef != IntPtr.Zero)
				CFReleaseInternal (cfRef);
		}

		#region Managing Keychains

		[DllImport (SecurityLib)]
		static extern SecStatusCode SecKeychainCreate (byte[] pathName, uint passwordLength, byte[] password,
		                                          bool promptUser,  IntPtr initialAccess, out IntPtr keychain);

		[DllImport (SecurityLib)]
		static extern SecStatusCode SecKeychainDelete (IntPtr keychain);

		[DllImport (SecurityLib)]
		static extern SecStatusCode SecKeychainOpen (byte[] pathName, out IntPtr keychain);

		internal static IntPtr CreateKeychain (string path, string password)
		{
			var passwd = Encoding.UTF8.GetBytes (password);

			var status = SecKeychainCreate (path.ToNullTerminatedUtf8 (), (uint) passwd.Length, passwd, false, IntPtr.Zero, out IntPtr result);
			if (status != SecStatusCode.Success)
				throw new Exception (status.GetStatusDescription ());

			return result;
		}

		internal static bool TryDeleteKeychain (string path)
		{
			var result = SecKeychainOpen (path.ToNullTerminatedUtf8 (), out IntPtr ptr);
			try {
				if (result == SecStatusCode.Success) {
					DeleteKeychain (ptr);
					return true;
				}
			} catch {
				// If we call 'SecKeychainOpen' on a keychain path that does not exist,
				// we get a return value of 'success' and we also get a non-null handle.
				// As such there's no way for the test suite to safely check if a keychain
				// exists so it can delete a pre-existing one before exceuting. Work around
				// this by wrapping in a try/catch. The 'DeleteKeyChain (IntPtr)' method 
				// will throw a 'key chain does not exist' error if there is no pre-existing
				// keychain
			} finally {
				CFRelease (ptr);
			}
			return false;
		}

		internal static void DeleteKeychain (IntPtr keychain)
		{
			var status = SecKeychainDelete (keychain);
			if (status != SecStatusCode.Success)
				throw new Exception (status.GetStatusDescription ());
		}

		#endregion

		#region Storing and Retrieving Passwords

		[DllImport (SecurityLib)]
		static extern SecStatusCode SecKeychainAddInternetPassword (IntPtr keychain, uint serverNameLength, byte[] serverName, uint securityDomainLength,
		                                                       byte[] securityDomain, uint accountNameLength, byte[] accountName, uint pathLength,
		                                                       byte[] path, ushort port, SecProtocolType protocol, SecAuthenticationType authType,
		                                                       uint passwordLength, byte[] passwordData, ref IntPtr itemRef);
		[DllImport (SecurityLib)]
		static extern SecStatusCode SecKeychainFindInternetPassword (IntPtr keychain, uint serverNameLength, byte[] serverName, uint securityDomainLength,
		                                                        byte[] securityDomain, uint accountNameLength, byte[] accountName, uint pathLength,
		                                                        byte[] path, ushort port, SecProtocolType protocol, SecAuthenticationType authType,
		                                                        out uint passwordLength, out IntPtr passwordData, ref IntPtr itemRef);

		[DllImport (SecurityLib)]
		static extern SecStatusCode SecKeychainFindInternetPassword (IntPtr keychain, uint serverNameLength, byte[] serverName, uint securityDomainLength,
		                                                        byte[] securityDomain, uint accountNameLength, byte[] accountName, uint pathLength,
		                                                        byte[] path, ushort port, SecProtocolType protocol, SecAuthenticationType authType,
		                                                        IntPtr passwordLengthRef, IntPtr passwordDataRef, ref IntPtr itemRef);

		[DllImport (SecurityLib)]
		static extern SecStatusCode SecKeychainAddGenericPassword (IntPtr keychain, uint serviceNameLength, byte[] serviceName,
		                                                      uint accountNameLength, byte[] accountName, uint passwordLength,
		                                                      byte[] passwordData, ref IntPtr itemRef);
		[DllImport (SecurityLib)]
		static extern SecStatusCode SecKeychainFindGenericPassword (IntPtr keychain, uint serviceNameLength, byte[] serviceName,
		                                                       uint accountNameLength, byte[] accountName, out uint passwordLength,
		                                                       out IntPtr passwordData, ref IntPtr itemRef);

		#endregion

		#region Creating and Deleting Keychain Items

		[StructLayout (LayoutKind.Sequential)]
		struct SecKeychainAttributeList
		{
			public int Count;
			public IntPtr Attrs;

			public SecKeychainAttributeList (int count, IntPtr attrs)
			{
				Count = count;
				Attrs = attrs;
			}
		}

		[StructLayout (LayoutKind.Sequential)]
		struct SecKeychainAttribute
		{
			public SecItemAttr Tag;
			public uint Length;
			public IntPtr Data;

			public SecKeychainAttribute (SecItemAttr tag, uint length, IntPtr data)
			{
				Tag = tag;
				Length = length;
				Data = data;
			}
		}

		[DllImport (SecurityLib)]
		static extern unsafe SecStatusCode SecKeychainItemCreateFromContent (SecItemClass itemClass, SecKeychainAttributeList *attrList,
		                                                                uint passwordLength, byte[] password, IntPtr keychain,
		                                                                IntPtr initialAccess, IntPtr itemRef);

		[DllImport (SecurityLib)]
		static extern SecStatusCode SecKeychainItemDelete (IntPtr itemRef);

		#endregion

		#region Managing Keychain Items

		[StructLayout (LayoutKind.Sequential)]
		unsafe struct SecKeychainAttributeInfo
		{
			public uint Count;
			public int* Tag;
			public int* Format;
		}

		[DllImport (SecurityLib)]
		static extern unsafe SecStatusCode SecKeychainItemFreeAttributesAndData (SecKeychainAttributeList* list, IntPtr data);

		[DllImport (SecurityLib)]
		static extern unsafe SecStatusCode SecKeychainItemCopyAttributesAndData (IntPtr itemRef, SecKeychainAttributeInfo* info, ref SecItemClass itemClass,
		                                                                         SecKeychainAttributeList** attrList, IntPtr lengthRef, IntPtr outDataRef);

		[DllImport (SecurityLib)]
		static extern unsafe SecStatusCode SecKeychainItemModifyAttributesAndData (IntPtr itemRef, SecKeychainAttributeList *attrList, uint length, byte [] data);

		[DllImport (SecurityLib)]
		static extern SecStatusCode SecKeychainItemCopyContent (IntPtr itemRef, ref SecItemClass itemClass, IntPtr attrList, ref uint length, ref IntPtr data);

		[DllImport (SecurityLib)]
		static extern SecStatusCode SecKeychainItemFreeContent (IntPtr attrList, IntPtr data);

		#endregion

		static SecAuthenticationType GetSecAuthenticationType (string query)
		{
			if (string.IsNullOrEmpty (query))
				return SecAuthenticationType.Any;

			string auth = "default";
			foreach (var pair in query.Substring (1).Split (new char[] { '&' })) {
				var kvp = pair.Split (new char[] { '=' });
				if (string.Equals (kvp[0], "auth", StringComparison.InvariantCultureIgnoreCase) && kvp.Length == 2) {
					auth = kvp[1].ToLowerInvariant ();
					break;
				}
			}

			switch (auth) {
			case "ntlm": return SecAuthenticationType.NTLM;
			case "msn": return SecAuthenticationType.MSN;
			case "dpa": return SecAuthenticationType.DPA;
			case "rpa": return SecAuthenticationType.RPA;
			case "httpbasic": case "basic": return SecAuthenticationType.HTTPBasic;
			case "httpdigest": case "digest": return SecAuthenticationType.HTTPDigest;
			case "htmlform": case "form": return SecAuthenticationType.HTMLForm;
			case "default": return SecAuthenticationType.Default;
			default: return SecAuthenticationType.Any;
			}
		}

		static SecProtocolType GetSecProtocolType (string protocol)
		{
			switch (protocol.ToLowerInvariant ()) {
			case "ftp": return SecProtocolType.FTP;
			case "ftpaccount": return SecProtocolType.FTPAccount;
			case "http": return SecProtocolType.HTTP;
			case "irc": return SecProtocolType.IRC;
			case "nntp": return SecProtocolType.NNTP;
			case "pop3": return SecProtocolType.POP3;
			case "smtp": return SecProtocolType.SMTP;
			case "socks": return SecProtocolType.SOCKS;
			case "imap": return SecProtocolType.IMAP;
			case "ldap": return SecProtocolType.LDAP;
			case "appletalk": return SecProtocolType.AppleTalk;
			case "afp": return SecProtocolType.AFP;
			case "telnet": return SecProtocolType.Telnet;
			case "ssh": return SecProtocolType.SSH;
			case "ftps": return SecProtocolType.FTPS;
			case "httpproxy": return SecProtocolType.HTTPProxy;
			case "httpsproxy": return SecProtocolType.HTTPSProxy;
			case "ftpproxy": return SecProtocolType.FTPProxy;
			case "cifs": return SecProtocolType.CIFS;
			case "smb": return SecProtocolType.SMB;
			case "rtsp": return SecProtocolType.RTSP;
			case "rtspproxy": return SecProtocolType.RTSPProxy;
			case "daap": return SecProtocolType.DAAP;
			case "eppc": return SecProtocolType.EPPC;
			case "ipp": return SecProtocolType.IPP;
			case "nntps": return SecProtocolType.NNTPS;
			case "ldaps": return SecProtocolType.LDAPS;
			case "telnets": return SecProtocolType.TelnetS;
			case "imaps": return SecProtocolType.IMAPS;
			case "ircs": return SecProtocolType.IRCS;
			case "pop3s": return SecProtocolType.POP3S;
			case "cvspserver": return SecProtocolType.CVSpserver;
			case "svn": return SecProtocolType.SVN;
			default: return SecProtocolType.Any;
			}
		}

		static unsafe SecStatusCode ReplaceInternetPassword (IntPtr item, byte[] desc, byte[] passwd)
		{
			fixed (byte* descPtr = desc) {
				SecKeychainAttribute* attrs = stackalloc SecKeychainAttribute [1];
				int n = 0;

				if (desc != null)
					attrs[n++] = new SecKeychainAttribute (SecItemAttr.Description, (uint) desc.Length, (IntPtr) descPtr);

				SecKeychainAttributeList attrList = new SecKeychainAttributeList (n, (IntPtr) attrs);

				return SecKeychainItemModifyAttributesAndData (item, &attrList, (uint) passwd.Length, passwd);
			}
		}

		static unsafe SecStatusCode AddInternetPassword (byte[] label, byte[] desc, SecAuthenticationType auth, byte[] user, byte[] passwd, SecProtocolType protocol, byte[] host, int port, byte[] path)
		{
			// Note: the following code does more-or-less the same as:
			//SecKeychainAddInternetPassword (CurrentKeychain, (uint) host.Length, host, 0, null,
			//                                (uint) user.Length, user, (uint) path.Length, path, (ushort) port,
			//                                protocol, auth, (uint) passwd.Length, passwd, ref item);

			fixed (byte* labelPtr = label, descPtr = desc, userPtr = user, hostPtr = host, pathPtr = path) {
				SecKeychainAttribute* attrs = stackalloc SecKeychainAttribute [8];
				int* protoPtr = (int*) &protocol;
				int* authPtr = (int*) &auth;
				int* portPtr = &port;
				int n = 0;

				attrs[n++] = new SecKeychainAttribute (SecItemAttr.Label,    (uint) label.Length, (IntPtr) labelPtr);
				if (desc != null)
					attrs[n++] = new SecKeychainAttribute (SecItemAttr.Description, (uint) desc.Length, (IntPtr) descPtr);
				attrs[n++] = new SecKeychainAttribute (SecItemAttr.Account,  (uint) user.Length,  (IntPtr) userPtr);
				attrs[n++] = new SecKeychainAttribute (SecItemAttr.Protocol, (uint) 4,            (IntPtr) protoPtr);
				attrs[n++] = new SecKeychainAttribute (SecItemAttr.AuthType, (uint) 4,            (IntPtr) authPtr);
				attrs[n++] = new SecKeychainAttribute (SecItemAttr.Server,   (uint) host.Length,  (IntPtr) hostPtr);
				attrs[n++] = new SecKeychainAttribute (SecItemAttr.Port,     (uint) 4,            (IntPtr) portPtr);
				attrs[n++] = new SecKeychainAttribute (SecItemAttr.Path,     (uint) path.Length,  (IntPtr) pathPtr);

				SecKeychainAttributeList attrList = new SecKeychainAttributeList (n, (IntPtr) attrs);

				var result = SecKeychainItemCreateFromContent (SecItemClass.InternetPassword, &attrList, (uint) passwd.Length, passwd, CurrentKeychain, IntPtr.Zero, IntPtr.Zero);

				return result;
			}
		}

		public static unsafe void AddInternetPassword (Uri uri, string username, string password)
		{
			byte[] path = uri.ToPathBytes ();
			byte[] passwd = Encoding.UTF8.GetBytes (password);
			byte[] host = Encoding.UTF8.GetBytes (uri.Host);
			byte[] user = Encoding.UTF8.GetBytes (username);
			var auth = GetSecAuthenticationType (uri.Query);
			var protocol = GetSecProtocolType (uri.Scheme);
			IntPtr item = IntPtr.Zero;
			int port = uri.Port;
			byte[] desc = null;

			if (auth == SecAuthenticationType.HTMLForm)
				desc = WebFormPassword;

			// See if there is already a password there for this uri
			var result = SecKeychainFindInternetPassword (CurrentKeychain, (uint)host.Length, host, 0, null,
											  (uint)user.Length, user, (uint)path.Length, path, (ushort)port,
											  protocol, auth, IntPtr.Zero, IntPtr.Zero, ref item);

			try {
				if (result == SecStatusCode.Success) {
					// If there is, replace it with the new one
					result = ReplaceInternetPassword (item, desc, passwd);
				} else {
					var label = Encoding.UTF8.GetBytes (string.Format ("{0} ({1})", uri.Host, username));

					result = AddInternetPassword (label, desc, auth, user, passwd, protocol, host, port, path);
				}
			} finally {
				CFRelease (item);
			}

			if (result != SecStatusCode.Success && result != SecStatusCode.DuplicateItem)
				throw new Exception ("Could not add internet password to keychain: " + result.GetStatusDescription ());
		}

		static readonly byte[] WebFormPassword = Encoding.UTF8.GetBytes ("Web form password");

		public static unsafe void AddInternetPassword (Uri uri, string password) =>
			AddInternetPassword (uri, Uri.UnescapeDataString (uri.UserInfo), password);

		static unsafe string GetUsernameFromKeychainItemRef (IntPtr itemRef)
		{
			int[] formatConstants = { (int) CssmDbAttributeFormat.String };
			int[] attributeTags = { (int) SecItemAttr.Account };

			fixed (int* tags = attributeTags, formats = formatConstants) {
				var attributeInfo = new SecKeychainAttributeInfo {
					Count = 1,
					Tag = tags,
					Format = formats
				};
				SecKeychainAttributeList* attributeList = null;
				SecItemClass itemClass = 0;

				try {
					SecStatusCode status = SecKeychainItemCopyAttributesAndData (itemRef, &attributeInfo, ref itemClass, &attributeList, IntPtr.Zero, IntPtr.Zero);

					if (status == SecStatusCode.ItemNotFound)
						throw new Exception ("Could not add internet password to keychain: " + status.GetStatusDescription ());

					if (status != SecStatusCode.Success)
						throw new Exception ("Could not find internet username and password: " + status.GetStatusDescription ());

					var userNameAttr = (SecKeychainAttribute*)attributeList->Attrs;

					if (userNameAttr->Length == 0)
						return null;

					return Marshal.PtrToStringAuto (userNameAttr->Data, (int)userNameAttr->Length);
				} finally {
					SecKeychainItemFreeAttributesAndData (attributeList, IntPtr.Zero);
				}
			}
		}

		public static unsafe Tuple<string, string> FindInternetUserNameAndPassword (Uri uri)
		{
			var protocol = GetSecProtocolType (uri.Scheme);
			return FindInternetUserNameAndPassword (uri, protocol);
		}

		public static unsafe Tuple<string, string> FindInternetUserNameAndPassword (Uri uri, SecProtocolType protocol)
		{
			byte[] path = uri.ToPathBytes ();
			byte[] host = Encoding.UTF8.GetBytes (uri.Host);
			var auth = GetSecAuthenticationType (uri.Query);
			IntPtr passwordData;
			IntPtr item = IntPtr.Zero;
			uint passwordLength = 0;

			var result = SecKeychainFindInternetPassword (
				CurrentKeychain, (uint) host.Length, host, 0, null,
				0, null, (uint) path.Length, path, (ushort) uri.Port,
				protocol, auth, out passwordLength, out passwordData, ref item);

			try {
				if (result != SecStatusCode.Success)
					return null;

				var username = GetUsernameFromKeychainItemRef (item);
				return Tuple.Create (username, Marshal.PtrToStringAuto (passwordData, (int) passwordLength));
			} finally {
				SecKeychainItemFreeContent (IntPtr.Zero, passwordData);
				CFRelease (item);
			}
		}

		public static string FindInternetPassword (Uri uri)
		{
			byte[] path = uri.ToPathBytes ();
			byte[] user = Encoding.UTF8.GetBytes (Uri.UnescapeDataString (uri.UserInfo));
			byte[] host = Encoding.UTF8.GetBytes (uri.Host);
			var auth = GetSecAuthenticationType (uri.Query);
			var protocol = GetSecProtocolType (uri.Scheme);
			IntPtr passwordData = IntPtr.Zero;
			IntPtr item = IntPtr.Zero;
			uint passwordLength = 0;

			try {
				// Look for an internet password for the given protocol and auth mechanism
				var result = SecKeychainFindInternetPassword (CurrentKeychain, (uint)host.Length, host, 0, null,
															  (uint)user.Length, user, (uint)path.Length, path, (ushort)uri.Port,
															  protocol, auth, out passwordLength, out passwordData, ref item);

				// Fall back to looking for a password for SecProtocolType.Any && SecAuthenticationType.Any
				if (result == SecStatusCode.ItemNotFound && protocol != SecProtocolType.Any)
					result = SecKeychainFindInternetPassword (CurrentKeychain, (uint)host.Length, host, 0, null,
															  (uint)user.Length, user, (uint)path.Length, path, (ushort)uri.Port,
															  0, auth, out passwordLength, out passwordData, ref item);

				if (result != SecStatusCode.Success)
					return null;

				return Marshal.PtrToStringAuto (passwordData, (int)passwordLength);
			} finally {
				CFRelease (item);

				SecKeychainItemFreeContent (IntPtr.Zero, passwordData);
			}
		}

		public static void RemoveInternetPassword (Uri uri)
		{
			byte[] path = uri.ToPathBytes ();
			byte[] user = Encoding.UTF8.GetBytes (Uri.UnescapeDataString (uri.UserInfo));
			byte[] host = Encoding.UTF8.GetBytes (uri.Host);
			var auth = GetSecAuthenticationType (uri.Query);
			var protocol = GetSecProtocolType (uri.Scheme);
			IntPtr item = IntPtr.Zero;

			// Look for an internet password for the given protocol and auth mechanism
			var result = SecKeychainFindInternetPassword (CurrentKeychain, (uint) host.Length, host, 0, null,
				(uint) user.Length, user, (uint) path.Length, path, (ushort) uri.Port,
				protocol, auth, IntPtr.Zero, IntPtr.Zero, ref item);

			// Fall back to looking for a password for SecProtocolType.Any && SecAuthenticationType.Any
			if (result == SecStatusCode.ItemNotFound && protocol != SecProtocolType.Any)
				result = SecKeychainFindInternetPassword (CurrentKeychain, (uint) host.Length, host, 0, null,
					(uint) user.Length, user, (uint) path.Length, path, (ushort) uri.Port,
					0, auth, IntPtr.Zero, IntPtr.Zero, ref item);

			try {
				if (result != SecStatusCode.Success)
					return;

				SecKeychainItemDelete (item);
			} finally {
				CFRelease (item);
			}
		}

		public static void RemoveInternetUserNameAndPassword (Uri uri)
		{
			byte[] path = uri.ToPathBytes ();
			byte[] host = Encoding.UTF8.GetBytes (uri.Host);
			var auth = GetSecAuthenticationType (uri.Query);
			IntPtr item = IntPtr.Zero;

			var result = SecKeychainFindInternetPassword (
				CurrentKeychain, (uint) host.Length, host, 0, null,
				0, null, (uint) path.Length, path, (ushort) uri.Port,
				GetSecProtocolType (uri.Scheme), auth, IntPtr.Zero, IntPtr.Zero, ref item);

			try {
				if (result != SecStatusCode.Success)
					return;

				result = SecKeychainItemDelete (item);
			} finally {
				CFRelease (item);
			}
		}

		static byte [] ToPathBytes (this Uri uri)
		{
			var pathStr = string.Join (string.Empty, uri.Segments);
			byte[] path = pathStr.Length > 0 ? pathStr.ToNullTerminatedUtf8 (1) : Array.Empty<byte> (); // don't include the leading '/'
			return path;
		}

		// It seems that keychain APIs require null terminated native strings.
		internal static byte [] ToNullTerminatedUtf8 (this string str, int offset = 0)
		{
			unsafe {
				fixed (char* p = str) {
					return ToNullTerminatedUtf8 (p + offset, str.Length - offset);
				}
			}
		}

		static unsafe byte [] ToNullTerminatedUtf8 (char* p, int len)
		{
			if (len == 0)
				return Array.Empty<byte> ();

			var byteCount = Encoding.UTF8.GetByteCount (p, len);
			var bytes = new byte [byteCount + 1];
			fixed (byte* b = bytes) {
				Encoding.UTF8.GetBytes (p, len, b, byteCount);
			}
			return bytes;
		}
	}

	enum SecItemClass : uint
	{
		InternetPassword     = 1768842612, // 'inet'
		GenericPassword      = 1734700656, // 'genp'
		AppleSharePassword   = 1634953328, // 'ashp'
		Certificate          = 0x80000000 + 0x1000,
		PublicKey            = 0x0000000A + 5,
		PrivateKey           = 0x0000000A + 6,
		SymmetricKey         = 0x0000000A + 7
	}

	enum SecItemAttr : int
	{
		CreationDate         = 1667522932,
		ModDate              = 1835295092,
		Description          = 1684370275,
		Comment              = 1768123764,
		Creator              = 1668445298,
		Type                 = 1954115685,
		ScriptCode           = 1935897200,
		Label                = 1818321516,
		Invisible            = 1768846953,
		Negative             = 1852139361,
		CustomIcon           = 1668641641,
		Account              = 1633903476,
		Service              = 1937138533,
		Generic              = 1734700641,
		SecurityDomain       = 1935961454,
		Server               = 1936881266,
		AuthType             = 1635023216,
		Port                 = 1886351988,
		Path                 = 1885434984,
		Volume               = 1986817381,
		Address              = 1633969266,
		Signature            = 1936943463,
		Protocol             = 1886675820,
		CertificateType      = 1668577648,
		CertificateEncoding  = 1667591779,
		CrlType              = 1668445296,
		CrlEncoding          = 1668443747,
		Alias                = 1634494835,
	}

	enum SecAuthenticationType : int
	{
		NTLM                 = 1835824238,
		MSN                  = 1634628461,
		DPA                  = 1633775716,
		RPA                  = 1633775730,
		HTTPBasic            = 1886680168,
		HTTPDigest           = 1685353576,
		HTMLForm             = 1836216166,
		Default              = 1953261156,
		Any                  = 0
	}

	public enum SecProtocolType : int
	{
		FTP                  = 1718906912,
		FTPAccount           = 1718906977,
		HTTP                 = 1752462448,
		IRC                  = 1769104160,
		NNTP                 = 1852732528,
		POP3                 = 1886351411,
		SMTP                 = 1936553072,
		SOCKS                = 1936685088,
		IMAP                 = 1768776048,
		LDAP                 = 1818517872,
		AppleTalk            = 1635019883,
		AFP                  = 1634103328,
		Telnet               = 1952803950,
		SSH                  = 1936943136,
		FTPS                 = 1718906995,
		HTTPProxy            = 1752461432,
		HTTPSProxy           = 1752462200,
		FTPProxy             = 1718907000,
		CIFS                 = 1667851891,
		SMB                  = 1936548384,
		RTSP                 = 1920234352,
		RTSPProxy            = 1920234360,
		DAAP                 = 1684103536,
		EPPC                 = 1701867619,
		IPP                  = 1768976416,
		NNTPS                = 1853124723,
		LDAPS                = 1818521715,
		TelnetS              = 1952803955,
		IMAPS                = 1768779891,
		IRCS                 = 1769104243,
		POP3S                = 1886351475,
		CVSpserver           = 1668707184,
		SVN                  = 1937141280,
		Any                  = 0
	}

	enum CssmDbAttributeFormat : int
	{
		String               = 0,
		Int32                = 1,
		UInt32               = 2,
		BigNum               = 3,
		Real                 = 4,
		DateTime             = 5,
		Blob                 = 6,
		MultiUInt32          = 7,
		Complex              = 8
	}
}
