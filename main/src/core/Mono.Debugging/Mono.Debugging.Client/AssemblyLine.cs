// AssemblyLine.cs
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

namespace Mono.Debugging.Client
{
	[Serializable]
	public class AssemblyLine
	{
		long address;
		string code;
		int sourceLine;
		string addressSpace;
		
		public string Code {
			get {
				return code;
			}
		}
		
		public long Address {
			get {
				return address;
			}
		}
		
		public string AddressSpace {
			get {
				return addressSpace;
			}
		}
		
		public int SourceLine {
			get {
				return sourceLine;
			}
		}
		
		public bool IsOutOfRange {
			get { return address == -1 && code == null; }
		}
		
		public static readonly AssemblyLine OutOfRange = new AssemblyLine (-1, null, null);
		
		public AssemblyLine (long address, string code): this (address, "", code, -1)
		{
		}
		
		public AssemblyLine (long address, string code, int sourceLine): this (address, "", code, sourceLine)
		{
		}
		
		public AssemblyLine (long address, string addressSpace, string code): this (address, addressSpace, code, -1)
		{
		}
		
		public AssemblyLine (long address, string addressSpace, string code, int sourceLine)
		{
			this.address = address;
			this.code = code;
			this.sourceLine = sourceLine;
			this.addressSpace = addressSpace;
		}
	}
}
