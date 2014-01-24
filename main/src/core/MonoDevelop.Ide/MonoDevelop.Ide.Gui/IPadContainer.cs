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
using MonoDevelop.Components.Docking;
using MonoDevelop.Components.Commands;

namespace MonoDevelop.Ide.Gui
{
	public interface IPadWindow
	{
		string Id { get; }
		
		/// <summary>
		/// Title shown at the top of the pad, or at the tab label when in a notebook
		/// </summary>
		string Title { get; set; }
		
		/// <summary>
		/// Title shown at the top of the pad, or at the tab label when in a notebook
		/// </summary>
		IconId Icon { get; set; }
		
		/// <summary>
		/// True if the pad is visible in the current layout (although it may be minimized when in autohide mode
		/// </summary>
		bool Visible { get; set; }
		
		/// <summary>
		/// True when the pad is in autohide mode
		/// </summary>
		bool AutoHide { get; set; }
		
		/// <summary>
		/// The content of the pad is visible (that is, if the pad is active in the notebook on which it is
		/// docked, and it is not minimized.
		/// </summary>
		bool ContentVisible { get; }
		
		/// <summary>
		/// When set to True, the pad will be visible in all layouts
		/// </summary>
		bool Sticky { get; set; }
		
		/// <summary>
		/// When set to True, it flags the pad as "Work in progress". The pad's title will be shown in blue.
		/// </summary>
		bool IsWorking { get; set; }
		
		/// <summary>
		/// When set to True, it flags the pad as "Has errors". The pad's title will be shown in red. This flag
		/// will be automatically reset when the pad is made visible.
		/// </summary>
		bool HasErrors { get; set; }
		
		/// <summary>
		/// When set to True, it flags the pad as "Has New Data". The pad's title will be shown in bold. This flag
		/// will be automatically reset when the pad is made visible.
		/// </summary>
		bool HasNewData { get; set; }
		
		/// <summary>
		/// Interface providing the content widget
		/// </summary>
		IPadContent Content { get; }
		
		/// <summary>
		/// Interface providing the widget to be shown in the label of minimized pads
		/// </summary>
		IDockItemLabelProvider DockItemLabelProvider { get; set; }
		
		/// <summary>
		/// Returns a toolbar for the pad.
		/// </summary>
		DockItemToolbar GetToolbar (Gtk.PositionType position);
		
		/// <summary>
		/// Brings the pad to the front.
		/// </summary>
		void Activate (bool giveFocus);
		
		/// <summary>
		/// Fired when the pad is shown in the current layout (although it may be minimized)
		/// </summary>
		event EventHandler PadShown;
		
		/// <summary>
		/// Fired when the pad is hidden in the current layout
		/// </summary>
		event EventHandler PadHidden;
		
		/// <summary>
		/// Fired when the content of the pad is shown
		/// </summary>
		event EventHandler PadContentShown;
		
		/// <summary>
		/// Fired when the content of the pad is hidden
		/// </summary>
		event EventHandler PadContentHidden;
		
		/// <summary>
		/// Fired when the pad is destroyed
		/// </summary>
		event EventHandler PadDestroyed;
	}
	
	internal class PadWindow: IPadWindow
	{
		string title;
		IconId icon;
		bool isWorking;
		bool hasErrors;
		bool hasNewData;
		IPadContent content;
		PadCodon codon;
		DefaultWorkbench workbench;
		
		internal DockItem Item { get; set; }
		
		internal PadWindow (DefaultWorkbench workbench, PadCodon codon)
		{
			this.workbench = workbench;
			this.codon = codon;
			this.title = GettextCatalog.GetString (codon.Label);
			this.icon = codon.Icon;
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
				if (title != value) {
					title = value;
					if (StatusChanged != null)
						StatusChanged (this, EventArgs.Empty);
				}
			}
		}
		
		public IconId Icon  {
			get { return icon; }
			set { 
				if (icon != value) {
					icon = value;
					if (StatusChanged != null)
						StatusChanged (this, EventArgs.Empty);
				}
			}
		}
		
		public bool IsWorking {
			get { return isWorking; }
			set {
				isWorking = value;
				if (value) {
					hasErrors = false;
					hasNewData = false;
				}
				if (StatusChanged != null)
					StatusChanged (this, EventArgs.Empty);
			}
		}
		
		public bool HasErrors {
			get { return hasErrors; }
			set {
				hasErrors = value;
				if (value)
					isWorking = false;
				if (StatusChanged != null)
					StatusChanged (this, EventArgs.Empty);
			}
		}
		
		public bool HasNewData {
			get { return hasNewData; }
			set {
				hasNewData = value;
				if (value)
					isWorking = false;
				if (StatusChanged != null)
					StatusChanged (this, EventArgs.Empty);
			}
		}
		
		public string Id {
			get { return codon.PadId; }
		}
		
		public bool Visible {
			get {
				return Item.Visible;
			}
			set {
				Item.Visible = value;
			}
		}
		
		public bool AutoHide {
			get {
				return Item.Status == DockItemStatus.AutoHide;
			}
			set {
				if (value)
					Item.Status = DockItemStatus.AutoHide;
				else
					Item.Status = DockItemStatus.Dockable;
			}
		}

		public IDockItemLabelProvider DockItemLabelProvider {
			get { return Item.DockLabelProvider; }
			set { Item.DockLabelProvider = value; }
		}

		public bool ContentVisible {
			get { return workbench.IsContentVisible (codon); }
		}
		
		public bool Sticky {
			get {
				return workbench.IsSticky (codon);
			}
			set {
				workbench.SetSticky (codon, value);
			}
		}
		
		public DockItemToolbar GetToolbar (Gtk.PositionType position)
		{
			return Item.GetToolbar (position);
		}
		
		public void Activate (bool giveFocus)
		{
			CreateContent ();
			workbench.ActivatePad (codon, giveFocus);
		}
		
		void CreateContent ()
		{
			if (this.content == null) {
				this.content = codon.InitializePadContent (this);
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
		
		internal void NotifyContentShown ()
		{
			if (HasNewData)
				HasNewData = false;
			if (HasErrors)
				HasErrors = false;
			if (PadContentShown != null)
				PadContentShown (this, EventArgs.Empty);
		}
		
		internal void NotifyContentHidden ()
		{
			if (PadContentHidden != null)
				PadContentHidden (this, EventArgs.Empty);
		}
		
		internal void NotifyDestroyed ()
		{
			if (PadDestroyed != null)
				PadDestroyed (this, EventArgs.Empty);
		}
		
		public event EventHandler PadShown;
		public event EventHandler PadHidden;
		public event EventHandler PadContentShown;
		public event EventHandler PadContentHidden;
		public event EventHandler PadDestroyed;
		
		internal event EventHandler StatusChanged;
	}
}
