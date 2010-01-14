// 
// JobBar.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
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

using System;
using System.Collections.Generic;
using Gtk;
using MonoDevelop.Ide.Jobs;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Components;

namespace MonoDevelop.Ide.Jobs
{
	class JobBar: HBox
	{
		const int MaxSmallSlots = 3;
		
		int currentSmallSlots = 0;
		
		HBox slotBox;
		Label filler;
		MiniButton historyButton;
		bool isMainOperationRunning;
		VSeparator separator;
		VSeparator separatorRight;
		Image historyButtonImage;
		
		public event EventHandler MainOperationStarted;
		public event EventHandler MainOperationCompleted;
		
		public JobBar ()
		{
			WidgetFlags |= WidgetFlags.NoShowAll;
			Spacing = 3;
			JobService.JobStarted += HandleJobServiceJobAdded;
			JobService.JobRemoved += HandleJobServiceJobRemoved;
			IdeApp.Workspace.LastWorkspaceItemClosed += HandleIdeAppWorkspaceLastWorkspaceItemClosed;

			filler = new Label ();
			PackStart (filler, true, true, 0);
			slotBox = new HBox ();
			PackStart (slotBox, false, false, 0);
			
			separator = new VSeparator ();
			PackStart (separator, false, false, 9);
			
			HBox wb = new HBox (false, 2);
			historyButtonImage = new Image (Gtk.Stock.Execute, IconSize.Menu);
			wb.PackStart (new Label ("..."), false, false, 0);
			Arrow ar = new Arrow (ArrowType.Up, ShadowType.None);
			wb.PackStart (ar, false, false, 0);
			wb.BorderWidth = 1;
			historyButton = new MiniButton (wb);
			historyButton.ToggleMode = true;
			PackStart (historyButton, false, false, 0);
			historyButton.Clicked += HandleHistoryButtonClicked;
			
			separatorRight = new VSeparator ();
			PackStart (separatorRight, false, false, 9);
			
			Show ();
			slotBox.Show ();
		}

		public bool IsMainOperationRunning {
			get { return isMainOperationRunning; }
		}
		
		DateTime timeMainShifted;
		JobSlot shiftedSlot;
		
		public void ShiftMainOperation ()
		{
			Gtk.Widget[] children = slotBox.Children;
			if (children.Length > 0 && ((JobSlot)children[0]).Mode == JobSlotMode.Normal) {
				shiftedSlot = (JobSlot)children[0];
				shiftedSlot.Mode = JobSlotMode.Small;
				currentSmallSlots++;
				timeMainShifted = DateTime.Now;
				UpdateSlotSizes ();
			}
				
		}
		
		public void RestoreMainOperation ()
		{
			if (shiftedSlot == null || (DateTime.Now - timeMainShifted).TotalSeconds > 10)
				return;
			if (shiftedSlot.Mode == JobSlotMode.Small)
				currentSmallSlots--;
			shiftedSlot.Mode = JobSlotMode.Normal;
			UpdateSlotSizes ();
		}
		
		void HandleHistoryButtonClicked (object sender, EventArgs e)
		{
			if (historyButton.Pressed) {
				JobHistoryWindow win = new JobHistoryWindow (historyButton);
				int x, y;
				historyButton.GdkWindow.GetOrigin (out x, out y);
				win.Show ();
				win.Move (x, y - win.SizeRequest ().Height);
			}
		}

		void HandleJobServiceJobAdded (object sender, JobEventArgs e)
		{
			Gtk.Widget[] children = slotBox.Children;
			if (children.Length > 0 && ((JobSlot)children[0]).Mode == JobSlotMode.Normal) {
				((JobSlot)children[0]).Mode = JobSlotMode.Small;
				currentSmallSlots++;
			}
			
			foreach (JobSlot cslot in children) {
				if (cslot.CanRemove)
					DestroySlot (cslot);
			}
			
			JobSlot slot = new JobSlot (e.Job);
			slotBox.PackStart (slot, false, false, 0);
			BoxChild bc = (BoxChild) slotBox [slot];
			bc.Position = 0;
			slot.Mode = JobSlotMode.Normal;
			
			e.Job.Monitor.AsyncOperation.Completed += delegate {
				HandleJobCompleted (slot);
			};
			
			UpdateSlotSizes ();
			
			if (!isMainOperationRunning) {
				isMainOperationRunning = true;
				if (MainOperationStarted != null)
					MainOperationStarted (this, EventArgs.Empty);
			}
		}
		
		void HandleJobServiceJobRemoved (object sender, JobEventArgs e)
		{
			// If the workspace is closed, clean the job bar
			foreach (JobSlot cslot in slotBox.Children) {
				if (cslot.JobInstance == e.Job) {
					DestroySlot (cslot);
					UpdateSlotSizes ();
					UpdateHistoryButton ();
					break;
				}
			}
		}
		
		void DestroySlot (JobSlot cslot)
		{
			if (cslot == shiftedSlot)
				shiftedSlot = null;
			if (cslot.Mode == JobSlotMode.Small)
				currentSmallSlots--;
			slotBox.Remove (cslot);
			cslot.Destroy ();
		}
		
		void UpdateSlotSizes ()
		{
			Gtk.Widget[] children = slotBox.Children;
			if (currentSmallSlots > MaxSmallSlots) {
				for (int n = children.Length - 1; n >= 0; n--) {
					JobSlot cslot = (JobSlot) children [n];
					if (cslot.Mode == JobSlotMode.Small) {
						cslot.Mode = JobSlotMode.Mini;
						if (--currentSmallSlots <= MaxSmallSlots)
							break;
					}
				}
			}
			filler.Visible = (children.Length > 0 && ((JobSlot)children[0]).Mode != JobSlotMode.Normal);
			BoxChild bc = (BoxChild) this [slotBox];
			bc.Expand = bc.Fill = !filler.Visible;
			separator.Visible = (children.Length > 0 && ((JobSlot)children[children.Length - 1]).Mode != JobSlotMode.Normal);
		}
		
		void HandleJobCompleted (JobSlot slot)
		{
			//slot.Mode = JobSlotMode.Mini;
			UpdateSlotSizes ();
			UpdateHistoryButton ();
			
			if (slot.Mode == JobSlotMode.Normal) {
				isMainOperationRunning = false;
				if (MainOperationCompleted != null)
					MainOperationCompleted (this, EventArgs.Empty);
			}
		}
		
		void UpdateHistoryButton ()
		{
			Gdk.Pixbuf pix = GetComposedHistoryIcon ();
			if (pix == null) {
				historyButton.Hide ();
				separatorRight.Hide ();
				return;
			}
			
			historyButton.ShowAll ();
			historyButtonImage.Pixbuf = pix;
			separatorRight.Show ();
		}
		
		public Gdk.Pixbuf GetComposedHistoryIcon ()
		{
			int numIcons = 3;
			List<Gdk.Pixbuf> icons = new List<Gdk.Pixbuf> ();
			foreach (JobInstance jobi in JobService.GetJobHistory ()) {
				if (numIcons-- == 0)
					break;
				Gdk.Pixbuf pix = ImageService.GetPixbuf (jobi.Job.Icon, Gtk.IconSize.Menu);
				if (pix != null)
					icons.Add (pix);
			}
			if (icons.Count == 0)
				return null;

			icons.Reverse ();
			
			Gdk.Pixbuf res = null;
			float scale = 0.7f;

			for (int n = 0; n < icons.Count; n++) {
				Gdk.Pixbuf s = icons [n];
				Gdk.Pixbuf oldRes = res;
				
				int nw = (int) ((float)s.Width * scale);
				int nh = (int) ((float)s.Height * scale);
				s = s.ScaleSimple (nw, nh, Gdk.InterpType.Bilinear);
				
				if (n > 0) {
					res = JobService.GetComposedIcon (s, res, nw + 1);
					oldRes.Dispose ();
					s.Dispose ();
				}
				else
					res = s;
//				scale -= 0.1f;
			}
			
			return res;
		}
		
		void HandleIdeAppWorkspaceLastWorkspaceItemClosed (object sender, EventArgs e)
		{
			// If the workspace is closed, clean the job bar
			foreach (JobSlot cslot in slotBox.Children) {
				slotBox.Remove (cslot);
				cslot.Destroy ();
			}
			shiftedSlot = null;
			currentSmallSlots = 0;
			UpdateHistoryButton ();
		}
	}
	
	class JobSlot: HBox
	{
		Gtk.Label title;
		Image jobIcon;
		Image resultImage;
		ProgressBar progressBar;
		JobSlotMode mode;
		JobInstance jobi;
		Arrow arrow;
		HBox expandedPanel;
		Label filler = new Label ();
		SlotButton button = new SlotButton ();
		Gtk.Widget mainExpWidget;
		
		class SlotButton: ExpandableStatusBarPanel
		{
			public override Widget ExpandedPanel {
				get {
					return ((JobSlot)Parent).expandedPanel;
				}
			}
			
			public override Widget MainPanelWidget {
				get {
					return ((JobSlot)Parent).mainExpWidget;
				}
			}
		}
		
		public JobSlot (JobInstance jobi)
		{
			this.jobi = jobi;
			PackStart (button, false, false, 0);
			PackStart (filler, true, true, 0);
			
			HBox box = new HBox (false, 3);
			
			progressBar = new ProgressBar ();
			progressBar.HeightRequest = 1;
			box.PackStart (progressBar, false, false, 0);
			
			resultImage = new Image (Gtk.Stock.DialogWarning, Gtk.IconSize.Menu);
			box.PackStart (resultImage, false, false, 0);
			
			jobIcon = new Image (jobi.Job.Icon, Gtk.IconSize.Menu);
			box.PackStart (jobIcon, false, false, 0);
			jobIcon.Ypad = 1;
			jobIcon.Xpad = 3;
			
			title = new Label (NormalizeTitle (jobi.Job.Title));
			title.Ellipsize = Pango.EllipsizeMode.End;
			title.Xalign = 0;
			box.PackStart (title, true, true, 0);
			button.TooltipText = jobi.Job.Title;
			button.Expandable = !jobi.Monitor.AsyncOperation.IsCompleted || jobi.HasStatusView;
			
			arrow = new Arrow (ArrowType.Down, ShadowType.None);
			box.PackStart (arrow, false, false, 0);
			
			button.MainBox.Add (box);
			ShowAll ();
			filler.Hide ();
			
			resultImage.Hide ();
			
			jobi.ProgressChanged += HandleJobiProgressChanged;
			
			expandedPanel = new HBox (false, 3);
			jobi.Job.FillExtendedStatusPanel (jobi, expandedPanel, out mainExpWidget);
			expandedPanel.ShowAll ();
		}
		
		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			if (title.SizeRequest ().Width >= allocation.Width) {
				filler.Hide ();
				title.Ellipsize = Pango.EllipsizeMode.End;
			}
			else {
				filler.Show ();
				title.Ellipsize = Pango.EllipsizeMode.None;
			}
		}

		protected override void OnDestroyed ()
		{
			jobi.ProgressChanged -= HandleJobiProgressChanged;
			base.OnDestroyed ();
		}

		void HandleJobiProgressChanged (object sender, JobEventArgs e)
		{
			progressBar.Fraction = jobi.ProgressFraction;
			title.Text = jobi.StatusMessage;
			TooltipText = jobi.StatusMessage;
			button.Expandable = !jobi.Monitor.AsyncOperation.IsCompleted || jobi.HasStatusView;
			if (jobi.Monitor.AsyncOperation.IsCompleted) {
				progressBar.Visible = false;
				jobIcon.Stock = jobi.ComposedStatusIcon;
				UpdateLabel ();
			}
		}
		
		public JobInstance JobInstance {
			get { return this.jobi; }
		}
		
		public bool CanRemove {
			get { return jobi.Monitor.AsyncOperation.IsCompleted; }
		}
		
		public JobSlotMode Mode {
			get {
				return mode;
			}
			set {
				mode = value;
				Box.BoxChild bc = (Box.BoxChild) ((Gtk.Container)Parent) [this];
				switch (value) {
					case JobSlotMode.Normal:
						progressBar.Visible = !jobi.Monitor.AsyncOperation.IsCompleted;
						title.Visible = true;
						title.WidthRequest = -1;
						progressBar.WidthRequest = -1;
						bc.Expand = bc.Fill = true;
						arrow.Visible = false;
						filler.Visible = true;
						break;
					case JobSlotMode.Small:
						progressBar.Visible = !jobi.Monitor.AsyncOperation.IsCompleted;
						title.Visible = true;
						title.WidthRequest = 50;
						progressBar.WidthRequest = 20;
						bc.Expand = bc.Fill = false;
						arrow.Visible = true;
						filler.Visible = false;
						break;
					case JobSlotMode.Mini:
						progressBar.Visible = false;
						title.Visible = false;
						bc.Expand = bc.Fill = false;
						arrow.Visible = true;
						filler.Visible = false;
						break;
				}
				UpdateLabel ();
			}
		}
	
		void UpdateLabel ()
		{
			title.Text = NormalizeTitle (jobi.StatusMessage);
		}
		
		string NormalizeTitle (string title)
		{
			if (title == null)
				return title;
			int i = title.IndexOfAny (new char[] { '\r','\n'});
			if (i != -1)
				return title.Substring (0, i);
			else
				return title;
		}
	}
	
	enum JobSlotMode
	{
		Normal,
		Small,
		Mini
	}
}
