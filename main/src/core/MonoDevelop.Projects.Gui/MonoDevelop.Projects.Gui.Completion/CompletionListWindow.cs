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
using System.Collections;

using Gtk;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;

namespace MonoDevelop.Projects.Gui.Completion
{
	public class CompletionWindowManager
	{
		static CompletionListWindow wnd;
		
		static CompletionWindowManager ()
		{
			wnd = new CompletionListWindow ();
		}
		
		public static bool ShowWindow (char firstChar, ICompletionDataProvider provider, ICompletionWidget completionWidget, ICodeCompletionContext completionContext, CompletionDelegate closedDelegate)
		{
			try {
				if (!wnd.ShowListWindow (firstChar, provider,  completionWidget, completionContext, closedDelegate)) {
					provider.Dispose ();
					return false;
				}
				return true;
			} catch (Exception ex) {
				LoggingService.LogError (ex.ToString ());
				return false;
			}
		}
		
		public static bool ProcessKeyEvent (Gdk.Key key, Gdk.ModifierType modifier)
		{
			if (!wnd.Visible) return false;
			return wnd.ProcessKeyEvent (key, modifier);
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
		ICompletionData[] completionData;
		DeclarationViewWindow declarationviewwindow = new DeclarationViewWindow ();
		ICompletionData currentData;
		ICompletionDataProvider provider;
		IMutableCompletionDataProvider mutableProvider;
		Widget parsingMessage;
		char firstChar;
		CompletionDelegate closedDelegate;
		int initialWordLength;
		
		const int declarationWindowMargin = 3;
		static DataComparer dataComparer = new DataComparer ();
		
		class DataComparer: IComparer
		{
			public int Compare (object x, object y)
			{
				ICompletionData d1 = x as ICompletionData;
				ICompletionData d2 = y as ICompletionData;
				return String.Compare (d1.Text[0], d2.Text[0], true);
			}
		}
		
		internal CompletionListWindow ()
		{
			SizeAllocated += new SizeAllocatedHandler (ListSizeChanged);
			WindowTransparencyDecorator.Attach (this);
		}
		
		public bool ProcessKeyEvent (Gdk.Key key, Gdk.ModifierType modifier)
		{
			ListWindow.KeyAction ka = ProcessKey (key, modifier);
			
			if ((ka & ListWindow.KeyAction.CloseWindow) != 0)
				Hide ();
				
			if ((ka & ListWindow.KeyAction.Complete) != 0) {
				UpdateWord ();
			}
			
			if ((ka & ListWindow.KeyAction.Ignore) != 0)
				return true;

			if ((ka & ListWindow.KeyAction.Process) != 0) {
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
		
		internal bool ShowListWindow (char firstChar, ICompletionDataProvider provider, ICompletionWidget completionWidget, ICodeCompletionContext completionContext, CompletionDelegate closedDelegate)
		{
			if (mutableProvider != null) {
				mutableProvider.CompletionDataChanging -= OnCompletionDataChanging;
				mutableProvider.CompletionDataChanged -= OnCompletionDataChanged;
			}
			
			//initialWordLength = 0;
			this.provider = provider;
			this.completionContext = completionContext;
			this.closedDelegate = closedDelegate;
			mutableProvider = provider as IMutableCompletionDataProvider;
			
			if (mutableProvider != null) {
				mutableProvider.CompletionDataChanging += OnCompletionDataChanging;
				mutableProvider.CompletionDataChanged += OnCompletionDataChanged;
			
				if (mutableProvider.IsChanging)
					OnCompletionDataChanging (null, null);
			}
			
			this.completionWidget = completionWidget;
			this.firstChar = firstChar;

			if (FillList (true)) {
				// makes control-space in midle of words to work
				string text = completionWidget.GetCompletionText (completionContext);
				if (text.Length == 0) {
					text = provider.DefaultCompletionString;
					if (text != null && text.Length > 0)
						SelectEntry (text);
					initialWordLength = completionWidget.SelectedLength;
					return true;
				}
				
				initialWordLength = text.Length + completionWidget.SelectedLength;
				PartialWord = text; 
				//if there is only one matching result we take it by default
				if (IsUniqueMatch && !IsChanging)
				{	
					UpdateWord ();
					Hide ();
				}
				return true;
			}
			else
				return false;
		}
		
		bool FillList (bool reshow)
		{
			completionData = provider.GenerateCompletionData (completionWidget, firstChar);
			if ((completionData == null || completionData.Length == 0) && !IsChanging)
				return false;
			
			this.Style = completionWidget.GtkStyle;
			
			if (completionData == null)
				completionData = new ICompletionData [0];
			else
				Array.Sort (completionData, dataComparer);
			
			DataProvider = this;

			int x = completionContext.TriggerXCoord;
			int y = completionContext.TriggerYCoord;
			
			int w, h;
			GetSize (out w, out h);
			
			if ((x + w) > Screen.Width)
				x = Screen.Width - w;
			
			if ((y + h) > Screen.Height)
			{
				y = y - completionContext.TriggerTextHeight - h;
			}

			Move (x, y);
			
			if (reshow)
				Show ();
			return true;
		}
		
		void UpdateWord ()
		{
			string word = CompleteWord;
			
			if (word != null) {
				if (Selection != -1) {
					IActionCompletionData ac = completionData [Selection] as IActionCompletionData;
					if (ac != null) {
						ac.InsertAction (completionWidget, completionContext);
						return;
					}
				}
				int replaceLen = completionContext.TriggerWordLength + PartialWord.Length - initialWordLength;
				string pword = completionWidget.GetText (completionContext.TriggerOffset, completionContext.TriggerOffset + replaceLen);
				
				completionWidget.SetCompletionText (completionContext, pword, word);
			}
		}
		
		public new void Hide ()
		{
			base.Hide ();
			declarationviewwindow.HideAll ();
			if (provider != null) {
				provider.Dispose ();
				provider = null;
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

		protected override bool OnButtonPressEvent (Gdk.EventButton evnt)
		{
			bool ret = base.OnButtonPressEvent (evnt);
			if (evnt.Button == 1 && evnt.Type == Gdk.EventType.TwoButtonPress) {
				UpdateWord ();
				Hide ();
			}
			return ret;
		}
		
		protected override void OnSelectionChanged ()
		{
			base.OnSelectionChanged ();
			UpdateDeclarationView ();
		}
		
		void UpdateDeclarationView ()
		{
			if (completionData == null || List.Selection >= completionData.Length || List.Selection == -1)
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

			ICompletionData data = completionData[List.Selection];
			ICompletionDataWithMarkup datawMarkup = data as ICompletionDataWithMarkup;
			CodeCompletionData ccdata = data as CodeCompletionData;

			string descMarkup = datawMarkup != null ? datawMarkup.DescriptionPango : data.Description;

			declarationviewwindow.Hide ();
			
			if (data != currentData) {
				declarationviewwindow.Clear ();
				declarationviewwindow.Realize ();
	
				declarationviewwindow.AddOverload (descMarkup);

				if (ccdata != null) {
					foreach (CodeCompletionData odata in ccdata.GetOverloads ()) {
						ICompletionDataWithMarkup odatawMarkup = odata as ICompletionDataWithMarkup;
						declarationviewwindow.AddOverload (odatawMarkup == null ? odata.Description : odatawMarkup.DescriptionPango);
					}
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
			declarationviewwindow.Multiple = (ccdata != null && ccdata.Overloads != 0);

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
		
		public int ItemCount 
		{ 
			get { return completionData.Length; } 
		}
		
		public string GetText (int n)
		{
			return completionData[n].Text[0];
		}
		
		public string GetCompletionText (int n)
		{
			return completionData[n].CompletionString;
		}
		
		public Gdk.Pixbuf GetIcon (int n)
		{
			return RenderIcon (completionData[n].Image, Gtk.IconSize.Menu, "");
		}
		
		internal bool IsChanging {
			get { return mutableProvider != null && mutableProvider.IsChanging; }
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
				FillList (false);
				if (last != null)
					SelectEntry (last);
			}
		}
	}
	
	public delegate void CompletionDelegate ();
}
