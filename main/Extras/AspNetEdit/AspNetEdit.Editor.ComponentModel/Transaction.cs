 /* 
 * DesignerTransaction.cs - Skeleton transactions for designer support.
 *  Does not yet track changes.
 * 
 * Authors: 
 *  Michael Hutchinson <m.j.hutchinson@gmail.com>
 *  
 * Copyright (C) 2005 Michael Hutchinson
 *
 * This sourcecode is licenced under The MIT License:
 * 
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to permit
 * persons to whom the Software is furnished to do so, subject to the
 * following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
 * OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN
 * NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
 * OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
 * USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.ComponentModel.Design;

namespace AspNetEdit.Editor.ComponentModel
{
	internal class Transaction : DesignerTransaction
	{
		private DesignerHost host;

		public Transaction (DesignerHost host, string description)
			: base (description)
		{
			this.host = host;
		}

		protected override void OnCancel ()
		{
			if (host == null)
				return;
			host.OnTransactionClosing ( false);
			host.OnTransactionClosed (false, this);
			host = null;
		}

		protected override void OnCommit ()
		{
			if (host == null)
				return;
			host.OnTransactionClosing (true);
			host.OnTransactionClosed (true, this);
			host = null;
		}
	}
}
