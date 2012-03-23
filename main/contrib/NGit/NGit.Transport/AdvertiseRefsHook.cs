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

using NGit.Transport;
using Sharpen;

namespace NGit.Transport
{
	/// <summary>Hook to allow callers to take over advertising refs to the client.</summary>
	/// <remarks>Hook to allow callers to take over advertising refs to the client.</remarks>
	public abstract class AdvertiseRefsHook
	{
		private sealed class _AdvertiseRefsHook_54 : AdvertiseRefsHook
		{
			public _AdvertiseRefsHook_54()
			{
			}

			public override void AdvertiseRefs(UploadPack uploadPack)
			{
			}

			// Do nothing.
			public override void AdvertiseRefs(ReceivePack receivePack)
			{
			}
		}

		/// <summary>A simple hook that advertises the default refs.</summary>
		/// <remarks>
		/// A simple hook that advertises the default refs.
		/// <p>
		/// The method implementations do nothing to preserve the default behavior; see
		/// <see cref="UploadPack.SetAdvertisedRefs(System.Collections.Generic.IDictionary{K, V})
		/// 	">UploadPack.SetAdvertisedRefs(System.Collections.Generic.IDictionary&lt;K, V&gt;)
		/// 	</see>
		/// and
		/// <see cref="ReceivePack.SetAdvertisedRefs(System.Collections.Generic.IDictionary{K, V}, System.Collections.Generic.ICollection{E})
		/// 	">ReceivePack.SetAdvertisedRefs(System.Collections.Generic.IDictionary&lt;K, V&gt;, System.Collections.Generic.ICollection&lt;E&gt;)
		/// 	</see>
		/// .
		/// </remarks>
		public static AdvertiseRefsHook DEFAULT = new _AdvertiseRefsHook_54();

		// Do nothing.
		/// <summary>Advertise refs for upload-pack.</summary>
		/// <remarks>Advertise refs for upload-pack.</remarks>
		/// <param name="uploadPack">
		/// instance on which to call
		/// <see cref="UploadPack.SetAdvertisedRefs(System.Collections.Generic.IDictionary{K, V})
		/// 	">UploadPack.SetAdvertisedRefs(System.Collections.Generic.IDictionary&lt;K, V&gt;)
		/// 	</see>
		/// if necessary.
		/// </param>
		/// <exception cref="ServiceMayNotContinueException">abort; the message will be sent to the user.
		/// 	</exception>
		/// <exception cref="NGit.Transport.ServiceMayNotContinueException"></exception>
		public abstract void AdvertiseRefs(UploadPack uploadPack);

		/// <summary>Advertise refs for receive-pack.</summary>
		/// <remarks>Advertise refs for receive-pack.</remarks>
		/// <param name="receivePack">
		/// instance on which to call
		/// <see cref="ReceivePack.SetAdvertisedRefs(System.Collections.Generic.IDictionary{K, V}, System.Collections.Generic.ICollection{E})
		/// 	">ReceivePack.SetAdvertisedRefs(System.Collections.Generic.IDictionary&lt;K, V&gt;, System.Collections.Generic.ICollection&lt;E&gt;)
		/// 	</see>
		/// if necessary.
		/// </param>
		/// <exception cref="ServiceMayNotContinueException">abort; the message will be sent to the user.
		/// 	</exception>
		/// <exception cref="NGit.Transport.ServiceMayNotContinueException"></exception>
		public abstract void AdvertiseRefs(ReceivePack receivePack);
	}
}
