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

namespace MonoDevelop.IPhone
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
			
			var list = new List<string> ();
			
			while (SecKeychainSearchCopyNext (searchRef, out itemRef) == OSStatus.Ok) {
				IntPtr commonName;
				if (SecCertificateCopyCommonName (itemRef, out commonName) == OSStatus.Ok) {
					list.Add (FetchString (commonName));
					CFRelease (commonName);
				}
				CFRelease (itemRef);
			}
			CFRelease (searchRef);
			return list;
		}
		
		public static IList<string> GetAllSigningIdentities ()
		{
			IntPtr searchRef, itemRef, certRef, commonName;
			
			//null keychain means use default
			var res = SecIdentitySearchCreate (IntPtr.Zero, CssmKeyUse.Sign, out searchRef);
			if (res != OSStatus.Ok)
				throw new Exception ("Could not enumerate certificates from the keychain. Error:\n" + GetError (res));
			
			var list = new List<string> ();
			
			while (SecIdentitySearchCopyNext (searchRef, out itemRef) == OSStatus.Ok) {
				if (SecIdentityCopyCertificate (itemRef, out certRef) == OSStatus.Ok) {
					if (SecCertificateCopyCommonName (certRef, out commonName) == OSStatus.Ok) {
						string name = FetchString (commonName);
						if (name != null)
							list.Add (name);
						CFRelease (commonName);
					}
					CFRelease (certRef);
				}
				CFRelease (itemRef);
			}
			CFRelease (searchRef);
			return list;
		}
		
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
					CssmData data;
					if (SecCertificateGetData (certRef, out data) == OSStatus.Ok)
						list.Add (new X509Certificate2 (data.GetCopy ()));
				}
				CFRelease (itemRef);
			}
			CFRelease (searchRef);
			return list;
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
			int start = cert.Subject.IndexOf ("CN=");
			if (start < 0)
				return null;
			int end = cert.Subject.IndexOf (",", start);
			if (end < start)
				return null;
			return cert.Subject.Substring (start + 3, end - start - 3);
		}
		
		public const string DEV_CERT_PREFIX  = "iPhone Developer:";
		public const string DIST_CERT_PREFIX = "iPhone Distribution:";
		
		public static string GetStoredCertificateName (IPhoneProject project, bool distribution)
		{
			var keys = project.UserProperties.GetValue<SigningKeyInformation> ("IPhoneSigningKeys");
			if (keys != null) {
				string key = distribution? keys.Distribution : keys.Developer;
				if (!String.IsNullOrEmpty (key))
					return key;
			}
			return null;
		}
		
		/// <summary>
		/// Gets the name of a valid iPhone code signing certificate.
		/// </summary>
		/// <param name="distribution">Whether the key is for distribution</param>
		/// <param name="hint">An optional substring hint for the key name.</param>
		/// <returns>A valid certificate name, or null if no certificate is available.</returns>
		public static string GetInstalledCertificateName (bool distribution, string hint)
		{	
			string cmp = distribution? DIST_CERT_PREFIX : DEV_CERT_PREFIX;
			var certs = GetAllSigningIdentities ().Where (c => c.StartsWith (cmp));
			
			if (String.IsNullOrEmpty (hint))
				return certs.FirstOrDefault ();
			
			string best = null;
			foreach (string certName in certs) {
				if (certName.Contains (hint))
					return certName;
				else if (best == null)
					best = certName;
			}
			return best;
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
	
	[DataItem]
	sealed class SigningKeyInformation
	{
		
		public SigningKeyInformation (string developer, string distribution)
		{
			this.Distribution = NullIfEmpty (distribution);
			this.Developer = NullIfEmpty (developer);
		}
		
		public SigningKeyInformation ()
		{
		}
		
		[ItemProperty]
		public string Developer { get; private set; }
		
		[ItemProperty]
		public string Distribution { get; private set; }
		
		public static SigningKeyInformation Default {
			get {
				string dev = PropertyService.Get<string> ("IPhoneSigningKey.Developer", null);
				string dist = PropertyService.Get<string> ("IPhoneSigningKey.Distribution", null);
				return new SigningKeyInformation (dev, dist);
			}
		}
		
		string NullIfEmpty (string s)
		{
			return (s == null || s.Length == 0)? null : s;
		}
	}
}
