/*
This code is derived from jgit (http://eclipse.org/jgit).
Copyright owners are documented in jgit's IP log.

This program and the accompanying materials are made available
under the terms of the Eclipse Distribution License v1.0 which
accompanies this distribution, is reproduced below, and is
available at http://www.eclipse.org/org/documents/edl-v10.php

All rights reserved.

Redistribution and use in source and binary forms, with or
without modification, are permitted provided that the following
conditions are met:

- Redistributions of source code must retain the above copyright
  notice, this list of conditions and the following disclaimer.

- Redistributions in binary form must reproduce the above
  copyright notice, this list of conditions and the following
  disclaimer in the documentation and/or other materials provided
  with the distribution.

- Neither the name of the Eclipse Foundation, Inc. nor the
  names of its contributors may be used to endorse or promote
  products derived from this software without specific prior
  written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using System;
using System.Collections.Generic;
using System.Text;
using NGit.Transport;
using NGit.Util;
using Sharpen;

namespace NGit.Transport
{
	/// <summary>Support class to populate user authentication data on a connection.</summary>
	/// <remarks>
	/// Support class to populate user authentication data on a connection.
	/// <p>
	/// Instances of an HttpAuthMethod are not thread-safe, as some implementations
	/// may need to maintain per-connection state information.
	/// </remarks>
	internal abstract class HttpAuthMethod
	{
		/// <summary>No authentication is configured.</summary>
		/// <remarks>No authentication is configured.</remarks>
		internal static readonly HttpAuthMethod NONE = new HttpAuthMethod.None();

		/// <summary>Handle an authentication failure and possibly return a new response.</summary>
		/// <remarks>Handle an authentication failure and possibly return a new response.</remarks>
		/// <param name="conn">the connection that failed.</param>
		/// <returns>new authentication method to try.</returns>
		internal static HttpAuthMethod ScanResponse(HttpURLConnection conn)
		{
			string hdr = conn.GetHeaderField(HttpSupport.HDR_WWW_AUTHENTICATE);
			if (hdr == null || hdr.Length == 0)
			{
				return NONE;
			}
			int sp = hdr.IndexOf(' ');
			if (sp < 0)
			{
				return NONE;
			}
			string type = Sharpen.Runtime.Substring(hdr, 0, sp);
			if (Sharpen.Runtime.EqualsIgnoreCase(HttpAuthMethod.Basic.NAME, type))
			{
				return new HttpAuthMethod.Basic();
			}
			else
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(HttpAuthMethod.Digest.NAME, type))
				{
					return new HttpAuthMethod.Digest(Sharpen.Runtime.Substring(hdr, sp + 1));
				}
				else
				{
					return NONE;
				}
			}
		}

		/// <summary>Update this method with the credentials from the URIish.</summary>
		/// <remarks>Update this method with the credentials from the URIish.</remarks>
		/// <param name="uri">the URI used to create the connection.</param>
		/// <param name="credentialsProvider">
		/// the credentials provider, or null. If provided,
		/// <see cref="URIish.GetPass()">credentials in the URI</see>
		/// are ignored.
		/// </param>
		/// <returns>
		/// true if the authentication method is able to provide
		/// authorization for the given URI
		/// </returns>
		internal virtual bool Authorize(URIish uri, CredentialsProvider credentialsProvider
			)
		{
			string username;
			string password;
			if (credentialsProvider != null)
			{
				CredentialItem.Username u = new CredentialItem.Username();
				CredentialItem.Password p = new CredentialItem.Password();
				if (credentialsProvider.Supports(u, p) && credentialsProvider.Get(uri, u, p))
				{
					username = u.GetValue();
					password = Sharpen.Extensions.CreateString(p.GetValue());
					p.Clear();
				}
				else
				{
					return false;
				}
			}
			else
			{
				username = uri.GetUser();
				password = uri.GetPass();
			}
			if (username != null)
			{
				Authorize(username, password);
				return true;
			}
			return false;
		}

		/// <summary>Update this method with the given username and password pair.</summary>
		/// <remarks>Update this method with the given username and password pair.</remarks>
		/// <param name="user"></param>
		/// <param name="pass"></param>
		internal abstract void Authorize(string user, string pass);

		/// <summary>Update connection properties based on this authentication method.</summary>
		/// <remarks>Update connection properties based on this authentication method.</remarks>
		/// <param name="conn"></param>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		internal abstract void ConfigureRequest(HttpURLConnection conn);

		/// <summary>Performs no user authentication.</summary>
		/// <remarks>Performs no user authentication.</remarks>
		private class None : HttpAuthMethod
		{
			internal override void Authorize(string user, string pass)
			{
			}

			// Do nothing when no authentication is enabled.
			/// <exception cref="System.IO.IOException"></exception>
			internal override void ConfigureRequest(HttpURLConnection conn)
			{
			}
			// Do nothing when no authentication is enabled.
		}

		/// <summary>Performs HTTP basic authentication (plaintext username/password).</summary>
		/// <remarks>Performs HTTP basic authentication (plaintext username/password).</remarks>
		private class Basic : HttpAuthMethod
		{
			internal static readonly string NAME = "Basic";

			private string user;

			private string pass;

			internal override void Authorize(string username, string password)
			{
				this.user = username;
				this.pass = password;
			}

			/// <exception cref="System.IO.IOException"></exception>
			internal override void ConfigureRequest(HttpURLConnection conn)
			{
				string ident = user + ":" + pass;
				string enc = Base64.EncodeBytes(Sharpen.Runtime.GetBytesForString(ident, "UTF-8")
					);
				conn.SetRequestProperty(HttpSupport.HDR_AUTHORIZATION, NAME + " " + enc);
			}
		}

		/// <summary>Performs HTTP digest authentication.</summary>
		/// <remarks>Performs HTTP digest authentication.</remarks>
		private class Digest : HttpAuthMethod
		{
			internal static readonly string NAME = "Digest";

			private static readonly Random PRNG = new Random();

			private readonly IDictionary<string, string> @params;

			private int requestCount;

			private string user;

			private string pass;

			internal Digest(string hdr)
			{
				@params = Parse(hdr);
				string qop = @params.Get("qop");
				if ("auth".Equals(qop))
				{
					byte[] bin = new byte[8];
					PRNG.NextBytes(bin);
					@params.Put("cnonce", Base64.EncodeBytes(bin));
				}
			}

			internal override void Authorize(string username, string password)
			{
				this.user = username;
				this.pass = password;
			}

			/// <exception cref="System.IO.IOException"></exception>
			internal override void ConfigureRequest(HttpURLConnection conn)
			{
				IDictionary<string, string> r = new LinkedHashMap<string, string>();
				string realm = @params.Get("realm");
				string nonce = @params.Get("nonce");
				string cnonce = @params.Get("cnonce");
				string uri = Uri(conn.GetURL());
				string qop = @params.Get("qop");
				string method = conn.GetRequestMethod();
				string A1 = user + ":" + realm + ":" + pass;
				string A2 = method + ":" + uri;
				r.Put("username", user);
				r.Put("realm", realm);
				r.Put("nonce", nonce);
				r.Put("uri", uri);
				string response;
				string nc;
				if ("auth".Equals(qop))
				{
					nc = string.Format("%08x", ++requestCount);
					response = KD(H(A1), nonce + ":" + nc + ":" + cnonce + ":" + qop + ":" + H(A2));
				}
				else
				{
					nc = null;
					response = KD(H(A1), nonce + ":" + H(A2));
				}
				r.Put("response", response);
				if (@params.ContainsKey("algorithm"))
				{
					r.Put("algorithm", "MD5");
				}
				if (cnonce != null && qop != null)
				{
					r.Put("cnonce", cnonce);
				}
				if (@params.ContainsKey("opaque"))
				{
					r.Put("opaque", @params.Get("opaque"));
				}
				if (qop != null)
				{
					r.Put("qop", qop);
				}
				if (nc != null)
				{
					r.Put("nc", nc);
				}
				StringBuilder v = new StringBuilder();
				foreach (KeyValuePair<string, string> e in r.EntrySet())
				{
					if (v.Length > 0)
					{
						v.Append(", ");
					}
					v.Append(e.Key);
					v.Append('=');
					v.Append('"');
					v.Append(e.Value);
					v.Append('"');
				}
				conn.SetRequestProperty(HttpSupport.HDR_AUTHORIZATION, NAME + " " + v);
			}

			private static string Uri(System.Uri u)
			{
				StringBuilder r = new StringBuilder();
				r.Append(u.Scheme);
				r.Append("://");
				r.Append(u.GetHost());
				if (0 < u.Port)
				{
					if (u.Port == 80 && "http".Equals(u.Scheme))
					{
					}
					else
					{
						if (u.Port == 443 && "https".Equals(u.Scheme))
						{
						}
						else
						{
							r.Append(':').Append(u.Port);
						}
					}
				}
				r.Append(u.AbsolutePath);
				if (u.GetQuery() != null)
				{
					r.Append('?').Append(u.GetQuery());
				}
				return r.ToString();
			}

			private static string H(string data)
			{
				try
				{
					MessageDigest md = NewMD5();
					md.Update(Sharpen.Runtime.GetBytesForString(data, "UTF-8"));
					return Lhex(md.Digest());
				}
				catch (UnsupportedEncodingException e)
				{
					throw new RuntimeException("UTF-8 encoding not available", e);
				}
			}

			private static string KD(string secret, string data)
			{
				try
				{
					MessageDigest md = NewMD5();
					md.Update(Sharpen.Runtime.GetBytesForString(secret, "UTF-8"));
					md.Update(unchecked((byte)':'));
					md.Update(Sharpen.Runtime.GetBytesForString(data, "UTF-8"));
					return Lhex(md.Digest());
				}
				catch (UnsupportedEncodingException e)
				{
					throw new RuntimeException("UTF-8 encoding not available", e);
				}
			}

			private static MessageDigest NewMD5()
			{
				try
				{
					return MessageDigest.GetInstance("MD5");
				}
				catch (NoSuchAlgorithmException e)
				{
					throw new RuntimeException("No MD5 available", e);
				}
			}

			private static readonly char[] LHEX = new char[] { '0', '1', '2', '3', '4', '5', 
				'6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f' };

			//
			private static string Lhex(byte[] bin)
			{
				StringBuilder r = new StringBuilder(bin.Length * 2);
				for (int i = 0; i < bin.Length; i++)
				{
					byte b = bin[i];
					r.Append(LHEX[(b >> 4) & unchecked((int)(0x0f))]);
					r.Append(LHEX[b & unchecked((int)(0x0f))]);
				}
				return r.ToString();
			}

			private static IDictionary<string, string> Parse(string auth)
			{
				IDictionary<string, string> p = new Dictionary<string, string>();
				int next = 0;
				while (next < auth.Length)
				{
					if (next < auth.Length && auth[next] == ',')
					{
						next++;
					}
					while (next < auth.Length && char.IsWhiteSpace(auth[next]))
					{
						next++;
					}
					int eq = auth.IndexOf('=', next);
					if (eq < 0 || eq + 1 == auth.Length)
					{
						return Sharpen.Collections.EmptyMap<string, string>();
					}
					string name = Sharpen.Runtime.Substring(auth, next, eq);
					string value;
					if (auth[eq + 1] == '"')
					{
						int dq = auth.IndexOf('"', eq + 2);
						if (dq < 0)
						{
							return Sharpen.Collections.EmptyMap<string, string>();
						}
						value = Sharpen.Runtime.Substring(auth, eq + 2, dq);
						next = dq + 1;
					}
					else
					{
						int space = auth.IndexOf(' ', eq + 1);
						int comma = auth.IndexOf(',', eq + 1);
						if (space < 0)
						{
							space = auth.Length;
						}
						if (comma < 0)
						{
							comma = auth.Length;
						}
						int e = Math.Min(space, comma);
						value = Sharpen.Runtime.Substring(auth, eq + 1, e);
						next = e + 1;
					}
					p.Put(name, value);
				}
				return p;
			}
		}
	}
}
