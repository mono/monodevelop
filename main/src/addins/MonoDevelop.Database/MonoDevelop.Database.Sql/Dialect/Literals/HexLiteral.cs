//
// Authors:
//	Ben Motmans  <ben.motmans@gmail.com>
//
// Copyright (c) 2007 Ben Motmans
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;
using System.Data;
using System.Collections.Generic;

namespace MonoDevelop.Database.Sql
{
	public class HexLiteral : StringLiteral
	{
		public HexLiteral (string value)
		{
			if (value.Length % 2 != 0)
				throw new ArgumentException ("Invalid hexadecimal value, the length must be a factor of 2.");
			
			Value = value;
		}
		
		public HexLiteral (byte[] value)
		{
			Value = ToHexString (value);
		}
		
		public HexLiteral (int value)
		{
			Value = ToHexString (BitConverter.GetBytes (value));
		}
		
		public HexLiteral (uint value)
		{
			Value = ToHexString (BitConverter.GetBytes (value));
		}
		
		public HexLiteral (long value)
		{
			Value = ToHexString (BitConverter.GetBytes (value));
		}
		
		public HexLiteral (ulong value)
		{
			Value = ToHexString (BitConverter.GetBytes (value));
		}
		
		public HexLiteral (float value)
		{
			Value = ToHexString (BitConverter.GetBytes (value));
		}
		
		public HexLiteral (double value)
		{
			Value = ToHexString (BitConverter.GetBytes (value));
		}
		
		// see: http://www.codeproject.com/csharp/hexencoding.asp?msg=1064904#xx1064904xx
		internal static string ToHexString (byte[] bytes)
		{
			char[] hexDigits = {
				'0', '1', '2', '3', '4', '5', '6', '7',
				'8', '9', 'A', 'B', 'C', 'D', 'E', 'F'
			};

			char[] chars = new char[bytes.Length * 2];
			for (int i = 0; i < bytes.Length; i++) {
				int b = bytes[i];
				chars[i * 2] = hexDigits[b >> 4];
				chars[i * 2 + 1] = hexDigits[b & 0xF];
			}
			return new string(chars);
		}
	}
}