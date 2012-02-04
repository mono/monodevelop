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
using System.Text;
using NGit;
using NGit.Transport;
using NGit.Util;
using Sharpen;

namespace NGit.Transport
{
	/// <summary>
	/// Support for the start of
	/// <see cref="UploadPack">UploadPack</see>
	/// and
	/// <see cref="ReceivePack">ReceivePack</see>
	/// .
	/// </summary>
	public abstract class RefAdvertiser
	{
		/// <summary>
		/// Advertiser which frames lines in a
		/// <see cref="PacketLineOut">PacketLineOut</see>
		/// format.
		/// </summary>
		public class PacketLineOutRefAdvertiser : RefAdvertiser
		{
			private readonly PacketLineOut pckOut;

			/// <summary>Create a new advertiser for the supplied stream.</summary>
			/// <remarks>Create a new advertiser for the supplied stream.</remarks>
			/// <param name="out">the output stream.</param>
			public PacketLineOutRefAdvertiser(PacketLineOut @out)
			{
				pckOut = @out;
			}

			/// <exception cref="System.IO.IOException"></exception>
			protected internal override void WriteOne(CharSequence line)
			{
				pckOut.WriteString(line.ToString());
			}

			/// <exception cref="System.IO.IOException"></exception>
			protected internal override void End()
			{
				pckOut.End();
			}
		}

		private readonly StringBuilder tmpLine = new StringBuilder(100);

		private readonly char[] tmpId = new char[Constants.OBJECT_ID_STRING_LENGTH];

		private readonly ICollection<string> capablities = new LinkedHashSet<string>();

		private readonly ICollection<ObjectId> sent = new HashSet<ObjectId>();

		private Repository repository;

		private bool derefTags;

		private bool first = true;

		/// <summary>Initialize this advertiser with a repository for peeling tags.</summary>
		/// <remarks>Initialize this advertiser with a repository for peeling tags.</remarks>
		/// <param name="src">the repository to read from.</param>
		public virtual void Init(Repository src)
		{
			repository = src;
		}

		/// <summary>Toggle tag peeling.</summary>
		/// <remarks>
		/// Toggle tag peeling.
		/// <p>
		/// <p>
		/// This method must be invoked prior to any of the following:
		/// <ul>
		/// <li>
		/// <see cref="Send(System.Collections.Generic.IDictionary{K, V})">Send(System.Collections.Generic.IDictionary&lt;K, V&gt;)
		/// 	</see>
		/// </ul>
		/// </remarks>
		/// <param name="deref">
		/// true to show the dereferenced value of a tag as the special
		/// ref <code>$tag^{}</code> ; false to omit it from the output.
		/// </param>
		public virtual void SetDerefTags(bool deref)
		{
			derefTags = deref;
		}

		/// <summary>Add one protocol capability to the initial advertisement.</summary>
		/// <remarks>
		/// Add one protocol capability to the initial advertisement.
		/// <p>
		/// This method must be invoked prior to any of the following:
		/// <ul>
		/// <li>
		/// <see cref="Send(System.Collections.Generic.IDictionary{K, V})">Send(System.Collections.Generic.IDictionary&lt;K, V&gt;)
		/// 	</see>
		/// <li>
		/// <see cref="AdvertiseHave(NGit.AnyObjectId)">AdvertiseHave(NGit.AnyObjectId)</see>
		/// </ul>
		/// </remarks>
		/// <param name="name">
		/// the name of a single protocol capability supported by the
		/// caller. The set of capabilities are sent to the client in the
		/// advertisement, allowing the client to later selectively enable
		/// features it recognizes.
		/// </param>
		public virtual void AdvertiseCapability(string name)
		{
			capablities.AddItem(name);
		}

		/// <summary>Format an advertisement for the supplied refs.</summary>
		/// <remarks>Format an advertisement for the supplied refs.</remarks>
		/// <param name="refs">
		/// zero or more refs to format for the client. The collection is
		/// sorted before display if necessary, and therefore may appear
		/// in any order.
		/// </param>
		/// <returns>set of ObjectIds that were advertised to the client.</returns>
		/// <exception cref="System.IO.IOException">
		/// the underlying output stream failed to write out an
		/// advertisement record.
		/// </exception>
		public virtual ICollection<ObjectId> Send(IDictionary<string, Ref> refs)
		{
			foreach (Ref refit in GetSortedRefs(refs))
			{
				Ref @ref = refit;
				if (@ref.GetObjectId() == null)
				{
					continue;
				}
				AdvertiseAny(@ref.GetObjectId(), @ref.GetName());
				if (!derefTags)
				{
					continue;
				}
				if (!@ref.IsPeeled())
				{
					if (repository == null)
					{
						continue;
					}
					@ref = repository.Peel(@ref);
				}
				if (@ref.GetPeeledObjectId() != null)
				{
					AdvertiseAny(@ref.GetPeeledObjectId(), @ref.GetName() + "^{}");
				}
			}
			return sent;
		}

		private Iterable<Ref> GetSortedRefs(IDictionary<string, Ref> all)
		{
			if (all is RefMap || (all is SortedDictionary<string,Ref>))
			{
				return all.Values.AsIterable ();
			}
			return RefComparator.Sort(all.Values).AsIterable ();
		}

		/// <summary>
		/// Advertise one object is available using the magic
		/// <code>.have</code>
		/// .
		/// <p>
		/// The magic
		/// <code>.have</code>
		/// advertisement is not available for fetching by a
		/// client, but can be used by a client when considering a delta base
		/// candidate before transferring data in a push. Within the record created
		/// by this method the ref name is simply the invalid string
		/// <code>.have</code>
		/// .
		/// </summary>
		/// <param name="id">identity of the object that is assumed to exist.</param>
		/// <exception cref="System.IO.IOException">
		/// the underlying output stream failed to write out an
		/// advertisement record.
		/// </exception>
		public virtual void AdvertiseHave(AnyObjectId id)
		{
			AdvertiseAnyOnce(id, ".have");
		}

		/// <returns>true if no advertisements have been sent yet.</returns>
		public virtual bool IsEmpty()
		{
			return first;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void AdvertiseAnyOnce(AnyObjectId obj, string refName)
		{
			if (!sent.Contains(obj))
			{
				AdvertiseAny(obj, refName);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void AdvertiseAny(AnyObjectId obj, string refName)
		{
			sent.AddItem(obj.ToObjectId());
			AdvertiseId(obj, refName);
		}

		/// <summary>Advertise one object under a specific name.</summary>
		/// <remarks>
		/// Advertise one object under a specific name.
		/// <p>
		/// If the advertised object is a tag, this method does not advertise the
		/// peeled version of it.
		/// </remarks>
		/// <param name="id">the object to advertise.</param>
		/// <param name="refName">
		/// name of the reference to advertise the object as, can be any
		/// string not including the NUL byte.
		/// </param>
		/// <exception cref="System.IO.IOException">
		/// the underlying output stream failed to write out an
		/// advertisement record.
		/// </exception>
		public virtual void AdvertiseId(AnyObjectId id, string refName)
		{
			tmpLine.Length = 0;
			id.CopyTo(tmpId, tmpLine);
			tmpLine.Append(' ');
			tmpLine.Append(refName);
			if (first)
			{
				first = false;
				if (!capablities.IsEmpty())
				{
					tmpLine.Append('\0');
					foreach (string capName in capablities)
					{
						tmpLine.Append(' ');
						tmpLine.Append(capName);
					}
					tmpLine.Append(' ');
				}
			}
			tmpLine.Append('\n');
			WriteOne(tmpLine);
		}

		/// <summary>Write a single advertisement line.</summary>
		/// <remarks>Write a single advertisement line.</remarks>
		/// <param name="line">
		/// the advertisement line to be written. The line always ends
		/// with LF. Never null or the empty string.
		/// </param>
		/// <exception cref="System.IO.IOException">
		/// the underlying output stream failed to write out an
		/// advertisement record.
		/// </exception>
		protected internal abstract void WriteOne(CharSequence line);

		/// <summary>Mark the end of the advertisements.</summary>
		/// <remarks>Mark the end of the advertisements.</remarks>
		/// <exception cref="System.IO.IOException">
		/// the underlying output stream failed to write out an
		/// advertisement record.
		/// </exception>
		protected internal abstract void End();
	}
}
