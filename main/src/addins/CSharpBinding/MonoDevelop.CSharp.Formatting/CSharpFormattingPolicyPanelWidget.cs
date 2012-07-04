// 
// CSharpFormattingPolicyPanelWidget.cs
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
namespace MonoDevelop.CSharp.Formatting
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class CSharpFormattingPolicyPanelWidget : Gtk.Bin
	{
		Mono.TextEditor.TextEditor texteditor = new Mono.TextEditor.TextEditor ();
//		Gtk.ListStore model = new Gtk.ListStore (typeof(string));
//		List<CSharpFormattingPolicy> policies = new List<CSharpFormattingPolicy> ();
		const string example = @"using System;
namespace Example { 
	public class Test
	{
		public static void Main (string[] args)
		{
			for (int i = 0; i < 10; i++) {
				Console.WriteLine (""{0}: Test"", i);
			}
		}
	}
}";
		CSharpFormattingPolicy policy;
		public CSharpFormattingPolicy Policy {
			get {
				return policy;
			}
			set {
				policy = value;
				FormatSample ();
			}
		}
		
		public CSharpFormattingPolicyPanelWidget ()
		{
			this.Build ();
			policy = new CSharpFormattingPolicy ();
			buttonEdit.Clicked += HandleButtonEditClicked;
			
			var options = MonoDevelop.SourceEditor.DefaultSourceEditorOptions.Instance;
			texteditor.Options.FontName = options.FontName;
			texteditor.Options.ColorScheme = options.ColorScheme;
			texteditor.Options.ShowFoldMargin = false;
			texteditor.Options.ShowIconMargin = false;
			texteditor.Options.ShowLineNumberMargin = false;
			texteditor.Document.ReadOnly = true;
			texteditor.Document.MimeType = CSharpFormatter.MimeType;
			scrolledwindow1.Child = texteditor;
			ShowAll ();
		}

		public void FormatSample ()
		{
			var formatter = new CSharpFormatter ();
			texteditor.Document.Text = formatter.FormatText (policy, null, CSharpFormatter.MimeType, example, 0, example.Length);
		}

		void HandleButtonEditClicked (object sender, EventArgs e)
		{
			var editDialog = new CSharpFormattingProfileDialog (policy);
			MessageService.ShowCustomDialog (editDialog);
			editDialog.Destroy ();
		}
	}
}

