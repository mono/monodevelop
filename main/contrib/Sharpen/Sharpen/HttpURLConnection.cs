// 
// HttpURLConnection.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using System.Net;

namespace Sharpen
{
	public class URLConnection
	{
	}
	
	public class HttpsURLConnection: HttpURLConnection
	{
		internal HttpsURLConnection (Uri uri, Proxy p): base (uri, p)
		{
		}
		
		internal void SetSSLSocketFactory (object factory)
		{
			// TODO
		}
	}
	
	public class HttpURLConnection: URLConnection
	{
		public const int HTTP_OK = 200;
		public const int HTTP_NOT_FOUND = 404;
		public const int HTTP_FORBIDDEN = 403;
		public const int HTTP_UNAUTHORIZED = 401;
		
		HttpWebRequest request;
		HttpWebResponse reqResponse;
		Uri url;
		
		internal HttpURLConnection (Uri uri, Proxy p)
		{
			url = uri;
			request = (HttpWebRequest) HttpWebRequest.Create (uri);
		}
		
		HttpWebResponse Response {
			get {
				if (reqResponse == null)
					reqResponse = (HttpWebResponse) request.GetResponse ();
				return reqResponse;
			}
		}
		
		public void SetUseCaches (bool u)
		{
			if (u)
				request.CachePolicy = new System.Net.Cache.RequestCachePolicy (System.Net.Cache.RequestCacheLevel.Default);
			else
				request.CachePolicy = new System.Net.Cache.RequestCachePolicy (System.Net.Cache.RequestCacheLevel.BypassCache);
		}
		
		public void SetRequestMethod (string method)
		{
			request.Method = method;
		}
		
		public string GetRequestMethod ()
		{
			return request.Method;
		}
		
		public void SetInstanceFollowRedirects (bool redirects)
		{
			request.AllowAutoRedirect = redirects;
		}
		
		public void SetDoOutput (bool dooutput)
		{
			// Not required?
		}
		
		public void SetFixedLengthStreamingMode (int len)
		{
			request.SendChunked = false;
		}
		
		public void SetChunkedStreamingMode (int n)
		{
			request.SendChunked = true;
		}
		
		public void SetRequestProperty (string key, string value)
		{
			request.Headers.Set (key, value);
		}
		
		public string GetResponseMessage ()
		{
			return Response.StatusDescription;
		}
		
		public void SetConnectTimeout (int ms)
		{
			request.Timeout = ms;
		}
		
		public void SetReadTimeout (int ms)
		{
			// Not available
		}
		
		public InputStream GetInputStream ()
		{
			return Response.GetResponseStream ();
		}
		
		public OutputStream GetOutputStream ()
		{
			return request.GetRequestStream ();
		}
		
		public string GetHeaderField (string header)
		{
			return Response.GetResponseHeader (header);
		}
		
		public string GetContentType ()
		{
			return Response.ContentType;
		}
		
		public int GetContentLength ()
		{
			return (int) Response.ContentLength;
		}
		
		public int GetResponseCode ()
		{
			return (int) Response.StatusCode;
		}
		
		public Uri GetURL ()
		{
			return url;
		}
	}
}

