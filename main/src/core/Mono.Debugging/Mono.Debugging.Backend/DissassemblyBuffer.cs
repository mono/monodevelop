// DissassemblyBuffer.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
//

using System;
using System.Collections.Generic;
using Mono.Debugging.Client;

namespace Mono.Debugging.Backend
{
	public abstract class DissassemblyBuffer
	{
		List<AssemblyLine> lines = new List<AssemblyLine> ();
		int baseIndex = 0;
		long baseAddress;
		
		const int AddrPerLine = 4;
		const int ExtraDownLines = 5;
		const int ExtraUpLines = 20;
		
		public DissassemblyBuffer (long baseAddress)
		{
			this.baseAddress = baseAddress;
		}
		
		public AssemblyLine[] GetLines (int firstIndex, int lastIndex)
		{
			Console.WriteLine ("pp GET LINES: " + firstIndex + " " + lastIndex + " " + baseIndex);
			
			if (lastIndex >= 0)
				FillDown (lastIndex);
			if (firstIndex < 0)
				FillUp (firstIndex);
			
			AssemblyLine[] array = new AssemblyLine [lastIndex - firstIndex + 1];
			lines.CopyTo (baseIndex + firstIndex, array, 0, lastIndex - firstIndex + 1);
			return array;
		}
		
		public void FillUp (int targetLine)
		{
			if (baseIndex + targetLine >= 0)
				return;
			
			// Lines we are missing
			int linesReq = -(baseIndex + targetLine);
			
			Console.WriteLine ("pp FILLUP: " + linesReq);
			
			// Last known valid address
			long lastAddr = lines.Count > 0 ? lines [0].Address : baseAddress;
			
			// Addresses we are going to query to get the required lines
			long addr = lastAddr - (linesReq + ExtraUpLines) * AddrPerLine; // 4 is just a guess
			
			int lastCount = 0;
			bool limitFound = false;
			AssemblyLine[] alines;
			do {
				alines = GetLines (addr, lastAddr);
				if (alines.Length <= lastCount) {
					limitFound = true;
					break;
				}
				addr -= (linesReq + ExtraUpLines - alines.Length) * AddrPerLine;
				lastCount = alines.Length;
			}
			while (alines.Length < linesReq + ExtraUpLines);
			
			int max = limitFound ? alines.Length : alines.Length - ExtraUpLines;
			if (max < 0) max = 0;
			
			// Fill the lines
			for (int n=0; n < max; n++)
				lines.Insert (n, alines [n + (alines.Length - max)]);

			long firstAddr = lines [0].Address;
			for (int n=0; n < (linesReq - max); n++) {
				AssemblyLine line = new AssemblyLine (--firstAddr, "");
				lines.Insert (0, line);
				max++;
			}
			baseIndex += max;
		}
		
		public void FillDown (int targetLine)
		{
			if (baseIndex + targetLine < lines.Count)
				return;
			
			// Lines we are missing
			int linesReq = (baseIndex + targetLine) - lines.Count + 1;
			
			Console.WriteLine ("pp FILLDOWN: " + linesReq);
			
			// Last known valid address
			long lastAddr = lines.Count > 0 ? lines [lines.Count - 1].Address : baseAddress;
			
			// Addresses we are going to query to get the required lines
			long addr = lastAddr + (linesReq + ExtraDownLines) * AddrPerLine; // 4 is just a guess
			
			int lastCount = 0;
			bool limitFound = false;
			AssemblyLine[] alines;
			do {
				alines = GetLines (lastAddr, addr);
				if (alines.Length <= lastCount) {
					limitFound = true;
					break;
				}
				addr += (linesReq + ExtraDownLines - alines.Length) * AddrPerLine;
				lastCount = alines.Length;
			}
			while (alines.Length < linesReq + ExtraDownLines);
			
			int max = limitFound ? alines.Length : alines.Length - ExtraDownLines;
			
			// Fill the lines
			for (int n=0; n < max; n++)
				lines.Add (alines [n]);

			lastAddr = lines [lines.Count - 1].Address;
			for (int n=0; n < (linesReq - max); n++) {
				AssemblyLine line = new AssemblyLine (++lastAddr, "");
				lines.Add (line);
			}
		}
		
		public abstract AssemblyLine[] GetLines (long startAddr, long endAddr);
	}
}
