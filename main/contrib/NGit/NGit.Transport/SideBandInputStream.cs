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
using System.IO;
using NGit;
using NGit.Errors;
using NGit.Transport;
using NGit.Util;
using Sharpen;

namespace NGit.Transport
{
	/// <summary>Unmultiplexes the data portion of a side-band channel.</summary>
	/// <remarks>
	/// Unmultiplexes the data portion of a side-band channel.
	/// <p>
	/// Reading from this input stream obtains data from channel 1, which is
	/// typically the bulk data stream.
	/// <p>
	/// Channel 2 is transparently unpacked and "scraped" to update a progress
	/// monitor. The scraping is performed behind the scenes as part of any of the
	/// read methods offered by this stream.
	/// <p>
	/// Channel 3 results in an exception being thrown, as the remote side has issued
	/// an unrecoverable error.
	/// </remarks>
	/// <seealso cref="SideBandOutputStream">SideBandOutputStream</seealso>
	internal class SideBandInputStream : InputStream
	{
		private static readonly string PFX_REMOTE = JGitText.Get().prefixRemote;

		internal const int CH_DATA = 1;

		internal const int CH_PROGRESS = 2;

		internal const int CH_ERROR = 3;

		private static Sharpen.Pattern P_UNBOUNDED = Sharpen.Pattern.Compile("^([\\w ]+): +(\\d+)(?:, done\\.)? *[\r\n]$"
			);

		private static Sharpen.Pattern P_BOUNDED = Sharpen.Pattern.Compile("^([\\w ]+): +\\d+% +\\( *(\\d+)/ *(\\d+)\\)(?:, done\\.)? *[\r\n]$"
			);

		private readonly InputStream rawIn;

		private readonly PacketLineIn pckIn;

		private readonly ProgressMonitor monitor;

		private readonly TextWriter messages;

		private string progressBuffer = string.Empty;

		private string currentTask;

		private int lastCnt;

		private bool eof;

		private int channel;

		private int available;

		internal SideBandInputStream(InputStream @in, ProgressMonitor progress, TextWriter
			 messageStream)
		{
			rawIn = @in;
			pckIn = new PacketLineIn(rawIn);
			monitor = progress;
			messages = messageStream;
			currentTask = string.Empty;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override int Read()
		{
			NeedDataPacket();
			if (eof)
			{
				return -1;
			}
			available--;
			return rawIn.Read();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override int Read(byte[] b, int off, int len)
		{
			int r = 0;
			while (len > 0)
			{
				NeedDataPacket();
				if (eof)
				{
					break;
				}
				int n = rawIn.Read(b, off, Math.Min(len, available));
				if (n < 0)
				{
					break;
				}
				r += n;
				off += n;
				len -= n;
				available -= n;
			}
			return eof && r == 0 ? -1 : r;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void NeedDataPacket()
		{
			if (eof || (channel == CH_DATA && available > 0))
			{
				return;
			}
			for (; ; )
			{
				available = pckIn.ReadLength();
				if (available == 0)
				{
					eof = true;
					return;
				}
				channel = rawIn.Read() & unchecked((int)(0xff));
				available -= SideBandOutputStream.HDR_SIZE;
				// length header plus channel indicator
				if (available == 0)
				{
					continue;
				}
				switch (channel)
				{
					case CH_DATA:
					{
						return;
					}

					case CH_PROGRESS:
					{
						Progress(ReadString(available));
						continue;
						goto case CH_ERROR;
					}

					case CH_ERROR:
					{
						eof = true;
						throw new TransportException(PFX_REMOTE + ReadString(available));
					}

					default:
					{
						throw new PackProtocolException(MessageFormat.Format(JGitText.Get().invalidChannel
							, channel));
					}
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void Progress(string pkt)
		{
			pkt = progressBuffer + pkt;
			for (; ; )
			{
				int lf = pkt.IndexOf('\n');
				int cr = pkt.IndexOf('\r');
				int s;
				if (0 <= lf && 0 <= cr)
				{
					s = Math.Min(lf, cr);
				}
				else
				{
					if (0 <= lf)
					{
						s = lf;
					}
					else
					{
						if (0 <= cr)
						{
							s = cr;
						}
						else
						{
							break;
						}
					}
				}
				DoProgressLine(Sharpen.Runtime.Substring(pkt, 0, s + 1));
				pkt = Sharpen.Runtime.Substring(pkt, s + 1);
			}
			progressBuffer = pkt;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void DoProgressLine(string msg)
		{
			Matcher matcher;
			matcher = P_BOUNDED.Matcher(msg);
			if (matcher.Matches())
			{
				string taskname = matcher.Group(1);
				if (!currentTask.Equals(taskname))
				{
					currentTask = taskname;
					lastCnt = 0;
					BeginTask(System.Convert.ToInt32(matcher.Group(3)));
				}
				int cnt = System.Convert.ToInt32(matcher.Group(2));
				monitor.Update(cnt - lastCnt);
				lastCnt = cnt;
				return;
			}
			matcher = P_UNBOUNDED.Matcher(msg);
			if (matcher.Matches())
			{
				string taskname = matcher.Group(1);
				if (!currentTask.Equals(taskname))
				{
					currentTask = taskname;
					lastCnt = 0;
					BeginTask(ProgressMonitor.UNKNOWN);
				}
				int cnt = System.Convert.ToInt32(matcher.Group(2));
				monitor.Update(cnt - lastCnt);
				lastCnt = cnt;
				return;
			}
			messages.Write(msg);
		}

		private void BeginTask(int totalWorkUnits)
		{
			monitor.BeginTask(PFX_REMOTE + currentTask, totalWorkUnits);
		}

		/// <exception cref="System.IO.IOException"></exception>
		private string ReadString(int len)
		{
			byte[] raw = new byte[len];
			IOUtil.ReadFully(rawIn, raw, 0, len);
			return RawParseUtils.Decode(Constants.CHARSET, raw, 0, len);
		}
	}
}
