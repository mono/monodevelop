// 
// IExecutionModeEditor.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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

namespace MonoDevelop.Ide.Execution
{
	/// <summary>
	/// This interface can be used to implement a widget which edits the arguments
	/// of a parameterized execution handler
	/// </summary>
	public interface IExecutionConfigurationEditor
	{
		/// <summary>
		/// Call to create the editor widget.
		/// </summary>
		/// <param name="ctx">
		/// Information about the execution context.
		/// </param>
		/// <param name="data">
		/// Data to edit. It can be null if there is no data to load, in which case
		/// the widget should be initialized to the default configuration.
		/// This data MUST NOT be modified by the editor. The Save method should
		/// return a new instance with the new data.
		/// </param>
		Gtk.Widget Load (CommandExecutionContext ctx, object data);
		
		/// <summary>
		/// Called to get the data entered by the user.
		/// The returned object must be serializable by MD.Core.Serialization.
		/// Notice that Save() may not be called if the user cancels the dialog,
		/// </summary>
		object Save ();
	}
}
