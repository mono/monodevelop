// 
// CPlusPlusFormattingPolicyPanelWidget.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
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
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using System.Collections.Generic;
using MonoDevelop.Ide.CodeFormatting;

namespace CBinding.Formatting
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class CFormattingPolicyPanelWidget : Gtk.Bin
	{
		Mono.TextEditor.TextEditor texteditor = new Mono.TextEditor.TextEditor ();
//		Gtk.ListStore model = new Gtk.ListStore (typeof(string));
//		List<CPlusPlusFormattingPolicy> policies = new List<CPlusPlusFormattingPolicy> ();
		const string example = @"namespace Example { 
	class Test
	{
		static void Main (int argc, char **argv)
		{
			for (int i = 0; i < argc; i++) {
				cout << i << "": "" << argv[i] << endl;
			}
		}
	}
}";
		CFormattingPolicy policy;
		public CFormattingPolicy Policy {
			get {
				return policy;
			}
			set {
				policy = value;
				FormatSample ();
			}
		}
		
		public CFormattingPolicyPanelWidget ()
		{
			this.Build ();
			buttonEdit.Clicked += HandleButtonEditClicked;
			
			var options = MonoDevelop.SourceEditor.DefaultSourceEditorOptions.Instance;
			texteditor.Options.FontName = options.FontName;
			texteditor.Options.ColorScheme = options.ColorScheme;
			texteditor.Options.ShowFoldMargin = false;
			texteditor.Options.ShowIconMargin = false;
			texteditor.Options.ShowLineNumberMargin = false;
			texteditor.Options.ShowInvalidLines = false;
			texteditor.Document.ReadOnly = true;
			texteditor.Document.MimeType = CFormatter.MimeType;
			scrolledwindow1.Child = texteditor;
			ShowAll ();
		}

		public void FormatSample ()
		{
			var formatter = CodeFormatterService.GetFormatter (CFormatter.MimeType);
			var parent = new MonoDevelop.Projects.DotNetAssemblyProject ();
			parent.Policies.Set<CFormattingPolicy> (policy, CFormatter.MimeType);
			texteditor.Document.Text = formatter.FormatText (parent.Policies, example);
		}

		void HandleButtonEditClicked (object sender, EventArgs e)
		{
			var editDialog = new CFormattingProfileDialog (policy);
			MessageService.ShowCustomDialog (editDialog);
			editDialog.Destroy ();
		}
	}
}
