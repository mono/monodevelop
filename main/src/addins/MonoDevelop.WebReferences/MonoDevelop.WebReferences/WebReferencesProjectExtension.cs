//
// WebReferencesProjectExtension.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2018 Microsoft Corporation. All rights reserved.
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



using MonoDevelop.Projects;
using System.Collections.Generic;
using System.Linq;

namespace MonoDevelop.WebReferences
{
	[ExportProjectModelExtension]
	class WebReferencesProjectExtension : DotNetProjectExtension
	{
		protected override void OnItemReady ()
		{
			base.OnItemReady ();
		}

		void ClearCache ()
		{
			webReferenceItemsWcf = null;
			webReferenceItemsWS = null;
		}

		protected override void OnItemsAdded (IEnumerable<ProjectItem> objs)
		{
			base.OnItemsAdded (objs);
			ClearCache ();
		}

		protected override void OnItemsRemoved (IEnumerable<ProjectItem> objs)
		{
			base.OnItemsRemoved (objs);
			ClearCache ();
		}

		List<WebReferenceItem> webReferenceItemsWcf;
		internal IEnumerable<WebReferenceItem> GetWebReferenceItemsWCF ()
		{
			if (webReferenceItemsWcf == null)
				webReferenceItemsWcf = WebReferencesService.WcfEngine.GetReferenceItems (Project).ToList ();
			return webReferenceItemsWcf;
		}

		List<WebReferenceItem> webReferenceItemsWS;
		internal IEnumerable<WebReferenceItem> GetWebReferenceItemsWS ()
		{
			if (webReferenceItemsWS == null)
				webReferenceItemsWS = WebReferencesService.WsEngine.GetReferenceItems (Project).ToList ();
			return webReferenceItemsWS;
		}
	}
}
