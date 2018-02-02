// 
// MutableCompletionDataList.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.Ide.CodeCompletion
{
	public interface IMutableCompletionDataList : ICompletionDataList, IDisposable
	{
		bool IsChanging { get; }
		event EventHandler Changing;
		event EventHandler Changed;
	}

	[Obsolete ("This is no longer functional")]
	public class ProjectDomCompletionDataList : CompletionDataList, IMutableCompletionDataList
	{
		public ProjectDomCompletionDataList ()
		{
		}

		void HandleParseOperationStarted (object sender, EventArgs e)
		{
			OnChanging ();
		}

		void HandleParseOperationFinished (object sender, EventArgs e)
		{
			OnChanged ();
		}
		
		#region IMutableCompletionDataList implementation 
		
		EventHandler changing;
		EventHandler changed;
		
		public event EventHandler Changing {
			add {
				changing += value;
			}
			remove {
				changing -= value;
			}
		}
		
		public event EventHandler Changed {
			add {
				changed += value;
			}
			remove {
				changed -= value;
			}
		}
		
		public bool IsChanging {
			get { return false; }
		}
		
		protected virtual void OnChanging ()
		{
			if (changing != null)
				Gtk.Application.Invoke (changing);
		}
		
		protected virtual void OnChanged ()
		{
			if (changed != null)
				Gtk.Application.Invoke (changed);
		}
		
		#endregion 
		
		#region IDisposable implementation 
		
		bool disposed;
		
		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}
		
		public virtual void Dispose (bool disposing)
		{
			if (!disposed) {
				disposed = true;
			}
		}
		
		~ProjectDomCompletionDataList ()
		{
			Dispose (false);
		}
		
		#endregion 
	}
}
