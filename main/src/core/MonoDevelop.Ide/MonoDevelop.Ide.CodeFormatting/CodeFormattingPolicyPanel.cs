// 
// CodeFormattingPolicyPanel.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Mike Krüger
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
using System.Collections.Generic;
using Gtk;
using MonoDevelop.Core;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Projects.Gui.Dialogs;
using MonoDevelop.Projects.Text;

namespace MonoDevelop.Ide.CodeFormatting
{
	class CodeFormattingPolicyPanel : PolicyOptionsPanel<CodeFormattingPolicy>
	{
		protected override string PolicyTitleWithMnemonic {
			get { return GettextCatalog.GetString ("Code _Format"); }
		}
		
		CodeFormattingPolicyPanelWidget panel;
		
		public override Widget CreatePanelWidget ()
		{
			panel = new CodeFormattingPolicyPanelWidget ();
			panel.ShowAll ();
			return panel;
		}
		
		protected override void LoadFrom (CodeFormattingPolicy policy)
		{
			panel.Policy = policy;
		}
		
		protected override CodeFormattingPolicy GetPolicy ()
		{
			return panel.Policy;
		}
	}
	
	[System.ComponentModel.ToolboxItem(true)]
	public partial class CodeFormattingPolicyPanelWidget : Gtk.Bin
	{
		List<string> policies = new List<string> ();
		public CodeFormattingPolicy Policy {
			get;
			set;
		}
		
		public CodeFormattingPolicyPanelWidget()
		{
			this.Build();
			buttonAdd.Clicked += delegate {
				AddPolicyDialog addPolicy = new AddPolicyDialog ();
				ResponseType response = (ResponseType)addPolicy.Run ();
				if (response == ResponseType.Ok) {
					policies.Add (addPolicy.NewPolicyName);
					comboboxFormattingPolicies.AppendText (addPolicy.NewPolicyName);
				}
				addPolicy.Destroy ();
			};
			
			buttonEdit.Clicked += delegate {
				EditFormattingPolicyDialog d = new EditFormattingPolicyDialog ();
				d.SetFormat (TextFileService.GetFormatDescription ("text/x-csharp"), new CodeFormatSettings (comboboxFormattingPolicies.Name));
				d.Run ();
				d.Destroy ();
			};
			
			buttonImport.Clicked += delegate {
				
			};
			
			buttonRemove.Clicked += delegate {
				
			};
		}
	}
	
	[DataItem ("CodeFormat")]
	public class CodeFormattingPolicy  : IEquatable<CodeFormattingPolicy>
	{
		[ItemProperty]
		public string CodeStyle { 
			get; 
			set; 
		}
	
		#region IEquatable<CodeFormattingPolicy> implementation
		public bool Equals (CodeFormattingPolicy other)
		{
			return other != null && CodeStyle == other.CodeStyle;
		}
		#endregion
	}
}
