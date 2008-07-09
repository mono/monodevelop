// ObjectValueKind.cs
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
	[Flags]
	public enum ObjectValueFlags: uint {
		None = 0,
		Object = 1,
		Array = 1 << 1,
		Primitive = 1 << 2,
		Unknown = 1 << 3,
		Error = 1 << 4,
		KindMask = 0x000000ff,
		
		Field = 1 << 8,
		Property = 1 << 9,
		Parameter = 1 << 10,
		Variable = 1 << 11,
		ArrayElement = 1 << 12,
		Method = 1 << 13,
		Literal = 1 << 14,
		Type = 1 << 15,
		Namespace = 1 << 16,
		OriginMask = 0x0001ff00,
		
		Global = 1 << 17,	// For fields, it means static
		ReadOnly = 1 << 18,
		
		// For field and property
		Public = 1 << 24,
		Protected = 1 << 25,
		Internal = 1 << 26,
		Private = 1 << 27,
		InternalProtected = Internal | Protected,
		AccessMask = Public | Protected | Internal | Private
	}
}
