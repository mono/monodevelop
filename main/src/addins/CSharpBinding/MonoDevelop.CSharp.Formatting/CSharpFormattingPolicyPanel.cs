// 
// CSharpFormattingPolicyPanel.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.Editor;

namespace MonoDevelop.CSharp.Formatting
{
	class CSharpFormattingPolicyPanel : MimeTypePolicyOptionsPanel<CSharpFormattingPolicy>
	{
		CSharpFormattingPolicyPanelWidget panel;
		
		static CSharpFormattingPolicyPanel ()
		{
			// ensure that custom text editor shemes are loaded.
			TextEditorDisplayBinding.InitSourceEditor ();
		}
		
		public override Widget CreatePanelWidget ()
		{
			return panel = new CSharpFormattingPolicyPanelWidget ();
		}
		
		protected override void LoadFrom (CSharpFormattingPolicy policy)
		{
			panel.SetPolicy (policy.Clone (), GetCurrentOtherPolicy<TextStylePolicy> ());
		}

		public override void PanelSelected ()
		{
			panel.SetPolicy (GetCurrentOtherPolicy<TextStylePolicy> ());
		}
		
		protected override CSharpFormattingPolicy GetPolicy ()
		{
			// return cloned policy
			return panel.Policy;
		}

		protected override void OnPolicyStored ()
		{
			base.OnPolicyStored ();
			DefaultSourceEditorOptions.Instance.FireChange ();
		}
	}
}
