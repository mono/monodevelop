/*
 * Copyright (C) 2008, Kevin Thompson <kevin.thompson@theautomaters.com>
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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitSharp.Core.Util
{
    public static class Hex
    {
        private static readonly byte[] _hexCharToValue;
        private static char[] _valueToHexChar;
        private static byte[] _valueToHexByte;
        private const int Nibble = 4;
        
		static Hex()
        {
            _valueToHexChar = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f' };
            _valueToHexByte = new byte[_valueToHexChar.Length];
            for (int i = 0; i < _valueToHexChar.Length; i++)
                _valueToHexByte[i] = (byte)_valueToHexChar[i];

            _hexCharToValue = new byte['f' + 1];
            for (int i = 0; i < _hexCharToValue.Length; i++)
                _hexCharToValue[i] = byte.MaxValue;
            for (char i = '0'; i <= '9'; i++)
                _hexCharToValue[i] = (byte)(i - '0');
            for (char i = 'a'; i <= 'f'; i++)
                _hexCharToValue[i] = (byte)((i - 'a') + 10);
        }

        public static byte HexCharToValue(Char c)
        {
            return _hexCharToValue[c];
        }

        private static byte HexCharToValue(byte c)
        {
            return _hexCharToValue[c];
        }

        private static int HexStringToUInt32(byte[] bs, int offset)
        {
            return RawParseUtils.parseHexInt32(bs,offset);
        }
        
        public static void FillHexByteArray(byte[] dest, int offset, int value)
        {
            uint uvalue = (uint)value;
            int curOffset = offset + 7;
            while (curOffset >= offset && uvalue != 0)
            {
                dest[curOffset--] = _valueToHexByte[uvalue & 0xf];
                uvalue >>= Nibble;
            }

            while (curOffset >= offset)
            {
            	dest[curOffset--] = _valueToHexByte[0];
            }
        }

        public static void FillHexCharArray(char[] dest, int offset, int value)
        {
            uint uvalue = (uint)value;
            int curOffset = offset + 7;
            while (curOffset >= offset && uvalue != 0)
            {
                dest[curOffset--] = _valueToHexChar[uvalue & 0xf];
                uvalue >>= Nibble;
            }

            while (curOffset >= offset)
            {
            	dest[curOffset--] = '0';
            }
        }

		public static int HexUInt32(byte[] bs, int p, int end)
		{
			if (8 <= end - p)
			{
				return HexStringToUInt32(bs, p);
			}

			int r = 0, n = 0;
			while (n < 8 && p < end)
			{
				int v = HexCharToValue(bs[p++]);
				if (v < 0)
				{
					throw new IndexOutOfRangeException();
				}
				r <<= 4;
				r |= v;
				n++;
			}

			return r << (8 - n) * 4;
		}

    }
}
