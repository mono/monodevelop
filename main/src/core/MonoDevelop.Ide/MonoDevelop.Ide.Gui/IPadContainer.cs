//
// IPadContainer.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
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
using System.Drawing;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Codons;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;

namespace MonoDevelop.Ide.Gui
{
	public interface IPadWindow
	{
		string Id { get; }
		string Title { get; set; }
		string Icon { get; set; }
		bool Visible { get; set; }
		bool Sticky { get; set; }
		IPadContent Content { get; }
		
		void Activate ();
		
		event EventHandler PadShown;
		event EventHandler PadHidden;
		event EventHandler PadDestroyed;
	}
	
	internal class PadWindow: IPadWindow
	{
		string title;
		string icon;
		IPadContent content;
		PadCodon codon;
		IWorkbenchLayout layout;
		
		static IPadWindow lastWindow;
		static IPadWindow lastLocationList;
		
		internal PadWindow (IWorkbenchLayout layout, PadCodon codon)
		{
			this.layout = layout;
			this.codon = codon;
			this.title = GettextCatalog.GetString (codon.Label);
			this.icon = codon.Icon;
		}
		
		// This property keeps track of the last focused pad. It is used by the
		// ShowNext/ShowPrevious commands to know which pad has to show the next/previous item.
		internal static IPadWindow LastActivePadWindow {
			get { return lastWindow; }
			set {
				lastWindow = value;
				if (lastWindow != null && lastWindow.Content is MonoDevelop.Ide.Gui.Pads.ILocationListPad)
					lastLocationList = lastWindow;
			}
		}
		
		internal static IPadWindow LastActiveLocationList {
			get { return lastLocationList; }
		}
		
		public IPadContent Content {
			get {
				CreateContent ();
				return content; 
			}
		}
		
		public string Title {
			get { return title; }
			set { 
				title = value;
				if (TitleChanged != null)
					TitleChanged (this, EventArgs.Empty);
			}
		}
		
		public string Icon  {
			get { return icon; }
			set { 
				icon = value;
				if (IconChanged != null)
					IconChanged (this, EventArgs.Empty);
			}
		}
		
		public string Id {
			get { return codon.PadId; }
		}
		
		public bool Visible {
			get {
				return layout.IsVisible (codon);
			}
			set {
				if (value) {
					layout.ShowPad (codon);
				}
				else {
					layout.HidePad (codon);
				}
			}
		}

		public bool Sticky {
			get {
				return layout.IsSticky (codon);
			}
			set {
				layout.SetSticky (codon, value);
			}
		}
		
		public void Activate ()
		{
			CreateContent ();
			layout.ActivatePad (codon);
		}
		
		void CreateContent ()
		{
			if (this.content == null) {
				this.content = codon.PadContent;
			}
		}
		
		internal IMementoCapable GetMementoCapable ()
		{
			// Don't create the content if not already created
			return content as IMementoCapable;
		}
		
		internal void NotifyShown ()
		{
			if (PadShown != null)
				PadShown (this, EventArgs.Empty);
		}
		
		internal void NotifyHidden ()
		{
			if (PadHidden != null)
				PadHidden (this, EventArgs.Empty);
		}
		
		internal void NotifyDestroyed ()
		{
			if (PadDestroyed != null)
				PadDestroyed (this, EventArgs.Empty);
		}
		
		public event EventHandler PadShown;
		public event EventHandler PadHidden;
		public event EventHandler PadDestroyed;
		
		internal event EventHandler TitleChanged;
		internal event EventHandler IconChanged;
	}
}
