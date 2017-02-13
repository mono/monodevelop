//
// CommandArrayInfo.cs
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MonoDevelop.Components.Commands
{
	public class CommandArrayInfo: IEnumerable<CommandInfo>
	{
		List<CommandInfo> list = new List<CommandInfo> ();
		CommandInfo defaultInfo;
		bool bypass;
		internal object UpdateHandlerData;
		Task updateTask;
		CancellationTokenSource cancellationTokenSource;
		
		internal CommandArrayInfo (CommandInfo defaultInfo)
		{
			this.defaultInfo = defaultInfo;
		}

		internal CommandInfo ParentCommandInfo { get; set; }

		public void Clear ()
		{
			list.Clear ();
			NotifyChanged ();
		}
		
		public CommandInfo FindCommandInfo (object dataItem)
		{
			foreach (var ci in list) {
				if (ci.HandlesItem (dataItem))
					return ci;
				else if (ci.ArrayInfo != null) {
					var r = ci.ArrayInfo.FindCommandInfo (dataItem);
					if (r != null)
						return r;
				}
				else if (ci is CommandInfoSet) {
					var r = ((CommandInfoSet)ci).CommandInfos.FindCommandInfo (dataItem);
					if (r != null)
						return r;
				}
			}
			return null;
		}
		
		public void Insert (int index, CommandInfoSet infoSet)
		{
			Insert (index, infoSet, null);
		}
		
		public void Insert (int index, CommandInfo info, object dataItem)
		{
			info.DataItem = dataItem;
			if (info.Text == null) info.Text = defaultInfo.Text;
			if (info.Icon.IsNull) info.Icon = defaultInfo.Icon;
			list.Insert (index, info);
			info.ParentCommandArrayInfo = this;
			NotifyChanged ();
		}

		public CommandInfo Insert (int index, string text, object dataItem)
		{
			CommandInfo info = new CommandInfo (text);
			Insert (index, info, dataItem);
			return info;
		}
	
		public void Add (CommandInfoSet infoSet)
		{
			Add (infoSet, null);
		}
		
		public void Add (CommandInfo info, object dataItem)
		{
			info.DataItem = dataItem;
			if (info.Text == null) info.Text = defaultInfo.Text;
			if (info.Icon.IsNull) info.Icon = defaultInfo.Icon;
			list.Add (info);
			info.ParentCommandArrayInfo = this;
			NotifyChanged ();
		}

		public CommandInfo Add (string text, object dataItem)
		{
			CommandInfo info = new CommandInfo (text);
			Add (info, dataItem);
			return info;
		}
		
		public CommandInfo this [int n] {
			get { return list [n]; }
		}
		
		public int Count {
			get { return list.Count; }
		}
			
		
		public void AddSeparator ()
		{
			CommandInfo info = new CommandInfo ("-");
			info.IsArraySeparator = true;
			Add (info, null);
		}

		public CommandInfo DefaultCommandInfo {
			get { return defaultInfo; }
		}

		public List<CommandInfo>.Enumerator GetEnumerator ()
		{
			return list.GetEnumerator ();
		}

		IEnumerator<CommandInfo> IEnumerable<CommandInfo>.GetEnumerator ()
		{
			return list.GetEnumerator ();
		}

		IEnumerator IEnumerable.GetEnumerator ()
		{
			return GetEnumerator ();
		}
		
		// When set in an update handler, the command manager will ignore this handler method
		// and will keep looking in the command route.
		public bool Bypass {
			get { return bypass; }
			set { bypass = value; }
		}

		internal void NotifyChanged ()
		{
			Changed?.Invoke (this, EventArgs.Empty);
			if (ParentCommandInfo != null)
				ParentCommandInfo.NotifyChanged ();
		}

		public event EventHandler Changed;

		public CancellationToken AsyncUpdateCancellationToken {
			get {
				if (cancellationTokenSource == null)
					cancellationTokenSource = new CancellationTokenSource ();
				return cancellationTokenSource.Token;
			}
		}

		public bool IsUpdatingAsynchronously {
			get { return updateTask != null; }
		}

		public void SetUpdateTask (Task task)
		{
			updateTask = task;
		}

		internal void CancelAsyncUpdate ()
		{
			if (cancellationTokenSource != null)
				cancellationTokenSource.Cancel ();
		}
	}
}
