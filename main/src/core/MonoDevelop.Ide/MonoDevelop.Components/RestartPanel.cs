//
// RestartPanel.cs
//
// Author:
//       iain <>
//
// Copyright (c) 2017 
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

using Gtk;
using MonoDevelop.Components.AtkCocoaHelper;
using MonoDevelop.Core;

namespace MonoDevelop.Components
{
	public class RestartPanel : Table
	{
		public event EventHandler<EventArgs> RestartRequested;

		public RestartPanel () : base (2, 3, false)
		{
			RowSpacing = 6;
			ColumnSpacing = 6;

			var btnRestart = new Button () {
				Label = GettextCatalog.GetString ("Restart {0}", BrandingService.ApplicationName),
				CanFocus = true, UseUnderline = true
			};
			Attach (btnRestart, 1, 2, 1, 2, AttachOptions.Fill, AttachOptions.Fill, 0, 0);

			var imageRestart = new ImageView ("md-information", IconSize.Menu);
			Attach (imageRestart, 0, 1, 0, 1, AttachOptions.Fill, AttachOptions.Fill, 0, 0);

			var labelRestart = new Label (GettextCatalog.GetString ("These preferences will take effect next time you start {0}", BrandingService.ApplicationName));
			Attach (labelRestart, 1, 3, 0, 1, AttachOptions.Fill, AttachOptions.Fill, 0, 0);

			imageRestart.SetCommonAccessibilityAttributes ("IDEStyleOptionsPanel.RestartImage", null,
			                                               GettextCatalog.GetString ("A restart is required before these changes take effect"));
			imageRestart.SetAccessibilityLabelRelationship (labelRestart);


			btnRestart.Clicked += (sender, e) => {
				RestartRequested?.Invoke (this, EventArgs.Empty);

			};
		}
	}
}
