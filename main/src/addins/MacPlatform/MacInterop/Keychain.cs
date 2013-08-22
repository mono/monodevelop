// 
// Keychain.cs
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
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

using MonoDevelop.Core;

namespace MonoDevelop.MacInterop
{
	public static class Keychain
	{
		#region P/Invoke signatures
		
		const string SecurityLib = "/System/Library/Frameworks/Security.framework/Security";
		const string CFLib = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
		
		[DllImport (CFLib, EntryPoint="CFRelease")]
		static extern void CFReleaseInternal (IntPtr cfRef);
		
		static void CFRelease (IntPtr cfRef)
		{
			if (cfRef != IntPtr.Zero)
				CFReleaseInternal (cfRef);
		}
		
		[DllImport (SecurityLib)]
		static extern OSStatus SecKeychainItemFreeContent (IntPtr attrList, IntPtr data);

		[DllImport (SecurityLib)]
		static extern OSStatus SecKeychainAddGenericPassword (IntPtr keychain, uint serviceNameLength, byte[] serviceName,
		                                                      uint accountNameLength, byte[] accountName, uint passwordLength,
		                                                      byte[] passwordData, ref IntPtr itemRef);
		[DllImport (SecurityLib)]
		static extern OSStatus SecKeychainFindGenericPassword (IntPtr keychain, uint serviceNameLength, byte[] serviceName,
		                                                       uint accountNameLength, byte[] accountName, out uint passwordLength,
		                                                       out IntPtr passwordData, ref IntPtr itemRef);

		[DllImport (SecurityLib)]
		static extern OSStatus SecKeychainAddInternetPassword (IntPtr keychain, uint serverNameLength, byte[] serverName, uint securityDomainLength,
		                                                       byte[] securityDomain, uint accountNameLength, byte[] accountName, uint pathLength,
		                                                       byte[] path, ushort port, SecProtocolType protocol, SecAuthenticationType authType,
		                                                       uint passwordLength, byte[] passwordData, ref IntPtr itemRef);
		[DllImport (SecurityLib)]
		static extern OSStatus SecKeychainFindInternetPassword (IntPtr keychain, uint serverNameLength, byte[] serverName, uint securityDomainLength,
		                                                        byte[] securityDomain, uint accountNameLength, byte[] accountName, uint pathLength,
		                                                        byte[] path, ushort port, SecProtocolType protocol, SecAuthenticationType authType,
		                                                        out uint passwordLength, out IntPtr passwordData, ref IntPtr itemRef);

		[DllImport (SecurityLib)]
		static extern unsafe OSStatus SecKeychainItemCreateFromContent (SecItemClass itemClass, SecKeychainAttributeList *attrList,
		                                                                uint passwordLength, byte[] password, IntPtr keychain,
		                                                                IntPtr initialAccess, ref IntPtr itemRef);

		[DllImport (SecurityLib)]
		static extern OSStatus SecKeychainItemModifyAttributesAndData (IntPtr itemRef, IntPtr attrList, uint length, byte [] data);
		
		[DllImport (SecurityLib)]
		static extern OSStatus SecKeychainSearchCreateFromAttributes (IntPtr keychainOrArray, SecItemClass itemClass, IntPtr attrList, out IntPtr searchRef);
		
		[DllImport (SecurityLib)]
		static extern OSStatus SecKeychainSearchCopyNext (IntPtr searchRef, out IntPtr itemRef);

		[DllImport (SecurityLib)]
		static extern OSStatus SecCertificateCopyCommonName (IntPtr certificate, out IntPtr commonName);
		
		[DllImport (SecurityLib)]
		static extern OSStatus SecIdentitySearchCreate (IntPtr keychainOrArray, CssmKeyUse keyUsage, out IntPtr searchRef);
		
		[DllImport (SecurityLib)]
		static extern OSStatus SecIdentitySearchCopyNext (IntPtr searchRef, out IntPtr identity);
		
		[DllImport (SecurityLib)]
		static extern OSStatus SecIdentityCopyCertificate (IntPtr identityRef, out IntPtr certificateRef);
		
		[DllImport (SecurityLib)]
		static extern IntPtr SecCopyErrorMessageString (OSStatus status, IntPtr reserved);
		
		/* argh, OS 10.6 only
		[DllImport (SecurityLib)]
		static extern IntPtr SecCertificateCopyData (IntPtr certificate);
		
		[DllImport (CFLib)]
		static extern long CFDataGetLength (IntPtr theData);
		
		[DllImport (CFLib)]
		static extern void CFDataGetBytes (IntPtr theData, CFRange range, IntPtr buffer);
		*/
		
		[DllImport (SecurityLib)]
		static extern OSStatus SecCertificateGetData (IntPtr certificate, out CssmData data);


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
		
		struct CssmData
		{
			/// <summary>Length in bytes</summary>
			public UInt32 Length;
			/// <summary>Pointer to the byte array</summary>
			public IntPtr Data;
			
			public byte[] GetCopy ()
			{
				byte[] buffer = new byte[(int)Length];
				Marshal.Copy (Data, buffer, 0, buffer.Length);
				return buffer;
			}
		}
		
		#endregion
		
		#region CFString handling
		
		struct CFRange {
			public int Location, Length;
			public CFRange (int l, int len)
			{
				Location = l;
				Length = len;
			}
		}
		
		[DllImport (CFLib, CharSet=CharSet.Unicode)]
		extern static int CFStringGetLength (IntPtr handle);

		[DllImport (CFLib, CharSet=CharSet.Unicode)]
		extern static IntPtr CFStringGetCharactersPtr (IntPtr handle);
		
		[DllImport (CFLib, CharSet=CharSet.Unicode)]
		extern static IntPtr CFStringGetCharacters (IntPtr handle, CFRange range, IntPtr buffer);
		
		static string FetchString (IntPtr handle)
		{
			if (handle == IntPtr.Zero)
				return null;
			
			string str;
			
			int l = CFStringGetLength (handle);
			IntPtr u = CFStringGetCharactersPtr (handle);
			IntPtr buffer = IntPtr.Zero;
			if (u == IntPtr.Zero){
				CFRange r = new CFRange (0, l);
				buffer = Marshal.AllocCoTaskMem (l * 2);
				CFStringGetCharacters (handle, r, buffer);
				u = buffer;
			}
			unsafe {
				str = new string ((char *) u, 0, l);
			}
			
			if (buffer != IntPtr.Zero)
				Marshal.FreeCoTaskMem (buffer);
			
			return str;
		}
		
		#endregion
		
		static string GetError (OSStatus status)
		{
			IntPtr str = IntPtr.Zero;
			try {
				str = SecCopyErrorMessageString (status, IntPtr.Zero);
				return FetchString (str);
			} catch {
				return status.ToString ();
			} finally {
				if (str != IntPtr.Zero)
					CFRelease (str);
			}
		}
		
		public static IList<string> GetAllCertificateNames ()
		{
			IntPtr attrList = IntPtr.Zero; //match any attributes
			IntPtr searchRef, itemRef;
			
			//null keychain means use default
			var res = SecKeychainSearchCreateFromAttributes (IntPtr.Zero, SecItemClass.Certificate, attrList, out searchRef);
			if (res != OSStatus.Ok)
				throw new Exception ("Could not enumerate certificates from the keychain. Error:\n" + GetError (res));
			
			var names = new HashSet<string> ();
			
			OSStatus searchStatus;
			while ((searchStatus = SecKeychainSearchCopyNext (searchRef, out itemRef)) == OSStatus.Ok) {
				IntPtr commonName;
				if (SecCertificateCopyCommonName (itemRef, out commonName) == OSStatus.Ok) {
					names.Add (FetchString (commonName));
					CFRelease (commonName);
				}
				CFRelease (itemRef);
			}
			if (searchStatus != OSStatus.ItemNotFound)
				LoggingService.LogWarning ("Unexpected error retrieving certificates from keychain:\n" + GetError (searchStatus));
			
			CFRelease (searchRef);
			return names.ToList ();
		}
		
		public static IList<string> GetAllSigningIdentities ()
		{
			IntPtr searchRef, itemRef, certRef, commonName;
			
			//null keychain means use default
			var res = SecIdentitySearchCreate (IntPtr.Zero, CssmKeyUse.Sign, out searchRef);
			if (res != OSStatus.Ok)
				throw new Exception ("Could not enumerate certificates from the keychain. Error:\n" + GetError (res));
			
			var identities = new HashSet<string> ();
			
			OSStatus searchStatus;
			while ((searchStatus = SecIdentitySearchCopyNext (searchRef, out itemRef)) == OSStatus.Ok) {
				if (SecIdentityCopyCertificate (itemRef, out certRef) == OSStatus.Ok) {
					if (SecCertificateCopyCommonName (certRef, out commonName) == OSStatus.Ok) {
						string name = FetchString (commonName);
						if (name != null)
							identities.Add (name);
						CFRelease (commonName);
					}
					CFRelease (certRef);
				}
				CFRelease (itemRef);
			}
			if (searchStatus != OSStatus.ItemNotFound)
				LoggingService.LogWarning ("Unexpected error retrieving identities from keychain:\n" + GetError (searchStatus));
			
			CFRelease (searchRef);
			return identities.ToList ();
		}
		
		public static IEnumerable<X509Certificate2> FindNamedSigningCertificates (Func<string,bool> nameCheck)
		{
			return GetAllSigningCertificates ().Where (x => {
				var y = GetCertificateCommonName (x);
				return !string.IsNullOrEmpty (y) && nameCheck (y);
			});
		}
		
		public static IList<X509Certificate2> GetAllSigningCertificates ()
		{
			IntPtr searchRef, itemRef, certRef;
			
			//null keychain means use default
			var res = SecIdentitySearchCreate (IntPtr.Zero, CssmKeyUse.Sign, out searchRef);
			if (res != OSStatus.Ok)
				throw new Exception ("Could not enumerate certificates from the keychain. Error:\n" + GetError (res));
			
			var certs = new HashSet<X509Certificate2> ();
			
			OSStatus searchStatus;
			while ((searchStatus = SecIdentitySearchCopyNext (searchRef, out itemRef)) == OSStatus.Ok) {
				if (SecIdentityCopyCertificate (itemRef, out certRef) == OSStatus.Ok) {
					CssmData data;
					if (SecCertificateGetData (certRef, out data) == OSStatus.Ok) {
						try {
							certs.Add (new X509Certificate2 (data.GetCopy ()));
						} catch (Exception ex) {
							LoggingService.LogWarning ("Error loading signing certificate from keychain", ex);
						}
					}
				}
				CFRelease (itemRef);
			}
			if (searchStatus != OSStatus.ItemNotFound)
				LoggingService.LogWarning ("Unexpected error code retrieving signing certificates from keychain:\n" + GetError (searchStatus));
			
			CFRelease (searchRef);
			return certs.ToList ();
		}
		
		/* 10.6 only
		
		public static IList<X509Certificate2> GetAllSigningCertificates ()
		{
			IntPtr searchRef, itemRef, certRef;
			
			//null keychain means use default
			var res = SecIdentitySearchCreate (IntPtr.Zero, CssmKeyUse.Sign, out searchRef);
			if (res != OSStatus.Ok)
				throw new Exception ("Could not enumerate certificates from the keychain. Error:\n" + GetError (res));
			
			var list = new List<X509Certificate2> ();
			
			while (SecIdentitySearchCopyNext (searchRef, out itemRef) == OSStatus.Ok) {
				if (SecIdentityCopyCertificate (itemRef, out certRef) == OSStatus.Ok) {
					IntPtr cfData = SecCertificateCopyData (certRef);
					byte[] data = GetData (cfData);
					if (data == null)
						continue;
				
					CFRelease (cfData);
					CFRelease (certRef);
					list.Add (new X509Certificate2 (buffer));
				}
				CFRelease (itemRef);
			}
			CFRelease (searchRef);
			return list;
		}
		
		static byte[] GetData (IntPtr cfData)
		{
			if (cfData == IntPtr.Zero)
				return null;
			
			long len = CFDataGetLength (cfData);
			if (len < 1 || len > int.MaxValue)
				return null;
				
			byte[] buffer = new byte [(int)len];
			unsafe {
				fixed (byte *bufPtr = buffer) {
					CFDataGetBytes (cfData, new CFRange (0, (int)len), (IntPtr)bufPtr);
				}
			}
			return buffer;
		} */
		
		public static string GetCertificateCommonName (X509Certificate2 cert)
		{
			return cert.GetNameInfo (X509NameType.SimpleName, false);
		}

		static SecAuthenticationType GetSecAuthenticationType (string query)
		{
			if (string.IsNullOrEmpty (query))
				return SecAuthenticationType.Any;

			string auth = "default";
			foreach (var pair in query.Substring (1).Split (new char[] { '&' })) {
				var kvp = pair.ToLowerInvariant ().Split (new char[] { '=' });
				if (kvp[0] == "auth" && kvp.Length == 2) {
					auth = kvp[1];
					break;
				}
			}

			switch (auth.ToLowerInvariant ()) {
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

		public static unsafe void AddInternetPassword (Uri uri, string password)
		{
			byte[] path = Encoding.UTF8.GetBytes (string.Join (string.Empty, uri.Segments).Substring (1)); // don't include the leading '/'
			byte[] user = Encoding.UTF8.GetBytes (Uri.UnescapeDataString (uri.UserInfo));
			byte[] passwd = Encoding.UTF8.GetBytes (password);
			byte[] host = Encoding.UTF8.GetBytes (uri.Host);
			var auth = GetSecAuthenticationType (uri.Query);
			var protocol = GetSecProtocolType (uri.Scheme);
			IntPtr passwordPtr = IntPtr.Zero;
			IntPtr itemRef = IntPtr.Zero;
			uint passwordLength = 0;
			int port = uri.Port;
			
			// See if there is already a password there for this uri
			var result = SecKeychainFindInternetPassword (IntPtr.Zero, (uint) host.Length, host, 0, null,
			                                              (uint) user.Length, user, (uint) path.Length, path, (ushort) port,
			                                              protocol, auth, out passwordLength, out passwordPtr, ref itemRef);

			if (result == OSStatus.Ok) {
				// If there is, replace it with the new one
				result = SecKeychainItemModifyAttributesAndData (itemRef, IntPtr.Zero, (uint) passwd.Length, passwd);
				CFRelease (itemRef);
			} else {
				// Otherwise add a new entry with the password
//				result = SecKeychainAddInternetPassword (IntPtr.Zero, (uint) uri.Host.Length, uri.Host, 0, null,
//			                                             (uint) user.Length, user, (uint) path.Length, path, (ushort) uri.Port,
//				                                         (int) protocol, (int) auth, (uint) passwordBytes.Length, passwordBytes, ref itemRef);

				var label = Encoding.UTF8.GetBytes (string.Format ("{0} ({1})", uri.Host, Uri.UnescapeDataString (uri.UserInfo)));
				fixed (byte* labelPtr = label, userPtr = user, hostPtr = host, pathPtr = path) {
					SecKeychainAttribute* attrs = stackalloc SecKeychainAttribute [7];
					int* protoPtr = (int*) &protocol;
					int* authPtr = (int*) &auth;
					int* portPtr = &port;

					attrs[0] = new SecKeychainAttribute (SecItemAttr.Label,    (uint) label.Length, (IntPtr) labelPtr);
					attrs[1] = new SecKeychainAttribute (SecItemAttr.Account,  (uint) user.Length,  (IntPtr) userPtr);
					attrs[2] = new SecKeychainAttribute (SecItemAttr.Protocol, (uint) 4,            (IntPtr) protoPtr);
					attrs[3] = new SecKeychainAttribute (SecItemAttr.AuthType, (uint) 4,            (IntPtr) authPtr);
					attrs[4] = new SecKeychainAttribute (SecItemAttr.Server,   (uint) host.Length,  (IntPtr) hostPtr);
					attrs[5] = new SecKeychainAttribute (SecItemAttr.Port,     (uint) 4,            (IntPtr) portPtr);
					attrs[6] = new SecKeychainAttribute (SecItemAttr.Path,     (uint) path.Length,  (IntPtr) pathPtr);

					SecKeychainAttributeList attrList = new SecKeychainAttributeList (6, (IntPtr) attrs);

					itemRef = IntPtr.Zero;
					result = SecKeychainItemCreateFromContent (SecItemClass.InternetPassword, &attrList, (uint) passwd.Length, passwd, IntPtr.Zero, IntPtr.Zero, ref itemRef);
					CFRelease (itemRef);
				}
			}
			
			if (result != OSStatus.Ok)
				throw new Exception ("Could not add internet password to keychain: " + GetError (result));
		}

		public static string FindInternetPassword (Uri uri)
		{
			byte[] path = Encoding.UTF8.GetBytes (string.Join (string.Empty, uri.Segments).Substring (1)); // don't include the leading '/'
			byte[] user = Encoding.UTF8.GetBytes (Uri.UnescapeDataString (uri.UserInfo));
			byte[] host = Encoding.UTF8.GetBytes (uri.Host);
			var auth = GetSecAuthenticationType (uri.Query);
			var protocol = GetSecProtocolType (uri.Scheme);
			IntPtr passwordPtr = IntPtr.Zero;
			IntPtr itemRef = IntPtr.Zero;
			uint passwordLength = 0;

			// Look for an internet password for the given protocol and auth mechanism
			var result = SecKeychainFindInternetPassword (IntPtr.Zero, (uint) host.Length, host, 0, null,
			                                              (uint) user.Length, user, (uint) path.Length, path, (ushort) uri.Port,
			                                              protocol, auth, out passwordLength, out passwordPtr, ref itemRef);

			// Fall back to looking for a password for SecProtocolType.Any && SecAuthenticationType.Any
			if (result == OSStatus.ItemNotFound && protocol != SecProtocolType.Any)
				result = SecKeychainFindInternetPassword (IntPtr.Zero, (uint) host.Length, host, 0, null,
				                                          (uint) user.Length, user, (uint) path.Length, path, (ushort) uri.Port,
				                                          0, auth, out passwordLength, out passwordPtr, ref itemRef);

			CFRelease (itemRef);

			if (result == OSStatus.ItemNotFound)
				return null;

			if (result != OSStatus.Ok)
				throw new Exception ("Could not find internet password: " + GetError (result));

			return Marshal.PtrToStringAuto (passwordPtr, (int) passwordLength);
		}
		
		enum SecItemClass : uint
		{
			InternetPassword = 1768842612, // 'inet'
			GenericPassword = 1734700656,  // 'genp'
			AppleSharePassword = 1634953328, // 'ashp'
			Certificate =  0x80000000 + 0x1000,
			PublicKey = 0x0000000A + 5,
			PrivateKey = 0x0000000A + 6,
			SymmetricKey = 0x0000000A + 7
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
		
		enum OSStatus
		{
			Ok = 0,
			ItemNotFound = -25300,
		}
		
		enum SecKeyAttribute
		{
			KeyClass =          0,
			PrintName =         1,
			Alias =             2,
			Permanent =         3,
			Private =           4,
			Modifiable =        5,
			Label =             6,
			ApplicationTag =    7,
			KeyCreator =        8,
			KeyType =           9,
			KeySizeInBits =    10,
			EffectiveKeySize = 11,
			StartDate =        12,
			EndDate =          13,
			Sensitive =        14,
			AlwaysSensitive =  15,
			Extractable =      16,
			NeverExtractable = 17,
			Encrypt =          18,
			Decrypt =          19,
			Derive =           20,
			Sign =             21,
			Verify =           22,
			SignRecover =      23,
			VerifyRecover =    24,
			Wrap =             25,
			Unwrap =           26,
		}

		enum SecAuthenticationType : int
		{
			NTLM        = 1835824238,
			MSN         = 1634628461,
			DPA         = 1633775716,
			RPA         = 1633775730,
			HTTPBasic   = 1886680168,
			HTTPDigest  = 1685353576,
			HTMLForm    = 1836216166,
			Default     = 1953261156,
			Any         = 0
		}

		enum SecProtocolType : int
		{
			FTP         = 1718906912,
			FTPAccount  = 1718906977,
			HTTP        = 1752462448,
			IRC         = 1769104160,
			NNTP        = 1852732528,
			POP3        = 1886351411,
			SMTP        = 1936553072,
			SOCKS       = 1936685088,
			IMAP        = 1768776048,
			LDAP        = 1818517872,
			AppleTalk   = 1635019883,
			AFP         = 1634103328,
			Telnet      = 1952803950,
			SSH         = 1936943136,
			FTPS        = 1718906995,
			HTTPProxy   = 1752461432,
			HTTPSProxy  = 1752462200,
			FTPProxy    = 1718907000,
			CIFS        = 1667851891,
			SMB         = 1936548384,
			RTSP        = 1920234352,
			RTSPProxy   = 1920234360,
			DAAP        = 1684103536,
			EPPC        = 1701867619,
			IPP         = 1768976416,
			NNTPS       = 1853124723,
			LDAPS       = 1818521715,
			TelnetS     = 1952803955,
			IMAPS       = 1768779891,
			IRCS        = 1769104243,
			POP3S       = 1886351475,
			CVSpserver  = 1668707184,
			SVN         = 1937141280,
			Any         = 0
		}
		
		[Flags]
		enum CssmKeyUse : uint
		{
			Any =				0x80000000,
			Encrypt =			0x00000001,
			Decrypt =			0x00000002,
			Sign =				0x00000004,
			Verify =			0x00000008,
			SignRecover =		0x00000010,
			VerifyRecover =		0x00000020,
			Wrap =				0x00000040,
			Unwrap =			0x00000080,
			Derive =			0x00000100
		}
		
		[Flags]
		enum CssmTPAppleCertStatus : uint
		{
			Expired         = 0x00000001,
			NotValidYet     = 0x00000002,
			IsInInputCerts  = 0x00000004,
			IsInAnchors     = 0x00000008,
			IsRoot          = 0x00000010,
			IsFromNet       = 0x00000020
		}
	}
}
