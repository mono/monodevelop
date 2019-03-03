// 
// Keychain.cs
//
// Authors: Michael Hutchinson <mhutchinson@novell.com>
//          Jeffrey Stedfast <jeff@xamarin.com>
//			Javier Suárez <jsuarez@microsoft.com>
//
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
// Copyright (c) 2013 Xamarin Inc. (http://www.xamarin.com)
// Copyright (c) 2019 Microsoft Corporation (http://www.microsoft.com)
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
using Security;
using Foundation;

namespace MonoDevelop.MacInterop
{
	public static class Keychain
	{
		public static void AddInternetPassword (Uri uri, string password)
		{
			// See if there is already a password there for this uri
			var record = SecKeychainFindInternetPassword (uri, out SecRecord searchRecord);

			if (record == null) {
				record = uri.ToSecRecord (password: password);

				SecStatusCode result = SecKeyChain.Add (record);

				if (result != SecStatusCode.Success && result != SecStatusCode.DuplicateItem)
					throw new Exception ("Could not add internet password to keychain: " + result.GetStatusDescription ());

				return;
			}

			record.ValueData = NSData.FromString (password);

			// If there is, replace it with the new one
			SecKeyChain.Update (searchRecord, record);
		}

		public static void AddInternetPassword (Uri uri, string username, string password)
		{
			// See if there is already a password there for this uri
			var record = SecKeychainFindInternetPassword (uri, out SecRecord searchRecord);

			if (record == null) {
				record = uri.ToSecRecord (username, password);

				SecStatusCode result = SecKeyChain.Add (record);

				if (result != SecStatusCode.Success && result != SecStatusCode.DuplicateItem)
					throw new Exception ("Could not add internet password to keychain: " + result.GetStatusDescription ());

				return;
			}

			// If there is, replace it with the new one
			var update = uri.ToSecRecord (username, password);
			SecKeyChain.Update (record, update);
		}

		public static string FindInternetPassword (Uri url)
		{
			// See if there is already a password there for this uri
			var record = SecKeychainFindInternetPassword (url, out _);

			if (record != null)
				return NSString.FromData (record.ValueData, NSStringEncoding.UTF8);

			return null;
		}

		public static unsafe Tuple<string, string> FindInternetUserNameAndPassword (Uri uri)
		{
			return FindInternetUserNameAndPassword (uri, GetSecProtocolType (uri.Scheme));
		}

		public static Tuple<string, string> FindInternetUserNameAndPassword (Uri url, SecProtocol protocol)
		{
			var record = SecKeychainFindInternetPassword (url, protocol, out _);

			if (record != null) {

				string username = record.Account != null ? NSString.FromData (record.Account, NSStringEncoding.UTF8) : null;
				string password = record.ValueData != null ? NSString.FromData (record.ValueData, NSStringEncoding.UTF8) : null;

				return Tuple.Create (username, password);
			}
			return null;
		}

		public static void RemoveInternetPassword (Uri url)
		{
			var record = SecKeychainFindInternetPassword (url, out _);

			if (record != null) {
				var result = SecKeyChain.Remove (record);

				if (result != SecStatusCode.Success)
					throw new Exception ("Could not delete internet password from keychain: " + result.GetStatusDescription ());
			}
		}

		public static void RemoveInternetUserNameAndPassword (Uri url)
		{
			var record = SecKeychainFindInternetPassword (url, out _);

			if (record != null) {
				SecStatusCode result = SecKeyChain.Remove (record);

				if (result != SecStatusCode.Success)
					throw new Exception ("Could not delete internet password from keychain: " + result.GetStatusDescription ());
			}
		}

		static SecRecord SecKeychainFindInternetPassword (Uri uri, out SecRecord searchRecord)
		{
			return SecKeychainFindInternetPassword (uri, GetSecProtocolType (uri.Scheme), out searchRecord);
		}

		static SecRecord SecKeychainFindInternetPassword (Uri uri, SecProtocol protocol, out SecRecord searchRecord)
		{
			// Look for an internet password for the given protocol and auth mechanism
			searchRecord = uri.ToSecRecord ();
			if (protocol != SecProtocol.Invalid)
				searchRecord.Protocol = protocol;

			var data = SecKeyChain.QueryAsRecord (searchRecord, out SecStatusCode code);

			if (code == SecStatusCode.ItemNotFound) {
				// Fall back to looking for a password without use SecProtocol && SecAuthenticationType
				searchRecord.Protocol = SecProtocol.Http; // Http is the default used by SecKeyChain internally
				searchRecord.AuthenticationType = SecAuthenticationType.Default;

				data = SecKeyChain.QueryAsRecord (searchRecord, out code);
			}

			if (code != SecStatusCode.Success)
				return null;

			return data;
		}

		static readonly string WebFormPassword = "Web form password";

		static SecRecord ToSecRecord (this Uri uri, string username = null, string password = null)
		{
			var record = new SecRecord (SecKind.InternetPassword) {
				Server = uri.Host,
				Path = string.Join (string.Empty, uri.Segments),
				Port = uri.Port,
			};
			var protocol = GetSecProtocolType (uri.Scheme);
			if (protocol != SecProtocol.Invalid)
				record.Protocol = protocol;
			var authType = GetSecAuthenticationType (uri.Query);
			if (authType != SecAuthenticationType.Default)
				record.AuthenticationType = authType;

			if (record.AuthenticationType == SecAuthenticationType.HtmlForm)
				record.Description = WebFormPassword;

			var account = Uri.UnescapeDataString (uri.UserInfo);
			if (string.IsNullOrEmpty (account)) // account from Uri has always priority
				account = username;

			if (!string.IsNullOrEmpty (account))
				record.Account = account;

			if (password != null)
				record.ValueData = NSData.FromString (password);

			return record;
		}

		static SecProtocol GetSecProtocolType (string protocol)
		{
			switch (protocol.ToLowerInvariant ()) {
			case "ftp": return SecProtocol.Ftp;
			case "ftpaccount": return SecProtocol.FtpAccount;
			case "http": return SecProtocol.Http;
			case "https": return SecProtocol.Https;
			case "irc": return SecProtocol.Irc;
			case "nntp": return SecProtocol.Nntp;
			case "pop3": return SecProtocol.Pop3;
			case "pop3s": return SecProtocol.Pop3s;
			case "smtp": return SecProtocol.Smtp;
			case "socks": return SecProtocol.Socks;
			case "imap": return SecProtocol.Imap;
			case "imaps": return SecProtocol.Imaps;
			case "ldap": return SecProtocol.Ldap;
			case "ldaps": return SecProtocol.Ldaps;
			case "appletalk": return SecProtocol.AppleTalk;
			case "afp": return SecProtocol.Afp;
			case "telnet": return SecProtocol.Telnet;
			case "ssh": return SecProtocol.Ssh;
			case "ftps": return SecProtocol.Ftps;
			case "httpproxy": return SecProtocol.HttpProxy;
			case "httpsproxy": return SecProtocol.HttpProxy;
			case "ftpproxy": return SecProtocol.FtpProxy;
			case "smb": return SecProtocol.Smb;
			case "rtsp": return SecProtocol.Rtsp;
			case "rtspproxy": return SecProtocol.RtspProxy;
			case "daap": return SecProtocol.Daap;
			case "eppc": return SecProtocol.Eppc;
			case "ipp": return SecProtocol.Ipp;
			case "nntps": return SecProtocol.Nntps;
			case "telnets": return SecProtocol.Telnets;
			case "ircs": return SecProtocol.Ircs;
			default: return SecProtocol.Invalid;
			}
		}

		static SecAuthenticationType GetSecAuthenticationType (string query)
		{
			if (string.IsNullOrEmpty (query))
				return SecAuthenticationType.Default;

			string auth = "default";
			foreach (var pair in query.Substring (1).Split (new char [] { '&' })) {
				var kvp = pair.Split (new char [] { '=' });
				if (string.Equals (kvp [0], "auth", StringComparison.InvariantCultureIgnoreCase) && kvp.Length == 2) {
					auth = kvp [1].ToLowerInvariant ();
					break;
				}
			}

			switch (auth) {
			case "ntlm": return SecAuthenticationType.Ntlm;
			case "msn": return SecAuthenticationType.Msn;
			case "dpa": return SecAuthenticationType.Dpa;
			case "rpa": return SecAuthenticationType.Rpa;
			case "httpbasic": case "basic": return SecAuthenticationType.HttpBasic;
			case "httpdigest": case "digest": return SecAuthenticationType.HttpDigest;
			case "htmlform": case "form": return SecAuthenticationType.HtmlForm;
			default: return SecAuthenticationType.Default;
			}
		}
	}
}