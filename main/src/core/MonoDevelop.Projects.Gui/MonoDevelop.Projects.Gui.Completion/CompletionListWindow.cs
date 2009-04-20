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
using MonoDevelop.Core.Gui;
using MonoDevelop.Components;

namespace MonoDevelop.Projects.Gui.Completion
{
	public class CompletionWindowManager
	{
		static CompletionListWindow wnd;
		
		static CompletionWindowManager ()
		{
			wnd = new CompletionListWindow ();
		}
		
		public static bool ShowWindow (char firstChar, ICompletionDataList list, ICompletionWidget completionWidget,
		                               ICodeCompletionContext completionContext, CompletionDelegate closedDelegate)
		{
			try {
				if (!wnd.ShowListWindow (firstChar, list,  completionWidget, completionContext, closedDelegate)) {
					if (list is IDisposable)
						((IDisposable)list).Dispose ();
					return false;
				}
				return true;
			} catch (Exception ex) {
				LoggingService.LogError (ex.ToString ());
				return false;
			}
		}
		
		public static bool PreProcessKeyEvent (Gdk.Key key, Gdk.ModifierType modifier, out KeyAction ka)
		{
			if (!wnd.Visible) {
				ka = KeyAction.None;
				return false;
			}
			return wnd.PreProcessKeyEvent (key, modifier, out ka);
		}
		
		
		public static void PostProcessKeyEvent (KeyAction ka)
		{
			wnd.PostProcessKeyEvent (ka);
		}
		
		public static void HideWindow ()
		{
			wnd.Hide ();
		}
	}
	
	internal class CompletionListWindow : ListWindow, IListDataProvider
	{
		internal ICompletionWidget completionWidget;
		ICodeCompletionContext completionContext;
		DeclarationViewWindow declarationviewwindow = new DeclarationViewWindow ();
		ICompletionData currentData;
		ICompletionDataList completionDataList;
		IMutableCompletionDataList mutableList;
		Widget parsingMessage;
		CompletionDelegate closedDelegate;
		int initialWordLength;
		int previousWidth = -1, previousHeight = -1;
		
		const int declarationWindowMargin = 3;
		
		internal CompletionListWindow ()
		{
			SizeAllocated += new SizeAllocatedHandler (ListSizeChanged);
			WindowTransparencyDecorator.Attach (this);
		}
		
		protected override void OnDestroyed ()
		{
			if (declarationviewwindow != null) {
				declarationviewwindow.Destroy ();
				declarationviewwindow = null;
			}
			base.OnDestroyed ();
		}

		public void PostProcessKeyEvent (KeyAction ka)
		{
			if ((ka & KeyAction.Complete) != 0) 
				UpdateWord ();
		}
		
		public bool PreProcessKeyEvent (Gdk.Key key, Gdk.ModifierType modifier, out KeyAction ka)
		{
			 ka = ProcessKey (key, modifier);
			
			if ((ka & KeyAction.CloseWindow) != 0)
				Hide ();
			
			if ((ka & KeyAction.Ignore) != 0)
				return true;

			if ((ka & KeyAction.Process) != 0) {
				if (key == Gdk.Key.Left || key == Gdk.Key.Right) {
					// Close if there's a modifier active EXCEPT lock keys and Modifiers
					// Makes an exception for Mod1Mask (usually alt), shift and control
					// This prevents the window from closing if the num/scroll/caps lock are active
					// FIXME: modifier mappings depend on X server settings
					if ((modifier & ~(Gdk.ModifierType.LockMask | (Gdk.ModifierType.ModifierMask 
					    & ~Gdk.ModifierType.Mod1Mask & ~Gdk.ModifierType.ControlMask & ~Gdk.ModifierType.ShiftMask))
					) != 0) {
						Hide ();
						return false;
					}
					
					if (declarationviewwindow.Multiple) {
						if (key == Gdk.Key.Left)
							declarationviewwindow.OverloadLeft ();
						else
							declarationviewwindow.OverloadRight ();
						UpdateDeclarationView ();
					}
					return true;
				}
			}
			return false;
		}
		
		internal bool ShowListWindow (char firstChar, ICompletionDataList list, ICompletionWidget completionWidget,
		                              ICodeCompletionContext completionContext, CompletionDelegate closedDelegate)
		{
			if (mutableList != null) {
				mutableList.Changing -= OnCompletionDataChanging;
				mutableList.Changed -= OnCompletionDataChanged;
				HideFooter ();
			}
			
			//initialWordLength = 0;
			this.completionDataList = list;
			this.completionContext = completionContext;
			this.closedDelegate = closedDelegate;
			mutableList = completionDataList as IMutableCompletionDataList;
			
			if (mutableList != null) {
				mutableList.Changing += OnCompletionDataChanging;
				mutableList.Changed += OnCompletionDataChanged;
			
				if (mutableList.IsChanging)
					OnCompletionDataChanging (null, null);
			}
			
			this.completionWidget = completionWidget;

			if (FillList ()) {
				Reset (true);
				
				// makes control-space in midle of words to work
				string text = completionWidget.GetCompletionText (completionContext);
				if (text.Length == 0) {
					text = completionDataList.DefaultCompletionString;
					SelectEntry (text);
					initialWordLength = completionWidget.SelectedLength;
					Show ();
					return true;
				}
				
				initialWordLength = text.Length + completionWidget.SelectedLength;
				PartialWord = text; 
				//if there is only one matching result we take it by default
				if (completionDataList.AutoCompleteUniqueMatch && IsUniqueMatch && !IsChanging)
				{	
					UpdateWord ();
					Hide ();
				} else {
					Show ();
				}
				return true;
			}
			else {
				Hide ();
				return false;
			}
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
			
			this.Style = completionWidget.GtkStyle;
			
			//sort, sinking obsolete items to the bottoms
			//the string comparison is ordinal as that makes it an order of magnitude faster, which 
			//which makes completion triggering noticeably more responsive
			if (!completionDataList.IsSorted)
				completionDataList.Sort (new DataItemComparer ());
			
			DataProvider = this;
			
			Reposition (true);
			
			return true;
		}
		
		void Reposition (bool force)
		{
			int x = completionContext.TriggerXCoord - TextOffset;
			int y = completionContext.TriggerYCoord;
			
			int w, h;
			GetSize (out w, out h);
			
			if (!force && previousHeight != h && previousWidth != w)
				return;
			
			previousHeight = h;
			previousWidth = w;
			
			if ((x + w) > Screen.Width)
				x = Screen.Width - w;
			
			if ((y + h) > Screen.Height)
			{
				y = y - completionContext.TriggerTextHeight - h;
			}
			
			Move (x, y);
		}
		
		//smaller lists get size reallocated after FillList, so we have to reposition them
		//if the window size has changed since we last positioned it
		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			Reposition (false);
		}
		
		void UpdateWord ()
		{
			if (Selection == -1 || SelectionDisabled)
				return;
			
			ICompletionData item = currentData ?? completionDataList[Selection];
			if (item == null)
				return;
			string word = item.CompletionText;
			IActionCompletionData ac = item as IActionCompletionData;
			if (ac != null) {
				ac.InsertCompletionText (completionWidget, completionContext);
				return;
			}
			string partialWord = PartialWord;
			int partialWordLength = partialWord != null ? partialWord.Length : 0;
			int replaceLen = completionContext.TriggerWordLength + partialWordLength - initialWordLength;
			string pword = completionWidget.GetText (completionContext.TriggerOffset, completionContext.TriggerOffset + replaceLen);
			
			completionWidget.SetCompletionText (completionContext, pword, word);
		}
		
		public new void Hide ()
		{
			base.Hide ();
			declarationviewwindow.HideAll ();
			
			if (mutableList != null) {
				mutableList.Changing -= OnCompletionDataChanging;
				mutableList.Changed -= OnCompletionDataChanged;
				mutableList = null;
			}
			
			if (completionDataList != null) {
				if (completionDataList is IDisposable) {
					((IDisposable)completionDataList).Dispose ();
				}
				completionDataList = null;
			}
			
			if (closedDelegate != null) {
				closedDelegate ();
				closedDelegate = null;
			}
		}
		
		void ListSizeChanged (object obj, SizeAllocatedArgs args)
		{
			UpdateDeclarationView ();
		}

		protected override void DoubleClick ()
		{
			UpdateWord ();
			Hide ();
		}
		
		protected override void OnSelectionChanged ()
		{
			base.OnSelectionChanged ();
			UpdateDeclarationView ();
		}
		
		void UpdateDeclarationView ()
		{
			if (completionDataList == null || List.Selection >= completionDataList.Count || List.Selection == -1)
				return;

			if (List.GdkWindow == null) return;
			Gdk.Rectangle rect = List.GetRowArea (List.Selection);
			int listpos_x = 0, listpos_y = 0;
			while (listpos_x == 0 || listpos_y == 0)
				GetPosition (out listpos_x, out listpos_y);
			int vert = listpos_y + rect.Y;
			
			int lvWidth = 0, lvHeight = 0;
			while (lvWidth == 0)
				this.GdkWindow.GetSize (out lvWidth, out lvHeight);
			if (vert >= listpos_y + lvHeight - 2) {
				vert = listpos_y + lvHeight - rect.Height;
			} else if (vert < listpos_y) {
				vert = listpos_y;
			}

			ICompletionData data = completionDataList[List.Selection];
			IOverloadedCompletionData overloadedData = data as IOverloadedCompletionData;
			
			IList<ICompletionData> overloads; 
			if (overloadedData != null) {
				overloads = new List<ICompletionData> (overloadedData.GetOverloadedData ());
			} else {
				overloads = new ICompletionData[] { data };
			}

			declarationviewwindow.Hide ();
			
			if (data != currentData) {
				declarationviewwindow.Clear ();
				declarationviewwindow.Realize ();
				
				foreach (ICompletionData overload in overloads) {
					bool oDataHasMarkup = (overload.DisplayFlags & DisplayFlags.DescriptionHasMarkup) != 0;
						declarationviewwindow.AddOverload (oDataHasMarkup
							? overload.Description
							: GLib.Markup.EscapeText (overload.Description));
				}
			}
			
			currentData = data;
			if (declarationviewwindow.DescriptionMarkup.Length == 0)
				return;

			int dvwWidth, dvwHeight;

			declarationviewwindow.Move (this.Screen.Width+1, vert);
			
			declarationviewwindow.SetFixedWidth (-1);
			declarationviewwindow.ReshowWithInitialSize ();
			declarationviewwindow.ShowAll ();
			declarationviewwindow.Multiple = (overloadedData != null && overloadedData.IsOverloaded);

			declarationviewwindow.GdkWindow.GetSize (out dvwWidth, out dvwHeight);

			int horiz = listpos_x + lvWidth + declarationWindowMargin;
			if (this.Screen.Width - horiz >= lvWidth) {
				if (this.Screen.Width - horiz < dvwWidth)
					declarationviewwindow.SetFixedWidth (this.Screen.Width - horiz);
			} else {
				if (listpos_x - dvwWidth - declarationWindowMargin < 0) {
					declarationviewwindow.SetFixedWidth (listpos_x - declarationWindowMargin);
					dvwWidth = declarationviewwindow.SizeRequest ().Width;
				}
				horiz = listpos_x - dvwWidth - declarationWindowMargin;
			}

			declarationviewwindow.Move (horiz, vert);
		}
		
		#region IListDataProvider
		
		int IListDataProvider.ItemCount 
		{ 
			get { return completionDataList.Count; } 
		}
		
		string IListDataProvider.GetText (int n)
		{
			return completionDataList[n].DisplayText;
		}
		
		bool IListDataProvider.HasMarkup (int n)
		{
			return (completionDataList[n].DisplayFlags & DisplayFlags.Obsolete) != 0;
		}
		
		//NOTE: we only ever return markup for items marked as obsolete
		string IListDataProvider.GetMarkup (int n)
		{
			return "<s>" + GLib.Markup.EscapeText (completionDataList[n].DisplayText) + "</s>";
		}
		
		string IListDataProvider.GetCompletionText (int n)
		{
			return completionDataList[n].CompletionText;
		}
		
		Gdk.Pixbuf IListDataProvider.GetIcon (int n)
		{
			string iconName = completionDataList[n].Icon;
			if (string.IsNullOrEmpty (iconName))
				return null;
			return PixbufService.GetPixbuf (iconName, Gtk.IconSize.Menu);
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
			//try to capture full selection state so as not to interrupt user
			string last = null;
			if (Visible) {
				if (SelectionDisabled)
					last = PartialWord;
				else
					last = CompleteWord;
			}

			HideFooter ();
			if (Visible) {
				//don't reset the user-entered word when refilling the list
				Reset (false);
				FillList ();
				if (last != null)
					SelectEntry (last);
			}
		}
	}
	
	public delegate void CompletionDelegate ();
}
