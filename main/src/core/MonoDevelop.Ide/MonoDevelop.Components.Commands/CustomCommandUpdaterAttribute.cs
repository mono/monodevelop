// CustomCommandUpdaterAttribute.cs
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

namespace MonoDevelop.Components.Commands
{
	/// <summary>
	/// Allows customizing the command update behavior at class or method level
	/// </summary>
	/// <remarks>
	/// This base class can be used to create attribute classes which provide custom
	/// command updating behavior. It can be applied to classes or methods. The overriden CommandUpdate method
	/// will be called to update the command status.
	/// 
	/// When applied to a class, the CommandUpdate method is called for all commands for which there is a command
	/// update handler in the class. 
	/// 
	/// When applied to a method, the method must be a command update handler (it has to have a [CommandUpdateHandler] attribute),
	/// and the CommandUpdate method is called only for the command that this method handles.
	/// </remarks>
	public abstract class CustomCommandUpdaterAttribute: Attribute, ICommandUpdateHandler, ICommandArrayUpdateHandler
	{
		ICommandUpdateHandler next;
		ICommandArrayUpdateHandler nextArray;
		
		void ICommandUpdateHandler.CommandUpdate (object target, CommandInfo cinfo)
		{
			CommandUpdate (target, cinfo);
		}
		
		ICommandUpdateHandler ICommandUpdateHandler.Next {
			get {
				return next;
			}
			set {
				next = value;
			}
		}
		
		void ICommandArrayUpdateHandler.CommandUpdate (object target, CommandArrayInfo cinfo)
		{
			CommandUpdate (target, cinfo);
		}
		
		ICommandArrayUpdateHandler ICommandArrayUpdateHandler.Next {
			get {
				return nextArray;
			}
			set {
				nextArray = value;
			}
		}
		
		/// <summary>
		/// Updates the status of the command
		/// </summary>
		/// <param name='target'>
		/// Object that implements the command handler
		/// </param>
		/// <param name='cinfo'>
		/// Command info to be updated
		/// </param>
		/// <remarks>
		/// The default implementation of this method calls the update handler implemented
		/// in the target object. A custom implementation of this method can call
		/// base.CommandUpdate at any point to get the result of the default implementation.
		/// </remarks>
		protected virtual void CommandUpdate (object target, CommandInfo cinfo)
		{
			next.CommandUpdate (target, cinfo);
		}

		/// <summary>
		/// Updates the status of the command
		/// </summary>
		/// <param name='target'>
		/// Object that implements the command handler
		/// </param>
		/// <param name='cinfo'>
		/// Command info to be updated
		/// </param>
		/// <remarks>
		/// The default implementation of this method calls the update handler implemented
		/// in the target object. A custom implementation of this method can call
		/// base.CommandUpdate at any point to get the result of the default implementation.
		/// </remarks>
		protected virtual void CommandUpdate (object target, CommandArrayInfo cinfo)
		{
			nextArray.CommandUpdate (target, cinfo);
		}
	}
}
