//// <file>
////     <copyright see="prj:///doc/copyright.txt"/>
////     <license see="prj:///doc/license.txt"/>
////     <owner name="Mike KrÃ¼ger" email="mike@icsharpcode.net"/>
////     <version value="$version"/>
//// </file>
//
//using System;
//using System.Collections;
//using System.Drawing;
//using System.Drawing.Text;
//
//using MonoDevelop.TextEditor.Document;
//using MonoDevelop.Services;
//using MonoDevelop.Gui;
//using MonoDevelop.TextEditor;
//
//namespace MonoDevelop.DefaultEditor.Gui.Editor
//{
//	/// <summary>
//	/// reperesents a visual error, this class is needed by the errordrawer.
//	/// </summary>
//	public class VisualError
//	{
//		int    offset;
//		int    length;
//		string description;
//		
//		public int Offset {
//			get {
//				return offset;
//			}
//			set {
//				offset = value;
//			}
//		}
//		
//		public int Length {
//			get {
//				return length;
//			}
//			set {
//				length = value;
//			}
//		}
//		
//		public string Description {
//			get {
//				return description;
//			}
//		}
//		
//		public VisualError(int offset, int length, string description)
//		{
//			this.offset      = offset;
//			this.length      = length;
//			this.description = description;
//		}
//	}
//	
//	/// <summary>
//	/// This class draws error underlines.
//	/// </summary>
//	public class ErrorDrawer
//	{
//		ArrayList       errors = new ArrayList();
//		TextAreaControl textarea;
//		
//		public ErrorDrawer(TextAreaControl textarea)
//		{
//			this.textarea = textarea;
//			textarea.Document.DocumentChanged += new DocumentAggregatorEventHandler(MoveIndices);
//			textarea.TextAreaPainter.ToolTipEvent += new ToolTipEvent(ToolTip);
//			textarea.TextAreaPainter.LinePainter += new LinePainter(ErrorPainter);
//			
//			TaskService taskService = (TaskService)MonoDevelop.Core.Services.ServiceManager.Services.GetService(typeof(TaskService));
//			taskService.TasksChanged += new EventHandler(SetErrors);
//			textarea.FileNameChanged += new EventHandler(SetErrors);
//		}
//		
//		public void MoveIndices(object sender, DocumentAggregatorEventArgs e)
//		{
//			ArrayList newerrors = new ArrayList();
//			bool redraw = false;
//			lock (this) {
//				foreach (VisualError error in errors) {
//					if (e.Length == -1) {        // insert
//						if (e.Offset <= error.Offset) {
//							error.Offset += e.Text.Length;
//							redraw = true;
//						} else if (e.Offset < error.Offset + error.Length) {
//							error.Length += e.Text.Length;
//							redraw = true;
//						}
//					} else if (e.Text == null) { // remove
//						if (e.Offset < error.Offset) {
//							error.Offset -= e.Length;
//							redraw = true;
//						} else if (e.Offset == error.Offset) {
//							error.Length -= e.Length;
//						} else if (e.Offset <= error.Offset + error.Length) {
//							if (e.Offset + e.Length <= error.Offset + error.Length) {
//								error.Length = error.Length - e.Length;
//							} else {
//								error.Length = e.Offset - error.Offset;
//							}
//						}
//					} else { // replace
//						if (e.Offset <= error.Offset) {
//							error.Offset -= e.Length;
//							error.Offset += e.Text.Length;
//							redraw = true;
//						} 
//					}
//					if (error.Offset > 0 && error.Offset + error.Length < e.Document.TextLength) {
//						newerrors.Add(error);
//					}
//				}
//				errors = newerrors;
//			}
//			
//			if (redraw) {
//				textarea.Refresh();
//			}
//		}
//		
//		void ClearErrors()
//		{
//			ArrayList lines = new ArrayList();
//			foreach (VisualError error in errors) {
//				lines.Add(textarea.Document.GetLineNumberForOffset(error.Offset));
//			}
//			lock (this) {
//				errors.Clear();
//			}
//			foreach (int line in lines) {
//				textarea.Document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.SingleLine, new Point(0, line)));
//			}
//			textarea.Document.CommitUpdate();
//		}
//		
//		void SetErrors(object sender, EventArgs e)
//		{
//			ClearErrors();
//			TaskService taskService = (TaskService)MonoDevelop.Core.Services.ServiceManager.Services.GetService(typeof(TaskService));
//			foreach (Task task in taskService.Tasks) {
//				if (task.FileName == textarea.FileName && (task.TaskType == TaskType.Warning || 
//				                                           task.TaskType == TaskType.Error)) {
//					if (task.Line >= 0 && task.Line < textarea.Document.TotalNumberOfLines) {
//						LineSegment line = textarea.Document.GetLineSegment(task.Line);
//						int offset = Math.Min(textarea.Document.TextLength, line.Offset + task.Column);
//						int length = Math.Max(1, TextUtilities.FindWordEnd(textarea.Document, offset) - offset);
//						AddError(new VisualError(offset, length, task.Description));
//					}
//				}
//			}
//			
//			textarea.Refresh();
//		}
//		
//		bool AddError(VisualError newerror)
//		{
////			Console.WriteLine("Add Error");			
//			lock (this) {
//				
//				foreach (VisualError error in errors) {
//					if (error.Offset == newerror.Offset && error.Length == newerror.Length) {
//						return false;
//					}
//				}
//				
//				errors.Add(newerror);
//			}
//			
//			int lineNr = textarea.Document.GetLineNumberForOffset(newerror.Offset);
//			textarea.Document.RequestUpdate(new TextAreaUpdate(TextAreaUpdateType.SingleLine, new Point(0, lineNr)));
//			textarea.Document.CommitUpdate();
////			Console.WriteLine("Add Error .DONE");
//			return true;
//		}
//		
//		void ToolTip(int xpos, int ypos, ref bool toolTipSet)
//		{
////			Console.WriteLine("Tool Tip");
//			
//			lock (this) {
//				foreach (VisualError error in errors) {
//					Point errorpos  = textarea.Document.OffsetToView(error.Offset);
//					Rectangle r = new Rectangle((int)(errorpos.X * textarea.TextAreaPainter.FontWidth), 
//												(int)(errorpos.Y * textarea.TextAreaPainter.FontHeight), 
//							                    (int)(error.Length * textarea.TextAreaPainter.FontWidth), 
//	                    						(int)textarea.TextAreaPainter.FontHeight);
//					if (r.Contains(xpos, ypos)) {
//						textarea.TextAreaPainter.ToolTip.SetToolTip(textarea.TextAreaPainter, error.Description);
//						toolTipSet = true;
//						break;
//					}
//				}
//			}
////			Console.WriteLine("Tool Tip .DONE");
//		}
//		
//		void DrawWaveLine(Graphics g, int from, int to, int ypos)
//		{
//			Pen pen = Pens.Red;
//			for (int i = from; i < to; i+= 6) {
//				g.DrawLine(pen, i,     ypos + 3, i + 3, ypos + 1);
//				g.DrawLine(pen, i + 3, ypos + 1, i + 6, ypos + 3);
//			}
//		}
//
//		void ErrorPainter(Graphics g, int line, RectangleF rect, PointF pos, int virtualLeft, int virtualTop)
//		{
////			Console.WriteLine("Paint");
//			if (textarea.Properties.GetProperty("ShowErrors", true)) {
//				lock(this) {
//					foreach (VisualError error in errors) {
//						try {
//							int offsetLineNumber = textarea.Document.GetLineNumberForOffset(error.Offset);
//							
//							if (offsetLineNumber == line) {
//								LineSegment lineSegment = textarea.Document.GetLineSegment(offsetLineNumber);
//								int xPos = error.Offset - lineSegment.Offset;
//								DrawWaveLine(g,
//									(int)(pos.X + textarea.TextAreaPainter.CalculateVisualXPos(offsetLineNumber, xPos) - virtualLeft),
//									(int)(pos.X + textarea.TextAreaPainter.CalculateVisualXPos(offsetLineNumber, xPos + error.Length) - virtualLeft), 
//									(int)(pos.Y + textarea.TextAreaPainter.FontHeight - 3) - virtualTop);
//							} 
//						} catch (Exception) {}
//					}
//				}
//			}
////			Console.WriteLine("Paint .DONE");
//		}
//	}
//}
