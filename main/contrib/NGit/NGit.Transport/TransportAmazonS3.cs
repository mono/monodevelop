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
using System.IO;
using NGit;
using NGit.Errors;
using NGit.Transport;
using Sharpen;

namespace NGit.Transport
{
	/// <summary>Transport over the non-Git aware Amazon S3 protocol.</summary>
	/// <remarks>
	/// Transport over the non-Git aware Amazon S3 protocol.
	/// <p>
	/// This transport communicates with the Amazon S3 servers (a non-free commercial
	/// hosting service that users must subscribe to). Some users may find transport
	/// to and from S3 to be a useful backup service.
	/// <p>
	/// The transport does not require any specialized Git support on the remote
	/// (server side) repository, as Amazon does not provide any such support.
	/// Repository files are retrieved directly through the S3 API, which uses
	/// extended HTTP/1.1 semantics. This make it possible to read or write Git data
	/// from a remote repository that is stored on S3.
	/// <p>
	/// Unlike the HTTP variant (see
	/// <see cref="TransportHttp">TransportHttp</see>
	/// ) we rely upon being able
	/// to list objects in a bucket, as the S3 API supports this function. By listing
	/// the bucket contents we can avoid relying on <code>objects/info/packs</code>
	/// or <code>info/refs</code> in the remote repository.
	/// <p>
	/// Concurrent pushing over this transport is not supported. Multiple concurrent
	/// push operations may cause confusion in the repository state.
	/// </remarks>
	/// <seealso cref="WalkFetchConnection">WalkFetchConnection</seealso>
	/// <seealso cref="WalkPushConnection">WalkPushConnection</seealso>
	public class TransportAmazonS3 : HttpTransport, WalkTransport
	{
		internal static readonly string S3_SCHEME = "amazon-s3";

		internal static bool CanHandle(URIish uri)
		{
			if (!uri.IsRemote())
			{
				return false;
			}
			return S3_SCHEME.Equals(uri.GetScheme());
		}

		/// <summary>User information necessary to connect to S3.</summary>
		/// <remarks>User information necessary to connect to S3.</remarks>
		private readonly AmazonS3 s3;

		/// <summary>Bucket the remote repository is stored in.</summary>
		/// <remarks>Bucket the remote repository is stored in.</remarks>
		private readonly string bucket;

		/// <summary>Key prefix which all objects related to the repository start with.</summary>
		/// <remarks>
		/// Key prefix which all objects related to the repository start with.
		/// <p>
		/// The prefix does not start with "/".
		/// <p>
		/// The prefix does not end with "/". The trailing slash is stripped during
		/// the constructor if a trailing slash was supplied in the URIish.
		/// <p>
		/// All files within the remote repository start with
		/// <code>keyPrefix + "/"</code>.
		/// </remarks>
		private readonly string keyPrefix;

		/// <exception cref="System.NotSupportedException"></exception>
		protected internal TransportAmazonS3(Repository local, URIish uri) : base(local, 
			uri)
		{
			s3 = new AmazonS3(LoadProperties());
			bucket = uri.GetHost();
			string p = uri.GetPath();
			if (p.StartsWith("/"))
			{
				p = Sharpen.Runtime.Substring(p, 1);
			}
			if (p.EndsWith("/"))
			{
				p = Sharpen.Runtime.Substring(p, 0, p.Length - 1);
			}
			keyPrefix = p;
		}

		/// <exception cref="System.NotSupportedException"></exception>
		private Properties LoadProperties()
		{
			if (local.Directory != null)
			{
				FilePath propsFile = new FilePath(local.Directory, uri.GetUser());
				if (propsFile.IsFile())
				{
					return LoadPropertiesFile(propsFile);
				}
			}
			FilePath propsFile_1 = new FilePath(local.FileSystem.UserHome(), uri.GetUser());
			if (propsFile_1.IsFile())
			{
				return LoadPropertiesFile(propsFile_1);
			}
			Properties props = new Properties();
			props.SetProperty("accesskey", uri.GetUser());
			props.SetProperty("secretkey", uri.GetPass());
			return props;
		}

		/// <exception cref="System.NotSupportedException"></exception>
		private static Properties LoadPropertiesFile(FilePath propsFile)
		{
			try
			{
				return AmazonS3.Properties(propsFile);
			}
			catch (IOException e)
			{
				throw new NotSupportedException(MessageFormat.Format(JGitText.Get().cannotReadFile
					, propsFile), e);
			}
		}

		/// <exception cref="NGit.Errors.TransportException"></exception>
		public override FetchConnection OpenFetch()
		{
			TransportAmazonS3.DatabaseS3 c = new TransportAmazonS3.DatabaseS3(this, bucket, keyPrefix
				 + "/objects");
			WalkFetchConnection r = new WalkFetchConnection(this, c);
			r.Available(c.ReadAdvertisedRefs());
			return r;
		}

		/// <exception cref="NGit.Errors.TransportException"></exception>
		public override PushConnection OpenPush()
		{
			TransportAmazonS3.DatabaseS3 c = new TransportAmazonS3.DatabaseS3(this, bucket, keyPrefix
				 + "/objects");
			WalkPushConnection r = new WalkPushConnection(this, c);
			r.Available(c.ReadAdvertisedRefs());
			return r;
		}

		public override void Close()
		{
		}

		internal class DatabaseS3 : WalkRemoteObjectDatabase
		{
			private readonly string bucketName;

			private readonly string objectsKey;

			internal DatabaseS3(TransportAmazonS3 _enclosing, string b, string o)
			{
				this._enclosing = _enclosing;
				// No explicit connections are maintained.
				this.bucketName = b;
				this.objectsKey = o;
			}

			private string ResolveKey(string subpath)
			{
				if (subpath.EndsWith("/"))
				{
					subpath = Sharpen.Runtime.Substring(subpath, 0, subpath.Length - 1);
				}
				string k = this.objectsKey;
				while (subpath.StartsWith(WalkRemoteObjectDatabase.ROOT_DIR))
				{
					k = Sharpen.Runtime.Substring(k, 0, k.LastIndexOf('/'));
					subpath = Sharpen.Runtime.Substring(subpath, 3);
				}
				return k + "/" + subpath;
			}

			internal override URIish GetURI()
			{
				URIish u = new URIish();
				u = u.SetScheme(TransportAmazonS3.S3_SCHEME);
				u = u.SetHost(this.bucketName);
				u = u.SetPath("/" + this.objectsKey);
				return u;
			}

			/// <exception cref="System.IO.IOException"></exception>
			internal override ICollection<WalkRemoteObjectDatabase> GetAlternates()
			{
				try
				{
					return this.ReadAlternates(WalkRemoteObjectDatabase.INFO_ALTERNATES);
				}
				catch (FileNotFoundException)
				{
				}
				// Fall through.
				return null;
			}

			/// <exception cref="System.IO.IOException"></exception>
			internal override WalkRemoteObjectDatabase OpenAlternate(string location)
			{
				return new TransportAmazonS3.DatabaseS3(this, this.bucketName, this.ResolveKey(location
					));
			}

			/// <exception cref="System.IO.IOException"></exception>
			internal override ICollection<string> GetPackNames()
			{
				HashSet<string> have = new HashSet<string>();
				Sharpen.Collections.AddAll(have, this._enclosing.s3.List(this._enclosing.bucket, 
					this.ResolveKey("pack")));
				ICollection<string> packs = new AList<string>();
				foreach (string n in have)
				{
					if (!n.StartsWith("pack-") || !n.EndsWith(".pack"))
					{
						continue;
					}
					string @in = Sharpen.Runtime.Substring(n, 0, n.Length - 5) + ".idx";
					if (have.Contains(@in))
					{
						packs.AddItem(n);
					}
				}
				return packs;
			}

			/// <exception cref="System.IO.IOException"></exception>
			internal override WalkRemoteObjectDatabase.FileStream Open(string path)
			{
				URLConnection c = this._enclosing.s3.Get(this._enclosing.bucket, this.ResolveKey(
					path));
				InputStream raw = c.GetInputStream();
				InputStream @in = this._enclosing.s3.Decrypt(c);
				int len = c.GetContentLength();
				return new WalkRemoteObjectDatabase.FileStream(@in, raw == @in ? len : -1);
			}

			/// <exception cref="System.IO.IOException"></exception>
			internal override void DeleteFile(string path)
			{
				this._enclosing.s3.Delete(this._enclosing.bucket, this.ResolveKey(path));
			}

			/// <exception cref="System.IO.IOException"></exception>
			internal override OutputStream WriteFile(string path, ProgressMonitor monitor, string
				 monitorTask)
			{
				return this._enclosing.s3.BeginPut(this._enclosing.bucket, this.ResolveKey(path), 
					monitor, monitorTask);
			}

			/// <exception cref="System.IO.IOException"></exception>
			internal override void WriteFile(string path, byte[] data)
			{
				this._enclosing.s3.Put(this._enclosing.bucket, this.ResolveKey(path), data);
			}

			/// <exception cref="NGit.Errors.TransportException"></exception>
			internal virtual IDictionary<string, Ref> ReadAdvertisedRefs()
			{
				SortedDictionary<string, Ref> avail = new SortedDictionary<string, Ref>();
				this.ReadPackedRefs(avail);
				this.ReadLooseRefs(avail);
				this.ReadRef(avail, Constants.HEAD);
				return avail;
			}

			/// <exception cref="NGit.Errors.TransportException"></exception>
			private void ReadLooseRefs(SortedDictionary<string, Ref> avail)
			{
				try
				{
					foreach (string n in this._enclosing.s3.List(this._enclosing.bucket, this.ResolveKey
						(WalkRemoteObjectDatabase.ROOT_DIR + "refs")))
					{
						this.ReadRef(avail, "refs/" + n);
					}
				}
				catch (IOException e)
				{
					throw new TransportException(this.GetURI(), JGitText.Get().cannotListRefs, e);
				}
			}

			/// <exception cref="NGit.Errors.TransportException"></exception>
			private Ref ReadRef(SortedDictionary<string, Ref> avail, string rn)
			{
				string s;
				string @ref = WalkRemoteObjectDatabase.ROOT_DIR + rn;
				try
				{
					BufferedReader br = this.OpenReader(@ref);
					try
					{
						s = br.ReadLine();
					}
					finally
					{
						br.Close();
					}
				}
				catch (FileNotFoundException)
				{
					return null;
				}
				catch (IOException err)
				{
					throw new TransportException(this.GetURI(), MessageFormat.Format(JGitText.Get().transportExceptionReadRef
						, @ref), err);
				}
				if (s == null)
				{
					throw new TransportException(this.GetURI(), MessageFormat.Format(JGitText.Get().transportExceptionEmptyRef
						, rn));
				}
				if (s.StartsWith("ref: "))
				{
					string target = Sharpen.Runtime.Substring(s, "ref: ".Length);
					Ref r = avail.Get(target);
					if (r == null)
					{
						r = this.ReadRef(avail, target);
					}
					if (r == null)
					{
						r = new ObjectIdRef.Unpeeled(RefStorage.NEW, target, null);
					}
					r = new SymbolicRef(rn, r);
					avail.Put(r.GetName(), r);
					return r;
				}
				if (ObjectId.IsId(s))
				{
					Ref r = new ObjectIdRef.Unpeeled(this.Loose(avail.Get(rn)), rn, ObjectId.FromString
						(s));
					avail.Put(r.GetName(), r);
					return r;
				}
				throw new TransportException(this.GetURI(), MessageFormat.Format(JGitText.Get().transportExceptionBadRef
					, rn, s));
			}

			private RefStorage Loose(Ref r)
			{
				if (r != null && r.GetStorage() == RefStorage.PACKED)
				{
					return RefStorage.LOOSE_PACKED;
				}
				return RefStorage.LOOSE;
			}

			internal override void Close()
			{
			}

			private readonly TransportAmazonS3 _enclosing;
			// We do not maintain persistent connections.
		}
	}
}
