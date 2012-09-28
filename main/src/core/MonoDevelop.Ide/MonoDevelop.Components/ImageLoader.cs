// 
// ImageLoader.cs
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
using System.Net;
using MonoDevelop.Core;
using MonoDevelop.Core.ProgressMonitoring;
using System.IO;

namespace MonoDevelop.Components
{
	public class ImageLoader
	{
		string url;
		Gdk.Pixbuf image;
		NullProgressMonitor loadMonitor;
		IAsyncOperation loadOperation;
		
		public ImageLoader (string url)
		{
			loadMonitor = new NullProgressMonitor ();
			loadOperation = loadMonitor.AsyncOperation;
			this.url = url;
			Load ();
		}
		
		public Gdk.Pixbuf Pixbuf {
			get {
				return image;
			}
		}
		
		public IAsyncOperation LoadOperation {
			get { return loadOperation; }
		}
		
		void Load ()
		{
			var monitor = loadMonitor;
			System.Threading.ThreadPool.QueueUserWorkItem (delegate {
				try {
					HttpWebRequest req = (HttpWebRequest) HttpWebRequest.Create (url);
					WebResponse resp = req.GetResponse ();
					MemoryStream ms = new MemoryStream ();
					using (var s = resp.GetResponseStream ()) {
						s.CopyTo (ms);
					}
					var data = ms.ToArray ();

					MonoDevelop.Ide.DispatchService.GuiSyncDispatch (delegate {
						Gdk.PixbufLoader l = new Gdk.PixbufLoader (resp.ContentType);
						l.Write (data);
						image = l.Pixbuf;
						l.Close ();
						monitor.Dispose ();
					});
					
					// Replace the async operation to avoid holding references to all
					// objects that subscribed the Completed event.
					loadOperation = NullAsyncOperation.Success;
				} catch (Exception ex) {
					loadMonitor.ReportError (null, ex);
					Gtk.Application.Invoke (delegate {
						monitor.Dispose ();
					});
					loadOperation = NullAsyncOperation.Failure;
				}
				loadMonitor = null;
			});
		}
	}
}

