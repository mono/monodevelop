//
// AddinInfoDialog.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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
using System.Text;
using Gtk;
using Mono.Addins;
using Mono.Addins.Setup;
using Mono.Addins.Description;
using Mono.Unix;

namespace Mono.Addins.Gui
{
	partial class AddinInfoDialog : Dialog
	{
		AddinHeader info;
		
		public AddinInfoDialog (AddinHeader info)
		{
			Build ();
			this.info = info;
			packageImage.Stock = "md-package";
			packageImage.IconSize = (int)IconSize.Dialog;
			Fill ();
		}
		
		void Fill ()
		{
			StringBuilder sb = new StringBuilder ();
			sb.Append ("<b><big>" + info.Name + "</big></b>\n\n");
			
			if (info.Description != "")
				sb.Append (info.Description + "\n\n");
			
			sb.Append ("<small>");
			
			sb.Append ("<b>").Append (Catalog.GetString ("Version:")).Append ("</b>\n").Append (info.Version).Append ("\n\n");
			
			if (info.Author != "")
				sb.Append ("<b>").Append (Catalog.GetString ("Author:")).Append ("</b>\n").Append (info.Author).Append ("\n\n");
			
			if (info.Copyright != "")
				sb.Append ("<b>").Append (Catalog.GetString ("Copyright:")).Append ("</b>\n").Append (info.Copyright).Append ("\n\n");
			
			if (info.Dependencies.Count > 0) {
				sb.Append ("<b>").Append (Catalog.GetString ("Add-in Dependencies:")).Append ("</b>\n");
				foreach (Dependency dep in info.Dependencies)
					sb.Append (dep.Name + "\n");
			}
			
			sb.Append ("</small>");
			
//			linkLabel.Visible = info.Url != "";
				
			infoLabel.Markup = sb.ToString ();
		}
		
		public override void Dispose ()
		{
			base.Dispose ();
			Destroy ();
		}
	}
}
