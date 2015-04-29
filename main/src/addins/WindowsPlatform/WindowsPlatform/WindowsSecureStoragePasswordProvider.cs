using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;
using MonoDevelop.Core;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

namespace MonoDevelop.Platform.Windows
{
	public class WindowsSecureStoragePasswordProvider : IPasswordProvider
	{
		public void AddWebPassword (Uri uri, string password)
		{
			AddWebUserNameAndPassword (uri, uri.UserInfo, password);
		}

		public string GetWebPassword (Uri uri)
		{
			var pair = GetWebUserNameAndPassword (uri);
			return pair == null ? null : pair.Item2;
		}

		public void AddWebUserNameAndPassword (Uri url, string username, string password)
		{
			var didWrite = WriteCredential (url.Host, username, password);
			if (didWrite) return;

			var lastError = (ErrorCode)Marshal.GetLastWin32Error ();
			switch (lastError) {
				case ErrorCode.NoSuchLogonSession:
					LoggingService.LogError ("Tried saving credentials, but the logon session does not exist.");
					break;
				case ErrorCode.InvalidFlags:
					LoggingService.LogError ("Tried saving credentials, but got invalid flags set on credential.");
					break;
				default:
					LoggingService.LogError ("Tried saving credentials, but got unknown error code 0x{0:X}.", lastError);
					break;
			}
		}

		public Tuple<string, string> GetWebUserNameAndPassword (Uri url)
		{
			var read = ReadCredential (url.Host);

			if (read != null)
				return read;

			var lastError = (ErrorCode)Marshal.GetLastWin32Error ();
			switch (lastError) {
				case ErrorCode.NotFound:
					return null;
				case ErrorCode.NoSuchLogonSession:
					LoggingService.LogWarning ("Tried to retrieve credential, but got no logon session.");
					return null;
				case ErrorCode.InvalidFlags:
					LoggingService.LogWarning ("Tried to retrieve credential, but got invalid flags.");
					return null;
				default:
					LoggingService.LogWarning ("Tried to retrieve credentials, but got unknown error code 0x{0:X}.", lastError);
					return null;
			}
		}

		static bool WriteCredential (string targetName, string userName, string password)
		{
			if (targetName == null)
				throw new ArgumentNullException ("targetName");

			if (userName == null)
				throw new ArgumentNullException ("userName");

			if (password == null)
				throw new ArgumentNullException ("password");

			var passwordBytes = Encoding.Unicode.GetBytes (password);
			if (passwordBytes.Length > 512)
				throw new ArgumentException ("The secret message has exceeded 512 bytes.");

			// Go ahead with what we have are stuff it into the CredMan structures.
			var cred = new Credential {
				TargetName = targetName,
				CredentialBlob = password,
				CredentialBlobSize = (UInt32) passwordBytes.Length,
				AttributeCount = 0,
				Attributes = IntPtr.Zero,
				Comment = null,
				TargetAlias = null,
				Type = NativeCredentialType.Generic,
				Persist = PersistFlags.LocalMachine,
				UserName = userName,
			};

			var ncred = NativeCredential.GetNativeCredential (cred);
			// Write the info into the CredMan storage.
			var didWrite = NativeMethods.CredWrite (ref ncred, 0);

			Marshal.FreeCoTaskMem (ncred.TargetName);
			Marshal.FreeCoTaskMem (ncred.CredentialBlob);
			Marshal.FreeCoTaskMem (ncred.UserName);

			return didWrite;
		}

		static Tuple<string, string> ReadCredential (string targetName)
		{
			IntPtr nCredPtr;
			bool read = NativeMethods.CredRead (targetName, NativeCredentialType.Generic, 0, out nCredPtr);
			
			if (!read) return null;

			using (var critCred = new CriticalCredentialHandle (nCredPtr)) {
				var cred = critCred.GetCredential ();

				if (cred.HasValue)
					return Tuple.Create (cred.Value.UserName, cred.Value.CredentialBlob);
				return Tuple.Create (string.Empty, string.Empty);
			}
		}

		static bool RemoveCredential (string targetName)
		{
			if (targetName == null)
				throw new ArgumentNullException ("targetName");

			return NativeMethods.CredDelete (targetName, NativeCredentialType.Generic, 0);
		}

		public void RemoveWebPassword (Uri uri)
		{
			RemoveWebUserNameAndPassword (uri);
		}

		public void RemoveWebUserNameAndPassword (Uri uri)
		{
			var didDelete = RemoveCredential (uri.Host);
			if (didDelete) return;

			var lastError = (ErrorCode)Marshal.GetLastWin32Error ();
			switch (lastError) {
			case ErrorCode.NoSuchLogonSession:
				LoggingService.LogError ("Tried saving credentials, but the logon session does not exist.");
				break;
			case ErrorCode.InvalidFlags:
				LoggingService.LogError ("Tried saving credentials, but got invalid flags set on credential.");
				break;
			default:
				LoggingService.LogError ("Tried saving credentials, but got unknown error code 0x{0:X}.", lastError);
				break;
			}
		}
	}

	enum ErrorCode
	{
		NoSuchLogonSession = 1312,
		InvalidFlags = 1004,
		NotFound = 1168
	}

	[Flags]
	enum CredentialFlags
	{
		PromptNow = 0x2,
		UsernameTarget = 0x4,
	}

	enum NativeCredentialType
	{
		Generic = 0x1,
		DomainPassword = 0x2,
		DomainCertificate = 0x3,
		DomainVisiblePassword = 0x4,
		GenericCertificate = 0x5,
		DomainExtended = 0x6,
		Maximum = 0x7,
		MaximumExtended = Maximum + 1000,
	}

	enum PersistFlags
	{
		Session = 0x1,
		LocalMachine = 0x2,
		Enterprise = 0x3,
	}

	struct NativeCredential
	{
		public UInt32 Flags;
		public NativeCredentialType Type;
		public IntPtr TargetName;
		public IntPtr Comment;
		public FILETIME LastWritten;
		public UInt32 CredentialBlobSize;
		public IntPtr CredentialBlob;
		public UInt32 Persist;
		public UInt32 AttributeCount;
		public IntPtr Attributes;
		public IntPtr TargetAlias;
		public IntPtr UserName;

		/// <summary>
		/// This method derives a NativeCredential instance from a given Credential instance.
		/// </summary>
		/// <param name="cred">The managed Credential counterpart containing data to be stored.</param>
		/// <returns>A NativeCredential instance that is derived from the given Credential
		/// instance.</returns>
		internal static NativeCredential GetNativeCredential (Credential cred)
		{
			return new NativeCredential {
				AttributeCount = 0,
				Attributes = IntPtr.Zero,
				Comment = IntPtr.Zero,
				TargetAlias = IntPtr.Zero,
				Type = NativeCredentialType.Generic,
				Persist = (UInt32)PersistFlags.LocalMachine,
				CredentialBlobSize = cred.CredentialBlobSize,
				TargetName = Marshal.StringToCoTaskMemUni (cred.TargetName),
				CredentialBlob = Marshal.StringToCoTaskMemUni (cred.CredentialBlob),
				UserName = Marshal.StringToCoTaskMemUni (cred.UserName)
			};
		}
	}

	[StructLayout (LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	struct Credential
	{
		public UInt32 Flags;
		public NativeCredentialType Type;
		public string TargetName;
		public string Comment;
		public FILETIME LastWritten;
		public UInt32 CredentialBlobSize;
		public string CredentialBlob;
		public PersistFlags Persist;
		public UInt32 AttributeCount;
		public IntPtr Attributes;
		public string TargetAlias;
		public string UserName;
	}

	sealed class CriticalCredentialHandle : CriticalHandleZeroOrMinusOneIsInvalid
	{
		// Set the handle.
		internal CriticalCredentialHandle (IntPtr preexistingHandle)
		{
			SetHandle (preexistingHandle);
		}

		internal Credential? GetCredential ()
		{
			if (IsInvalid) 
				throw new InvalidOperationException ("Invalid CriticalHandle!");

			var ncred = (NativeCredential)Marshal.PtrToStructure (handle, typeof (NativeCredential));

			if (ncred.CredentialBlob == IntPtr.Zero || ncred.TargetName == IntPtr.Zero)
				return null;

			return new Credential {
				CredentialBlobSize = ncred.CredentialBlobSize,
				CredentialBlob = Marshal.PtrToStringUni (ncred.CredentialBlob, (int)ncred.CredentialBlobSize / 2),
				UserName = ncred.UserName == IntPtr.Zero ? string.Empty : Marshal.PtrToStringUni (ncred.UserName),
				TargetName = Marshal.PtrToStringUni (ncred.TargetName),
				TargetAlias = ncred.TargetAlias == IntPtr.Zero ? string.Empty : Marshal.PtrToStringUni (ncred.TargetAlias),
				Type = ncred.Type,
				Flags = ncred.Flags,
				Persist = (PersistFlags)ncred.Persist
			};
		}

		override protected bool ReleaseHandle ()
		{
			if (IsInvalid) 
				return false;

			NativeMethods.CredFree (handle);
			SetHandleAsInvalid ();

			return true;
		}
	}

	static class NativeMethods
	{
		const string ADVAPI32 = "advapi32.dll";

		[DllImport (ADVAPI32, CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "CredWriteW")]
		internal static extern bool CredWrite ([In] ref NativeCredential credential, [In] uint flags);

		[DllImport (ADVAPI32, CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "CredReadW")]
		internal static extern bool CredRead (string targetName, NativeCredentialType type, CredentialFlags flags,
			out IntPtr credential);

		[DllImport (ADVAPI32, CharSet = CharSet.Unicode, EntryPoint = "CredFree")]
		internal static extern bool CredFree ([In] IntPtr cred);

		[DllImport (ADVAPI32, CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "CredDeleteW")]
		internal static extern bool CredDelete (string targetName, NativeCredentialType type, CredentialFlags flags);
	}
}
