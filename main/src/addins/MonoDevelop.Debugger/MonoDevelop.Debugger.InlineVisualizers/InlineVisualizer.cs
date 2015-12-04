﻿//
// InlineVisualizer.cs
//
// Author:
//       David Karlaš <david.karlas@xamarin.com>
//
// Copyright (c) 2014 Xamarin, Inc (http://www.xamarin.com)
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
using Mono.Debugging.Client;

namespace MonoDevelop.Debugger
{
	public abstract class InlineVisualizer
	{
		/// <summary>
		/// Determines whether this instance can inline visualize the specified value
		/// </summary>
		/// <returns>
		/// <c>true</c> if this instance can inline visualize the specified value; otherwise, <c>false</c>.
		/// </returns>
		/// <param name='val'>
		/// The value
		/// </param>
		/// <remarks>
		/// This method must check the value and return <c>true</c> if it is able to display that value.
		/// Typically, this method will check the TypeName of the value.
		/// </remarks>
		public abstract bool CanInlineVisualize (ObjectValue val);

		/// <summary>
		/// Converts specified value into string to be displayed in debugger.
		/// </summary>
		/// <returns>String value representing value.</returns>
		/// <param name="val">Value.</param>
		public abstract string InlineVisualize (ObjectValue val);
	}
}

