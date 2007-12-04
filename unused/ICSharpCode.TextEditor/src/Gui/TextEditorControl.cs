//  TextEditorControl.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using System.Collections;
using System.IO;
using System.ComponentModel;
using System.Drawing.Printing;
using System.Diagnostics;
using System.Text;

using MonoDevelop.TextEditor.Document;
using MonoDevelop.TextEditor.Actions;

namespace MonoDevelop.TextEditor
{
	/// <summary>
	/// This class is used for a basic text area control
	/// </summary>
//	[ToolboxBitmap("MonoDevelop.TextEditor.TextEditorControl.bmp")]
	[ToolboxItem(true)]
	public class TextEditorControl : TextEditorControlBase
	{
		TextAreaControl primaryTextArea;
#if GTK
		// FIXME: GTKize
		//VPaned pane = new VPaned ();
#else
		Splitter        textAreaSplitter  = null;
#endif
		TextAreaControl secondaryTextArea = null;
		
		public PrintDocument PrintDocument {
			get {
				PrintDocument printDocument = new PrintDocument();
				printDocument.PrintPage += new PrintPageEventHandler(this.PrintPage);
				return null;
			}
		}
		
		public TextAreaControl ActiveTextAreaControl {
			get {
				return primaryTextArea;
			}
		}
		
		public TextEditorControl()
		{
			Document = (new DocumentFactory()).CreateDocument();
			Document.HighlightingStrategy = HighlightingStrategyFactory.CreateHighlightingStrategy();
			
			primaryTextArea  = new TextAreaControl(this);
#if GTK
			// FIXME: GTKize
			//pane.Add1(primaryTextArea);
			PackEnd(primaryTextArea, true, true, 0);
#else
			primaryTextArea  = new TextAreaControl(this);
			primaryTextArea.Dock = DockStyle.Fill;
			Controls.Add(primaryTextArea);
			ResizeRedraw = true;
#endif
			InitializeTextAreaControl(primaryTextArea);
			Document.UpdateCommited += new EventHandler(CommitUpdateRequested);
			OptionsChanged();
		}

		protected virtual void InitializeTextAreaControl(TextAreaControl newControl)
		{
		}
		
		public override void OptionsChanged()
		{
			primaryTextArea.OptionsChanged();
			if (secondaryTextArea != null) {
				secondaryTextArea.OptionsChanged();
			}
		}

#if GTK
		// FIXME: GTKize
#else
		public void Split()
		{
			if (secondaryTextArea == null) {
				secondaryTextArea = new TextAreaControl(this);
				secondaryTextArea.Dock = DockStyle.Bottom;
				secondaryTextArea.Height = Height / 2;
				textAreaSplitter =  new Splitter();
				textAreaSplitter.BorderStyle = BorderStyle.FixedSingle ;
				textAreaSplitter.Height = 8;
				textAreaSplitter.Dock = DockStyle.Bottom;
				Controls.Add(textAreaSplitter);
				Controls.Add(secondaryTextArea);
				InitializeTextAreaControl(secondaryTextArea);
				secondaryTextArea.OptionsChanged();
			} else {
				Controls.Remove(secondaryTextArea);
				Controls.Remove(textAreaSplitter);
				
				secondaryTextArea.Dispose();
				textAreaSplitter.Dispose();
				secondaryTextArea = null;
				textAreaSplitter  = null;
			}
		}
#endif
		
		public void Undo()
		{
			if (Document.ReadOnly) {
				return;
			}
			if (Document.UndoStack.CanUndo) {
				BeginUpdate();
				Document.UndoStack.Undo();
				
				Document.UpdateQueue.Clear();
				Document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.WholeTextArea));
				this.primaryTextArea.TextArea.UpdateMatchingBracket();
				if (secondaryTextArea != null) {
					this.secondaryTextArea.TextArea.UpdateMatchingBracket();
				}
				EndUpdate();
				//this.primaryTextArea.TextArea.Refresh ();
			}
		}
		
		public void Redo()
		{
			if (Document.ReadOnly) {
				return;
			}
			if (Document.UndoStack.CanRedo) {
				BeginUpdate();
				Document.UndoStack.Redo();
				
				Document.UpdateQueue.Clear();
				Document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.WholeTextArea));
				this.primaryTextArea.TextArea.UpdateMatchingBracket();
				if (secondaryTextArea != null) {
					this.secondaryTextArea.TextArea.UpdateMatchingBracket();
				}
				EndUpdate();
				//this.primaryTextArea.TextArea.Refresh ();
			}
		}
		
		public void SetHighlighting(string name)
		{
			Document.HighlightingStrategy = HighlightingStrategyFactory.CreateHighlightingStrategy(name);
		}
		
		
#region Update Methods
		public override void EndUpdate()
		{
			base.EndUpdate();
			Document.CommitUpdate();
		}
		
		void CommitUpdateRequested(object sender, EventArgs e)
		{
			if (IsUpdating) {
				return;
			}
			foreach (TextAreaUpdate update in Document.UpdateQueue) {
				switch (update.TextAreaUpdateType) {
					case TextAreaUpdateType.PositionToEnd:
						this.primaryTextArea.TextArea.UpdateToEnd(update.Position.Y);
						if (this.secondaryTextArea != null) {
							this.secondaryTextArea.TextArea.UpdateToEnd(update.Position.Y);
						}
						break;
					case TextAreaUpdateType.PositionToLineEnd:
					case TextAreaUpdateType.SingleLine:
						this.primaryTextArea.TextArea.UpdateLine(update.Position.Y);
						if (this.secondaryTextArea != null) {
							this.secondaryTextArea.TextArea.UpdateLine(update.Position.Y);
						}
						break;
					case TextAreaUpdateType.SinglePosition:
						this.primaryTextArea.TextArea.UpdateLine(update.Position.Y, update.Position.X, update.Position.X);
						if (this.secondaryTextArea != null) {
							this.secondaryTextArea.TextArea.UpdateLine(update.Position.Y, update.Position.X, update.Position.X);
						}
						break;
					case TextAreaUpdateType.LinesBetween:
						this.primaryTextArea.TextArea.UpdateLines(update.Position.X, update.Position.Y);
						if (this.secondaryTextArea != null) {
							this.secondaryTextArea.TextArea.UpdateLines(update.Position.X, update.Position.Y);
						}
						break;
					case TextAreaUpdateType.WholeTextArea:
						this.primaryTextArea.TextArea.Refresh();
						if (this.secondaryTextArea != null) {
							this.secondaryTextArea.TextArea.Refresh();
						}
						break;
				}
			}
			Document.UpdateQueue.Clear();
#if GTK
			// FIXME: GTKize
#else
			this.primaryTextArea.TextArea.Update();
			if (this.secondaryTextArea != null) {
				this.secondaryTextArea.TextArea.Update();
			}
#endif
//			Console.WriteLine("-------END");
		}
#endregion
		
#region Printing routines
		void PrintPage(object sender, PrintPageEventArgs ev)
		{
//			float leftMargin = ev.MarginBounds.Left;
//			float topMargin  = ev.MarginBounds.Top;
//			
//			// Calculate the number of lines per page.
//			int linesPerPage = ev.MarginBounds.Height / this.TextEditorProperties.Font.GetHeight(ev.Graphics);
			
			//TODO
		}
#endregion
	}
}
