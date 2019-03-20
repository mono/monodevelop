//
// StockIconAssembly.cs
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
using System.Collections.Generic;
using Microsoft.VisualStudio.Core.Imaging;
using Mono.Addins;

namespace MonoDevelop.Ide.Extensions
{
	[ExtensionNode (Description="A stock icon. It is possible to register several icons with the same 'id' and different sizes.")]
	internal class StockIconCodon : ExtensionNode
	{
		[NodeAttribute ("stockid", true, "Id of the stock icon.")]
		public string StockId { get; private set; }
		
		[NodeAttribute ("size", "Size of the icon.")]
		public Gtk.IconSize IconSize { get; private set; }

		[NodeAttribute ("resource", "Name of the resource where the icon is stored.")]
		public string Resource { get; private set; }

		[NodeAttribute ("file", "Name of the file where the icon is stored.")]
		public string File { get; private set; }

		[NodeAttribute ("icon", "Id of another icon or combination of icons to assign to this stock id.")]
		public string IconId { get; private set; }

		[NodeAttribute ("animation", "An animation specification.")]
		public string Animation { get; private set; }

		//these fields are assigned by reflection, suppress "never assigned" warning
		#pragma warning disable 649

		[NodeAttribute ("imageid", "One or more semicolon-separated Visual Studio ImageIds, in format `{guid}#454;{guid}#7832;...`. The KnownImages GUID may be omitted.")]
		string imageid;

		#pragma warning restore 649

		public IEnumerable<ImageId> GetImageIds() {
			if (imageid == null)
				yield break;

			int start = 0;

			do {
				int end = imageid.IndexOf (';', start);
				if (end < 0) {
					end = imageid.Length;
				}

				Guid guid = KnownImagesGuid;
				int hashIdx = imageid.IndexOf ('#', start, end - start);
				if (hashIdx > -1) {
					guid = Guid.Parse (imageid.Substring (start, hashIdx - start));
					start = hashIdx + 1;
				}

				int id = int.Parse (imageid.Substring (start, end - start));

				yield return new ImageId (guid, id);

				start = end + 1;
			}
			while (start < imageid.Length);
		}

		static readonly Guid KnownImagesGuid = Guid.Parse ("{ae27a6b0-e345-4288-96df-5eaf394ee369}");
	}
}
