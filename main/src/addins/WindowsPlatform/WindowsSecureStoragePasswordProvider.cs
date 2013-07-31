using System;
using System.Runtime.InteropServices;
using System.Text;
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
			var passwordBytes = Encoding.Unicode.GetBytes (password);
			var cred = new Credential {
				CredentialBlob = passwordBytes,
				CredentialBlobSize = (uint)passwordBytes.Length,
				Flags = 0,
				Persist = PersistFlags.LocalMachine,
				TargetName = url.Host,
				Type = NativeCredentialType.Generic,
				UserName = username,
			};

			var didWrite = NativeMethods.CredWrite (ref cred);
			if (didWrite) return;

			var lastError = (ErrorCode)Marshal.GetLastWin32Error ();
			switch (lastError) {
				case ErrorCode.NoSuchLogonSession:
					LoggingService.LogError ("Tried saving credentials, but the logon session does not exist.");
					break;
				case ErrorCode.BadUsername:
					LoggingService.LogError ("Tried saving credentials, but got bad username format.");
					break;
				case ErrorCode.InvalidFlags:
					LoggingService.LogError ("Tried saving credentials, but got invalid flags set on credential.");
					break;
				case ErrorCode.InvalidParameter:
					LoggingService.LogError ("Tried saving credentials, but cannot change protected field in existing credential. ");
					break;
				default:
					LoggingService.LogError ("Tried saving credentials, but got unknown error code 0x{0:X}.", lastError);
					break;
			}
		}

		public Tuple<string, string> GetWebUserNameAndPassword (Uri url)
		{
			var cred = new Credential ();
			var didRead = NativeMethods.CredRead (url.Host, NativeCredentialType.Generic, 0, ref cred);

			if (didRead)
				return Tuple.Create (cred.UserName, Encoding.Unicode.GetString (cred.CredentialBlob));

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
	}

	enum ErrorCode
	{
		NoSuchLogonSession = 0x520,
		InvalidParameter = 0x57,
		InvalidFlags = 0x3ec,
		BadUsername = 0x490,
		NotFound = BadUsername, // Not sure WTF is going on here, but this is based on the values I could find
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

	struct Credential
	{
		public CredentialFlags Flags;
		public NativeCredentialType Type;
		public string TargetName;
		public string Comment;
		public FILETIME LastWritten;
		public uint CredentialBlobSize;
		public byte[] CredentialBlob;
		public PersistFlags Persist;
		public uint AttributeCount;
		public PersistFlags Attributes;
		public string TargetAlias;
		public string UserName;
	}

	static class NativeMethods
	{
		[DllImport ("advapi32.dll", CharSet = CharSet.Unicode)]
		internal static extern bool CredWrite (ref Credential credential, uint flags = 0);

		[DllImport ("advapi32.dll", CharSet = CharSet.Unicode)]
		internal static extern bool CredRead (string targetName, NativeCredentialType type, CredentialFlags flags,
		                                      ref Credential credential);
	}
}
