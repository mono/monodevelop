//
// Copyright (C) Microsoft Corp. All rights reserved.
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

namespace MonoDevelop.Components.Commands
{
	/// <summary>
	/// Command targets can implement this interface to provide custom command handling.
	/// They will be be queried for handlers via these methods instead of being inspected
	/// for command handler attributes.
	/// </summary>
	public interface ICustomCommandTarget
	{
		ICommandHandler GetCommandHandler (object commandId);
		ICommandUpdater GetCommandUpdater (object commandId);
	}

	public interface ICommandHandler
	{
		void Run (object cmdTarget, Command cmd);
		void Run (object cmdTarget, Command cmd, object dataItem);
	}

	public interface ICommandUpdater
	{
		void Run (object cmdTarget, CommandInfo info);
		void Run (object cmdTarget, CommandArrayInfo info);
	}
}

