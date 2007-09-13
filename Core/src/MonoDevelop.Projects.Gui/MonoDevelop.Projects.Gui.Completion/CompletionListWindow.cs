using System;
using System.Collections;

using Gtk;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;

namespace MonoDevelop.Projects.Gui.Completion
{
	public class CompletionListWindow : ListWindow, IListDataProvider
	{
		ICompletionWidget completionWidget;
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
		
		public static event EventHandler WindowClosed;
		
		class DataComparer: IComparer
		{
			public int Compare (object x, object y)
			{
				ICompletionData d1 = x as ICompletionData;
				ICompletionData d2 = y as ICompletionData;
				return String.Compare (d1.Text[0], d2.Text[0], true);
			}
		}
		
		static CompletionListWindow wnd;
		
		static CompletionListWindow ()
		{
			wnd = new CompletionListWindow ();
		}
		
		internal CompletionListWindow ()
		{
			SizeAllocated += new SizeAllocatedHandler (ListSizeChanged);
		}
		
		public static void ShowWindow (char firstChar, ICompletionDataProvider provider, ICompletionWidget completionWidget, ICodeCompletionContext completionContext, CompletionDelegate closedDelegate)
		{
			try {
				if (!wnd.ShowListWindow (firstChar, provider,  completionWidget, completionContext, closedDelegate)) {
					provider.Dispose ();
					return;
				}
				
				// makes control-space in midle of words to work
				string text = wnd.completionWidget.GetCompletionText (completionContext);
				if (text.Length == 0) {
					text = provider.DefaultCompletionString;
					if (text != null && text.Length > 0)
						wnd.SelectEntry (text);
					wnd.initialWordLength = wnd.completionWidget.SelectedLength;
					return;
				}
				
				wnd.initialWordLength = text.Length + wnd.completionWidget.SelectedLength;
				wnd.PartialWord = text; 
				//if there is only one matching result we take it by default
				if (wnd.IsUniqueMatch && !wnd.IsChanging)
				{	
					wnd.UpdateWord ();
					wnd.Hide ();
				}
				
			} catch (Exception ex) {
				Runtime.LoggingService.Error (ex);
			}
		}
		
		bool ShowListWindow (char firstChar, ICompletionDataProvider provider, ICompletionWidget completionWidget, ICodeCompletionContext completionContext, CompletionDelegate closedDelegate)
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

			return FillList ();
		}
		
		bool FillList ()
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
			
			Show ();
			return true;
		}
		
		public static void HideWindow ()
		{
			wnd.Hide ();
		}
		
		public static bool ProcessKeyEvent (Gdk.Key key, Gdk.ModifierType modifier)
		{
			if (!wnd.Visible) return false;
			
			ListWindow.KeyAction ka = wnd.ProcessKey (key, modifier);
			
			if ((ka & ListWindow.KeyAction.CloseWindow) != 0)
				wnd.Hide ();
				
			if ((ka & ListWindow.KeyAction.Complete) != 0) {
				wnd.UpdateWord ();
			}
			
			if ((ka & ListWindow.KeyAction.Ignore) != 0)
				return true;

			if ((ka & ListWindow.KeyAction.Process) != 0) {
				if (key == Gdk.Key.Left || key == Gdk.Key.Right) {
					if (modifier != 0) {
						wnd.Hide ();
						return false;
					}
					if (wnd.declarationviewwindow.Multiple) {
						if (key == Gdk.Key.Left)
							wnd.declarationviewwindow.OverloadLeft ();
						else
							wnd.declarationviewwindow.OverloadRight ();
						wnd.UpdateDeclarationView ();
					}
					return true;
				}
			}

			return false;
		}
		
		void UpdateWord ()
		{
			string word = wnd.CompleteWord;
			
			if (word != null) {
				if (wnd.Selection != -1) {
					IActionCompletionData ac = completionData [wnd.Selection] as IActionCompletionData;
					if (ac != null) {
						ac.InsertAction (completionWidget, completionContext);
						return;
					}
				}
				int replaceLen = completionContext.TriggerWordLength + wnd.PartialWord.Length - initialWordLength;
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
				wnd.UpdateWord ();
				wnd.Hide ();
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
			wnd.ShowFooter (parsingMessage);
		}
		
		void OnCompletionDataChanged (object s, EventArgs args)
		{
			wnd.HideFooter ();
			if (Visible) {
				Reset ();
				FillList ();
			}
		}
	}
	
	public delegate void CompletionDelegate ();
}
