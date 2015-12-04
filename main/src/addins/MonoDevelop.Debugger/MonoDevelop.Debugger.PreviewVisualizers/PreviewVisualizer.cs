//
// PreviewVisualizer.cs
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
using MonoDevelop.Components;

namespace MonoDevelop.Debugger
{
	public abstract class PreviewVisualizer
	{
		/// <summary>
		/// Determines whether this instance can visualize the specified value
		/// </summary>
		/// <returns>
		/// <c>true</c> if this instance can visualize the specified value; otherwise, <c>false</c>.
		/// </returns>
		/// <param name='val'>
		/// The value
		/// </param>
		/// <remarks>
		/// This method must check the value and return <c>true</c> if it is able to display that value.
		/// Typically, this method will check the TypeName of the value.
		/// </remarks>
		public abstract bool CanVisualize (ObjectValue val);

		/// <summary>
		/// Gets a visualizer widget for a value
		/// </summary>
		/// <returns>
		/// The visualizer widget.
		/// </returns>
		/// <param name='val'>
		/// A value
		/// </param>
		/// <remarks>
		/// This method is called to get a widget for displaying the specified value.
		/// The method should create the widget and load the required information from
		/// the value. Notice that the ObjectValue.Value property returns a string
		/// representation of the value. If the visualizer needs to get values from
		/// the object properties, it can use the ObjectValue.GetRawValue method.
		/// </remarks>
		public abstract Control GetVisualizerWidget (ObjectValue val);
	}
}

