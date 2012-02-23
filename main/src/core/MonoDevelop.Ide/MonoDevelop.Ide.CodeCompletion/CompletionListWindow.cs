// CompletionListWindow.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2005 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using System.Collections.Generic;

using Gtk;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Components;
using System.Linq;
using ICSharpCode.NRefactory.Completion;
using MonoDevelop.Ide.Gui.Content;

namespace MonoDevelop.Ide.CodeCompletion
{
	public class CompletionListWindow : ListWindow, IListDataProvider
	{
		const int declarationWindowMargin = 3;
		
		DeclarationViewWindow declarationviewwindow = new DeclarationViewWindow ();
		ICompletionData currentData;
		Widget parsingMessage;
		System.Action closedDelegate;
		int initialWordLength;
		int previousWidth = -1, previousHeight = -1;
		
		public CodeCompletionContext CodeCompletionContext {
			get;
			set;
		}
		
		public int X { get; private set; }
		public int Y { get; private set; }
		
		public int InitialWordLength {
			get { return this.initialWordLength; }
		}
		
		IMutableCompletionDataList mutableList;
		ICompletionDataList completionDataList;
		public ICompletionDataList CompletionDataList {
			get { return this.completionDataList; }
			set {
				this.completionDataList = value;
			}
		}
		
		public CompletionListWindow (CompletionTextEditorExtension ext)
		{
			Ext = ext;
			SizeAllocated += new SizeAllocatedHandler (ListSizeChanged);
			Events = Gdk.EventMask.PropertyChangeMask;
			WindowTransparencyDecorator.Attach (this);
			DataProvider = this;
			HideDeclarationView ();
		}

		bool completionListClosed;
		void CloseCompletionList ()
		{
			if (!completionListClosed) {
				completionDataList.OnCompletionListClosed (EventArgs.Empty);
				completionListClosed = true;
			}
		}

		protected override void OnDestroyed ()
		{
			if (declarationviewwindow != null) {
				declarationviewwindow.Destroy ();
				declarationviewwindow = null;
			}
			
			if (mutableList != null) {
				mutableList.Changing -= OnCompletionDataChanging;
				mutableList.Changed -= OnCompletionDataChanged;
				mutableList = null;
			}

			if (completionDataList != null) {
				if (completionDataList is IDisposable) 
					((IDisposable)completionDataList).Dispose ();
				CloseCompletionList ();
				completionDataList = null;
			}

			if (closedDelegate != null) {
				closedDelegate ();
				closedDelegate = null;
			}
			
			HideDeclarationView ();
			
			if (declarationviewwindow != null) {
				declarationviewwindow.Destroy ();
				declarationviewwindow = null;
			}
			base.OnDestroyed ();
		}

		public void PostProcessKeyEvent (Gdk.Key key, char keyChar, Gdk.ModifierType modifier)
		{
			KeyActions ka = KeyActions.None;
			bool keyHandled = false;
			foreach (var handler in CompletionDataList.KeyHandler) {
				if (handler.PostProcessKey (this, key, keyChar, modifier, out ka)) {
					keyHandled = true;
					break;
				}
			}
			
			if (!keyHandled)
				ka = PostProcessKey (key, keyChar, modifier);
			if ((ka & KeyActions.Complete) != 0)
				CompleteWord (ref ka, key, keyChar, modifier);
			if ((ka & KeyActions.CloseWindow) != 0)
				CompletionWindowManager.DestroyWindow (Ext);
		}
		
		public void ToggleCategoryMode ()
		{
			this.List.InCategoryMode = !this.List.InCategoryMode;
			this.ResetSizes ();
			this.List.QueueDraw ();
		}
		
		public bool PreProcessKeyEvent (Gdk.Key key, char keyChar, Gdk.ModifierType modifier)
		{
			KeyActions ka = KeyActions.None;
			bool keyHandled = false;
			foreach (ICompletionKeyHandler handler in CompletionDataList.KeyHandler) {
				if (handler.PreProcessKey (this, key, keyChar, modifier, out ka)) {
					keyHandled = true;
					break;
				}
			}
			
			if (!keyHandled)
				ka = PreProcessKey (key, keyChar, modifier);
			if ((ka & KeyActions.Complete) != 0)
				CompleteWord (ref ka, key, keyChar, modifier);

			if ((ka & KeyActions.CloseWindow) != 0)
				CompletionWindowManager.DestroyWindow (Ext);

			if ((ka & KeyActions.Ignore) != 0)
				return true;
			
			if ((ka & KeyActions.Process) != 0) {
				if (key == Gdk.Key.Left || key == Gdk.Key.Right) {
					// Close if there's a modifier active EXCEPT lock keys and Modifiers
					// Makes an exception for Mod1Mask (usually alt), shift, control, meta and super
					// This prevents the window from closing if the num/scroll/caps lock are active
					// FIXME: modifier mappings depend on X server settings
					
//					if ((modifier & ~(Gdk.ModifierType.LockMask | (Gdk.ModifierType.ModifierMask & ~(Gdk.ModifierType.ShiftMask | Gdk.ModifierType.Mod1Mask | Gdk.ModifierType.ControlMask | Gdk.ModifierType.MetaMask | Gdk.ModifierType.SuperMask)))) != 0) {
					// this version doesn't work for my system - seems that I've a modifier active
					// that gdk doesn't know about. How about the 2nd version - should close on left/rigt + shift/mod1/control/meta/super
					if ((modifier & (Gdk.ModifierType.ShiftMask | Gdk.ModifierType.Mod1Mask | Gdk.ModifierType.ControlMask | Gdk.ModifierType.MetaMask | Gdk.ModifierType.SuperMask)) != 0) {
						CompletionWindowManager.DestroyWindow (Ext);
						return false;
					}
					
					if (declarationviewwindow.Multiple) {
						if (key == Gdk.Key.Left)
							declarationviewwindow.OverloadLeft ();
						else
							declarationviewwindow.OverloadRight ();
						UpdateDeclarationView ();
					} else {
						CompletionWindowManager.DestroyWindow (Ext);
						return false;
					}
					return true;
				}
				if (completionDataList != null && completionDataList.CompletionSelectionMode == CompletionSelectionMode.OwnTextField)
					return true;
			}
			return false;
		}
		
		public override void SelectEntry (string s)
		{
			base.SelectEntry (s);
			UpdateDeclarationView ();
		}
		
		internal bool ShowListWindow (char firstChar, ICompletionDataList list, ICompletionWidget completionWidget, CodeCompletionContext completionContext, System.Action closedDelegate)
		{
			if (mutableList != null) {
				mutableList.Changing -= OnCompletionDataChanging;
				mutableList.Changed -= OnCompletionDataChanged;
				HideFooter ();
			}
			//initialWordLength = 0;
			this.CompletionDataList = list;
			this.CompleteWithSpaceOrPunctuation = MonoDevelop.Core.PropertyService.Get ("CompleteWithSpaceOrPunctuation", true);
			
			this.CodeCompletionContext = completionContext;
			this.closedDelegate = closedDelegate;
			mutableList = completionDataList as IMutableCompletionDataList;
			List.PreviewCompletionString = completionDataList.CompletionSelectionMode == CompletionSelectionMode.OwnTextField;

			if (mutableList != null) {
				mutableList.Changing += OnCompletionDataChanging;
				mutableList.Changed += OnCompletionDataChanged;

				if (mutableList.IsChanging)
					OnCompletionDataChanging (null, null);
			}

			this.CompletionWidget = completionWidget;

			if (FillList ()) {
// not neccessarry, because list window is not reused anymore:
//				Reset (true);
				this.AutoSelect = list.AutoSelect;
				this.AutoCompleteEmptyMatch = list.AutoCompleteEmptyMatch;
				this.CloseOnSquareBrackets = list.CloseOnSquareBrackets;
				// makes control-space in midle of words to work
				string text = completionWidget.GetCompletionText (completionContext);
				DefaultCompletionString = completionDataList.DefaultCompletionString ?? "";
				if (text.Length == 0) {
					UpdateWordSelection ();
					initialWordLength = 0;//completionWidget.SelectedLength;
					StartOffset = completionWidget.CaretOffset;
					ResetSizes ();
					ShowAll ();
					UpdateWordSelection ();
					UpdateDeclarationView ();
					
					//if there is only one matching result we take it by default
					if (completionDataList.AutoCompleteUniqueMatch && IsUniqueMatch && !IsChanging) {
						CompleteWord ();
						CompletionWindowManager.DestroyWindow (Ext);
					}
					return true;
				}
				
				initialWordLength = text.Length /*+ completionWidget.SelectedLength*/;
				StartOffset = completionWidget.CaretOffset - initialWordLength;
				HideWhenWordDeleted = initialWordLength != 0;
				ResetSizes ();
				UpdateWordSelection ();
				//if there is only one matching result we take it by default
				if (completionDataList.AutoCompleteUniqueMatch && IsUniqueMatch && !IsChanging) {
					CompleteWord ();
					CompletionWindowManager.DestroyWindow (Ext);
				} else {
					ShowAll ();
					UpdateDeclarationView ();
				}
				return true;
			}
			CompletionWindowManager.DestroyWindow (Ext);
			
			return false;
		}
		
		class DataItemComparer : IComparer<ICompletionData>
		{
			public int Compare (ICompletionData a, ICompletionData b)
			{
				return ((a.DisplayFlags & DisplayFlags.Obsolete) == (b.DisplayFlags & DisplayFlags.Obsolete))
					? StringComparer.OrdinalIgnoreCase.Compare (a.DisplayText, b.DisplayText)
					: (a.DisplayFlags & DisplayFlags.Obsolete) != 0 ? 1 : -1;
			}
		}
		
		bool FillList ()
		{
			if ((completionDataList.Count == 0) && !IsChanging)
				return false;
			
			this.Style = CompletionWidget.GtkStyle;
			
			//sort, sinking obsolete items to the bottoms
			//the string comparison is ordinal as that makes it an order of magnitude faster, which 
			//which makes completion triggering noticeably more responsive
			if (!completionDataList.IsSorted)
				completionDataList.Sort (new DataItemComparer ());
			
			Reposition (true);
			return true;
		}
		
		enum WindowPositonY {
			None,
			Top,
			Bottom
		}
		WindowPositonY yPosition;
		
		void Reposition (bool force)
		{
			X = CodeCompletionContext.TriggerXCoord - TextOffset;
			Y = CodeCompletionContext.TriggerYCoord;

			int w, h;
			GetSize (out w, out h);

			if (!force && previousHeight != h && previousWidth != w)
				return;
			
			// Note: we add back the TextOffset here in case X and X+TextOffset are on different monitors.
			Gdk.Rectangle geometry = DesktopService.GetUsableMonitorGeometry (Screen, Screen.GetMonitorAtPoint (X + TextOffset, Y));
			
			previousHeight = h;
			previousWidth = w;
			
			if (X + w > geometry.Right)
				X = geometry.Right - w;
			else if (X < geometry.Left)
				X = geometry.Left;
			
			if (Y + h > geometry.Bottom || yPosition == WindowPositonY.Top) {
				// Put the completion-list window *above* the cursor
				Y = Y - CodeCompletionContext.TriggerTextHeight - h;
				yPosition = WindowPositonY.Top;
			} else {
				yPosition = WindowPositonY.Bottom;
			}
			
			curXPos = X;
			curYPos = Y;
			Move (X, Y);
			UpdateDeclarationView ();
		}
		
		//smaller lists get size reallocated after FillList, so we have to reposition them
		//if the window size has changed since we last positioned it
		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			Reposition (true);
		}
		
		public bool CompleteWord ()
		{
			KeyActions ka = KeyActions.None;
			return CompleteWord (ref ka, (Gdk.Key)0, '\0', Gdk.ModifierType.None);
		}
		
		public bool CompleteWord (ref KeyActions ka, Gdk.Key closeChar, char keyChar, Gdk.ModifierType modifier)
		{
			if (SelectionIndex == -1 || completionDataList == null)
				return false;
			var item = completionDataList [SelectionIndex];
			if (item == null)
				return false;
			// first close the completion list, then insert the text.
			// this is required because that's the logical event chain, otherwise things could be messed up
			CloseCompletionList ();
			((CompletionData)item).InsertCompletionText (this, ref ka, closeChar, keyChar, modifier);
			AddWordToHistory (PartialWord, item.CompletionText);
			OnWordCompleted (new CodeCompletionContextEventArgs (CompletionWidget, CodeCompletionContext, item.CompletionText));
			return true;
		}
		
		protected virtual void OnWordCompleted (CodeCompletionContextEventArgs e)
		{
			EventHandler<CodeCompletionContextEventArgs> handler = this.WordCompleted;
			if (handler != null)
				handler (this, e);
		}
		
		public event EventHandler<CodeCompletionContextEventArgs> WordCompleted;
		
		void ListSizeChanged (object obj, SizeAllocatedArgs args)
		{
			UpdateDeclarationView ();
		}

		protected override void DoubleClick ()
		{
			CompleteWord ();
			CompletionWindowManager.DestroyWindow (Ext);
		}
		
		protected override void OnSelectionChanged ()
		{
			base.OnSelectionChanged ();
			UpdateDeclarationView ();
		}
		
		bool declarationViewHidden = true;
		int declarationViewX = -1, declarationViewY = -1;
		uint declarationViewTimer = 0;
		uint declarationViewWindowOpacityTimer = 0;
		
		void UpdateDeclarationView ()
		{
			if (completionDataList == null || List.Selection >= completionDataList.Count || List.Selection == -1)
				return;
			if (List.GdkWindow == null)
				return;
			RemoveDeclarationViewTimer ();
			// no selection, try to find a selection
			if (List.SelectionIndex < 0 || List.SelectionIndex >= completionDataList.Count) {
				List.CompletionString = PartialWord;
				bool hasMismatches;
				List.Selection = FindMatchedEntry (List.CompletionString, out hasMismatches);
			}
			// no success, hide declaration view
			if (List.SelectionIndex < 0 || List.SelectionIndex >= completionDataList.Count) {
				HideDeclarationView ();
				return;
			}
			var data = completionDataList[List.SelectionIndex];
			
			IList<ICompletionData> overloads;
			if (data.HasOverloads) {
				overloads = new List<ICompletionData> (data.OverloadedData);
			} else {
				overloads = new ICompletionData[] { data };
			}
			
			if (data != currentData) {
				HideDeclarationView ();
				
				declarationviewwindow.Clear ();
				declarationviewwindow.Realize ();
				
				foreach (var overload in overloads) {
					bool hasMarkup = (overload.DisplayFlags & DisplayFlags.DescriptionHasMarkup) != 0;
					declarationviewwindow.AddOverload (hasMarkup ? overload.Description : GLib.Markup.EscapeText (overload.Description));
				}
				
				declarationviewwindow.Multiple = data.HasOverloads;
				currentData = data;
				if (data.HasOverloads) {
					for (int i = 0; i < overloads.Count; i++) {
						if ((overloads[i].DisplayFlags & DisplayFlags.Obsolete) != DisplayFlags.Obsolete) {
							declarationviewwindow.CurrentOverload = i;
							break;
						}
					}
				}
			}
			
			if (declarationviewwindow.DescriptionMarkup.Length == 0) {
				HideDeclarationView ();
				return;
			}
			
			if (currentData != null)
				declarationViewTimer = GLib.Timeout.Add (250, DelayedTooltipShow);
		}
		
		void HideDeclarationView ()
		{
			RemoveDeclarationViewTimer ();
			if (declarationviewwindow != null) {
				declarationviewwindow.Hide ();
				declarationviewwindow.Opacity = 0;
			}
			declarationViewHidden = true;
			declarationViewX = declarationViewY = -1; 
		}
		
		void RemoveDeclarationViewTimer ()
		{
			if (declarationViewWindowOpacityTimer != 0) {
				GLib.Source.Remove (declarationViewWindowOpacityTimer);
				declarationViewWindowOpacityTimer = 0;
			}
			if (declarationViewTimer != 0) {
				GLib.Source.Remove (declarationViewTimer);
				declarationViewTimer = 0;
			}
		}
		
		class OpacityTimer
		{
			public double Opacity { get; private set; }
			
			CompletionListWindow window;
//			static int num = 0;
//			int id;
			public OpacityTimer (CompletionListWindow window)
			{
//				id = num++;
				this.window = window;
				Opacity = 0.0;
				window.declarationviewwindow.Opacity = Opacity;
			}
			
			public bool Timer ()
			{
				Opacity = System.Math.Min (1.0, Opacity + 0.33);
				window.declarationviewwindow.Opacity = Opacity;
				bool result = Math.Round (Opacity * 10.0) < 10;
				if (!result)
					window.declarationViewWindowOpacityTimer = 0;
				return result;
			}
		}
		
		bool DelayedTooltipShow ()
		{
			Gdk.Rectangle rect = List.GetRowArea (List.Selection);
			if (rect.IsEmpty)
				return false;
			int listpos_x = 0, listpos_y = 0, i = 0;
			while ((listpos_x == 0 || listpos_y == 0) && (i++ < 10))
				GetPosition (out listpos_x, out listpos_y);
			if (i >= 10)
				return false;
			int vert = listpos_y + rect.Y;
			int lvWidth = 0, lvHeight = 0;
			while (lvWidth == 0)
				this.GdkWindow.GetSize (out lvWidth, out lvHeight);
			
			if (vert >= listpos_y + lvHeight - 2 || vert < listpos_y) {
				HideDeclarationView ();
				return false;
			}
			
/*			if (vert >= listpos_y + lvHeight - 2) {
				vert = listpos_y + lvHeight - rect.Height;
			} else if (vert < listpos_y) {
				vert = listpos_y;
			}*/
			
			if (declarationViewHidden) {
				declarationviewwindow.Move (this.Screen.Width + 1, vert);
				declarationviewwindow.SetFixedWidth (-1);
				declarationviewwindow.ReshowWithInitialSize ();
				declarationviewwindow.Show ();
				if (declarationViewWindowOpacityTimer != 0) 
					GLib.Source.Remove (declarationViewWindowOpacityTimer);
				declarationViewWindowOpacityTimer = GLib.Timeout.Add (50, new OpacityTimer (this).Timer);
				declarationViewHidden = false;
			}
			
			Gdk.Rectangle geometry = DesktopService.GetUsableMonitorGeometry (Screen, Screen.GetMonitorAtWindow (GdkWindow));
		
			Requisition req = declarationviewwindow.SizeRequest ();
			int dvwWidth = req.Width;
			int horiz = listpos_x + lvWidth + declarationWindowMargin;
			if (geometry.Right - horiz >= lvWidth) {
				if (geometry.Right - horiz < dvwWidth)
					declarationviewwindow.SetFixedWidth (geometry.Right - horiz);
			} else {
				if (listpos_x - dvwWidth - declarationWindowMargin < 0) {
					declarationviewwindow.SetFixedWidth (listpos_x - declarationWindowMargin);
					dvwWidth = declarationviewwindow.SizeRequest ().Width;
				}
				horiz = curXPos - dvwWidth - declarationWindowMargin;
			}
			
			if (declarationViewX != horiz || declarationViewY != vert) {
				declarationviewwindow.Move (horiz, vert);
				declarationViewX = horiz;
				declarationViewY = vert;
			}
			declarationViewTimer = 0;
			return false;
		}
		
		#region IListDataProvider
		
		int IListDataProvider.ItemCount 
		{ 
			get { return completionDataList.Count; } 
		}
		
		CompletionCategory IListDataProvider.GetCompletionCategory (int n)
		{
			return completionDataList[n].CompletionCategory;
		}
		
		string IListDataProvider.GetText (int n)
		{
			return completionDataList[n].DisplayText;
		}
		
		string IListDataProvider.GetDescription (int n)
		{
			return ((CompletionData)completionDataList[n]).DisplayDescription;
		}
		
		bool IListDataProvider.HasMarkup (int n)
		{
			return (completionDataList[n].DisplayFlags & DisplayFlags.Obsolete) != 0;
		}
		
		//NOTE: we only ever return markup for items marked as obsolete
		string IListDataProvider.GetMarkup (int n)
		{
			var completionData = completionDataList[n];
			if (!completionData.HasOverloads && (completionData.DisplayFlags & DisplayFlags.Obsolete) == DisplayFlags.Obsolete || 
			    completionData.OverloadedData.All (data => (data.DisplayFlags & DisplayFlags.Obsolete) == DisplayFlags.Obsolete))
				return "<s>" + GLib.Markup.EscapeText (completionDataList[n].DisplayText) + "</s>";
			return GLib.Markup.EscapeText (completionDataList[n].DisplayText);
		}
		
		string IListDataProvider.GetCompletionText (int n)
		{
			return completionDataList[n].CompletionText;
		}
		
		Gdk.Pixbuf IListDataProvider.GetIcon (int n)
		{
			string iconName = ((CompletionData)completionDataList[n]).Icon;
			if (string.IsNullOrEmpty (iconName))
				return null;
			return ImageService.GetPixbuf (iconName, Gtk.IconSize.Menu);
		}
		
		#endregion
		
		internal bool IsChanging {
			get { return mutableList != null && mutableList.IsChanging; }
		}
		
		void OnCompletionDataChanging (object s, EventArgs args)
		{
			if (parsingMessage == null) {
				VBox box = new VBox ();
				box.PackStart (new Gtk.HSeparator (), false, false, 0);
				HBox hbox = new HBox ();
				hbox.BorderWidth = 3;
				hbox.PackStart (new Gtk.Image ("md-parser", Gtk.IconSize.Menu), false, false, 0);
				Gtk.Label lab = new Gtk.Label (GettextCatalog.GetString ("Gathering class information..."));
				lab.Xalign = 0;
				hbox.PackStart (lab, true, true, 3);
				hbox.ShowAll ();
				parsingMessage = hbox;
			}
			ShowFooter (parsingMessage);
		}
		
		void OnCompletionDataChanged (object s, EventArgs args)
		{
			ResetSizes ();
			HideFooter ();
			
			//try to capture full selection state so as not to interrupt user
			string last = null;

			if (Visible) {
				last = List.AutoSelect ? CurrentCompletionText : PartialWord;
				//don't reset the user-entered word when refilling the list
				var tmp = this.List.AutoSelect;
				// Fill the list before resetting so that we get the correct size
				FillList ();
				Reset (false);
				this.List.AutoSelect = tmp;
				if (last != null )
					SelectEntry (last);
			}
		}
	}
}
