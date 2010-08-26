/*
"Miscellaneous Utility Library" Software Licence

Version 1.0

Copyright (c) 2004-2008 Jon Skeet and Marc Gravell.
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions
are met:

1. Redistributions of source code must retain the above copyright
notice, this list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright
notice, this list of conditions and the following disclaimer in the
documentation and/or other materials provided with the distribution.

3. The end-user documentation included with the redistribution, if
any, must include the following acknowledgment:

"This product includes software developed by Jon Skeet
and Marc Gravell. Contact skeet@pobox.com, or see 
http://www.pobox.com/~skeet/)."

Alternately, this acknowledgment may appear in the software itself,
if and wherever such third-party acknowledgments normally appear.

4. The name "Miscellaneous Utility Library" must not be used to endorse 
or promote products derived from this software without prior written 
permission. For written permission, please contact skeet@pobox.com.

5. Products derived from this software may not be called 
"Miscellaneous Utility Library", nor may "Miscellaneous Utility Library"
appear in their name, without prior written permission of Jon Skeet.

THIS SOFTWARE IS PROVIDED "AS IS" AND ANY EXPRESSED OR IMPLIED
WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF
MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.
IN NO EVENT SHALL JON SKEET BE LIABLE FOR ANY DIRECT, INDIRECT,
INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING,
BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT
LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN
ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
POSSIBILITY OF SUCH DAMAGE. 
 */

namespace MiscUtil.Conversion
{
	/// <summary>
	/// Implementation of EndianBitConverter which converts to/from little-endian
	/// byte arrays.
	/// </summary>
	public sealed class LittleEndianBitConverter : EndianBitConverter
	{
		/// <summary>
		/// Indicates the byte order ("endianess") in which data is converted using this class.
		/// </summary>
		/// <remarks>
		/// Different computer architectures store data using different byte orders. "Big-endian"
		/// means the most significant byte is on the left end of a word. "Little-endian" means the 
		/// most significant byte is on the right end of a word.
		/// </remarks>
		/// <returns>true if this converter is little-endian, false otherwise.</returns>
		public sealed override bool IsLittleEndian()
		{
			return true;
		}

		/// <summary>
		/// Indicates the byte order ("endianess") in which data is converted using this class.
		/// </summary>
		public sealed override Endianness Endianness 
		{ 
			get { return Endianness.LittleEndian; }
		}

		/// <summary>
		/// Copies the specified number of bytes from value to buffer, starting at index.
		/// </summary>
		/// <param name="value">The value to copy</param>
		/// <param name="bytes">The number of bytes to copy</param>
		/// <param name="buffer">The buffer to copy the bytes into</param>
		/// <param name="index">The index to start at</param>
		protected override void CopyBytesImpl(long value, int bytes, byte[] buffer, int index)
		{
			for (int i=0; i < bytes; i++)
			{
				buffer[i+index] = unchecked((byte)(value&0xff));
				value = value >> 8;
			}
		}
		
		/// <summary>
		/// Returns a value built from the specified number of bytes from the given buffer,
		/// starting at index.
		/// </summary>
		/// <param name="buffer">The data in byte array format</param>
		/// <param name="startIndex">The first index to use</param>
		/// <param name="bytesToConvert">The number of bytes to use</param>
		/// <returns>The value built from the given bytes</returns>
		protected override long FromBytes(byte[] buffer, int startIndex, int bytesToConvert)
		{
			long ret = 0;
			for (int i=0; i < bytesToConvert; i++)
			{
				ret = unchecked((ret << 8) | buffer[startIndex+bytesToConvert-1-i]);
			}
			return ret;
		}
	}
}
