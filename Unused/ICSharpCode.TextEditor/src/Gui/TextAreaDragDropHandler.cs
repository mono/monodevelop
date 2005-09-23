// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.IO;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Drawing.Text;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Diagnostics;
using System.Runtime.Remoting;
using System.Runtime.InteropServices;
using System.Xml;

using MonoDevelop.TextEditor.Document;

namespace MonoDevelop.TextEditor
{
	public class TextAreaDragDropHandler
	{
		TextArea textArea;
		
		public void Attach(TextArea textArea)
		{
			this.textArea = textArea;
			//textArea.AllowDrop = true;
			
			//textArea.DragEnter += new DragEventHandler(OnDragEnter);
			//textArea.DragDrop  += new DragEventHandler(OnDragDrop);
			//textArea.DragOver  += new DragEventHandler(OnDragOver);
		}
		
		
		/*static DragDropEffects GetDragDropEffect(DragEventArgs e)
		{
			if ((e.AllowedEffect & DragDropEffects.Move) > 0 &&
			    (e.AllowedEffect & DragDropEffects.Copy) > 0) {
				return (e.KeyState & 8) > 0 ? DragDropEffects.Copy : DragDropEffects.Move;
			} else if ((e.AllowedEffect & DragDropEffects.Move) > 0) {
				return DragDropEffects.Move;
			} else if ((e.AllowedEffect & DragDropEffects.Copy) > 0) {
				return DragDropEffects.Copy;
			}
			return DragDropEffects.None;
		}*/
		
		/*protected void OnDragEnter(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(typeof(string))) {
				//e.Effect = GetDragDropEffect(e);
			}
		}*/
		
		
		void InsertString(int offset, string str)
		{
			textArea.Document.Insert(offset, str);
			
			textArea.SelectionManager.SetSelection(new DefaultSelection(textArea.Document, 
			                                                            textArea.Document.OffsetToPosition(offset), 
			                                                            textArea.Document.OffsetToPosition(offset + str.Length)));
			textArea.Caret.Position = textArea.Document.OffsetToPosition(offset + str.Length);
			//textArea.Refresh();
		}
		
		/*protected void OnDragDrop(object sender, DragEventArgs e)
		{
			Point p = textArea.PointToClient(new Point(e.X, e.Y));
			
			if (e.Data.GetDataPresent(typeof(string))) {
				bool two = false;
				textArea.BeginUpdate();
				try {
					int offset = textArea.Caret.Offset;
					if (e.Data.GetDataPresent(typeof(DefaultSelection))) {
						ISelection sel = (ISelection)e.Data.GetData(typeof(DefaultSelection));
						if (sel.ContainsPosition(textArea.Caret.Position)) {
							return;
						}
						if (GetDragDropEffect(e) == DragDropEffects.Move) {
							int len = sel.Length;
							textArea.Document.Remove(sel.Offset, len);
							if (sel.Offset < offset) {
								offset -= len;
							}
						}
						two = true;
					}
					textArea.SelectionManager.ClearSelection();
					InsertString(offset, (string)e.Data.GetData(typeof(string)));
					if (two) {
						textArea.Document.UndoStack.UndoLast(2);
					}
					textArea.Document.UpdateQueue.Clear();
					textArea.Document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.WholeTextArea));
				} finally {
					textArea.EndUpdate();
				}
			}
		}
		
		protected void OnDragOver(object sender, DragEventArgs e)
		{
			if (!textArea.Focused) {
				textArea.Focus();
			}
			
			Point p = textArea.PointToClient(new Point(e.X, e.Y));
			
			if (textArea.TextView.DrawingPosition.Contains(p.X, p.Y)) {
				Point realmousepos= textArea.TextView.GetLogicalPosition(p.X - textArea.TextView.DrawingPosition.X,
				                                                         p.Y - textArea.TextView.DrawingPosition.Y);
				int lineNr = Math.Min(textArea.Document.TotalNumberOfLines - 1, Math.Max(0, realmousepos.Y));
				
				textArea.Caret.Position = new Point(realmousepos.X, lineNr);
				textArea.SetDesiredColumn();
				if (e.Data.GetDataPresent(typeof(string))) {
					e.Effect = GetDragDropEffect(e);
				}
			}
		}*/
	}
}
