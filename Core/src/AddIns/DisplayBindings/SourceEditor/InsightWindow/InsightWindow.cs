// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike KrÃÂ¼ger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System;
using System.Drawing;
using System.Reflection;
using System.Collections;

using MonoDevelop.Core.Services;
using Gtk;
using GtkSharp;

using MonoDevelop.SourceEditor.Gui;
using MonoDevelop.Internal.Project;

namespace MonoDevelop.SourceEditor.InsightWindow
{
	public class InsightWindow : Window
	{
		SourceEditorView  control;
		Stack             insightDataProviderStack = new Stack();

		Gtk.Label desc;
		Gtk.Label current;
		Gtk.Label max;
		string description;
		string fileName;
		Project project;

		StringParserService StringParserService = (StringParserService)ServiceManager.GetService (typeof (StringParserService)); 
		
		class InsightDataProviderStackElement 
		{
			public int                  currentData;
			public IInsightDataProvider dataProvider;
			
			public InsightDataProviderStackElement(IInsightDataProvider dataProvider)
			{
				this.currentData  = 0;
				this.dataProvider = dataProvider;
			}
		}
		
		public void AddInsightDataProvider(IInsightDataProvider provider)
		{
			provider.SetupDataProvider(project, fileName, control);
			if (provider.InsightDataCount > 0) {
				insightDataProviderStack.Push(new InsightDataProviderStackElement(provider));
			}
		}
		
		int CurrentData {
			get {
				return ((InsightDataProviderStackElement)insightDataProviderStack.Peek()).currentData;
			}
			set {
				((InsightDataProviderStackElement)insightDataProviderStack.Peek()).currentData = value;
				desc.Text = DataProvider.GetInsightData (CurrentData);
				current.Text = (CurrentData + 1).ToString ();
				max.Text = DataProvider.InsightDataCount.ToString ();
				ReshowWithInitialSize ();
			}
		}
		
		IInsightDataProvider DataProvider {
			get {
				if (insightDataProviderStack.Count == 0) {
					return null;
				}
				return ((InsightDataProviderStackElement)insightDataProviderStack.Peek()).dataProvider;
			}
		}
		
		void CloseCurrentDataProvider()
		{
			insightDataProviderStack.Pop();
			if (insightDataProviderStack.Count == 0) {
				Destroy ();
			} else {
				this.QueueDraw ();
			}
		}
		
		public void ShowInsightWindow()
		{
			if (!Visible) {
				if (insightDataProviderStack.Count > 0) {
//					control.TextAreaPainter.IHaveTheFocusLock = true;
					BeforeShow ();
					this.ShowAll ();
//					dialogKeyProcessor = new TextEditorControl.DialogKeyProcessor(ProcessTextAreaKey);
//					control.ProcessDialogKeyProcessor += dialogKeyProcessor;
					//control.GrabFocus ();

//					control.TextAreaPainter.IHaveTheFocus     = true;
//					control.TextAreaPainter.IHaveTheFocusLock = false;
					desc.Text = description = DataProvider.GetInsightData(CurrentData);
					
				}
			} else {

				this.QueueDraw ();

			}
		}
		
		public InsightWindow (SourceEditorView control, Project project, string fileName) : base (WindowType.Popup)
		{
			this.control             = control;
			this.fileName = fileName;
			this.project = project;
			/*System.Drawing.Point caretPos  = control.ActiveTextAreaControl.TextArea.Caret.Position;
			System.Drawing.Point visualPos = new System.Drawing.Point(control.ActiveTextAreaControl.TextArea.TextView.GetDrawingXPos(caretPos.Y, caretPos.X) + control.ActiveTextAreaControl.TextArea.TextView.DrawingPosition.X,
			          (int)((1 + caretPos.Y) * control.ActiveTextAreaControl.TextArea.TextView.FontHeight) - control.ActiveTextAreaControl.TextArea.VirtualTop.Y - 1 + control.ActiveTextAreaControl.TextArea.TextView.DrawingPosition.Y);*/
			
			
			//control.ActiveTextAreaControl.Caret.PositionChanged += new EventHandler(CaretOffsetChanged);
//			control.TextAreaPainter.IHaveTheFocusChanged += focusEventHandler;
			
			AddEvents ((int) (Gdk.EventMask.KeyPressMask));
			this.SkipPagerHint = true;
			this.SkipTaskbarHint = true;
			this.Decorated = false;
			this.BorderWidth = 2;
			this.TypeHint = Gdk.WindowTypeHint.Dialog;

			desc = new Gtk.Label ("");
			current = new Gtk.Label ("");
			max = new Gtk.Label ("");
			HBox mainBox = new HBox (false, 2);
			mainBox.PackStart (new Gtk.Image (Gtk.Stock.GotoTop, Gtk.IconSize.Menu), false, false, 2);
			mainBox.PackStart (current, false, false, 2);
			mainBox.PackStart (new Gtk.Label (" of "), false, false, 2);
			mainBox.PackStart (max, false, false, 2);
			mainBox.PackStart (new Gtk.Image (Gtk.Stock.GotoBottom, Gtk.IconSize.Menu), false, false, 2);
			mainBox.PackStart (desc);
			Gtk.Frame framer = new Gtk.Frame ();
			framer.Add (mainBox);
			this.Add (framer);
		}
		
		// Methods that are inserted into the TextArea :
		bool ProcessTextAreaKey(Gdk.Key keyData)
		{
			switch (keyData) {
				case Gdk.Key.Escape:
					Destroy ();
					return true;
				case Gdk.Key.Down:
					if (DataProvider != null && DataProvider.InsightDataCount > 0) {
						CurrentData = (CurrentData + 1) % DataProvider.InsightDataCount;
						this.QueueDraw ();
					}
					return true;
				case Gdk.Key.Up:
					if (DataProvider != null && DataProvider.InsightDataCount > 0) {
						CurrentData = (CurrentData + DataProvider.InsightDataCount - 1) % DataProvider.InsightDataCount;
						this.QueueDraw ();
					}
					return true;
			}
			return false;
		}
		
		protected override bool OnKeyPressEvent (Gdk.EventKey e)
		{
			bool rval;
			if (ProcessTextAreaKey (e.Key) == false) {
				control.SimulateKeyPress (ref e);
				rval = true;
			} else {
				rval = base.OnKeyPressEvent (e);
			}
			if (DataProvider != null && DataProvider.CharTyped ()) {
				CloseCurrentDataProvider ();
			}
			return rval;
		}
		
		void CaretOffsetChanged(object sender, EventArgs e)
		{
			// move the window under the caret (don't change the x position)

			//FIXME: This code nullrefs for some reason
			/*
			System.Drawing.Point caretPos  = control.ActiveTextAreaControl.Caret.Position;
			int y = (int)((1 + caretPos.Y) * control.ActiveTextAreaControl.TextArea.TextView.FontHeight) - control.ActiveTextAreaControl.TextArea.VirtualTop.Y - 1 + control.ActiveTextAreaControl.TextArea.TextView.DrawingPosition.Y;

#if GTK			
			System.Drawing.Point p = control.RootWindow.Position;
			
			p.X = RootWindow.Position.X;
			if (p.Y != RootWindow.Position.Y) {
				RootWindow.Move (p);
			}
#else
			Point p = control.ActiveTextAreaControl.PointToScreen(new Point(0, y));
			p.X = Location.X;
			if (p.Y != Location.Y) {
				Location = p;
			}
#endif
			
			while (DataProvider != null && DataProvider.CaretOffsetChanged()) {
				 CloseCurrentDataProvider();
			}*/
		}
		
/*		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);
			
			// take out the inserted methods
			//control.MoveCursor -= new GtkSharp.MoveCursorHandler(CaretOffsetChanged);
//			control.ProcessDialogKeyProcessor            -= dialogKeyProcessor;

//			control.TextAreaPainter.IHaveTheFocusChanged -= focusEventHandler;
//			control.TextAreaPainter.KeyPress             -= keyPressEventHandler;
		}*/
		
		protected void TextEditorLostFocus(object sender, EventArgs e)
		{
			if (!control.HasFocus) {
				Hide();
			}
		}

		protected void BeforeShow ()
		{
			//string methodCountMessage = null;
			if (DataProvider == null || DataProvider.InsightDataCount < 1) {
				description = "Unknown Method";
			} else {
				if (DataProvider.InsightDataCount > 1) {
					StringParserService stringParserService = (StringParserService)ServiceManager.GetService(typeof(StringParserService));
					stringParserService.Properties["CurrentMethodNumber"]  = (CurrentData + 1).ToString();
					stringParserService.Properties["NumberOfTotalMethods"] = DataProvider.InsightDataCount.ToString();
					//methodCountMessage = stringParserService.Parse("${res:MonoDevelop.DefaultEditor.Gui.Editor.InsightWindow.NumberOfText}");
				}
				
				//I know this call looks stupid, but really it isnt.
				CurrentData = CurrentData;
				QueueDraw ();
			}
		}
		
		/*protected override void OnPaint(PaintEventArgs pe)
		{
			string methodCountMessage = null, description;
			if (DataProvider == null || DataProvider.InsightDataCount < 1) {
				description = "Unknown Method";
			} else {
//				if (DataProvider.InsightDataCount > 1) {
//					StringParserService stringParserService = (StringParserService)ServiceManager.Services.GetService(typeof(StringParserService));
//					stringParserService.Properties["CurrentMethodNumber"]  = (CurrentData + 1).ToString();
//					stringParserService.Properties["NumberOfTotalMethods"] = DataProvider.InsightDataCount.ToString();
//					methodCountMessage = stringParserService.Parse("${res:MonoDevelop.DefaultEditor.Gui.Editor.InsightWindow.NumberOfText}");
//				}
				
				description = DataProvider.GetInsightData(CurrentData);
			}
			
			TipPainterTools.DrawHelpTipFromCombinedDescription(this, pe.Graphics,
				Font, methodCountMessage, description);
		}*/
	}
}
