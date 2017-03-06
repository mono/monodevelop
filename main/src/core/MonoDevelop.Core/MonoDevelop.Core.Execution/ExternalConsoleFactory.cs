//
// ExternalConsoleFactory.cs
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
using System.IO;
using System.Threading;

namespace MonoDevelop.Core.Execution
{
	public sealed class ExternalConsoleFactory: OperationConsoleFactory
	{
		public static ExternalConsoleFactory Instance = new ExternalConsoleFactory ();
		
		public ExternalConsole CreateConsole (bool closeOnDispose, CancellationToken cancellationToken = default (CancellationToken))
		{
			var c = new ExternalConsole (closeOnDispose, null);
			if (cancellationToken != default(CancellationToken))
				c.BindToCancelToken (cancellationToken);
			return c;
		}

		protected override OperationConsole OnCreateConsole (CreateConsoleOptions options)
		{
			return new ExternalConsole (!options.PauseWhenFinished, options.Title);
		}
	}
	
	public sealed class ExternalConsole: OperationConsole
	{
		internal ExternalConsole (bool closeOnDispose, string title)
		{
			CloseOnDispose = closeOnDispose;
			Title = title;
		}
		
		public bool CloseOnDispose { get; set; }

		public string Title { get; set; }

		public override TextReader In {
			get { return Console.In; }
		}
		
		public override TextWriter Out {
			get { return Console.Out; }
		}
		
		public override TextWriter Error {
			get { return Console.Error; }
		}
		
		public override TextWriter Log {
			get { return Out; }
		}
	}
}
