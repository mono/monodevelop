//
// IConsole.cs
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
	/// <summary>
	/// A console where an operation can use to write output or read input
	/// </summary>
	public abstract class OperationConsole: IDisposable
	{
		IDisposable cancelReg;

		/// <summary>
		/// Input stream
		/// </summary>
		public abstract TextReader In { get; }

		/// <summary>
		/// Output stream
		/// </summary>
		public abstract TextWriter Out { get; }

		/// <summary>
		/// Error stream
		/// </summary>
		public abstract TextWriter Error { get; }

		/// <summary>
		/// Log stream
		/// </summary>
		public abstract TextWriter Log { get; }

		/// <summary>
		/// Writes debug information to the console
		/// </summary>
		public virtual void Debug (int level, string category, string message)
		{
			if (level == 0 && string.IsNullOrEmpty (category)) {
				Log.Write (message);
			} else {
				Log.Write (string.Format ("[{0}:{1}] {2}", level, category, message));
			}
		}

		/// <summary>
		/// Gets the cancelation token to be used to cancel the operation that is using the console
		/// </summary>
		public CancellationToken CancellationToken {
			get { return CancellationSource.Token; }
		}

		protected CancellationTokenSource CancellationSource { get; set; }

		protected OperationConsole ()
		{
			CancellationSource = new CancellationTokenSource ();
		}

		protected OperationConsole (CancellationToken cancelToken): this ()
		{
			BindToCancelToken (cancelToken);
		}

		internal protected void BindToCancelToken (CancellationToken cancelToken)
		{
			cancelReg = cancelToken.Register (CancellationSource.Cancel);
		}

		public virtual void Dispose ()
		{
			if (cancelReg != null)
				cancelReg.Dispose ();
		}
	}
}
