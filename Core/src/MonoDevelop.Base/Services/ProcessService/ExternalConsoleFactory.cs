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

namespace MonoDevelop.Services
{
	public sealed class ExternalConsoleFactory: IConsoleFactory
	{
		public static ExternalConsoleFactory Instance = new ExternalConsoleFactory ();
		
		public IConsole CreateConsole (bool closeOnDispose)
		{
			return new ExternalConsole (closeOnDispose);
		}
	}
	
	public sealed class ExternalConsole: IConsole
	{
		bool closeOnDispose;
		
		internal ExternalConsole (bool closeOnDispose)
		{
			this.closeOnDispose = closeOnDispose;
		}
		
		public TextReader In {
			get { return Console.In; }
		}
		
		public TextWriter Out {
			get { return Console.Out; }
		}
		
		public TextWriter Error {
			get { return Console.Error; }
		}
		
		public bool CloseOnDispose {
			get { return closeOnDispose; }
		}
		
		public void Dispose ()
		{
		}
		
		public event EventHandler CancelRequested {
			add {}
			remove {}
		}
	}
}
