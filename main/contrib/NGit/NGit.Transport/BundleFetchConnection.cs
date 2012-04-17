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

using System.Collections.Generic;
using System.IO;
using NGit;
using NGit.Errors;
using NGit.Internal;
using NGit.Revwalk;
using NGit.Storage.File;
using NGit.Transport;
using NGit.Util;
using Sharpen;

namespace NGit.Transport
{
	/// <summary>Fetch connection for bundle based classes.</summary>
	/// <remarks>
	/// Fetch connection for bundle based classes. It used by
	/// instances of
	/// <see cref="TransportBundle">TransportBundle</see>
	/// </remarks>
	internal class BundleFetchConnection : BaseFetchConnection
	{
		private readonly NGit.Transport.Transport transport;

		internal InputStream bin;

		internal readonly IDictionary<ObjectId, string> prereqs = new Dictionary<ObjectId
			, string>();

		private string lockMessage;

		private PackLock packLock;

		/// <exception cref="NGit.Errors.TransportException"></exception>
		internal BundleFetchConnection(NGit.Transport.Transport transportBundle, InputStream
			 src)
		{
			transport = transportBundle;
			bin = new BufferedInputStream(src);
			try
			{
				switch (ReadSignature())
				{
					case 2:
					{
						ReadBundleV2();
						break;
					}

					default:
					{
						throw new TransportException(transport.uri, JGitText.Get().notABundle);
					}
				}
			}
			catch (TransportException err)
			{
				Close();
				throw;
			}
			catch (IOException err)
			{
				Close();
				throw new TransportException(transport.uri, err.Message, err);
			}
			catch (RuntimeException err)
			{
				Close();
				throw new TransportException(transport.uri, err.Message, err);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private int ReadSignature()
		{
			string rev = ReadLine(new byte[1024]);
			if (NGit.Transport.TransportBundleConstants.V2_BUNDLE_SIGNATURE.Equals(rev))
			{
				return 2;
			}
			throw new TransportException(transport.uri, JGitText.Get().notABundle);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void ReadBundleV2()
		{
			byte[] hdrbuf = new byte[1024];
			LinkedHashMap<string, Ref> avail = new LinkedHashMap<string, Ref>();
			for (; ; )
			{
				string line = ReadLine(hdrbuf);
				if (line.Length == 0)
				{
					break;
				}
				if (line[0] == '-')
				{
					ObjectId id = ObjectId.FromString(Sharpen.Runtime.Substring(line, 1, 41));
					string shortDesc = null;
					if (line.Length > 42)
					{
						shortDesc = Sharpen.Runtime.Substring(line, 42);
					}
					prereqs.Put(id, shortDesc);
					continue;
				}
				string name = Sharpen.Runtime.Substring(line, 41, line.Length);
				ObjectId id_1 = ObjectId.FromString(Sharpen.Runtime.Substring(line, 0, 40));
				Ref prior = avail.Put(name, new ObjectIdRef.Unpeeled(RefStorage.NETWORK, name, id_1
					));
				if (prior != null)
				{
					throw DuplicateAdvertisement(name);
				}
			}
			Available(avail);
		}

		private PackProtocolException DuplicateAdvertisement(string name)
		{
			return new PackProtocolException(transport.uri, MessageFormat.Format(JGitText.Get
				().duplicateAdvertisementsOf, name));
		}

		/// <exception cref="System.IO.IOException"></exception>
		private string ReadLine(byte[] hdrbuf)
		{
			bin.Mark(hdrbuf.Length);
			int cnt = bin.Read(hdrbuf);
			int lf = 0;
			while (lf < cnt && hdrbuf[lf] != '\n')
			{
				lf++;
			}
			bin.Reset();
			IOUtil.SkipFully(bin, lf);
			if (lf < cnt && hdrbuf[lf] == '\n')
			{
				IOUtil.SkipFully(bin, 1);
			}
			return RawParseUtils.Decode(Constants.CHARSET, hdrbuf, 0, lf);
		}

		public override bool DidFetchTestConnectivity()
		{
			return false;
		}

		/// <exception cref="NGit.Errors.TransportException"></exception>
		protected internal override void DoFetch(ProgressMonitor monitor, ICollection<Ref
			> want, ICollection<ObjectId> have)
		{
			VerifyPrerequisites();
			try
			{
				ObjectInserter ins = transport.local.NewObjectInserter();
				try
				{
					PackParser parser = ins.NewPackParser(bin);
					parser.SetAllowThin(true);
					parser.SetObjectChecking(transport.IsCheckFetchedObjects());
					parser.SetLockMessage(lockMessage);
					packLock = parser.Parse(NullProgressMonitor.INSTANCE);
					ins.Flush();
				}
				finally
				{
					ins.Release();
				}
			}
			catch (IOException err)
			{
				Close();
				throw new TransportException(transport.uri, err.Message, err);
			}
			catch (RuntimeException err)
			{
				Close();
				throw new TransportException(transport.uri, err.Message, err);
			}
		}

		public override void SetPackLockMessage(string message)
		{
			lockMessage = message;
		}

		public override ICollection<PackLock> GetPackLocks()
		{
			if (packLock != null)
			{
				return Sharpen.Collections.Singleton(packLock);
			}
			return Sharpen.Collections.EmptyList<PackLock>();
		}

		/// <exception cref="NGit.Errors.TransportException"></exception>
		private void VerifyPrerequisites()
		{
			if (prereqs.IsEmpty())
			{
				return;
			}
			RevWalk rw = new RevWalk(transport.local);
			try
			{
				RevFlag PREREQ = rw.NewFlag("PREREQ");
				RevFlag SEEN = rw.NewFlag("SEEN");
				IDictionary<ObjectId, string> missing = new Dictionary<ObjectId, string>();
				IList<RevObject> commits = new AList<RevObject>();
				foreach (KeyValuePair<ObjectId, string> e in prereqs.EntrySet())
				{
					ObjectId p = e.Key;
					try
					{
						RevCommit c = rw.ParseCommit(p);
						if (!c.Has(PREREQ))
						{
							c.Add(PREREQ);
							commits.AddItem(c);
						}
					}
					catch (MissingObjectException)
					{
						missing.Put(p, e.Value);
					}
					catch (IOException err)
					{
						throw new TransportException(transport.uri, MessageFormat.Format(JGitText.Get().cannotReadCommit
							, p.Name), err);
					}
				}
				if (!missing.IsEmpty())
				{
					throw new MissingBundlePrerequisiteException(transport.uri, missing);
				}
				foreach (Ref r in transport.local.GetAllRefs().Values)
				{
					try
					{
						rw.MarkStart(rw.ParseCommit(r.GetObjectId()));
					}
					catch (IOException)
					{
					}
				}
				// If we cannot read the value of the ref skip it.
				int remaining = commits.Count;
				try
				{
					RevCommit c;
					while ((c = rw.Next()) != null)
					{
						if (c.Has(PREREQ))
						{
							c.Add(SEEN);
							if (--remaining == 0)
							{
								break;
							}
						}
					}
				}
				catch (IOException err)
				{
					throw new TransportException(transport.uri, JGitText.Get().cannotReadObject, err);
				}
				if (remaining > 0)
				{
					foreach (RevObject o in commits)
					{
						if (!o.Has(SEEN))
						{
							missing.Put(o, prereqs.Get(o));
						}
					}
					throw new MissingBundlePrerequisiteException(transport.uri, missing);
				}
			}
			finally
			{
				rw.Release();
			}
		}

		public override void Close()
		{
			if (bin != null)
			{
				try
				{
					bin.Close();
				}
				catch (IOException)
				{
				}
				finally
				{
					// Ignore close failures.
					bin = null;
				}
			}
		}
	}
}
