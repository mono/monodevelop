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
using System.Globalization;
using System.IO;
using System.Text;
using NGit;
using NGit.Transport;
using NGit.Util;
using Org.Xml.Sax;
using Org.Xml.Sax.Helpers;
using Sharpen;

namespace NGit.Transport
{
	/// <summary>A simple HTTP REST client for the Amazon S3 service.</summary>
	/// <remarks>
	/// A simple HTTP REST client for the Amazon S3 service.
	/// <p>
	/// This client uses the REST API to communicate with the Amazon S3 servers and
	/// read or write content through a bucket that the user has access to. It is a
	/// very lightweight implementation of the S3 API and therefore does not have all
	/// of the bells and whistles of popular client implementations.
	/// <p>
	/// Authentication is always performed using the user's AWSAccessKeyId and their
	/// private AWSSecretAccessKey.
	/// <p>
	/// Optional client-side encryption may be enabled if requested. The format is
	/// compatible with <a href="http://jets3t.s3.amazonaws.com/index.html">jets3t</a>,
	/// a popular Java based Amazon S3 client library. Enabling encryption can hide
	/// sensitive data from the operators of the S3 service.
	/// </remarks>
	public class AmazonS3
	{
		private static readonly ICollection<string> SIGNED_HEADERS;

		private static readonly string HMAC = "HmacSHA1";

		private static readonly string DOMAIN = "s3.amazonaws.com";

		private static readonly string X_AMZ_ACL = "x-amz-acl";

		private static readonly string X_AMZ_META = "x-amz-meta-";

		static AmazonS3()
		{
			SIGNED_HEADERS = new HashSet<string>();
			SIGNED_HEADERS.AddItem("content-type");
			SIGNED_HEADERS.AddItem("content-md5");
			SIGNED_HEADERS.AddItem("date");
		}

		private static bool IsSignedHeader(string name)
		{
			string nameLC = StringUtils.ToLowerCase(name);
			return SIGNED_HEADERS.Contains(nameLC) || nameLC.StartsWith("x-amz-");
		}

		private static string ToCleanString(IList<string> list)
		{
			StringBuilder s = new StringBuilder();
			foreach (string v in list)
			{
				if (s.Length > 0)
				{
					s.Append(',');
				}
				s.Append(v.ReplaceAll("\n", string.Empty).Trim());
			}
			return s.ToString();
		}

		private static string Remove(IDictionary<string, string> m, string k)
		{
			string r = Sharpen.Collections.Remove(m, k);
			return r != null ? r : string.Empty;
		}

		private static string HttpNow()
		{
			string tz = "GMT";
			SimpleDateFormat fmt;
			fmt = new SimpleDateFormat("EEE, dd MMM yyyy HH:mm:ss", CultureInfo.InvariantCulture
				);
			fmt.SetTimeZone(Sharpen.Extensions.GetTimeZone(tz));
			return fmt.Format(new DateTime()) + " " + tz;
		}

		private static MessageDigest NewMD5()
		{
			try
			{
				return MessageDigest.GetInstance("MD5");
			}
			catch (NoSuchAlgorithmException e)
			{
				throw new RuntimeException(JGitText.Get().JRELacksMD5Implementation, e);
			}
		}

		/// <summary>AWSAccessKeyId, public string that identifies the user's account.</summary>
		/// <remarks>AWSAccessKeyId, public string that identifies the user's account.</remarks>
		private readonly string publicKey;

		/// <summary>Decoded form of the private AWSSecretAccessKey, to sign requests.</summary>
		/// <remarks>Decoded form of the private AWSSecretAccessKey, to sign requests.</remarks>
		private readonly SecretKeySpec privateKey;

		/// <summary>Our HTTP proxy support, in case we are behind a firewall.</summary>
		/// <remarks>Our HTTP proxy support, in case we are behind a firewall.</remarks>
		private readonly ProxySelector proxySelector;

		/// <summary>ACL to apply to created objects.</summary>
		/// <remarks>ACL to apply to created objects.</remarks>
		private readonly string acl;

		/// <summary>Maximum number of times to try an operation.</summary>
		/// <remarks>Maximum number of times to try an operation.</remarks>
		private readonly int maxAttempts;

		/// <summary>Encryption algorithm, may be a null instance that provides pass-through.
		/// 	</summary>
		/// <remarks>Encryption algorithm, may be a null instance that provides pass-through.
		/// 	</remarks>
		private readonly WalkEncryption encryption;

		/// <summary>Create a new S3 client for the supplied user information.</summary>
		/// <remarks>
		/// Create a new S3 client for the supplied user information.
		/// <p>
		/// The connection properties are a subset of those supported by the popular
		/// <a href="http://jets3t.s3.amazonaws.com/index.html">jets3t</a> library.
		/// For example:
		/// <pre>
		/// # AWS Access and Secret Keys (required)
		/// accesskey: &lt;YourAWSAccessKey&gt;
		/// secretkey: &lt;YourAWSSecretKey&gt;
		/// # Access Control List setting to apply to uploads, must be one of:
		/// # PRIVATE, PUBLIC_READ (defaults to PRIVATE).
		/// acl: PRIVATE
		/// # Number of times to retry after internal error from S3.
		/// httpclient.retry-max: 3
		/// # End-to-end encryption (hides content from S3 owners)
		/// password: &lt;encryption pass-phrase&gt;
		/// crypto.algorithm: PBEWithMD5AndDES
		/// </pre>
		/// </remarks>
		/// <param name="props">connection properties.</param>
		public AmazonS3(Sharpen.Properties props)
		{
			publicKey = props.GetProperty("accesskey");
			if (publicKey == null)
			{
				throw new ArgumentException(JGitText.Get().missingAccesskey);
			}
			string secret = props.GetProperty("secretkey");
			if (secret == null)
			{
				throw new ArgumentException(JGitText.Get().missingSecretkey);
			}
			privateKey = new SecretKeySpec(Constants.EncodeASCII(secret), HMAC);
			string pacl = props.GetProperty("acl", "PRIVATE");
			if (StringUtils.EqualsIgnoreCase("PRIVATE", pacl))
			{
				acl = "private";
			}
			else
			{
				if (StringUtils.EqualsIgnoreCase("PUBLIC", pacl))
				{
					acl = "public-read";
				}
				else
				{
					if (StringUtils.EqualsIgnoreCase("PUBLIC-READ", pacl))
					{
						acl = "public-read";
					}
					else
					{
						if (StringUtils.EqualsIgnoreCase("PUBLIC_READ", pacl))
						{
							acl = "public-read";
						}
						else
						{
							throw new ArgumentException("Invalid acl: " + pacl);
						}
					}
				}
			}
			try
			{
				string cPas = props.GetProperty("password");
				if (cPas != null)
				{
					string cAlg = props.GetProperty("crypto.algorithm");
					if (cAlg == null)
					{
						cAlg = "PBEWithMD5AndDES";
					}
					encryption = new WalkEncryption.ObjectEncryptionV2(cAlg, cPas);
				}
				else
				{
					encryption = WalkEncryption.NONE;
				}
			}
			catch (InvalidKeySpecException e)
			{
				throw new ArgumentException(JGitText.Get().invalidEncryption, e);
			}
			catch (NoSuchAlgorithmException e)
			{
				throw new ArgumentException(JGitText.Get().invalidEncryption, e);
			}
			maxAttempts = System.Convert.ToInt32(props.GetProperty("httpclient.retry-max", "3"
				));
			proxySelector = ProxySelector.GetDefault();
		}

		/// <summary>Get the content of a bucket object.</summary>
		/// <remarks>Get the content of a bucket object.</remarks>
		/// <param name="bucket">name of the bucket storing the object.</param>
		/// <param name="key">key of the object within its bucket.</param>
		/// <returns>
		/// connection to stream the content of the object. The request
		/// properties of the connection may not be modified by the caller as
		/// the request parameters have already been signed.
		/// </returns>
		/// <exception cref="System.IO.IOException">sending the request was not possible.</exception>
		public virtual URLConnection Get(string bucket, string key)
		{
			for (int curAttempt = 0; curAttempt < maxAttempts; curAttempt++)
			{
				HttpURLConnection c = Open("GET", bucket, key);
				Authorize(c);
				switch (HttpSupport.Response(c))
				{
					case HttpURLConnection.HTTP_OK:
					{
						encryption.Validate(c, X_AMZ_META);
						return c;
					}

					case HttpURLConnection.HTTP_NOT_FOUND:
					{
						throw new FileNotFoundException(key);
					}

					case HttpURLConnection.HTTP_INTERNAL_ERROR:
					{
						continue;
						goto default;
					}

					default:
					{
						throw Error("Reading", key, c);
					}
				}
			}
			throw MaxAttempts("Reading", key);
		}

		/// <summary>
		/// Decrypt an input stream from
		/// <see cref="Get(string, string)">Get(string, string)</see>
		/// .
		/// </summary>
		/// <param name="u">
		/// connection previously created by
		/// <see cref="Get(string, string)">Get(string, string)</see>
		/// }.
		/// </param>
		/// <returns>stream to read plain text from.</returns>
		/// <exception cref="System.IO.IOException">decryption could not be configured.</exception>
		public virtual InputStream Decrypt(URLConnection u)
		{
			return encryption.Decrypt(u.GetInputStream());
		}

		/// <summary>List the names of keys available within a bucket.</summary>
		/// <remarks>
		/// List the names of keys available within a bucket.
		/// <p>
		/// This method is primarily meant for obtaining a "recursive directory
		/// listing" rooted under the specified bucket and prefix location.
		/// </remarks>
		/// <param name="bucket">name of the bucket whose objects should be listed.</param>
		/// <param name="prefix">
		/// common prefix to filter the results by. Must not be null.
		/// Supplying the empty string will list all keys in the bucket.
		/// Supplying a non-empty string will act as though a trailing '/'
		/// appears in prefix, even if it does not.
		/// </param>
		/// <returns>
		/// list of keys starting with <code>prefix</code>, after removing
		/// <code>prefix</code> (or <code>prefix + "/"</code>)from all
		/// of them.
		/// </returns>
		/// <exception cref="System.IO.IOException">
		/// sending the request was not possible, or the response XML
		/// document could not be parsed properly.
		/// </exception>
		public virtual IList<string> List(string bucket, string prefix)
		{
			if (prefix.Length > 0 && !prefix.EndsWith("/"))
			{
				prefix += "/";
			}
			AmazonS3.ListParser lp = new AmazonS3.ListParser(this, bucket, prefix);
			do
			{
				lp.List();
			}
			while (lp.truncated);
			return lp.entries;
		}

		/// <summary>Delete a single object.</summary>
		/// <remarks>
		/// Delete a single object.
		/// <p>
		/// Deletion always succeeds, even if the object does not exist.
		/// </remarks>
		/// <param name="bucket">name of the bucket storing the object.</param>
		/// <param name="key">key of the object within its bucket.</param>
		/// <exception cref="System.IO.IOException">deletion failed due to communications error.
		/// 	</exception>
		public virtual void Delete(string bucket, string key)
		{
			for (int curAttempt = 0; curAttempt < maxAttempts; curAttempt++)
			{
				HttpURLConnection c = Open("DELETE", bucket, key);
				Authorize(c);
				switch (HttpSupport.Response(c))
				{
					case HttpURLConnection.HTTP_NO_CONTENT:
					{
						return;
					}

					case HttpURLConnection.HTTP_INTERNAL_ERROR:
					{
						continue;
						goto default;
					}

					default:
					{
						throw Error("Deletion", key, c);
					}
				}
			}
			throw MaxAttempts("Deletion", key);
		}

		/// <summary>Atomically create or replace a single small object.</summary>
		/// <remarks>
		/// Atomically create or replace a single small object.
		/// <p>
		/// This form is only suitable for smaller contents, where the caller can
		/// reasonable fit the entire thing into memory.
		/// <p>
		/// End-to-end data integrity is assured by internally computing the MD5
		/// checksum of the supplied data and transmitting the checksum along with
		/// the data itself.
		/// </remarks>
		/// <param name="bucket">name of the bucket storing the object.</param>
		/// <param name="key">key of the object within its bucket.</param>
		/// <param name="data">
		/// new data content for the object. Must not be null. Zero length
		/// array will create a zero length object.
		/// </param>
		/// <exception cref="System.IO.IOException">creation/updating failed due to communications error.
		/// 	</exception>
		public virtual void Put(string bucket, string key, byte[] data)
		{
			if (encryption != WalkEncryption.NONE)
			{
				// We have to copy to produce the cipher text anyway so use
				// the large object code path as it supports that behavior.
				//
				OutputStream os = BeginPut(bucket, key, null, null);
				os.Write(data);
				os.Close();
				return;
			}
			string md5str = Base64.EncodeBytes(NewMD5().Digest(data));
			string lenstr = data.Length.ToString();
			for (int curAttempt = 0; curAttempt < maxAttempts; curAttempt++)
			{
				HttpURLConnection c = Open("PUT", bucket, key);
				c.SetRequestProperty("Content-Length", lenstr);
				c.SetRequestProperty("Content-MD5", md5str);
				c.SetRequestProperty(X_AMZ_ACL, acl);
				Authorize(c);
				c.SetDoOutput(true);
				c.SetFixedLengthStreamingMode(data.Length);
				OutputStream os = c.GetOutputStream();
				try
				{
					os.Write(data);
				}
				finally
				{
					os.Close();
				}
				switch (HttpSupport.Response(c))
				{
					case HttpURLConnection.HTTP_OK:
					{
						return;
					}

					case HttpURLConnection.HTTP_INTERNAL_ERROR:
					{
						continue;
						goto default;
					}

					default:
					{
						throw Error("Writing", key, c);
					}
				}
			}
			throw MaxAttempts("Writing", key);
		}

		/// <summary>Atomically create or replace a single large object.</summary>
		/// <remarks>
		/// Atomically create or replace a single large object.
		/// <p>
		/// Initially the returned output stream buffers data into memory, but if the
		/// total number of written bytes starts to exceed an internal limit the data
		/// is spooled to a temporary file on the local drive.
		/// <p>
		/// Network transmission is attempted only when <code>close()</code> gets
		/// called at the end of output. Closing the returned stream can therefore
		/// take significant time, especially if the written content is very large.
		/// <p>
		/// End-to-end data integrity is assured by internally computing the MD5
		/// checksum of the supplied data and transmitting the checksum along with
		/// the data itself.
		/// </remarks>
		/// <param name="bucket">name of the bucket storing the object.</param>
		/// <param name="key">key of the object within its bucket.</param>
		/// <param name="monitor">
		/// (optional) progress monitor to post upload completion to
		/// during the stream's close method.
		/// </param>
		/// <param name="monitorTask">(optional) task name to display during the close method.
		/// 	</param>
		/// <returns>a stream which accepts the new data, and transmits once closed.</returns>
		/// <exception cref="System.IO.IOException">if encryption was enabled it could not be configured.
		/// 	</exception>
		public virtual OutputStream BeginPut(string bucket, string key, ProgressMonitor monitor
			, string monitorTask)
		{
			MessageDigest md5 = NewMD5();
			TemporaryBuffer buffer = new _LocalFile_455(this, bucket, key, md5, monitor, monitorTask
				);
			return encryption.Encrypt(new DigestOutputStream(buffer, md5));
		}

		private sealed class _LocalFile_455 : TemporaryBuffer.LocalFile
		{
			public _LocalFile_455(AmazonS3 _enclosing, string bucket, string key, MessageDigest
				 md5, ProgressMonitor monitor, string monitorTask)
			{
				this._enclosing = _enclosing;
				this.bucket = bucket;
				this.key = key;
				this.md5 = md5;
				this.monitor = monitor;
				this.monitorTask = monitorTask;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Close()
			{
				base.Close();
				try
				{
					this._enclosing.PutImpl(bucket, key, md5.Digest(), this, monitor, monitorTask);
				}
				finally
				{
					this.Destroy();
				}
			}

			private readonly AmazonS3 _enclosing;

			private readonly string bucket;

			private readonly string key;

			private readonly MessageDigest md5;

			private readonly ProgressMonitor monitor;

			private readonly string monitorTask;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void PutImpl(string bucket, string key, byte[] csum, TemporaryBuffer buf, 
			ProgressMonitor monitor, string monitorTask)
		{
			if (monitor == null)
			{
				monitor = NullProgressMonitor.INSTANCE;
			}
			if (monitorTask == null)
			{
				monitorTask = MessageFormat.Format(JGitText.Get().progressMonUploading, key);
			}
			string md5str = Base64.EncodeBytes(csum);
			long len = buf.Length();
			string lenstr = len.ToString();
			for (int curAttempt = 0; curAttempt < maxAttempts; curAttempt++)
			{
				HttpURLConnection c = Open("PUT", bucket, key);
				c.SetRequestProperty("Content-Length", lenstr);
				c.SetRequestProperty("Content-MD5", md5str);
				c.SetRequestProperty(X_AMZ_ACL, acl);
				encryption.Request(c, X_AMZ_META);
				Authorize(c);
				c.SetDoOutput(true);
				c.SetFixedLengthStreamingMode((int)len);
				monitor.BeginTask(monitorTask, (int)(len / 1024));
				OutputStream os = c.GetOutputStream();
				try
				{
					buf.WriteTo(os, monitor);
				}
				finally
				{
					monitor.EndTask();
					os.Close();
				}
				switch (HttpSupport.Response(c))
				{
					case HttpURLConnection.HTTP_OK:
					{
						return;
					}

					case HttpURLConnection.HTTP_INTERNAL_ERROR:
					{
						continue;
						goto default;
					}

					default:
					{
						throw Error("Writing", key, c);
					}
				}
			}
			throw MaxAttempts("Writing", key);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private IOException Error(string action, string key, HttpURLConnection c)
		{
			IOException err = new IOException(MessageFormat.Format(JGitText.Get().amazonS3ActionFailed
				, action, key, HttpSupport.Response(c), c.GetResponseMessage()));
			ByteArrayOutputStream b = new ByteArrayOutputStream();
			byte[] buf = new byte[2048];
			for (; ; )
			{
				int n = c.GetErrorStream().Read(buf);
				if (n < 0)
				{
					break;
				}
				if (n > 0)
				{
					b.Write(buf, 0, n);
				}
			}
			buf = b.ToByteArray();
			if (buf.Length > 0)
			{
				Sharpen.Extensions.InitCause(err, new IOException("\n" + Sharpen.Extensions.CreateString
					(buf)));
			}
			return err;
		}

		private IOException MaxAttempts(string action, string key)
		{
			return new IOException(MessageFormat.Format(JGitText.Get().amazonS3ActionFailedGivingUp
				, action, key, maxAttempts));
		}

		/// <exception cref="System.IO.IOException"></exception>
		private HttpURLConnection Open(string method, string bucket, string key)
		{
			IDictionary<string, string> noArgs = Sharpen.Collections.EmptyMap();
			return Open(method, bucket, key, noArgs);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private HttpURLConnection Open(string method, string bucket, string key, IDictionary
			<string, string> args)
		{
			StringBuilder urlstr = new StringBuilder();
			urlstr.Append("http://");
			urlstr.Append(bucket);
			urlstr.Append('.');
			urlstr.Append(DOMAIN);
			urlstr.Append('/');
			if (key.Length > 0)
			{
				HttpSupport.Encode(urlstr, key);
			}
			if (!args.IsEmpty())
			{
				Iterator<KeyValuePair<string, string>> i;
				urlstr.Append('?');
				i = args.EntrySet().Iterator();
				while (i.HasNext())
				{
					KeyValuePair<string, string> e = i.Next();
					urlstr.Append(e.Key);
					urlstr.Append('=');
					HttpSupport.Encode(urlstr, e.Value);
					if (i.HasNext())
					{
						urlstr.Append('&');
					}
				}
			}
			Uri url = new Uri(urlstr.ToString());
			Proxy proxy = HttpSupport.ProxyFor(proxySelector, url);
			HttpURLConnection c;
			c = (HttpURLConnection)url.OpenConnection(proxy);
			c.SetRequestMethod(method);
			c.SetRequestProperty("User-Agent", "jgit/1.0");
			c.SetRequestProperty("Date", HttpNow());
			return c;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void Authorize(HttpURLConnection c)
		{
			IDictionary<string, IList<string>> reqHdr = c.GetRequestProperties();
			SortedDictionary<string, string> sigHdr = new SortedDictionary<string, string>();
			foreach (KeyValuePair<string, IList<string>> entry in reqHdr.EntrySet())
			{
				string hdr = entry.Key;
				if (IsSignedHeader(hdr))
				{
					sigHdr.Put(StringUtils.ToLowerCase(hdr), ToCleanString(entry.Value));
				}
			}
			StringBuilder s = new StringBuilder();
			s.Append(c.GetRequestMethod());
			s.Append('\n');
			s.Append(Remove(sigHdr, "content-md5"));
			s.Append('\n');
			s.Append(Remove(sigHdr, "content-type"));
			s.Append('\n');
			s.Append(Remove(sigHdr, "date"));
			s.Append('\n');
			foreach (KeyValuePair<string, string> e in sigHdr.EntrySet())
			{
				s.Append(e.Key);
				s.Append(':');
				s.Append(e.Value);
				s.Append('\n');
			}
			string host = c.GetURL().GetHost();
			s.Append('/');
			s.Append(Sharpen.Runtime.Substring(host, 0, host.Length - DOMAIN.Length - 1));
			s.Append(c.GetURL().AbsolutePath);
			string sec;
			try
			{
				Mac m = Mac.GetInstance(HMAC);
				m.Init(privateKey);
				sec = Base64.EncodeBytes(m.DoFinal(Sharpen.Runtime.GetBytesForString(s.ToString()
					, "UTF-8")));
			}
			catch (NoSuchAlgorithmException e_1)
			{
				throw new IOException(MessageFormat.Format(JGitText.Get().noHMACsupport, HMAC, e_1
					.Message));
			}
			catch (InvalidKeyException e_1)
			{
				throw new IOException(MessageFormat.Format(JGitText.Get().invalidKey, e_1.Message
					));
			}
			c.SetRequestProperty("Authorization", "AWS " + publicKey + ":" + sec);
		}

		/// <exception cref="System.IO.FileNotFoundException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		internal static Sharpen.Properties Properties(FilePath authFile)
		{
			Sharpen.Properties p = new Sharpen.Properties();
			FileInputStream @in = new FileInputStream(authFile);
			try
			{
				p.Load(@in);
			}
			finally
			{
				@in.Close();
			}
			return p;
		}

		private sealed class ListParser : DefaultHandler
		{
			internal readonly IList<string> entries = new AList<string>();

			private readonly string bucket;

			private readonly string prefix;

			internal bool truncated;

			private StringBuilder data;

			internal ListParser(AmazonS3 _enclosing, string bn, string p)
			{
				this._enclosing = _enclosing;
				this.bucket = bn;
				this.prefix = p;
			}

			/// <exception cref="System.IO.IOException"></exception>
			internal void List()
			{
				IDictionary<string, string> args = new SortedDictionary<string, string>();
				if (this.prefix.Length > 0)
				{
					args.Put("prefix", this.prefix);
				}
				if (!this.entries.IsEmpty())
				{
					args.Put("marker", this.prefix + this.entries[this.entries.Count - 1]);
				}
				for (int curAttempt = 0; curAttempt < this._enclosing.maxAttempts; curAttempt++)
				{
					HttpURLConnection c = this._enclosing.Open("GET", this.bucket, string.Empty, args
						);
					this._enclosing.Authorize(c);
					switch (HttpSupport.Response(c))
					{
						case HttpURLConnection.HTTP_OK:
						{
							this.truncated = false;
							this.data = null;
							XMLReader xr;
							try
							{
								xr = XMLReaderFactory.CreateXMLReader();
							}
							catch (SAXException)
							{
								throw new IOException(JGitText.Get().noXMLParserAvailable);
							}
							xr.SetContentHandler(this);
							InputStream @in = c.GetInputStream();
							try
							{
								xr.Parse(new InputSource(@in));
							}
							catch (SAXException parsingError)
							{
								IOException p;
								p = new IOException(MessageFormat.Format(JGitText.Get().errorListing, this.prefix
									));
								Sharpen.Extensions.InitCause(p, parsingError);
								throw p;
							}
							finally
							{
								@in.Close();
							}
							return;
						}

						case HttpURLConnection.HTTP_INTERNAL_ERROR:
						{
							continue;
							goto default;
						}

						default:
						{
							throw this._enclosing.Error("Listing", this.prefix, c);
						}
					}
				}
				throw this._enclosing.MaxAttempts("Listing", this.prefix);
			}

			/// <exception cref="Org.Xml.Sax.SAXException"></exception>
			public override void StartElement(string uri, string name, string qName, Attributes
				 attributes)
			{
				if ("Key".Equals(name) || "IsTruncated".Equals(name))
				{
					this.data = new StringBuilder();
				}
			}

			/// <exception cref="Org.Xml.Sax.SAXException"></exception>
			public override void IgnorableWhitespace(char[] ch, int s, int n)
			{
				if (this.data != null)
				{
					this.data.Append(ch, s, n);
				}
			}

			/// <exception cref="Org.Xml.Sax.SAXException"></exception>
			public override void Characters(char[] ch, int s, int n)
			{
				if (this.data != null)
				{
					this.data.Append(ch, s, n);
				}
			}

			/// <exception cref="Org.Xml.Sax.SAXException"></exception>
			public override void EndElement(string uri, string name, string qName)
			{
				if ("Key".Equals(name))
				{
					this.entries.AddItem(Sharpen.Runtime.Substring(this.data.ToString(), this.prefix.
						Length));
				}
				else
				{
					if ("IsTruncated".Equals(name))
					{
						this.truncated = StringUtils.EqualsIgnoreCase("true", this.data.ToString());
					}
				}
				this.data = null;
			}

			private readonly AmazonS3 _enclosing;
		}
	}
}
