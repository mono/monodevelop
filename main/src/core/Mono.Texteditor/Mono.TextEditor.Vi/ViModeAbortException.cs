//
// ViModeAbortException.cs
//
// Author:
//       Tim Kellogg <timothy.kellogg@gmail.com>
//
// Copyright (c) 2013 Tim Kellogg
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
using System;

namespace Mono.TextEditor.Vi
{
	/// <summary>
	/// Thrown to indicate that the vi parser was unable to recognize the key
	/// sequence and should abort quietly.
	/// </summary>
	public class ViModeAbortException : Exception
	{
		/// <summary>
		/// Thrown to indicate that the vi parser was unable to recognize the key
		/// sequence and should abort quietly.
		/// </summary>
		public ViModeAbortException ()
		{
		}

		/// <summary>
		/// Thrown to indicate that the vi parser was unable to recognize the key
		/// sequence and should abort quietly.
		/// </summary>
		/// <param name="reason">The reason for the abort. May be displayed to the user</param>
		public ViModeAbortException (string reason) : base(reason)
		{
		}
	}
}

