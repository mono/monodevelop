/*
 * Copyright (C) 2008, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
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

using System;
using System.IO;
using System.Text.RegularExpressions;
using GitSharp.Core.Exceptions;
using GitSharp.Core.Util;

namespace GitSharp.Core.Transport
{

	/// <summary>
	/// Unmultiplexes the data portion of a side-band channel.
	/// <para/>
	/// Reading from this input stream obtains data from channel 1, which is
	/// typically the bulk data stream.
	/// <para/>
	/// Channel 2 is transparently unpacked and "scraped" to update a progress
	/// monitor. The scraping is performed behind the scenes as part of any of the
	/// read methods offered by this stream.
	/// <para/>
	/// Channel 3 results in an exception being thrown, as the remote side has issued
	/// an unrecoverable error.
	///
	/// <see cref="SideBandOutputStream"/>
	///</summary>
	public class SideBandInputStream : Stream
	{
		private const string PFX_REMOTE = "remote: ";
		public const int CH_DATA = 1;
		public const int CH_PROGRESS = 2;
		public const int CH_ERROR = 3;

		private static readonly Regex P_UNBOUNDED = new Regex("^([\\w ]+): +(\\d+)(?:, done\\.)? *$", RegexOptions.Singleline);
		private static readonly Regex P_BOUNDED = new Regex("^([\\w ]+): +\\d+% +\\( *(\\d+)/ *(\\d+)\\)(?:, done\\.)? *$", RegexOptions.Singleline);

		private readonly Stream rawIn;
		private readonly PacketLineIn pckIn;
		private readonly ProgressMonitor monitor;
		private string progressBuffer;
		private string currentTask;
		private int lastCnt;
		private bool eof;
		private int channel;
		private int available;

		public SideBandInputStream(Stream @in, ProgressMonitor progress)
		{
			rawIn = @in;
			pckIn = new PacketLineIn(rawIn);
			monitor = progress;
			currentTask = string.Empty;
			progressBuffer = string.Empty;
		}

		#region --> Not supported stream interface members

		/// <summary>
		/// This is not needed, but we are forced to implement the interface
		/// </summary>
		public override void Flush()
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// This is not needed, but we are forced to implement the interface
		/// </summary>
		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// This is not needed, but we are forced to implement the interface
		/// </summary>
		public override void SetLength(long value)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// This is not needed, but we are forced to implement the interface
		/// </summary>
		public override void Write(byte[] buffer, int offset, int count)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// This is not needed, but we are forced to implement the interface
		/// </summary>
		public override bool CanRead
		{
			get { return true; }
		}

		/// <summary>
		/// This is not needed, but we are forced to implement the interface
		/// </summary>
		public override bool CanSeek
		{
			get { return false; }
		}

		/// <summary>
		/// This is not needed, but we are forced to implement the interface
		/// </summary>
		public override bool CanWrite
		{
			get
			{
				return false;
			}
		}

		/// <summary>
		/// This is not needed, but we are forced to implement the interface
		/// </summary>
		public override long Length
		{
			get { throw new NotSupportedException(); }
		}

		/// <summary>
		/// This is not needed, but we are forced to implement the interface
		/// </summary>
		public override long Position
		{
			get
			{
				throw new NotSupportedException();
			}
			set
			{
				throw new NotSupportedException();
			}
		}

#endregion

		public override int ReadByte()
		{
			needDataPacket();
			if (eof)
				return -1;
			available--;
			return rawIn.ReadByte();
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			int r = 0;
			while (count > 0)
			{
				needDataPacket();
				if (eof)
					break;
				int n = rawIn.Read(buffer, offset, Math.Min(count, available));
				if (n < 0)
					break;
				r += n;
				offset += n;
				count -= n;
				available -= n;
			}
			return eof && r == 0 ? -1 : r;
		}

		private void needDataPacket()
		{
			if (eof || (channel == CH_DATA && available > 0))
				return;
			for (; ; )
			{
				available = pckIn.ReadLength();
				if (available == 0)
				{
					eof = true;
					return;
				}

				channel = rawIn.ReadByte() & 0xff;
				available -= SideBandOutputStream.HDR_SIZE; // length header plus channel indicator
				if (available == 0)
					continue;

				switch (channel)
				{
					case CH_DATA:
						return;
					case CH_PROGRESS:
						progress(readString(available));
						continue;
					case CH_ERROR:
						eof = true;
						throw new TransportException(PFX_REMOTE + readString(available));
					default:
						throw new TransportException("Invalid channel " + channel);
				}
			}
		}

		private void progress(string pkt)
		{
			pkt = progressBuffer + pkt;
			for (; ; )
			{
				int lf = pkt.IndexOf('\n');
				int cr = pkt.IndexOf('\r');
				int s;
				if (0 <= lf && 0 <= cr)
					s = Math.Min(lf, cr);
				else if (0 <= lf)
					s = lf;
				else if (0 <= cr)
					s = cr;
				else
					break;

				string msg = pkt.Slice(0, s);
				if (doProgressLine(msg))
					pkt = pkt.Substring(s + 1);
				else
					break;
			}
			progressBuffer = pkt;
		}

		private bool doProgressLine(string msg)
		{
			Match matcher = P_BOUNDED.Match(msg);
			if (matcher.Success)
			{
				string taskname = matcher.Groups[1].Value;
				if (!currentTask.Equals(taskname))
				{
					currentTask = taskname;
					lastCnt = 0;
					beginTask(int.Parse(matcher.Groups[3].Value));
				}
				int cnt = int.Parse(matcher.Groups[2].Value);
				monitor.Update(cnt - lastCnt);
				lastCnt = cnt;
				return true;
			}

			matcher = P_UNBOUNDED.Match(msg);
			if (matcher.Success)
			{
				string taskname = matcher.Groups[1].Value;
				if (!currentTask.Equals(taskname))
				{
					currentTask = taskname;
					lastCnt = 0;
					beginTask(ProgressMonitor.UNKNOWN);
				}
				int cnt = int.Parse(matcher.Groups[2].Value);
				monitor.Update(cnt - lastCnt);
				lastCnt = cnt;
				return true;
			}

			return false;
		}

		private void beginTask(int totalWorkUnits)
		{
			monitor.BeginTask(PFX_REMOTE + currentTask, totalWorkUnits);
		}

		private string readString(int len)
		{
			byte[] raw = new byte[len];
			IO.ReadFully(rawIn, raw, 0, len);
			return RawParseUtils.decode(Constants.CHARSET, raw, 0, len);
		}
	}
}