/*
 * Copyright (C) 2008-2009, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2008-2010, Google Inc.
 * Copyright (C) 2010, Henon <meinrad.recheis@gmail.com>
 *
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or
 * without modification, are permitted provided that the following
 * conditions are met:
 *
 * - Redistributions of source code must retain the above copyright
 *   notice, this list of conditions and the following disclaimer.
 *
 * - Redistributions in binary form must reproduce the above
 *   copyright notice, this list of conditions and the following
 *   disclaimer in the documentation and/or other materials provided
 *   with the distribution.
 *
 * - Neither the name of the Git Development Community nor the
 *   names of its contributors may be used to endorse or promote
 *   products derived from this software without specific prior
 *   written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
 * CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
 * STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
 * ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System.IO;

namespace GitSharp.Core.Transport
{
	/// <summary>
	/// Write Git style pkt-line formatting to an output stream.
	/// <para/>
	/// This class is not thread safe and may issue multiple writes to the underlying
	/// stream for each method call made.
	/// <para/>
	/// This class performs no buffering on its own. This makes it suitable to
	/// interleave writes performed by this class with writes performed directly
	/// against the underlying OutputStream.
	/// </summary>
	public class PacketLineOut
	{
		private readonly Stream _out;
		private readonly byte[] _lenbuffer;

		/// <summary>
		/// Create a new packet line writer.
		/// </summary>
		/// <param name="outputStream">stream</param>
		public PacketLineOut(Stream outputStream)
		{
			_out = outputStream;
			_lenbuffer = new byte[5];
		}

		/// <summary>
		/// Write a UTF-8 encoded string as a single length-delimited packet.
		/// </summary>
		/// <param name="s">string to write.</param>
		public void WriteString(string s)
		{
			WritePacket(Constants.encode(s));
		}

		/// <summary>
		/// Write a binary packet to the stream.
		/// </summary>
		/// <param name="packet">
		/// the packet to write; the length of the packet is equal to the
		/// size of the byte array.
		/// </param>
		public void WritePacket(byte[] packet)
		{
			FormatLength(packet.Length + 4);
			_out.Write(_lenbuffer, 0, 4);
			_out.Write(packet, 0, packet.Length);
		}

		/// <summary>
		/// Write a packet end marker, sometimes referred to as a flush command.
		/// <para/>
		/// Technically this is a magical packet type which can be detected
		/// separately from an empty string or an empty packet.
		/// <para/>
		/// Implicitly performs a flush on the underlying OutputStream to ensure the
		/// peer will receive all data written thus far.
		/// </summary>
		public void End()
		{
			FormatLength(0);
			_out.Write(_lenbuffer, 0, 4);
			Flush();
		}

		/// <summary>
		/// Flush the underlying OutputStream.
		/// <para/>
		/// Performs a flush on the underlying OutputStream to ensure the peer will
		/// receive all data written thus far.
		/// </summary>
		public void Flush()
		{
			_out.Flush();
		}

		private static readonly char[] hexchar = new[]
                                                     {
                                                         '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c',
                                                         'd', 'e', 'f'
                                                     };

		private void FormatLength(int w)
		{
			FormatLength(_lenbuffer, w);
		}

		public static void FormatLength(byte[] lenbuffer, int w)
		{
			int o = 3;
			while (o >= 0 && w != 0)
			{
				lenbuffer[o--] = (byte)hexchar[w & 0xf];
				w = (int)(((uint)w) >> 4);
			}
			while (o >= 0)
				lenbuffer[o--] = (byte)'0';
		}
	}

}