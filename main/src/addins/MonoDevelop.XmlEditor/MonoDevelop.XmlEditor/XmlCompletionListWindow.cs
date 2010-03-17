//
// MonoDevelop XML Editor
//
// Copyright (C) 2004-2007 MonoDevelop Team
//

using System;
using System.Collections;

using Gtk;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Ide.CodeCompletion;

namespace MonoDevelop.XmlEditor
{
	public class XmlCompletionListWindow : XmlEditorListWindow, IListDataProvider
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
		
		static XmlCompletionListWindow wnd;
		
		static XmlCompletionListWindow ()
		{
			wnd = new XmlCompletionListWindow ();
		}
		
		internal XmlCompletionListWindow ()
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
					return;
				}
				
				wnd.PartialWord = text; 
				//if there is only one matching result we take it by default
				if (wnd.IsUniqueMatch && !wnd.IsChanging)
				{	
					wnd.UpdateWord ();
					wnd.Hide ();
				}
				
			} catch (Exception ex) {
				Console.WriteLine (ex);
			}
		}
		
		bool ShowListWindow (char firstChar, ICompletionDataProvider provider, ICompletionWidget completionWidget, ICodeCompletionContext completionContext, CompletionDelegate closedDelegate)
		{
			if (mutableProvider != null) {
				mutableProvider.CompletionDataChanging -= OnCompletionDataChanging;
				mutableProvider.CompletionDataChanged -= OnCompletionDataChanged;
			}
			
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
			
			// HACK - make window bigger for namespace completion.
			// This should probably be handled by the ListWidget/ListWindow
			// itself.
			if (firstChar == '=') {
				this.Resize(this.List.WidthRequest + 220, this.List.HeightRequest);
			}
			return true;
		}
		
		public static void HideWindow ()
		{
			wnd.Hide ();
		}
		
		/// <summary>
		/// Modified this method so that punctuation characters
		/// will not close the completion window.
		/// </summary>
		public static bool ProcessKeyEvent (Gdk.EventKey e)
		{
			if (!wnd.Visible) return false;
			
			XmlEditorListWindow.KeyAction ka = wnd.ProcessKey (e);
			
			if ((ka & XmlEditorListWindow.KeyAction.CloseWindow) != 0) {
				if (e.Key == Gdk.Key.Escape || e.Key == Gdk.Key.BackSpace || !System.Char.IsPunctuation((char)e.KeyValue)) {
					wnd.Hide ();
				} 
			}
			
			if ((ka & XmlEditorListWindow.KeyAction.Complete) != 0) {
				switch (e.Key) {
					case Gdk.Key.Tab:
					case Gdk.Key.Return:
					case Gdk.Key.ISO_Enter:
					case Gdk.Key.Key_3270_Enter:
					case Gdk.Key.KP_Enter:
						wnd.Hide ();
						wnd.UpdateWord ();
						break;
					default:
						break;
				}
			}
			
			if ((ka & XmlEditorListWindow.KeyAction.Ignore) != 0)
				return true;

			if ((ka & XmlEditorListWindow.KeyAction.Process) != 0) {
				if (e.Key == Gdk.Key.Left) {
					if (wnd.declarationviewwindow.Multiple) {
						wnd.declarationviewwindow.OverloadLeft ();
						wnd.UpdateDeclarationView ();
					}
					return true;
				} else if (e.Key == Gdk.Key.Right) {
					if (wnd.declarationviewwindow.Multiple) {
						wnd.declarationviewwindow.OverloadRight ();
						wnd.UpdateDeclarationView ();
					}
					return true;
				}
			}

			return false;
		}
		
		/// <summary>
		/// HACK - Insert the completion string not the text displayed in the 
		/// completion list and move the cursor between the attribute 
		/// quotes when an attribute is inserted.
		/// This should really be done using methods in the 
		/// ICompletionWidget class.  Here I am cheating and using
		/// the XmlEditorView and XmlCompletionData directly (The
		/// XmlCompletionData.InsertAction is not used in MonoDevelop).
		/// </summary>
//		void UpdateWord ()
//		{
//			XmlCompletionData data = (XmlCompletionData)completionData[List.Selection];
//
//			string completeWord = data.CompletionString;
//			completionWidget.SetCompletionText(completionContext, wnd.PartialWord, completeWord);
//			if (data.XmlCompletionDataType == XmlCompletionData.DataType.XmlAttribute) {
//				// Position cursor inside attribute value string.
//				XmlEditorView view = (XmlEditorView)completionWidget;
//				TextIter iter = view.Buffer.GetIterAtMark(view.Buffer.InsertMark);
//				iter.Offset--;
//				view.Buffer.PlaceCursor(iter);	
//			}
//			//completionWidget.SetCompletionText(wnd.PartialWord, wnd.CompleteWord);
//		}
		
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
				completionWidget.SetCompletionText (completionContext, wnd.PartialWord, word);
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
		
		protected override bool IsValidCompletionChar (char c)
		{
			return XmlParser.IsXmlNameChar(c);
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
			XmlCompletionData ccdata = (XmlCompletionData) data;

			string descMarkup = datawMarkup != null ? datawMarkup.DescriptionPango : data.Description;

			declarationviewwindow.Hide ();
			
			if (data != currentData) {
				declarationviewwindow.Clear ();
				declarationviewwindow.Realize ();
	
				declarationviewwindow.AddOverload (descMarkup);

				//foreach (CodeCompletionData odata in ccdata.GetOverloads ()) {
				//	ICompletionDataWithMarkup odatawMarkup = odata as ICompletionDataWithMarkup;
				//	declarationviewwindow.AddOverload (odatawMarkup == null ? odata.Description : odatawMarkup.DescriptionPango);
				//}
			}
			
			currentData = data;
			
			if (declarationviewwindow.DescriptionMarkup.Length == 0)
				return;

			int dvwWidth, dvwHeight;

			declarationviewwindow.Move (this.Screen.Width+1, vert);
			
			declarationviewwindow.SetFixedWidth (-1);
			declarationviewwindow.ReshowWithInitialSize ();
			declarationviewwindow.ShowAll ();
			//declarationviewwindow.Multiple = (ccdata.Overloads != 0);

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