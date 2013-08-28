// 
// ValueVisualizer.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
	[Obsolete ("Please use the ValueVisualizer class")]
	public interface IValueVisualizer
	{
		/// <summary>
		/// Display name of the visualizer
		/// </summary>
		/// <remarks>
		/// This name is shown in a combo box at the top of the visualizer dialog when
		/// there is more than one visualizer available for a value
		/// </remarks>
		string Name { get; }
		
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
		bool CanVisualize (ObjectValue val);
		
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
		Gtk.Widget GetVisualizerWidget (ObjectValue val);
		
		/// <summary>
		/// Saves changes done in the visualizer
		/// </summary>
		/// <returns>
		/// <c>true</c> if the changes could be saved
		/// </returns>
		/// <param name='val'>
		/// The value on which to store changes
		/// </param>
		/// <remarks>
		/// This method is called to save changes done in the visualizer.
		/// The implementation should use ObjectValue.SetRawValue to store the changes.
		/// </remarks>
		bool StoreValue (ObjectValue val);
		
		/// <summary>
		/// Determines whether this instance supports editing the specified value
		/// </summary>
		/// <returns>
		/// <c>true</c> if this instance can edit the specified value; otherwise, <c>false</c>.
		/// </returns>
		/// <param name='val'>
		/// The value
		/// </param>
		/// <remarks>
		/// This method is called to determine if this visualizer supports value editing,
		/// in addition to visualization.
		/// The method is called only if CanVisualize returns <c>true</c> for the value, and
		/// if the value doesn't have the ReadOnly flag.
		/// Editing support is optional. 
		/// </remarks>
		bool CanEdit (ObjectValue val);
	}

	class ValueVisualizerWrapper: ValueVisualizer
	{
#pragma warning disable 618
		IValueVisualizer wrapped;

		public ValueVisualizerWrapper (IValueVisualizer wrapped)
		{
			this.wrapped = wrapped;
		}
#pragma warning restore 618

		public override bool CanVisualize (ObjectValue val)
		{
			return wrapped.CanVisualize (val);
		}

		public override Gtk.Widget GetVisualizerWidget (ObjectValue val)
		{
			return wrapped.GetVisualizerWidget (val);
		}

		public override string Name {
			get {
				return wrapped.Name;
			}
		}

		public override bool CanEdit (ObjectValue val)
		{
			return wrapped.CanEdit (val);
		}

		public override bool IsDefaultVisualizer (ObjectValue val)
		{
			return false;
		}

		public override bool StoreValue (ObjectValue val)
		{
			return wrapped.StoreValue (val);
		}
	}
}

