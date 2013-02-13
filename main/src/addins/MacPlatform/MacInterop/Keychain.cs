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
using System.Runtime.InteropServices;
using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.Core.Serialization;
using System.Security.Cryptography.X509Certificates;

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
		static extern OSStatus SecKeychainAddGenericPassword (IntPtr keychain, uint serviceNameLength, string serviceName,
		                                                      uint accountNameLength, string accountName, uint passwordLength,
		                                                      byte[] passwordData, ref IntPtr itemRef);
		[DllImport (SecurityLib)]
		static extern OSStatus SecKeychainFindGenericPassword (IntPtr keychain, uint serviceNameLength, string serviceName,
		                                                      uint accountNameLength, string accountName, out uint passwordLength,
		                                                      out IntPtr passwordData, ref IntPtr itemRef);

		[DllImport (SecurityLib)]
		static extern OSStatus SecKeychainAddInternetPassword (IntPtr keychain, uint serverNameLength, string serverName, uint securityDomainLength,
		                                                      string securityDomain, uint accountNameLength, string accountName, uint pathLength,
		                                                      string path, ushort port, int protocol, int authenticationType,
		                                                      uint passwordLength, byte[] passwordData, ref IntPtr itemRef);
		[DllImport (SecurityLib)]
		static extern OSStatus SecKeychainFindInternetPassword (IntPtr keychain, uint serverNameLength, string serverName, uint securityDomainLength,
		                                                      string securityDomain, uint accountNameLength, string accountName, uint pathLength,
		                                                      string path, ushort port, int protocol, int authenticationType,
		                                                      out uint passwordLength, out IntPtr passwordData, ref IntPtr itemRef);

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
				return y != null && y.Length > 0 && nameCheck (y);
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

		public static void AddInternetPassword (Uri uri, string password)
		{
			IntPtr itemRef = IntPtr.Zero;
			IntPtr passwordPtr = IntPtr.Zero;
			uint passwordLength = 0;
			var passwordBytes = System.Text.Encoding.UTF8.GetBytes (password);
			
			// See if there is already a password there for this uri
			var result = SecKeychainFindInternetPassword (IntPtr.Zero, (uint) uri.Host.Length, uri.Host, 0, null,
			                                              (uint) uri.UserInfo.Length, uri.UserInfo, (uint) uri.PathAndQuery.Length, uri.PathAndQuery,
			                                              (ushort) uri.Port, 0, 0, out passwordLength, out passwordPtr, ref itemRef);
			if (result == OSStatus.Ok) {
				// If there is, replace it with the new one
				result = SecKeychainItemModifyAttributesAndData (itemRef, IntPtr.Zero, (uint) passwordBytes.Length, passwordBytes);
			} else {
				// Otherwise add a new entry with the password
				result = SecKeychainAddInternetPassword (IntPtr.Zero, (uint) uri.Host.Length, uri.Host, 0, null,
			                                             (uint) uri.UserInfo.Length, uri.UserInfo, (uint) uri.PathAndQuery.Length, uri.PathAndQuery,
			                                             (ushort) uri.Port, 0, 0, (uint) passwordBytes.Length, passwordBytes, ref itemRef);
			}
			
			if (result != OSStatus.Ok)
				throw new Exception ("Could not add internet password to keychain: " + GetError (result));
		}

		public static string FindInternetPassword (Uri uri)
		{
			IntPtr itemRef = IntPtr.Zero;
			IntPtr password = IntPtr.Zero;
			uint passwordLength = 0;
			var result = SecKeychainFindInternetPassword (IntPtr.Zero, (uint) uri.Host.Length, uri.Host, 0, null,
			                                              (uint) uri.UserInfo.Length, uri.UserInfo, (uint) uri.PathAndQuery.Length, uri.PathAndQuery,
			                                              (ushort) uri.Port, 0, 0, out passwordLength, out password, ref itemRef);
			if (result == OSStatus.ItemNotFound)
				return null;

			if (result != OSStatus.Ok)
				throw new Exception ("Could not find internet password: " + GetError (result));

			return Marshal.PtrToStringAuto (password, (int) passwordLength);
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
