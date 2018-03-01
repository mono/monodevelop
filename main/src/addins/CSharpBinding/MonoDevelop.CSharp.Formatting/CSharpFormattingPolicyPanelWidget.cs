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
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.Editor;


namespace MonoDevelop.CSharp.Formatting
{
	[System.ComponentModel.ToolboxItem(true)]
	partial class CSharpFormattingPolicyPanelWidget : Gtk.Bin
	{
		readonly TextEditor texteditor = TextEditorFactory.CreateNewEditor ();

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
		TextStylePolicy textStylePolicy;
		CSharpFormattingPolicy policy;
		internal CSharpFormattingPolicy Policy {
			get {
				return policy;
			}
		}


		internal void SetPolicy (CSharpFormattingPolicy formattingPolicy, TextStylePolicy textStylePolicy)
		{
			policy = formattingPolicy;
			this.textStylePolicy = textStylePolicy;
			FormatSample ();
		}

		public void SetPolicy (TextStylePolicy textStylePolicy)
		{
			this.textStylePolicy = textStylePolicy;
			FormatSample ();
		}

		public CSharpFormattingPolicyPanelWidget ()
		{
			// ReSharper disable once DoNotCallOverridableMethodsInConstructor
			this.Build ();
			policy = new CSharpFormattingPolicy ();
			buttonEdit.Clicked += HandleButtonEditClicked;
			
			texteditor.Options = DefaultSourceEditorOptions.PlainEditor;
			texteditor.IsReadOnly = true;
			texteditor.MimeType = CSharpFormatter.MimeType;
			scrolledwindow1.AddWithViewport (texteditor);
			ShowAll ();
		}

		public void FormatSample ()
		{
			texteditor.Options = DefaultSourceEditorOptions.Instance.WithTextStyle (textStylePolicy);

			texteditor.Text = CSharpFormatter.FormatText (policy.CreateOptions (textStylePolicy), example, 0, example.Length);
		}

		void HandleButtonEditClicked (object sender, EventArgs e)
		{
			using (var editDialog = new CSharpFormattingProfileDialog (policy))
				MessageService.ShowCustomDialog (editDialog);
		}
	}
}

