// 
// PListEditorWidget.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2011 Xamarin <http://xamarin.com>
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

using Gtk;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Projects;

namespace MonoDevelop.MacDev.PlistEditor
{
	[System.ComponentModel.ToolboxItem (false)]
	public partial class PListEditorWidget : Notebook
	{
		class PListEditorSection : CompactScrolledWindow
		{
			VBox content = new VBox ();
			
			public PListEditorSection ()
			{
				AddWithViewport (content);
				ShowAll ();
				content.Hide ();
			}
			
			protected override void OnRealized ()
			{
				base.OnRealized ();
				content.Show ();
			}
			
			public void AddExpander (MacExpander expander)
			{
				content.PackStart (expander, false, false, 0);
			}
		}
		
		public PListEditorWidget (IPlistEditingHandler handler, Project proj, PDictionary plist)
		{
			var summaryScrolledWindow = new PListEditorSection ();
			AppendPage (summaryScrolledWindow, new Label (GettextCatalog.GetString ("Summary")));
			
			var advancedScrolledWindow = new PListEditorSection ();
			AppendPage (advancedScrolledWindow, new Label (GettextCatalog.GetString ("Advanced")));
			
			foreach (var section in handler.GetSections (proj, plist)) {
				var expander = new MacExpander () {
					ContentLabel = section.Name,
					Expandable = true,
				};
				expander.SetWidget (section.Widget);
				
				if (section.IsAdvanced) {
					advancedScrolledWindow.AddExpander (expander);
				} else {
					summaryScrolledWindow.AddExpander (expander);
				}
				
				if (section.CheckVisible != null) {
					expander.Visible = section.CheckVisible (plist);
					//capture section for closure
					var s = section;
					plist.Changed += delegate {
						expander.Visible = s.CheckVisible (plist);
					};
				}
			}
			Show ();
		}
	}
}