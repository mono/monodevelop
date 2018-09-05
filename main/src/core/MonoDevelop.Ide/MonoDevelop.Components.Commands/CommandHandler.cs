//
// CommandHandler.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Threading;
using System.Threading.Tasks;

namespace MonoDevelop.Components.Commands
{
	public abstract class CommandHandler
	{
		internal void InternalRun (object dataItem)
		{
			Run (dataItem);
		}

		internal void InternalRun ()
		{
			Run ();
		}

		internal void InternalUpdate (CommandInfo info)
		{
			Update (info);
		}
	
		internal void InternalUpdate (CommandArrayInfo info)
		{
			Update (info);
		}

		/// <summary>
		/// Runs this command.
		/// </summary>
		/// <remarks>This method will be executed when the command is dispatched.
		/// </remarks>
		protected virtual void Run ()
		{
		}
	
		/// <summary>
		/// Runs this command (for array commands)
		/// </summary>
		/// <param name="dataItem">Context data</param>
		/// <remarks>This method will be executed when the command is dispatched.
		/// </remarks>
		/// 
		protected virtual void Run (object dataItem)
		{
			Run ();
		}
	
		/// <summary>
		/// Updates the status of the command
		/// </summary>
		/// <param name="info">Information instance where to set the status of the command</param>
		protected virtual void Update (CommandInfo info)
		{
			var t = UpdateAsync (info, info.AsyncUpdateCancellationToken);
			if (t != null)
				info.SetUpdateTask (t);
		}
	
		protected virtual Task UpdateAsync (CommandInfo info, CancellationToken cancelToken)
		{
			return null;
		}

		protected virtual void Update (CommandArrayInfo info)
		{
			var t = UpdateAsync (info, info.AsyncUpdateCancellationToken);
			if (t != null)
				info.SetUpdateTask (t);
		}

		protected virtual Task UpdateAsync (CommandArrayInfo info, CancellationToken cancelToken)
		{
			return null;
		}
	}
}

