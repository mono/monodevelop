// 
// WebServiceEngine.cs
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
using MonoDevelop.Projects;
using System.Collections.Generic;
using MonoDevelop.WebReferences.WCF;
using MonoDevelop.WebReferences.WS;
using MonoDevelop.Core;

namespace MonoDevelop.WebReferences
{
	public static class WebReferencesService
	{
		public static WebServiceEngineWS WsEngine = new WebServiceEngineWS ();
		public static WebServiceEngineWCF WcfEngine = new WebServiceEngineWCF ();

		public static IEnumerable<WebReferenceItem> GetWebReferenceItemsWS (DotNetProject project)
		{
			var ext = project.GetService<WebReferencesProjectExtension> ();
			return ext.GetWebReferenceItemsWS ();
		}

		public static IEnumerable<WebReferenceItem> GetWebReferenceItemsWCF (DotNetProject project)
		{
			var ext = project.GetService<WebReferencesProjectExtension> ();
			return ext.GetWebReferenceItemsWCF ();
		}

		public static IEnumerable<WebReferenceItem> GetWebReferenceItems (DotNetProject project)
		{
			foreach (WebReferenceItem item in GetWebReferenceItemsWCF (project))
				yield return item;
			foreach (WebReferenceItem item in GetWebReferenceItemsWS (project))
				yield return item;
		}
		
		public static void NotifyWebReferencesChanged (DotNetProject project)
		{
			// This is called from a background thread when webreferences are being
			// updated asynchronously, so lets keep things simple for the users of
			// this event and just ensure we proxy it to the main thread.
			if (Runtime.IsMainThread) {
				if (WebReferencesChanged != null)
					WebReferencesChanged (null, new WebReferencesChangedEventArgs (project));
			} else {
				Runtime.RunInMainThread (() => {
					if (WebReferencesChanged != null)
						WebReferencesChanged (null, new WebReferencesChangedEventArgs (project));
				});
			}
		}
		
		public static event EventHandler<WebReferencesChangedEventArgs> WebReferencesChanged;
	}
	
	public class WebReferencesChangedEventArgs: EventArgs
	{
		readonly DotNetProject project;
		
		public WebReferencesChangedEventArgs (DotNetProject project)
		{
			this.project = project;
		}
		
		public DotNetProject Project {
			get { return project; }
		}
	}
}
