// CustomCommandTargetAttribute.cs
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
	/// Allows customizing the command execution behavior at class or method level
	/// </summary>
	/// <remarks>
	/// This base class can be used to create attribute classes which provide custom
	/// command execution behavior. It can be applied to classes or methods. The overriden Run method
	/// will be called to execute commands.
	/// 
	/// When applied to a class, the Run method is called for all commands for which there is a command
	/// handler in the class. 
	/// 
	/// When applied to a method, the method must be a command handler (it has to have a [CommandHandler] attribute),
	/// and the Run method is called only for the command that this method handles.
	/// </remarks>
	public class CustomCommandTargetAttribute: Attribute, ICommandTargetHandler, ICommandArrayTargetHandler
	{
		ICommandTargetHandler next;
		ICommandArrayTargetHandler nextArray;
		
		void ICommandArrayTargetHandler.Run (object target, Command cmd, object data)
		{
			Run (target, cmd, data);
		}
		
		void ICommandTargetHandler.Run (object target, Command cmd)
		{
			Run (target, cmd);
		}

		/// <summary>
		/// Runs a command
		/// </summary>
		/// <param name='target'>
		/// Object that implements the command handler
		/// </param>
		/// <param name='cmd'>
		/// The command to be executed
		/// </param>
		/// <param name='data'>
		/// Data item provided by the command update handler
		/// </param>
		/// <remarks>
		/// The default implementation of this method runs the command handler method
		/// to which this attribute is applied. A custom implementation of this method can call
		/// base.Run at any point to run the method handler, or not call it at all if it
		/// wants to provide a replacement implementation.
		/// </remarks>
		protected virtual void Run (object target, Command cmd, object data)
		{
			nextArray.Run (target, cmd, data);
		}
		
		/// <summary>
		/// Runs a command
		/// </summary>
		/// <param name='target'>
		/// Object that implements the command handler
		/// </param>
		/// <param name='cmd'>
		/// The command to be executed
		/// </param>
		/// <remarks>
		/// The default implementation of this method runs the command handler method
		/// to which this attribute is applied. A custom implementation of this method can call
		/// base.Run at any point to run the method handler, or not call it at all if it
		/// wants to provide a replacement implementation.
		/// </remarks>
		protected virtual void Run (object target, Command cmd)
		{
			next.Run (target, cmd);
		}

		ICommandTargetHandler ICommandTargetHandler.Next {
			get {
				return next;
			}
			set {
				next = value;
			}
		}
		
		ICommandArrayTargetHandler ICommandArrayTargetHandler.Next {
			get {
				return nextArray;
			}
			set {
				nextArray = value;
			}
		}
	}
}
