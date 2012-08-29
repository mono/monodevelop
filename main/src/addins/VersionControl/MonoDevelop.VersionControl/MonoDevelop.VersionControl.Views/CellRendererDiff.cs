
using System;
using System.Collections.Generic;
using Gtk;
using Gdk;
using MonoDevelop.Ide;
using MonoDevelop.Components;
using System.Text;

namespace MonoDevelop.VersionControl.Views
{
	class CellRendererDiff: Gtk.CellRendererText
	{
		Pango.Layout layout;
		Pango.FontDescription font;
		bool diffMode;
		int width, height, lineHeight;
		string[] lines;		
		int selectedLine = -1;
		TreePath selctedPath;
		TreePath path;
		int RightPadding = 4;
		
//		Gdk.Color baseAddColor = new Gdk.Color (133, 168, 133);
//		Gdk.Color baseRemoveColor = new Gdk.Color (178, 140, 140);
		Gdk.Color baseAddColor = new Gdk.Color (123, 200, 123).AddLight (0.1);
		Gdk.Color baseRemoveColor = new Gdk.Color (200, 140, 140).AddLight (0.1);
		
		int RoundedSectionRadius = 4;
		int LeftPaddingBlock = 19;
		
		public CellRendererDiff()
		{
			font = Pango.FontDescription.FromString (DesktopService.DefaultMonospaceFont);
		}
		
		void DisposeLayout ()
		{
			if (layout != null) {
				layout.Dispose ();
				layout = null;
			}
		}
		
		bool isDisposed = false;
		protected override void OnDestroyed ()
		{
			isDisposed = true;
			DisposeLayout ();
			if (font != null) {
				font.Dispose ();
				font = null;
			}
			base.OnDestroyed ();
		}
		
		public void Reset ()
		{
		}

		public void InitCell (Widget container, bool diffMode, string[] lines, TreePath path)
		{
			if (isDisposed)
				return;
			if (lines == null)
				throw new ArgumentNullException ("lines");
			this.lines = lines;
			this.diffMode = diffMode;
			this.path = path;
			
			if (diffMode) {
				if (lines != null && lines.Length > 0) {
					int maxlen = -1;
					int maxlin = -1;
					for (int n=0; n<lines.Length; n++) {
						string line = ProcessLine (lines [n]);
						if (line == null)
							throw new Exception ("Line " + n + " from diff was null.");
						if (line.Length > maxlen) {
							maxlen = lines [n].Length;
							maxlin = n;
						}
					}
					DisposeLayout ();
					layout = CreateLayout (container, lines [maxlin]);
					layout.GetPixelSize (out width, out lineHeight);
					height = lineHeight * lines.Length;
					width += LeftPaddingBlock + RightPadding;
				}
				else
					width = height = 0;
			}
			else {
				DisposeLayout ();
				layout = CreateLayout (container, string.Join (Environment.NewLine, lines));
				layout.GetPixelSize (out width, out height);
			}
		}
		
		Pango.Layout CreateLayout (Widget container, string text)
		{
			Pango.Layout layout = new Pango.Layout (container.PangoContext);
			layout.SingleParagraphMode = false;
			if (diffMode) {
				layout.FontDescription = font;
				layout.SetText (text);
			}
			else
				layout.SetMarkup (text);
			return layout;
		}
		
		string ProcessLine (string line)
		{
			if (line == null)
				return null;
			return line.Replace ("\t","    ");
		}
		
		const int leftSpace = 16;
		public bool DrawLeft { get; set; }
		
		protected override void Render (Drawable window, Widget widget, Gdk.Rectangle background_area, Gdk.Rectangle cell_area, Gdk.Rectangle expose_area, CellRendererState flags)
		{
			if (isDisposed)
				return;
			if (diffMode) {
				
				if (path.Equals (selctedPath)) {
					selectedLine = -1;
					selctedPath = null;
				}
				
				int w, maxy;
				window.GetSize (out w, out maxy);
				if (DrawLeft) {
					cell_area.Width += cell_area.X - leftSpace;
					cell_area.X = leftSpace;
				}
				var treeview = widget as FileTreeView;
				var p = treeview != null? treeview.CursorLocation : null;
				
				cell_area.Width -= RightPadding;
				
				window.DrawRectangle (widget.Style.BaseGC (Gtk.StateType.Normal), true, cell_area.X, cell_area.Y, cell_area.Width - 1, cell_area.Height);
				
				Gdk.GC normalGC = widget.Style.TextGC (StateType.Normal);
				Gdk.GC removedGC = new Gdk.GC (window);
				removedGC.Copy (normalGC);
				removedGC.RgbFgColor = baseRemoveColor.AddLight (-0.3);
				Gdk.GC addedGC = new Gdk.GC (window);
				addedGC.Copy (normalGC);
				addedGC.RgbFgColor = baseAddColor.AddLight (-0.3);
				Gdk.GC infoGC = new Gdk.GC (window);
				infoGC.Copy (normalGC);
				infoGC.RgbFgColor = widget.Style.Text (StateType.Normal).AddLight (0.2);
				
				Cairo.Context ctx = CairoHelper.Create (window);
				
				// Rendering is done in two steps:
				// 1) Get a list of blocks to render
				// 2) render the blocks
				
				int y = cell_area.Y + 2;
				
				// cline keeps track of the current source code line (the one to jump to when double clicking)
				int cline = 1;
				bool inHeader = true;
				BlockInfo currentBlock = null;
				
				List<BlockInfo> blocks = new List<BlockInfo> ();
				
				for (int n=0; n<lines.Length; n++, y += lineHeight) {
					
					string line = lines [n];
					if (line.Length == 0) {
						currentBlock = null;
						y -= lineHeight;
						continue;
					}
					
					char tag = line [0];
	
					if (line.StartsWith ("---") || line.StartsWith ("+++")) {
						// Ignore this part of the header.
						currentBlock = null;
						y -= lineHeight;
						continue;
					}
					if (tag == '@') {
						int l = ParseCurrentLine (line);
						if (l != -1) cline = l - 1;
						inHeader = false;
					} else if (tag != '-' && !inHeader)
						cline++;
					
					BlockType type;
					switch (tag) {
						case '-': type = BlockType.Removed; break;
						case '+': type = BlockType.Added; break;
						case '@': type = BlockType.Info; break;
						default: type = BlockType.Unchanged; break;
					}

					if (currentBlock == null || type != currentBlock.Type) {
						if (y > maxy)
							break;
					
						// Starting a new block. Mark section ends between a change block and a normal code block
						if (currentBlock != null && IsChangeBlock (currentBlock.Type) && !IsChangeBlock (type))
							currentBlock.SectionEnd = true;
						
						currentBlock = new BlockInfo () {
							YStart = y,
							FirstLine = n,
							Type = type,
							SourceLineStart = cline,
							SectionStart = (blocks.Count == 0 || !IsChangeBlock (blocks[blocks.Count - 1].Type)) && IsChangeBlock (type)
						};
						blocks.Add (currentBlock);
					}
					// Include the line in the current block
					currentBlock.YEnd = y + lineHeight;
					currentBlock.LastLine = n;
				}

				// Now render the blocks

				// The y position of the highlighted line
				int selectedLineRowTop = -1;

				BlockInfo lastCodeSegmentStart = null;
				BlockInfo lastCodeSegmentEnd = null;
				
				foreach (BlockInfo block in blocks)
				{
					if (block.Type == BlockType.Info) {
						// Finished drawing the content of a code segment. Now draw the segment border and label.
						if (lastCodeSegmentStart != null)
							DrawCodeSegmentBorder (infoGC, ctx, cell_area.X, cell_area.Width, lastCodeSegmentStart, lastCodeSegmentEnd, lines, widget, window);
						lastCodeSegmentStart = block;
					}
					
					lastCodeSegmentEnd = block;
					
					if (block.YEnd < 0)
						continue;
					
					// Draw the block background
					DrawBlockBg (ctx, cell_area.X + 1, cell_area.Width - 2, block);
					
					// Get all text for the current block
					StringBuilder sb = new StringBuilder ();
					for (int n=block.FirstLine; n <= block.LastLine; n++) {
						string s = ProcessLine (lines [n]);
						if (n > block.FirstLine)
							sb.Append ('\n');
						if (block.Type != BlockType.Info && s.Length > 0)
							sb.Append (s, 1, s.Length - 1);
						else
							sb.Append (s);
					}
					
					// Draw a special background for the selected line
					
					if (block.Type != BlockType.Info && p.HasValue && p.Value.X >= cell_area.X && p.Value.X <= cell_area.Right && p.Value.Y >= block.YStart && p.Value.Y <= block.YEnd) {
						int row = (p.Value.Y - block.YStart) / lineHeight;
						double yrow = block.YStart + lineHeight * row;
						double xrow = cell_area.X + LeftPaddingBlock;
						int wrow = cell_area.Width - 1 - LeftPaddingBlock;
						if (block.Type == BlockType.Added)
							ctx.Color = baseAddColor.AddLight (0.1).ToCairoColor ();
						else if (block.Type == BlockType.Removed)
							ctx.Color = baseRemoveColor.AddLight (0.1).ToCairoColor ();
						else {
							ctx.Color = widget.Style.Base (Gtk.StateType.Prelight).AddLight (0.1).ToCairoColor ();
							xrow -= LeftPaddingBlock;
							wrow += LeftPaddingBlock;
						}
						ctx.Rectangle (xrow, yrow, wrow, lineHeight);
						ctx.Fill ();
						selectedLine = block.SourceLineStart + row;
						selctedPath = path;
						selectedLineRowTop = (int)yrow;
					}
					
					// Draw the line text. Ignore header blocks, since they are drawn as labels in DrawCodeSegmentBorder
					
					if (block.Type != BlockType.Info) {
						layout.SetMarkup ("");
						layout.SetText (sb.ToString ());
						Gdk.GC gc;
						switch (block.Type) {
							case BlockType.Removed: gc = removedGC; break;
							case BlockType.Added: gc = addedGC; break;
							case BlockType.Info: gc = infoGC; break;
							default: gc = normalGC; break;
						}
						window.DrawLayout (gc, cell_area.X + 2 + LeftPaddingBlock, block.YStart, layout);
					}
					
					// Finally draw the change symbol at the left margin
					
					DrawChangeSymbol (ctx, cell_area.X + 1, cell_area.Width - 2, block);
				}
				
				// Finish the drawing of the code segment
				if (lastCodeSegmentStart != null)
					DrawCodeSegmentBorder (infoGC, ctx, cell_area.X, cell_area.Width, lastCodeSegmentStart, lastCodeSegmentEnd, lines, widget, window);
				
				// Draw the source line number at the current selected line. It must be done at the end because it must
				// be drawn over the source code text and segment borders.
				if (selectedLineRowTop != -1)
					DrawLineBox (normalGC, ctx, ((Gtk.TreeView)widget).VisibleRect.Right - 4, selectedLineRowTop, selectedLine, widget, window);
				
				((IDisposable)ctx).Dispose ();
				removedGC.Dispose ();
				addedGC.Dispose ();
				infoGC.Dispose ();
			} else {
				// Rendering a normal text row
				int y = cell_area.Y + (cell_area.Height - height)/2;
				window.DrawLayout (widget.Style.TextGC (GetState(widget, flags)), cell_area.X, y, layout);
			}
		}
		
		bool IsChangeBlock (BlockType t)
		{
			return t == BlockType.Added || t == BlockType.Removed;
		}
		
		class BlockInfo
		{
			public BlockType Type;
			public int YEnd;
			public int YStart;
			public int FirstLine;
			public int LastLine;
			public bool SectionStart;
			public bool SectionEnd;
			public int SourceLineStart;
		}
		
		enum BlockType
		{
			Info,
			Added,
			Removed,
			Unchanged
		}
		
		void DrawCodeSegmentBorder (Gdk.GC gc, Cairo.Context ctx, double x, int width, BlockInfo firstBlock, BlockInfo lastBlock, string[] lines, Gtk.Widget widget, Gdk.Drawable window)
		{
			int shadowSize = 2;
			int spacing = 4;
			int bottomSpacing = (lineHeight - spacing) / 2;
			
			ctx.Rectangle (x + shadowSize + 0.5, firstBlock.YStart + bottomSpacing + spacing - shadowSize + 0.5, width - shadowSize*2, shadowSize);
			ctx.Color = new Cairo.Color (0.9, 0.9, 0.9);
			ctx.LineWidth = 1;
			ctx.Fill ();
			
			ctx.Rectangle (x + shadowSize + 0.5, lastBlock.YEnd + bottomSpacing + 0.5, width - shadowSize*2, shadowSize);
			ctx.Color = new Cairo.Color (0.9, 0.9, 0.9);
			ctx.Fill ();
			
			ctx.Rectangle (x + 0.5, firstBlock.YStart + bottomSpacing + spacing + 0.5, width, lastBlock.YEnd - firstBlock.YStart - spacing);
			ctx.Color = new Cairo.Color (0.7,0.7,0.7);
			ctx.Stroke ();
			
			string text = lines[firstBlock.FirstLine].Replace ("@","").Replace ("-","");
			text = "<span size='x-small'>" + text.Replace ("+","</span><span size='small'>➜</span><span size='x-small'> ") + "</span>";
			
			layout.SetText ("");
			layout.SetMarkup (text);
			int tw,th;
			layout.GetPixelSize (out tw, out th);
			th--;
			
			int dy = (lineHeight - th) / 2;
			
			ctx.Rectangle (x + 2 + LeftPaddingBlock - 1 + 0.5, firstBlock.YStart + dy - 1 + 0.5, tw + 2, th + 2);
			ctx.LineWidth = 1;
			ctx.Color = widget.Style.Base (Gtk.StateType.Normal).ToCairoColor ();
			ctx.FillPreserve ();
			ctx.Color = new Cairo.Color (0.7, 0.7, 0.7);
			ctx.Stroke ();
				
			window.DrawLayout (gc, (int)(x + 2 + LeftPaddingBlock), firstBlock.YStart + dy, layout);
		}
		
		void DrawLineBox (Gdk.GC gc, Cairo.Context ctx, int right, int top, int line, Gtk.Widget widget, Gdk.Drawable window)
		{
			layout.SetText ("");
			layout.SetMarkup ("<small>" + line.ToString () + "</small>");
			int tw,th;
			layout.GetPixelSize (out tw, out th);
			th--;
			
			int dy = (lineHeight - th) / 2;
			
			ctx.Rectangle (right - tw - 2 + 0.5, top + dy - 1 + 0.5, tw + 2, th + 2);
			ctx.LineWidth = 1;
			ctx.Color = widget.Style.Base (Gtk.StateType.Normal).ToCairoColor ();
			ctx.FillPreserve ();
			ctx.Color = new Cairo.Color (0.7, 0.7, 0.7);
			ctx.Stroke ();

			window.DrawLayout (gc, right - tw - 1, top + dy, layout);
		}
		
		void DrawBlockBg (Cairo.Context ctx, double x, int width, BlockInfo block)
		{
			if (!IsChangeBlock (block.Type))
				return;
			
			Gdk.Color color = block.Type == BlockType.Added ? baseAddColor : baseRemoveColor;
			double y = block.YStart;
			int height = block.YEnd - block.YStart;
			
			double markerx = x + LeftPaddingBlock;
			double rd = RoundedSectionRadius;
			if (block.SectionStart) {
				ctx.Arc (x + rd, y + rd, rd, 180 * (Math.PI / 180), 270 * (Math.PI / 180));
				ctx.LineTo (markerx, y);
			} else {
				ctx.MoveTo (markerx, y);
			}
			
			ctx.LineTo (markerx, y + height);
			
			if (block.SectionEnd) {
				ctx.LineTo (x + rd, y + height);
				ctx.Arc (x + rd, y + height - rd, rd, 90 * (Math.PI / 180), 180 * (Math.PI / 180));
			} else {
				ctx.LineTo (x, y + height);
			}
			if (block.SectionStart) {
				ctx.LineTo (x, y + rd);
			} else {
				ctx.LineTo (x, y);
			}
			ctx.Color = color.AddLight (0.1).ToCairoColor ();
			ctx.Fill ();
			
			ctx.Rectangle (markerx, y, width - markerx, height);
			using (Cairo.Gradient pat = new Cairo.LinearGradient (x, y, x + width, y)) {
				pat.AddColorStop (0, color.AddLight (0.21).ToCairoColor ());
				pat.AddColorStop (1, color.AddLight (0.3).ToCairoColor ());
				ctx.Pattern = pat;
				ctx.Fill ();
			}
		}
		
		void DrawChangeSymbol (Cairo.Context ctx, double x, int width, BlockInfo block)
		{
			if (!IsChangeBlock (block.Type))
				return;
			
			Gdk.Color color = block.Type == BlockType.Added ? baseAddColor : baseRemoveColor;

			int ssize = 8;
			int barSize = 3;
			
			if (ssize - 2 > lineHeight)
				ssize = lineHeight - 2;
			if (ssize <= 0)
				return;

			double inSize = (ssize / 2) - (barSize / 2);
			double py = block.YStart + ((block.YEnd - block.YStart) / 2 - ssize / 2) + 0.5;
			double px = x + (LeftPaddingBlock/2) - (ssize / 2) + 0.5;
			
			if (block.Type == BlockType.Added) {
				ctx.MoveTo (px + inSize, py);
				ctx.RelLineTo (barSize, 0);
				ctx.RelLineTo (0, inSize);
				ctx.RelLineTo (inSize, 0);
				ctx.RelLineTo (0, barSize);
				ctx.RelLineTo (-inSize, 0);
				ctx.RelLineTo (0, inSize);
				ctx.RelLineTo (-barSize, 0);
				ctx.RelLineTo (0, -inSize);
				ctx.RelLineTo (-inSize, 0);
				ctx.RelLineTo (0, -barSize);
				ctx.RelLineTo (inSize, 0);
				ctx.RelLineTo (0, -inSize);
				ctx.ClosePath ();
			} else {
				ctx.MoveTo (px, py + inSize);
				ctx.RelLineTo (ssize, 0);
				ctx.RelLineTo (0, barSize);
				ctx.RelLineTo (-ssize, 0);
				ctx.RelLineTo (0, -barSize);
				ctx.ClosePath ();
			}
			
			ctx.Color = color.ToCairoColor ();
			ctx.FillPreserve ();
			ctx.Color = color.AddLight (-0.2).ToCairoColor ();;
			ctx.LineWidth = 1;
			ctx.Stroke ();
		}
		
		public override void GetSize (Widget widget, ref Rectangle cell_area, out int x_offset, out int y_offset, out int c_width, out int c_height)
		{
			x_offset = y_offset = 0;
			c_width = width;
			c_height = height;
			
			if (diffMode) {
				// Add some spacing for the margin
				c_width += 4;
				c_height += 4;
			}
		}
		
		StateType GetState (Gtk.Widget widget, CellRendererState flags)
		{
			if ((flags & CellRendererState.Selected) != 0)
				return widget.HasFocus ? StateType.Selected : StateType.Active;
			else
				return StateType.Normal;
		}
		
		int ParseCurrentLine (string line)
		{
			int i = line.IndexOf ('+');
			if (i == -1) return -1;
			i++;
			int j = line.IndexOf (',', i);
			if (j == -1) return -1;
			int cline;
			if (!int.TryParse (line.Substring (i, j - i), out cline))
			    return -1;
			return cline;
		}
		
		public int GetSelectedLine (TreePath cpath)
		{
			if (cpath.Equals (selctedPath))
				return selectedLine;
			else
				return -1;
		}
	}
}
