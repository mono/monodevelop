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

using MonoDevelop.TextEditor.Document;
using MonoDevelop.TextEditor.Util;
using MonoDevelop.TextEditor;
using Gtk;
using GtkSharp;

namespace MonoDevelop.TextEditor.Gui.InsightWindow
{
	public class InsightWindow : Window
	{
		static GLib.GType type;
		TextEditorControl control;
		Stack             insightDataProviderStack = new Stack();
		
		EventHandler         focusEventHandler;
#if GTK
		// FIXME: GTKize?
#else
		KeyPressEventHandler keyPressEventHandler;
#endif
		
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
			provider.SetupDataProvider(fileName, control.ActiveTextAreaControl.TextArea);
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
				Hide();
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
					//this.ShowAll ();
//					dialogKeyProcessor = new TextEditorControl.DialogKeyProcessor(ProcessTextAreaKey);
//					control.ProcessDialogKeyProcessor += dialogKeyProcessor;
					control.GrabFocus ();

//					control.TextAreaPainter.IHaveTheFocus     = true;
//					control.TextAreaPainter.IHaveTheFocusLock = false;
				}
			} else {

				this.QueueDraw ();

			}
		}
		string fileName;
		
		static InsightWindow ()
		{
			type = RegisterGType (typeof (InsightWindow));
		}
		
		public InsightWindow(TextEditorControl control, string fileName) : base (type)
		{
			this.control             = control;
			this.fileName = fileName;
			System.Drawing.Point caretPos  = control.ActiveTextAreaControl.TextArea.Caret.Position;
			System.Drawing.Point visualPos = new System.Drawing.Point(control.ActiveTextAreaControl.TextArea.TextView.GetDrawingXPos(caretPos.Y, caretPos.X) + control.ActiveTextAreaControl.TextArea.TextView.DrawingPosition.X,
			          (int)((1 + caretPos.Y) * control.ActiveTextAreaControl.TextArea.TextView.FontHeight) - control.ActiveTextAreaControl.TextArea.VirtualTop.Y - 1 + control.ActiveTextAreaControl.TextArea.TextView.DrawingPosition.Y);
			
			focusEventHandler = new EventHandler(TextEditorLostFocus);
			
			control.ActiveTextAreaControl.Caret.PositionChanged += new EventHandler(CaretOffsetChanged);
//			control.TextAreaPainter.IHaveTheFocusChanged += focusEventHandler;
			
//			control.TextAreaPainter.KeyPress += keyPressEventHandler;
			
#if GTK
			this.SkipPagerHint = true;
			this.SkipTaskbarHint = true;
			this.Decorated = false;
			this.BorderWidth = 2;
			this.TypeHint = Gdk.WindowTypeHint.Dialog;
#else
	 		Location = control.ActiveTextAreaControl.PointToScreen(visualPos);
			
			StartPosition   = FormStartPosition.Manual;
			FormBorderStyle = FormBorderStyle.None;
			TopMost         = true;
			ShowInTaskbar   = false;
			Size            = new Size(0, 0);
			
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.DoubleBuffer, true);
#endif
		}
		
		// Methods that are inserted into the TextArea :
		bool ProcessTextAreaKey(Gdk.Key keyData)
		{
			switch (keyData) {
				case Gdk.Key.Escape:
					Hide();
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
		
#if GTK
		// FIXME: GTKize
#else
		void KeyPressEvent(object sender, KeyPressEventArgs e)
		{
			if (DataProvider != null && DataProvider.CharTyped()) {
				CloseCurrentDataProvider();
			}
		}
#endif
		
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
		
#if !GTK
		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);
			
			// take out the inserted methods
			control.ActiveTextAreaControl.Caret.PositionChanged -= new EventHandler(CaretOffsetChanged);
//			control.ProcessDialogKeyProcessor            -= dialogKeyProcessor;
//			control.TextAreaPainter.IHaveTheFocusChanged -= focusEventHandler;
//			control.TextAreaPainter.KeyPress             -= keyPressEventHandler;
		}
#endif
		
		protected void TextEditorLostFocus(object sender, EventArgs e)
		{
			if (!control.ActiveTextAreaControl.TextArea.IsFocus) {
				Hide();
			}
		}

#if GTK
		protected void BeforeShow ()
		{
			string methodCountMessage = null, description;
			if (DataProvider == null || DataProvider.InsightDataCount < 1) {
				description = "Unknown Method";
			} else {
				/*if (DataProvider.InsightDataCount > 1) {
					StringParserService stringParserService = (StringParserService)ServiceManager.Services.GetService(typeof(StringParserService));
					stringParserService.Properties["CurrentMethodNumber"]  = (CurrentData + 1).ToString();
					stringParserService.Properties["NumberOfTotalMethods"] = DataProvider.InsightDataCount.ToString();
					methodCountMessage = stringParserService.Parse("${res:MonoDevelop.DefaultEditor.Gui.Editor.InsightWindow.NumberOfText}");
				}*/
				
				description = DataProvider.GetInsightData(CurrentData);
			}
			
			Console.WriteLine ("Current Data: {0}", CurrentData);
			Console.WriteLine ("Description: {0}", description);
			//TipPainterTools.DrawHelpTipFromCombinedDescription(this, pe.Graphics,
			//	Font, methodCountMessage, description);
		}
#else
		protected override void OnPaint(PaintEventArgs pe)
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
		}
		
		protected override void OnPaintBackground(PaintEventArgs pe)
		{
			pe.Graphics.FillRectangle(SystemBrushes.Info, pe.ClipRectangle);
		}
#endif
	}
}
