/*
Copyright (c) 2000,2001,2002,2003 ymnk, JCraft,Inc. All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

  1. Redistributions of source code must retain the above copyright notice,
     this list of conditions and the following disclaimer.

  2. Redistributions in binary form must reproduce the above copyright 
     notice, this list of conditions and the following disclaimer in 
     the documentation and/or other materials provided with the distribution.

  3. The names of the authors may not be used to endorse or promote products
     derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED ``AS IS'' AND ANY EXPRESSED OR IMPLIED WARRANTIES,
INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL JCRAFT,
INC. OR ANY CONTRIBUTORS TO THIS SOFTWARE BE LIABLE FOR ANY DIRECT, INDIRECT,
INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA,
OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE,
EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

This program is based on zlib-1.1.3, so all credit should go authors
Jean-loup Gailly(jloup@gzip.org) and Mark Adler(madler@alumni.caltech.edu)
and contributors of zlib.
*/

using NSch.ZLib;
using Sharpen;

namespace NSch.ZLib
{
	internal sealed class InfBlocks
	{
		private const int MANY = 1440;

		private static readonly int[] inflate_mask = new int[] { unchecked((int)(0x00000000
			)), unchecked((int)(0x00000001)), unchecked((int)(0x00000003)), unchecked((int)(
			0x00000007)), unchecked((int)(0x0000000f)), unchecked((int)(0x0000001f)), unchecked(
			(int)(0x0000003f)), unchecked((int)(0x0000007f)), unchecked((int)(0x000000ff)), 
			unchecked((int)(0x000001ff)), unchecked((int)(0x000003ff)), unchecked((int)(0x000007ff
			)), unchecked((int)(0x00000fff)), unchecked((int)(0x00001fff)), unchecked((int)(
			0x00003fff)), unchecked((int)(0x00007fff)), unchecked((int)(0x0000ffff)) };

		internal static readonly int[] border = new int[] { 16, 17, 18, 0, 8, 7, 9, 6, 10
			, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15 };

		private const int Z_OK = 0;

		private const int Z_STREAM_END = 1;

		private const int Z_NEED_DICT = 2;

		private const int Z_ERRNO = -1;

		private const int Z_STREAM_ERROR = -2;

		private const int Z_DATA_ERROR = -3;

		private const int Z_MEM_ERROR = -4;

		private const int Z_BUF_ERROR = -5;

		private const int Z_VERSION_ERROR = -6;

		private const int TYPE = 0;

		private const int LENS = 1;

		private const int STORED = 2;

		private const int TABLE = 3;

		private const int BTREE = 4;

		private const int DTREE = 5;

		private const int CODES = 6;

		private const int DRY = 7;

		private const int DONE = 8;

		private const int BAD = 9;

		internal int mode;

		internal int left;

		internal int table;

		internal int index;

		internal int[] blens;

		internal int[] bb = new int[1];

		internal int[] tb = new int[1];

		internal InfCodes codes = new InfCodes();

		internal int last;

		internal int bitk;

		internal int bitb;

		internal int[] hufts;

		internal byte[] window;

		internal int end;

		internal int read;

		internal int write;

		internal object checkfn;

		internal long check;

		internal InfTree inftree = new InfTree();

		internal InfBlocks(ZStream z, object checkfn, int w)
		{
			// And'ing with mask[n] masks the lower n bits
			// Table for deflate from PKZIP's appnote.txt.
			// Order of the bit length code lengths
			// get type bits (3, including end bit)
			// get lengths for stored
			// processing stored block
			// get table lengths
			// get bit lengths tree for a dynamic block
			// get length, distance trees for a dynamic block
			// processing fixed or dynamic block
			// output remaining window bytes
			// finished last block, done
			// ot a data error--stuck here
			// current inflate_block mode 
			// if STORED, bytes left to copy 
			// table lengths (14 bits) 
			// index into blens (or border) 
			// bit lengths of codes 
			// bit length tree depth 
			// bit length decoding tree 
			// if CODES, current state 
			// true if this block is the last block 
			// mode independent information 
			// bits in bit buffer 
			// bit buffer 
			// single malloc for tree space 
			// sliding window 
			// one byte after sliding window 
			// window read pointer 
			// window write pointer 
			// check function 
			// check on output 
			hufts = new int[MANY * 3];
			window = new byte[w];
			end = w;
			this.checkfn = checkfn;
			mode = TYPE;
			Reset(z, null);
		}

		internal void Reset(ZStream z, long[] c)
		{
			if (c != null)
			{
				c[0] = check;
			}
			if (mode == BTREE || mode == DTREE)
			{
			}
			if (mode == CODES)
			{
				codes.Free(z);
			}
			mode = TYPE;
			bitk = 0;
			bitb = 0;
			read = write = 0;
			if (checkfn != null)
			{
				z.adler = check = z._adler.Adler(0L, null, 0, 0);
			}
		}

		internal int Proc(ZStream z, int r)
		{
			int t;
			// temporary storage
			int b;
			// bit buffer
			int k;
			// bits in bit buffer
			int p;
			// input data pointer
			int n;
			// bytes available there
			int q;
			// output window write pointer
			int m;
			{
				// bytes to end of window or read pointer
				// copy input/output information to locals (UPDATE macro restores)
				p = z.next_in_index;
				n = z.avail_in;
				b = bitb;
				k = bitk;
			}
			{
				q = write;
				m = (int)(q < read ? read - q - 1 : end - q);
			}
			// process input based on current state
			while (true)
			{
				switch (mode)
				{
					case TYPE:
					{
						while (k < (3))
						{
							if (n != 0)
							{
								r = Z_OK;
							}
							else
							{
								bitb = b;
								bitk = k;
								z.avail_in = n;
								z.total_in += p - z.next_in_index;
								z.next_in_index = p;
								write = q;
								return Inflate_flush(z, r);
							}
							n--;
							b |= (z.next_in[p++] & unchecked((int)(0xff))) << k;
							k += 8;
						}
						t = (int)(b & 7);
						last = t & 1;
						switch ((int)(((uint)t) >> 1))
						{
							case 0:
							{
								// stored 
								b = (int)(((uint)b) >> (3));
								k -= (3);
								t = k & 7;
								// go to byte boundary
								b = (int)(((uint)b) >> (t));
								k -= (t);
								mode = LENS;
								// get length of stored block
								break;
							}

							case 1:
							{
								// fixed
								int[] bl = new int[1];
								int[] bd = new int[1];
								int[][] tl = new int[1][];
								int[][] td = new int[1][];
								InfTree.Inflate_trees_fixed(bl, bd, tl, td, z);
								codes.Init(bl[0], bd[0], tl[0], 0, td[0], 0, z);
								b = (int)(((uint)b) >> (3));
								k -= (3);
								mode = CODES;
								break;
							}

							case 2:
							{
								// dynamic
								b = (int)(((uint)b) >> (3));
								k -= (3);
								mode = TABLE;
								break;
							}

							case 3:
							{
								// illegal
								b = (int)(((uint)b) >> (3));
								k -= (3);
								mode = BAD;
								z.msg = "invalid block type";
								r = Z_DATA_ERROR;
								bitb = b;
								bitk = k;
								z.avail_in = n;
								z.total_in += p - z.next_in_index;
								z.next_in_index = p;
								write = q;
								return Inflate_flush(z, r);
							}
						}
						break;
					}

					case LENS:
					{
						while (k < (32))
						{
							if (n != 0)
							{
								r = Z_OK;
							}
							else
							{
								bitb = b;
								bitk = k;
								z.avail_in = n;
								z.total_in += p - z.next_in_index;
								z.next_in_index = p;
								write = q;
								return Inflate_flush(z, r);
							}
							n--;
							b |= (z.next_in[p++] & unchecked((int)(0xff))) << k;
							k += 8;
						}
						if ((((int)(((uint)(~b)) >> 16)) & unchecked((int)(0xffff))) != (b & unchecked((int
							)(0xffff))))
						{
							mode = BAD;
							z.msg = "invalid stored block lengths";
							r = Z_DATA_ERROR;
							bitb = b;
							bitk = k;
							z.avail_in = n;
							z.total_in += p - z.next_in_index;
							z.next_in_index = p;
							write = q;
							return Inflate_flush(z, r);
						}
						left = (b & unchecked((int)(0xffff)));
						b = k = 0;
						// dump bits
						mode = left != 0 ? STORED : (last != 0 ? DRY : TYPE);
						break;
					}

					case STORED:
					{
						if (n == 0)
						{
							bitb = b;
							bitk = k;
							z.avail_in = n;
							z.total_in += p - z.next_in_index;
							z.next_in_index = p;
							write = q;
							return Inflate_flush(z, r);
						}
						if (m == 0)
						{
							if (q == end && read != 0)
							{
								q = 0;
								m = (int)(q < read ? read - q - 1 : end - q);
							}
							if (m == 0)
							{
								write = q;
								r = Inflate_flush(z, r);
								q = write;
								m = (int)(q < read ? read - q - 1 : end - q);
								if (q == end && read != 0)
								{
									q = 0;
									m = (int)(q < read ? read - q - 1 : end - q);
								}
								if (m == 0)
								{
									bitb = b;
									bitk = k;
									z.avail_in = n;
									z.total_in += p - z.next_in_index;
									z.next_in_index = p;
									write = q;
									return Inflate_flush(z, r);
								}
							}
						}
						r = Z_OK;
						t = left;
						if (t > n)
						{
							t = n;
						}
						if (t > m)
						{
							t = m;
						}
						System.Array.Copy(z.next_in, p, window, q, t);
						p += t;
						n -= t;
						q += t;
						m -= t;
						if ((left -= t) != 0)
						{
							break;
						}
						mode = last != 0 ? DRY : TYPE;
						break;
					}

					case TABLE:
					{
						while (k < (14))
						{
							if (n != 0)
							{
								r = Z_OK;
							}
							else
							{
								bitb = b;
								bitk = k;
								z.avail_in = n;
								z.total_in += p - z.next_in_index;
								z.next_in_index = p;
								write = q;
								return Inflate_flush(z, r);
							}
							n--;
							b |= (z.next_in[p++] & unchecked((int)(0xff))) << k;
							k += 8;
						}
						table = t = (b & unchecked((int)(0x3fff)));
						if ((t & unchecked((int)(0x1f))) > 29 || ((t >> 5) & unchecked((int)(0x1f))) > 29)
						{
							mode = BAD;
							z.msg = "too many length or distance symbols";
							r = Z_DATA_ERROR;
							bitb = b;
							bitk = k;
							z.avail_in = n;
							z.total_in += p - z.next_in_index;
							z.next_in_index = p;
							write = q;
							return Inflate_flush(z, r);
						}
						t = 258 + (t & unchecked((int)(0x1f))) + ((t >> 5) & unchecked((int)(0x1f)));
						if (blens == null || blens.Length < t)
						{
							blens = new int[t];
						}
						else
						{
							for (int i = 0; i < t; i++)
							{
								blens[i] = 0;
							}
						}
						b = (int)(((uint)b) >> (14));
						k -= (14);
						index = 0;
						mode = BTREE;
						goto case BTREE;
					}

					case BTREE:
					{
						while (index < 4 + ((int)(((uint)table) >> 10)))
						{
							while (k < (3))
							{
								if (n != 0)
								{
									r = Z_OK;
								}
								else
								{
									bitb = b;
									bitk = k;
									z.avail_in = n;
									z.total_in += p - z.next_in_index;
									z.next_in_index = p;
									write = q;
									return Inflate_flush(z, r);
								}
								n--;
								b |= (z.next_in[p++] & unchecked((int)(0xff))) << k;
								k += 8;
							}
							blens[border[index++]] = b & 7;
							{
								b = (int)(((uint)b) >> (3));
								k -= (3);
							}
						}
						while (index < 19)
						{
							blens[border[index++]] = 0;
						}
						bb[0] = 7;
						t = inftree.Inflate_trees_bits(blens, bb, tb, hufts, z);
						if (t != Z_OK)
						{
							r = t;
							if (r == Z_DATA_ERROR)
							{
								blens = null;
								mode = BAD;
							}
							bitb = b;
							bitk = k;
							z.avail_in = n;
							z.total_in += p - z.next_in_index;
							z.next_in_index = p;
							write = q;
							return Inflate_flush(z, r);
						}
						index = 0;
						mode = DTREE;
						goto case DTREE;
					}

					case DTREE:
					{
						while (true)
						{
							t = table;
							if (!(index < 258 + (t & unchecked((int)(0x1f))) + ((t >> 5) & unchecked((int)(0x1f
								)))))
							{
								break;
							}
							int[] h;
							int i;
							int j;
							int c;
							t = bb[0];
							while (k < (t))
							{
								if (n != 0)
								{
									r = Z_OK;
								}
								else
								{
									bitb = b;
									bitk = k;
									z.avail_in = n;
									z.total_in += p - z.next_in_index;
									z.next_in_index = p;
									write = q;
									return Inflate_flush(z, r);
								}
								n--;
								b |= (z.next_in[p++] & unchecked((int)(0xff))) << k;
								k += 8;
							}
							if (tb[0] == -1)
							{
							}
							//System.err.println("null...");
							t = hufts[(tb[0] + (b & inflate_mask[t])) * 3 + 1];
							c = hufts[(tb[0] + (b & inflate_mask[t])) * 3 + 2];
							if (c < 16)
							{
								b = (int)(((uint)b) >> (t));
								k -= (t);
								blens[index++] = c;
							}
							else
							{
								// c == 16..18
								i = c == 18 ? 7 : c - 14;
								j = c == 18 ? 11 : 3;
								while (k < (t + i))
								{
									if (n != 0)
									{
										r = Z_OK;
									}
									else
									{
										bitb = b;
										bitk = k;
										z.avail_in = n;
										z.total_in += p - z.next_in_index;
										z.next_in_index = p;
										write = q;
										return Inflate_flush(z, r);
									}
									n--;
									b |= (z.next_in[p++] & unchecked((int)(0xff))) << k;
									k += 8;
								}
								b = (int)(((uint)b) >> (t));
								k -= (t);
								j += (b & inflate_mask[i]);
								b = (int)(((uint)b) >> (i));
								k -= (i);
								i = index;
								t = table;
								if (i + j > 258 + (t & unchecked((int)(0x1f))) + ((t >> 5) & unchecked((int)(0x1f
									))) || (c == 16 && i < 1))
								{
									blens = null;
									mode = BAD;
									z.msg = "invalid bit length repeat";
									r = Z_DATA_ERROR;
									bitb = b;
									bitk = k;
									z.avail_in = n;
									z.total_in += p - z.next_in_index;
									z.next_in_index = p;
									write = q;
									return Inflate_flush(z, r);
								}
								c = c == 16 ? blens[i - 1] : 0;
								do
								{
									blens[i++] = c;
								}
								while (--j != 0);
								index = i;
							}
						}
						tb[0] = -1;
						int[] bl = new int[1];
						int[] bd = new int[1];
						int[] tl = new int[1];
						int[] td = new int[1];
						bl[0] = 9;
						// must be <= 9 for lookahead assumptions
						bd[0] = 6;
						// must be <= 9 for lookahead assumptions
						t = table;
						t = inftree.Inflate_trees_dynamic(257 + (t & unchecked((int)(0x1f))), 1 + ((t >> 
							5) & unchecked((int)(0x1f))), blens, bl, bd, tl, td, hufts, z);
						if (t != Z_OK)
						{
							if (t == Z_DATA_ERROR)
							{
								blens = null;
								mode = BAD;
							}
							r = t;
							bitb = b;
							bitk = k;
							z.avail_in = n;
							z.total_in += p - z.next_in_index;
							z.next_in_index = p;
							write = q;
							return Inflate_flush(z, r);
						}
						codes.Init(bl[0], bd[0], hufts, tl[0], hufts, td[0], z);
						mode = CODES;
						goto case CODES;
					}

					case CODES:
					{
						bitb = b;
						bitk = k;
						z.avail_in = n;
						z.total_in += p - z.next_in_index;
						z.next_in_index = p;
						write = q;
						if ((r = codes.Proc(this, z, r)) != Z_STREAM_END)
						{
							return Inflate_flush(z, r);
						}
						r = Z_OK;
						codes.Free(z);
						p = z.next_in_index;
						n = z.avail_in;
						b = bitb;
						k = bitk;
						q = write;
						m = (int)(q < read ? read - q - 1 : end - q);
						if (last == 0)
						{
							mode = TYPE;
							break;
						}
						mode = DRY;
						goto case DRY;
					}

					case DRY:
					{
						write = q;
						r = Inflate_flush(z, r);
						q = write;
						m = (int)(q < read ? read - q - 1 : end - q);
						if (read != write)
						{
							bitb = b;
							bitk = k;
							z.avail_in = n;
							z.total_in += p - z.next_in_index;
							z.next_in_index = p;
							write = q;
							return Inflate_flush(z, r);
						}
						mode = DONE;
						goto case DONE;
					}

					case DONE:
					{
						r = Z_STREAM_END;
						bitb = b;
						bitk = k;
						z.avail_in = n;
						z.total_in += p - z.next_in_index;
						z.next_in_index = p;
						write = q;
						return Inflate_flush(z, r);
					}

					case BAD:
					{
						r = Z_DATA_ERROR;
						bitb = b;
						bitk = k;
						z.avail_in = n;
						z.total_in += p - z.next_in_index;
						z.next_in_index = p;
						write = q;
						return Inflate_flush(z, r);
					}

					default:
					{
						r = Z_STREAM_ERROR;
						bitb = b;
						bitk = k;
						z.avail_in = n;
						z.total_in += p - z.next_in_index;
						z.next_in_index = p;
						write = q;
						return Inflate_flush(z, r);
						break;
					}
				}
			}
		}

		internal void Free(ZStream z)
		{
			Reset(z, null);
			window = null;
			hufts = null;
		}

		//ZFREE(z, s);
		internal void Set_dictionary(byte[] d, int start, int n)
		{
			System.Array.Copy(d, start, window, 0, n);
			read = write = n;
		}

		// Returns true if inflate is currently at the end of a block generated
		// by Z_SYNC_FLUSH or Z_FULL_FLUSH. 
		internal int Sync_point()
		{
			return mode == LENS ? 1 : 0;
		}

		// copy as much as possible from the sliding window to the output area
		internal int Inflate_flush(ZStream z, int r)
		{
			int n;
			int p;
			int q;
			// local copies of source and destination pointers
			p = z.next_out_index;
			q = read;
			// compute number of bytes to copy as far as end of window
			n = (int)((q <= write ? write : end) - q);
			if (n > z.avail_out)
			{
				n = z.avail_out;
			}
			if (n != 0 && r == Z_BUF_ERROR)
			{
				r = Z_OK;
			}
			// update counters
			z.avail_out -= n;
			z.total_out += n;
			// update check information
			if (checkfn != null)
			{
				z.adler = check = z._adler.Adler(check, window, q, n);
			}
			// copy as far as end of window
			System.Array.Copy(window, q, z.next_out, p, n);
			p += n;
			q += n;
			// see if more to copy at beginning of window
			if (q == end)
			{
				// wrap pointers
				q = 0;
				if (write == end)
				{
					write = 0;
				}
				// compute bytes to copy
				n = write - q;
				if (n > z.avail_out)
				{
					n = z.avail_out;
				}
				if (n != 0 && r == Z_BUF_ERROR)
				{
					r = Z_OK;
				}
				// update counters
				z.avail_out -= n;
				z.total_out += n;
				// update check information
				if (checkfn != null)
				{
					z.adler = check = z._adler.Adler(check, window, q, n);
				}
				// copy
				System.Array.Copy(window, q, z.next_out, p, n);
				p += n;
				q += n;
			}
			// update pointers
			z.next_out_index = p;
			read = q;
			// done
			return r;
		}
	}
}
