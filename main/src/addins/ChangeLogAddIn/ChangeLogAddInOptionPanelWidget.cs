// ChangeLogAddInOptionPanelWidget.cs
//
// Author:
//   Jacob Ilsø Christensen
//
// Copyright (C) 2006  Jacob Ilsø Christensen
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

namespace MonoDevelop.ChangeLogAddIn
{
	public partial class ChangeLogAddInOptionPanelWidget : Gtk.Bin
	{
		public ChangeLogAddInOptionPanelWidget()
		{
			Build ();
		}
		
		public void LoadPanelContents()
		{
			nameEntry.Text = PropertyService.Get ("ChangeLogAddIn.Name", string.Empty);
			emailEntry.Text = PropertyService.Get ("ChangeLogAddIn.Email", string.Empty);
			integrationCheck.Active = PropertyService.Get ("ChangeLogAddIn.VersionControlIntegration", true);
		}
		
		public void StorePanelContents()
		{
			PropertyService.Set("ChangeLogAddIn.Name", nameEntry.Text);
			PropertyService.Set("ChangeLogAddIn.Email", emailEntry.Text);
			PropertyService.Set("ChangeLogAddIn.VersionControlIntegration", integrationCheck.Active);
			PropertyService.SaveProperties ();
		}
	}
}
