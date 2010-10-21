// 
// PlatformDialog.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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

using System;
using Mono.Addins;

namespace MonoDevelop.Components.Extensions
{
	/// <summary>
	/// Base class for platform dialog data
	/// </summary>
	public class PlatformDialogData
	{
		public string Title { get; set; }
		public Gtk.Window TransientFor { get; set; }
	}
	
	public interface IDialogHandler<T> where T: PlatformDialogData
	{
		bool Run (T data);
	}
	
	/// <summary>
	/// Base class to be used to implement platform-specific dialogs.
	/// T is the handler type.
	/// U is the data type where data will be hold.
	/// </summary>
	public abstract class PlatformDialog<T> where T: PlatformDialogData, new()
	{
		IDialogHandler<T> handler;
		bool gotHandler;
		
		/// <summary>
		/// Dialog data
		/// </summary>
		protected T data = new T ();
		
		protected IDialogHandler<T> Handler {
			get {
				if (!gotHandler) {
					gotHandler = true;
					foreach (object h in AddinManager.GetExtensionObjects ("/MonoDevelop/Components/DialogHandlers", true)) {
						if (h is IDialogHandler<T>) {
							handler = (IDialogHandler<T>) h;
							break;
						}
					}
				}
				return handler;
			}
		}
		
		/// <summary>
		/// Title of the dialog.
		/// </summary>
		public string Title {
			get { return data.Title; }
			set { data.Title = value; }
		}

		/// <summary>
		/// Parent window.
		/// </summary>
		public Gtk.Window TransientFor {
			get { return data.TransientFor; }
			set { data.TransientFor = value; }
		}
		
		
		/// <summary>Shows the dialog </summary>
		/// <returns> True if the user clicked OK or equivalent.</returns>
		public virtual bool Run ()
		{
			if (Handler != null)
				return Handler.Run (data);
			else
				return RunDefault ();
		}
		
		protected abstract bool RunDefault ();
	}
}
