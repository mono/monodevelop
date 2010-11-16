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
using System.Text;
using NGit;
using Sharpen;

namespace NGit.Util
{
	/// <summary>Extra utilities to support usage of HTTP.</summary>
	/// <remarks>Extra utilities to support usage of HTTP.</remarks>
	public class HttpSupport
	{
		/// <summary>
		/// The
		/// <code>GET</code>
		/// HTTP method.
		/// </summary>
		public static readonly string METHOD_GET = "GET";

		/// <summary>
		/// The
		/// <code>POST</code>
		/// HTTP method.
		/// </summary>
		public static readonly string METHOD_POST = "POST";

		/// <summary>
		/// The
		/// <code>Cache-Control</code>
		/// header.
		/// </summary>
		public static readonly string HDR_CACHE_CONTROL = "Cache-Control";

		/// <summary>
		/// The
		/// <code>Pragma</code>
		/// header.
		/// </summary>
		public static readonly string HDR_PRAGMA = "Pragma";

		/// <summary>
		/// The
		/// <code>User-Agent</code>
		/// header.
		/// </summary>
		public static readonly string HDR_USER_AGENT = "User-Agent";

		/// <summary>
		/// The
		/// <code>Date</code>
		/// header.
		/// </summary>
		public static readonly string HDR_DATE = "Date";

		/// <summary>
		/// The
		/// <code>Expires</code>
		/// header.
		/// </summary>
		public static readonly string HDR_EXPIRES = "Expires";

		/// <summary>
		/// The
		/// <code>ETag</code>
		/// header.
		/// </summary>
		public static readonly string HDR_ETAG = "ETag";

		/// <summary>
		/// The
		/// <code>If-None-Match</code>
		/// header.
		/// </summary>
		public static readonly string HDR_IF_NONE_MATCH = "If-None-Match";

		/// <summary>
		/// The
		/// <code>Last-Modified</code>
		/// header.
		/// </summary>
		public static readonly string HDR_LAST_MODIFIED = "Last-Modified";

		/// <summary>
		/// The
		/// <code>If-Modified-Since</code>
		/// header.
		/// </summary>
		public static readonly string HDR_IF_MODIFIED_SINCE = "If-Modified-Since";

		/// <summary>
		/// The
		/// <code>Accept</code>
		/// header.
		/// </summary>
		public static readonly string HDR_ACCEPT = "Accept";

		/// <summary>
		/// The
		/// <code>Content-Type</code>
		/// header.
		/// </summary>
		public static readonly string HDR_CONTENT_TYPE = "Content-Type";

		/// <summary>
		/// The
		/// <code>Content-Length</code>
		/// header.
		/// </summary>
		public static readonly string HDR_CONTENT_LENGTH = "Content-Length";

		/// <summary>
		/// The
		/// <code>Content-Encoding</code>
		/// header.
		/// </summary>
		public static readonly string HDR_CONTENT_ENCODING = "Content-Encoding";

		/// <summary>
		/// The
		/// <code>Content-Range</code>
		/// header.
		/// </summary>
		public static readonly string HDR_CONTENT_RANGE = "Content-Range";

		/// <summary>
		/// The
		/// <code>Accept-Ranges</code>
		/// header.
		/// </summary>
		public static readonly string HDR_ACCEPT_RANGES = "Accept-Ranges";

		/// <summary>
		/// The
		/// <code>If-Range</code>
		/// header.
		/// </summary>
		public static readonly string HDR_IF_RANGE = "If-Range";

		/// <summary>
		/// The
		/// <code>Range</code>
		/// header.
		/// </summary>
		public static readonly string HDR_RANGE = "Range";

		/// <summary>
		/// The
		/// <code>Accept-Encoding</code>
		/// header.
		/// </summary>
		public static readonly string HDR_ACCEPT_ENCODING = "Accept-Encoding";

		/// <summary>
		/// The
		/// <code>gzip</code>
		/// encoding value for
		/// <see cref="HDR_ACCEPT_ENCODING">HDR_ACCEPT_ENCODING</see>
		/// .
		/// </summary>
		public static readonly string ENCODING_GZIP = "gzip";

		/// <summary>
		/// The standard
		/// <code>text/plain</code>
		/// MIME type.
		/// </summary>
		public static readonly string TEXT_PLAIN = "text/plain";

		/// <summary>
		/// The
		/// <code>Authorization</code>
		/// header.
		/// </summary>
		public static readonly string HDR_AUTHORIZATION = "Authorization";

		/// <summary>
		/// The
		/// <code>WWW-Authenticate</code>
		/// header.
		/// </summary>
		public static readonly string HDR_WWW_AUTHENTICATE = "WWW-Authenticate";

		/// <summary>URL encode a value string into an output buffer.</summary>
		/// <remarks>URL encode a value string into an output buffer.</remarks>
		/// <param name="urlstr">the output buffer.</param>
		/// <param name="key">value which must be encoded to protected special characters.</param>
		public static void Encode(StringBuilder urlstr, string key)
		{
			if (key == null || key.Length == 0)
			{
				return;
			}
			try
			{
				urlstr.Append(URLEncoder.Encode(key, "UTF-8"));
			}
			catch (UnsupportedEncodingException e)
			{
				throw new RuntimeException(JGitText.Get().couldNotURLEncodeToUTF8, e);
			}
		}

		/// <summary>Get the HTTP response code from the request.</summary>
		/// <remarks>
		/// Get the HTTP response code from the request.
		/// <p>
		/// Roughly the same as <code>c.getResponseCode()</code> but the
		/// ConnectException is translated to be more understandable.
		/// </remarks>
		/// <param name="c">connection the code should be obtained from.</param>
		/// <returns>
		/// r HTTP status code, usually 200 to indicate success. See
		/// <see cref="Sharpen.HttpURLConnection">Sharpen.HttpURLConnection</see>
		/// for other defined constants.
		/// </returns>
		/// <exception cref="System.IO.IOException">communications error prevented obtaining the response code.
		/// 	</exception>
		public static int Response(HttpURLConnection c)
		{
			try
			{
				return c.GetResponseCode();
			}
			catch (ConnectException ce)
			{
				string host = c.GetURL().GetHost();
				// The standard J2SE error message is not very useful.
				//
				if ("Connection timed out: connect".Equals(ce.Message))
				{
					throw new ConnectException(MessageFormat.Format(JGitText.Get().connectionTimeOut, 
						host));
				}
				throw new ConnectException(ce.Message + " " + host);
			}
		}

		/// <summary>Determine the proxy server (if any) needed to obtain a URL.</summary>
		/// <remarks>Determine the proxy server (if any) needed to obtain a URL.</remarks>
		/// <param name="proxySelector">proxy support for the caller.</param>
		/// <param name="u">location of the server caller wants to talk to.</param>
		/// <returns>proxy to communicate with the supplied URL.</returns>
		/// <exception cref="Sharpen.ConnectException">
		/// the proxy could not be computed as the supplied URL could not
		/// be read. This failure should never occur.
		/// </exception>
		public static Proxy ProxyFor(ProxySelector proxySelector, Uri u)
		{
			try
			{
				return proxySelector.Select(u.ToURI())[0];
			}
			catch (URISyntaxException e)
			{
				ConnectException err;
				err = new ConnectException(MessageFormat.Format(JGitText.Get().cannotDetermineProxyFor
					, u));
				Sharpen.Extensions.InitCause(err, e);
				throw err;
			}
		}

		public HttpSupport()
		{
		}
		// Utility class only.
	}
}
