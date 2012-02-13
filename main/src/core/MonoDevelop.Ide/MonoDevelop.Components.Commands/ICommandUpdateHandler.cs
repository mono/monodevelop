// ICommandUpdateHandler.cs
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
	/// This interface can be used to create attribute classes which provide custom
	/// command updating behavior. CustomCommandUpdaterAttribute is a
	/// default implementation of this interface which may be more convenient to use.
	/// </summary>
	public interface ICommandUpdateHandler
	{
		/// <summary>
		/// Next update handler. Set by the command manager.
		/// </summary>
		ICommandUpdateHandler Next { get; set; }
		
		/// <summary>
		/// Called to update the command status. Can call Next.CommandUpdate to execute the default update handler.
		/// </summary>
		void CommandUpdate (object target, CommandInfo cinfo);
	}
	
	public interface ICommandArrayUpdateHandler
	{
		ICommandArrayUpdateHandler Next { get; set; }
		void CommandUpdate (object target, CommandArrayInfo cinfo);
	}

	/// <summary>
	/// This interface can be used to create attribute classes which provide custom
	/// command execution behavior. CustomCommandTargetAttribute is a
	/// default implementation of this interface which may be more convenient to use.
	/// </summary>
	public interface ICommandTargetHandler
	{
		/// <summary>
		/// Next command handler. Set by the command manager.
		/// </summary>
		ICommandTargetHandler Next { get; set; }
		
		/// <summary>
		/// Executes the command. Can call Next.Run to execute the default command handler.
		/// </summary>
		void Run (object target, Command cmd);
	}

	public interface ICommandArrayTargetHandler
	{
		ICommandArrayTargetHandler Next { get; set; }
		void Run (object target, Command cmd, object data);
	}
}
