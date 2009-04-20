// 
// StandardHeaderPolicyPanel.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2009 Novell, Inc (http://www.novell.com)
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
using Gtk;
using MonoDevelop.Core;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Projects.Gui.Dialogs;

namespace MonoDevelop.Ide.StandardHeader
{
	
	class StandardHeaderPolicyPanel : PolicyOptionsPanel<StandardHeaderPolicy>
	{
		StandardHeaderPolicyPanelWidget panel;
		
		protected override string PolicyTitleWithMnemonic {
			get { return GettextCatalog.GetString ("Standard _Header"); }
		}
		
		public override Widget CreatePanelWidget ()
		{
			panel = new StandardHeaderPolicyPanelWidget (this);
			panel.ShowAll ();
			return panel;
		}
		
		protected override void LoadFrom (StandardHeaderPolicy policy)
		{
			panel.LoadFrom (policy);
		}
		
		protected override StandardHeaderPolicy GetPolicy ()
		{
			return panel.GetPolicy ();
		}
	}
	
	partial class StandardHeaderPolicyPanelWidget : Gtk.Bin
	{
		StandardHeaderPolicyPanel parent;
		
		public StandardHeaderPolicyPanelWidget (StandardHeaderPolicyPanel parent)
		{
			this.parent = parent;
			this.Build ();
			headerText.Buffer.Changed += NotifyChanged; 
			includeAutoCheck.Toggled += NotifyChanged;
		}

		void NotifyChanged (object sender, EventArgs e)
		{
			parent.UpdateSelectedNamedPolicy ();
		}
		
		public void LoadFrom (StandardHeaderPolicy policy)
		{
			headerText.Buffer.Text = policy.Text ?? "";
			includeAutoCheck.Active = policy.IncludeInNewFiles;
		}
		
		public StandardHeaderPolicy GetPolicy ()
		{
			string text = headerText.Buffer.Text;
			return new StandardHeaderPolicy (text, includeAutoCheck.Active);
		}
	}
	
	[DataItem ("StandardHeader")]
	public class StandardHeaderPolicy : IEquatable<StandardHeaderPolicy>
	{
		public StandardHeaderPolicy (string text, bool includeInNewFiles)
		{
			this.Text = text ?? "";
			this.IncludeInNewFiles = includeInNewFiles;
		}
		
		public StandardHeaderPolicy ()
		{
			//sane defaults
			IncludeInNewFiles = true;
		}
		
		[ItemProperty]
		public string Text { get; private set; }
		
		[ItemProperty]
		public bool IncludeInNewFiles { get; private set; }
		
		public bool Equals (StandardHeaderPolicy other)
		{
			return other != null && other.Text == Text;
		}
	}
}
