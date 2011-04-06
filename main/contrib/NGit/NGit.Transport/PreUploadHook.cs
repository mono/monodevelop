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
using NGit;
using NGit.Transport;
using Sharpen;

namespace NGit.Transport
{
	/// <summary>
	/// Hook invoked by
	/// <see cref="UploadPack">UploadPack</see>
	/// before during critical phases.
	/// <p>
	/// If any hook function throws
	/// <see cref="UploadPackMayNotContinueException">UploadPackMayNotContinueException</see>
	/// then
	/// processing stops immediately and the exception is thrown up the call stack.
	/// Most phases of UploadPack will try to report the exception's message text to
	/// the end-user over the client's protocol connection.
	/// </summary>
	public abstract class PreUploadHook
	{
		private sealed class _PreUploadHook_60 : PreUploadHook
		{
			public _PreUploadHook_60()
			{
			}

			/// <exception cref="NGit.Transport.UploadPackMayNotContinueException"></exception>
			public override void OnPreAdvertiseRefs(UploadPack up)
			{
			}

			// Do nothing.
			/// <exception cref="NGit.Transport.UploadPackMayNotContinueException"></exception>
			public override void OnBeginNegotiateRound<_T0>(UploadPack up, ICollection<_T0> wants
				, int cntOffered)
			{
			}

			// Do nothing.
			/// <exception cref="NGit.Transport.UploadPackMayNotContinueException"></exception>
			public override void OnEndNegotiateRound<_T0>(UploadPack up, ICollection<_T0> wants
				, int cntCommon, int cntNotFound, bool ready)
			{
			}

			// Do nothing.
			/// <exception cref="NGit.Transport.UploadPackMayNotContinueException"></exception>
			public override void OnSendPack<_T0, _T1>(UploadPack up, ICollection<_T0> wants, 
				ICollection<_T1> haves)
			{
			}
		}

		/// <summary>A simple no-op hook.</summary>
		/// <remarks>A simple no-op hook.</remarks>
		public static readonly PreUploadHook NULL = new _PreUploadHook_60();

		// Do nothing.
		/// <summary>
		/// Invoked just before
		/// <see cref="UploadPack.SendAdvertisedRefs(RefAdvertiser)">UploadPack.SendAdvertisedRefs(RefAdvertiser)
		/// 	</see>
		/// .
		/// </summary>
		/// <param name="up">the upload pack instance handling the connection.</param>
		/// <exception cref="UploadPackMayNotContinueException">abort; the message will be sent to the user.
		/// 	</exception>
		/// <exception cref="NGit.Transport.UploadPackMayNotContinueException"></exception>
		public abstract void OnPreAdvertiseRefs(UploadPack up);

		/// <summary>Invoked before negotiation round is started.</summary>
		/// <remarks>Invoked before negotiation round is started.</remarks>
		/// <param name="up">the upload pack instance handling the connection.</param>
		/// <param name="wants">the list of wanted objects.</param>
		/// <param name="cntOffered">number of objects the client has offered.</param>
		/// <exception cref="UploadPackMayNotContinueException">abort; the message will be sent to the user.
		/// 	</exception>
		/// <exception cref="NGit.Transport.UploadPackMayNotContinueException"></exception>
		public abstract void OnBeginNegotiateRound<_T0>(UploadPack up, ICollection<_T0> wants
			, int cntOffered) where _T0:ObjectId;

		/// <summary>Invoked after a negotiation round is completed.</summary>
		/// <remarks>Invoked after a negotiation round is completed.</remarks>
		/// <param name="up">the upload pack instance handling the connection.</param>
		/// <param name="wants">the list of wanted objects.</param>
		/// <param name="cntCommon">
		/// number of objects this round found to be common. In a smart
		/// HTTP transaction this includes the objects that were
		/// previously found to be common.
		/// </param>
		/// <param name="cntNotFound">
		/// number of objects in this round the local repository does not
		/// have, but that were offered as potential common bases.
		/// </param>
		/// <param name="ready">
		/// true if a pack is ready to be sent (the commit graph was
		/// successfully cut).
		/// </param>
		/// <exception cref="UploadPackMayNotContinueException">abort; the message will be sent to the user.
		/// 	</exception>
		/// <exception cref="NGit.Transport.UploadPackMayNotContinueException"></exception>
		public abstract void OnEndNegotiateRound<_T0>(UploadPack up, ICollection<_T0> wants
			, int cntCommon, int cntNotFound, bool ready) where _T0:ObjectId;

		/// <summary>Invoked just before a pack will be sent to the client.</summary>
		/// <remarks>Invoked just before a pack will be sent to the client.</remarks>
		/// <param name="up">the upload pack instance handling the connection.</param>
		/// <param name="wants">
		/// the list of wanted objects. These may be RevObject or
		/// RevCommit if the processed parsed them. Implementors should
		/// not rely on the values being parsed.
		/// </param>
		/// <param name="haves">
		/// the list of common objects. Empty on an initial clone request.
		/// These may be RevObject or RevCommit if the processed parsed
		/// them. Implementors should not rely on the values being parsed.
		/// </param>
		/// <exception cref="UploadPackMayNotContinueException">abort; the message will be sent to the user.
		/// 	</exception>
		/// <exception cref="NGit.Transport.UploadPackMayNotContinueException"></exception>
		public abstract void OnSendPack<_T0, _T1>(UploadPack up, ICollection<_T0> wants, 
			ICollection<_T1> haves) where _T0:ObjectId where _T1:ObjectId;
	}
}
