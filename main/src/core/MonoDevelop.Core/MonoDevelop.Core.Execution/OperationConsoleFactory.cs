//
// IConsoleFactory.cs
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

namespace MonoDevelop.Core.Execution
{
	public abstract class OperationConsoleFactory
	{
		public class CreateConsoleOptions 
		{
			public static readonly CreateConsoleOptions Default = new CreateConsoleOptions { BringToFront = true };

			public bool BringToFront { get; private set; }
			public string Title { get; private set; }

			CreateConsoleOptions ()
			{
			}

			CreateConsoleOptions (CreateConsoleOptions options)
			{
				this.BringToFront = options.BringToFront;
				this.Title = options.Title;
			}

			public CreateConsoleOptions (bool bringToFront)
			{
				BringToFront = bringToFront;
			}

			public CreateConsoleOptions WithBringToFront (bool bringToFront)
			{
				if (bringToFront == BringToFront)
					return this;
				var result = new CreateConsoleOptions (this);
				result.BringToFront = bringToFront;
				return result;
			}

			public CreateConsoleOptions WithTitle (string title)
			{
				if (title == Title)
					return this;
				var result = new CreateConsoleOptions (this);
				result.Title = title;
				return result;
			}
		}

		public OperationConsole CreateConsole (CancellationToken cancellationToken = default (CancellationToken))
		{
			return CreateConsole (CreateConsoleOptions.Default, cancellationToken);
		}

		public OperationConsole CreateConsole (CreateConsoleOptions options, CancellationToken cancellationToken = default (CancellationToken))
		{
			var c = OnCreateConsole (options);
			if (cancellationToken != default(CancellationToken))
				c.BindToCancelToken (cancellationToken);
			return c;
		}

		protected abstract OperationConsole OnCreateConsole (CreateConsoleOptions options);
	}
}
