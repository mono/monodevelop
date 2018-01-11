// GuiSyncContext.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2005 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Gui
{
	public class GuiSyncContext: SyncContext
	{
		public override void Dispatch (StatefulMessageHandler cb, object ob)
		{
			if (Runtime.IsMainThread)
				cb (ob);
			else {
				Runtime.MainSynchronizationContext.Send (state => RunCallback (state), (cb, ob));
			}
		}
		
		public override void AsyncDispatch (StatefulMessageHandler cb, object ob)
		{
			if (Runtime.IsMainThread)
				cb (ob);
			else
				Runtime.MainSynchronizationContext.Post (state => RunCallback (state), (cb, ob));
		}

		static void RunCallback (object state)
		{
			var (cb, ob) = (ValueTuple<StatefulMessageHandler, object>)state;
			cb (ob);
		}
	}
}
