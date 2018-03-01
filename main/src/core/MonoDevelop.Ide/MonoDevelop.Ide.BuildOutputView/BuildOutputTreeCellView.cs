using System;
using System.Collections.Generic;
using MonoDevelop.Ide.Gui;
using Xwt;
using Xwt.Drawing;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.BuildOutputView
{
	static class Styles
	{
		public static Xwt.Drawing.Color CellBackgroundColor { get; internal set; }
		public static Xwt.Drawing.Color CellSelectionColor { get; internal set; }
		public static Xwt.Drawing.Color CellStrongSelectionColor { get; internal set; }
		public static Xwt.Drawing.Color CellTextColor { get; internal set; }
		public static Xwt.Drawing.Color CellTextSelectionColor { get; internal set; }
		public static Xwt.Drawing.Color CellTextSkippedColor { get; internal set; }
		public static Xwt.Drawing.Color CellTextSkippedSelectionColor { get; internal set; }

		static Styles ()
		{
			LoadStyles ();
			Ide.Gui.Styles.Changed += (o, e) => LoadStyles ();
		}

		public static void LoadStyles ()
		{
			if (IdeApp.Preferences.UserInterfaceTheme == Theme.Light) {
				CellBackgroundColor = Ide.Gui.Styles.PadBackground;
			} else {
				CellBackgroundColor = Xwt.Drawing.Color.FromName ("#3c3c3c");
			}

			// Shared
			CellTextColor = Ide.Gui.Styles.BaseForegroundColor;
			CellStrongSelectionColor = Ide.Gui.Styles.BaseSelectionBackgroundColor;
			CellSelectionColor = Ide.Gui.Styles.BaseSelectionBackgroundColor;
			CellTextSelectionColor = Ide.Gui.Styles.BaseSelectionTextColor;
			CellTextSkippedColor = Ide.Gui.Styles.SecondaryTextColor;
			CellTextSkippedSelectionColor = Ide.Gui.Styles.SecondarySelectionTextColor;
		}
	}

	static class Resources
	{
		public static readonly Xwt.Drawing.Image BuildIcon = ImageService.GetIcon (Ide.Gui.Stock.BuildSolution, Gtk.IconSize.Menu);
		public static readonly Xwt.Drawing.Image MessageIcon = ImageService.GetIcon (Ide.Gui.Stock.MessageLog, Gtk.IconSize.Menu);
		public static readonly Xwt.Drawing.Image ErrorIcon = ImageService.GetIcon (Ide.Gui.Stock.BuildError, Gtk.IconSize.Menu);
		public static readonly Xwt.Drawing.Image ErrorIconSmall = ImageService.GetIcon (Ide.Gui.Stock.BuildErrorSmall, Gtk.IconSize.Menu);
		public static readonly Xwt.Drawing.Image ProjectIcon = ImageService.GetIcon (Ide.Gui.Stock.BuildProject, Gtk.IconSize.Menu);
		public static readonly Xwt.Drawing.Image TargetIcon = ImageService.GetIcon (Ide.Gui.Stock.BuildTarget, Gtk.IconSize.Menu);
		public static readonly Xwt.Drawing.Image TaskIcon = ImageService.GetIcon (Ide.Gui.Stock.BuildTask, Gtk.IconSize.Menu);
		public static readonly Xwt.Drawing.Image WarningIcon = ImageService.GetIcon (Ide.Gui.Stock.BuildWarning, Gtk.IconSize.Menu);
		public static readonly Xwt.Drawing.Image WarningIconSmall = ImageService.GetIcon (Ide.Gui.Stock.BuildWarningSmall, Gtk.IconSize.Menu);
		public static readonly Xwt.Drawing.Image FolderIcon = ImageService.GetIcon (Ide.Gui.Stock.OpenFolder, Gtk.IconSize.Menu);
	}

	class BuildOutputTreeCellView : CanvasCellView
	{
		static readonly Xwt.Drawing.Image BuildExpandIcon = ImageService.GetIcon (Ide.Gui.Stock.BuildError, Gtk.IconSize.Menu).WithSize (16);
		static readonly Xwt.Drawing.Image BuildExpandDisabledIcon = ImageService.GetIcon (Ide.Gui.Stock.BuildExpandDisabled, Gtk.IconSize.Menu).WithSize (16);
		static readonly Xwt.Drawing.Image BuildCollapseIcon = ImageService.GetIcon (Ide.Gui.Stock.BuildCollapse, Gtk.IconSize.Menu).WithSize (16);
		static readonly Xwt.Drawing.Image BuildCollapseDisabledIcon = ImageService.GetIcon (Ide.Gui.Stock.BuildCollapseDisabled, Gtk.IconSize.Menu).WithSize (16);

		class ViewStatus
		{
			public bool Expanded { get; set; }
			public double LastRenderWidth;
			public double LastCalculatedHeight;
		}

		const int FontSize = 11;
		const int DescriptionPaddingHeight = 0;
		const int LinesDisplayedCount = 1;
		const int DefaultInformationContainerWidth = 370;
		const int ImageSize = 20;

		public Color BackgroundColor { get; set; }
		public Color StrongSelectionColor { get; set; }
		public Color SelectionColor { get; set; }

		public bool UseStrongSelectionColor { get; set; }

		public IDataField<bool> HasBackgroundColorField { get; set; }
		public IDataField<BuildOutputNode> BuildOutputNodeField { get; set; }

		Font defaultFontLayout;

		//This give us height and width of a character with this font
		double fontHeight;
		int informationContainerWidth => DefaultInformationContainerWidth;

		IBuildOutputContextProvider contextProvider;

		bool IsRootNode (BuildOutputNode buildOutputNode) => buildOutputNode.Parent == null;
		bool IsRowExpanded (BuildOutputNode buildOutputNode) => ((Xwt.TreeView)ParentWidget)?.IsRowExpanded (buildOutputNode) ?? false;
		string GetInformationMessage (BuildOutputNode buildOutputNode) => GettextCatalog.GetString ("{0} | {1}    Started at {2}", buildOutputNode.Configuration, buildOutputNode.Platform, buildOutputNode.StartTime.ToString ("h:m tt on MMMM d, yyyy"));

		public BuildOutputTreeCellView (IBuildOutputContextProvider context)
		{
			BackgroundColor = Styles.CellBackgroundColor;
			StrongSelectionColor = Styles.CellStrongSelectionColor;
			SelectionColor = Styles.CellSelectionColor;
			UseStrongSelectionColor = true;
			contextProvider = context;
		}

		protected override void OnDraw(Context ctx, Xwt.Rectangle cellArea)
		{
			//Draw the node background
			FillCellBackground (ctx);

			var buildOutputNode = GetValue (BuildOutputNodeField);
			DrawImageRow (ctx, cellArea, buildOutputNode);

			TextLayout layout = new TextLayout ();

			var status = GetViewStatus (buildOutputNode);

			// Store the width, it will be used for calculating height in OnGetRequiredSize() when in expanded mode.
			status.LastRenderWidth = cellArea.Width;

			layout.Text = buildOutputNode.Message;

			var textSize = layout.GetSize ();

			// Render the selection
			if (selectionRow == buildOutputNode && selectionStart != selectionEnd)
				layout.SetBackground (Colors.LightBlue, Math.Min (selectionStart, selectionEnd), Math.Abs (selectionEnd - selectionStart));

			// Text doesn't fit. We need to render the expand icon
			if (textSize.Width > cellArea.Width) {
				layout.Width = Math.Max (1, cellArea.Width - BuildExpandIcon.Width - MoreLinkSpacing);
				if (!status.Expanded)
					layout.Trimming = TextTrimming.WordElipsis;
				else
					textSize = layout.GetSize (); // The height may have changed. We need the real height since we check it at the end of the method

				// Draw the text
				ctx.DrawTextLayout (layout, cellArea.X, cellArea.Y);

				// Draw the image

				var imageRect = new Rectangle (cellArea.X + layout.Width + MoreLinkSpacing, cellArea.Y, BuildExpandIcon.Width, BuildExpandIcon.Height);
				bool hover = pointerPosition != Point.Zero && imageRect.Contains (pointerPosition);
				Image icon;
				if (status.Expanded)
					icon = hover ? BuildCollapseIcon : BuildCollapseDisabledIcon;
				else
					icon = hover ? BuildExpandIcon : BuildExpandDisabledIcon;
				ctx.DrawImage (icon, imageRect.X, imageRect.Y);
			} else {
				ctx.DrawTextLayout (layout, cellArea.X, cellArea.Y);
			}

			//DrawNodeText (ctx, cellArea, buildOutputNode);

			if (!IsRootNode (buildOutputNode)) {
				DrawNodeInformation (ctx, cellArea, buildOutputNode);
			} else if (buildOutputNode.NodeType == BuildOutputNodeType.Build) {
				DrawFirstNodeInformation (ctx, cellArea, buildOutputNode);
			}

			// If the height required by the text is not the same as what was calculated in OnGetRequiredSize(), it means that
			// the required height has changed. In that case call QueueResize(), so that OnGetRequiredSize() is called
			// again and the row is properly resized.

			if (status.Expanded && textSize.Height != status.LastCalculatedHeight)
				QueueResize ();
		}

		void CalcLayout (out TextLayout layout, out Rectangle cellArea, out Rectangle expanderRect)
		{
			var node = GetValue (BuildOutputNodeField);
			var status = GetViewStatus (node);
			expanderRect = Rectangle.Zero;
			cellArea = BackgroundBounds;
			layout = new TextLayout ();
			layout.Text = node.Message;
			var textSize = layout.GetSize ();
			if (textSize.Width > cellArea.Width) {
				layout.Width = Math.Max (1, cellArea.Width - BuildExpandIcon.Width - MoreLinkSpacing);
				if (!status.Expanded)
					layout.Trimming = TextTrimming.WordElipsis;
				var expanderX = cellArea.X + cellArea.Width - BuildExpandIcon.Width;
				if (expanderX > 0)
					expanderRect = new Rectangle (expanderX, cellArea.Y, BuildExpandIcon.Width, BuildExpandIcon.Height);
			}
		}

		#region Mouse Events

		Point pointerPosition;

		protected override void OnMouseMoved (MouseMovedEventArgs args)
		{
			CalcLayout (out var layout, out var cellArea, out var expanderRect);

			if (expanderRect != Rectangle.Zero && expanderRect.Contains (args.Position)) {
				pointerPosition = args.Position;
				QueueDraw ();
			} else if (pointerPosition != Point.Zero) {
				pointerPosition = Point.Zero;
				QueueDraw ();
			}

			var layoutSize = layout.GetSize ();
			var insideText = new Rectangle (cellArea.TopLeft, layoutSize).Contains (args.Position);
			var node = GetValue (BuildOutputNodeField);

			if (dragging && insideText && selectionRow == node) {
				var pos = layout.GetIndexFromCoordinates (args.Position.X - cellArea.X, args.Position.Y - cellArea.Y);
				if (pos != -1) {
					selectionEnd = pos;
					QueueDraw ();
				}
			} else {
				ParentWidget.Cursor = insideText ? CursorType.IBeam : CursorType.Arrow;
			}
		}

		protected override void OnButtonPressed (ButtonEventArgs args)
		{
			CalcLayout (out var layout, out var cellArea, out var expanderRect);

			var node = GetValue (BuildOutputNodeField);
			var status = GetViewStatus (node);

			if (expanderRect != Rectangle.Zero && expanderRect.Contains (args.Position)) {
				status.Expanded = !status.Expanded;
				QueueResize ();
				return;
			}

			var pos = layout.GetIndexFromCoordinates (args.Position.X - cellArea.X, args.Position.Y - cellArea.Y);
			if (pos != -1) {
				selectionStart = selectionEnd = pos;
				selectionRow = node;
				dragging = true;
			} else
				selectionRow = null;

			QueueDraw ();

			base.OnButtonPressed (args);
		}

		protected override void OnButtonReleased (ButtonEventArgs args)
		{
			if (dragging) {
				dragging = false;
				QueueDraw ();
			}
			base.OnButtonReleased (args);
		}

		protected override void OnMouseExited ()
		{
			pointerPosition = Point.Zero;
			ParentWidget.Cursor = CursorType.Arrow;
			base.OnMouseExited ();
		}

		#endregion

		// This could also be stored in the data store. In this example we keep it in
		// an internal dictionary to clearly separate the data model from the view model.
		// This is a simple implementation, it doesn't take into account that nodes could
		// be removed
		Dictionary<BuildOutputNode, ViewStatus> viewStatus = new Dictionary<BuildOutputNode, ViewStatus> ();

		ViewStatus GetViewStatus (BuildOutputNode node)
		{
			if (!viewStatus.TryGetValue (node, out var status))
				status = viewStatus [node] = new ViewStatus ();
			return status;
		}

		// Used to track the selection
		int selectionStart;
		int selectionEnd;
		BuildOutputNode selectionRow;
		bool dragging;

		void DrawFirstNodeInformation (Context ctx, Xwt.Rectangle cellArea, BuildOutputNode buildOutputNode)
		{
			UpdateInformationTextColor (ctx);
			var textStartX = cellArea.Width - informationContainerWidth;
			DrawText (ctx, cellArea, textStartX, GetInformationMessage (buildOutputNode), cellArea.Width - textStartX);
		}

		void DrawNodeInformation (Context ctx, Xwt.Rectangle cellArea, BuildOutputNode buildOutputNode)
		{
			if (!buildOutputNode.HasChildren)
				return;

			UpdateInformationTextColor (ctx);

			var textStartX = cellArea.X + (cellArea.Width - informationContainerWidth);

			//Duration text
			var duration = buildOutputNode.GetDurationAsString (contextProvider.IsShowingDiagnostics);
			if (duration != "") {
				DrawText (ctx, cellArea, textStartX, duration, informationContainerWidth);
			}
		
			//Error and Warnings count
			if (!IsRowExpanded (buildOutputNode) &&
			    (buildOutputNode.NodeType == BuildOutputNodeType.Task || buildOutputNode.NodeType == BuildOutputNodeType.Target) &&
			    (buildOutputNode.ErrorCount > 0 || buildOutputNode.WarningCount > 0)) {
				
				textStartX += 55;

				DrawImage (ctx, cellArea, Resources.ErrorIcon, textStartX, ImageSize);
				textStartX += ImageSize + 2;
				var errors = buildOutputNode.ErrorCount.ToString ();

				var layout = DrawText (ctx, cellArea, textStartX, errors, trimming: TextTrimming.Word);
				textStartX += layout.GetSize ().Width;

				DrawImage (ctx, cellArea, Resources.WarningIcon, textStartX, ImageSize);
				textStartX += ImageSize + 2;
				DrawText (ctx, cellArea, textStartX, buildOutputNode.WarningCount.ToString (), 10, trimming: TextTrimming.Word);
			}
		}

		TextLayout DrawText (Context ctx, Xwt.Rectangle cellArea, double x, string text, double width = 0, Font font = null, TextTrimming trimming = TextTrimming.WordElipsis) 
		{
			if (width < 0) {
				throw new Exception ("width cannot be negative");
			}

			var descriptionTextLayout = new TextLayout ();
			if (width != 0) {
				descriptionTextLayout.Width = width;
			}

			descriptionTextLayout.Height = cellArea.Height;
			descriptionTextLayout.Trimming = trimming;

			if (font == null) {
				descriptionTextLayout.Font = defaultFontLayout.WithWeight (FontWeight.Light);
			} else {
				descriptionTextLayout.Font = font;
			}

			descriptionTextLayout.Text = text;

			ctx.DrawTextLayout (descriptionTextLayout, x, cellArea.Top + ((cellArea.Height - fontHeight) * .5));
			return descriptionTextLayout;
		}

		void DrawNodeText (Context ctx, Xwt.Rectangle cellArea, BuildOutputNode buildOutputNode)
		{
			UpdateTextColor (ctx, buildOutputNode);

			Font font;
			if (IsRootNode (buildOutputNode)) {
				font = defaultFontLayout.WithWeight (FontWeight.Bold);
			} else {
				font = defaultFontLayout.WithWeight (FontWeight.Light);
			}

			var startX = cellArea.Left + ImageSize - 3;
			var width = (cellArea.Width - informationContainerWidth) - startX;

			var layout = DrawText (ctx, cellArea, startX, buildOutputNode.Message, width, font);

			// For build summary, display error/warning summary
			if (buildOutputNode.NodeType == BuildOutputNodeType.BuildSummary) {

				startX += layout.GetSize ().Width + 25;
				DrawImage (ctx, cellArea, Resources.ErrorIconSmall, startX, ImageSize);

				startX += ImageSize + 2;
				var errors = GettextCatalog.GetString ("{0} errors", buildOutputNode.ErrorCount.ToString ());
				layout = DrawText (ctx, cellArea, startX, errors, width, font);

				startX += layout.GetSize ().Width;
				DrawImage (ctx, cellArea, Resources.WarningIconSmall, startX, ImageSize);

				var warnings = GettextCatalog.GetString ("{0} warnings", buildOutputNode.WarningCount.ToString ());
				startX += ImageSize + 2;
				DrawText (ctx, cellArea, startX, warnings, font: font);
			}
		}

		void DrawImageRow (Context ctx, Xwt.Rectangle cellArea, BuildOutputNode buildOutputNode)
		{
			DrawImage (ctx, cellArea, GetRowIcon (buildOutputNode), (cellArea.Left - 3), ImageSize);
		}

		Image GetRowIcon (BuildOutputNode buildOutputNode) 
		{
			if ((buildOutputNode.NodeType == BuildOutputNodeType.Task || buildOutputNode.NodeType == BuildOutputNodeType.Target) && !IsRowExpanded (buildOutputNode)) {
				if (buildOutputNode.HasErrors) {
					return Resources.ErrorIcon;
				}  else if (buildOutputNode.HasWarnings) {
					return Resources.WarningIcon;
				}
			}
			return buildOutputNode.GetImage ();
		}

		void DrawImage (Context ctx, Xwt.Rectangle cellArea, Image image, double x, int imageSize)
		{
			ctx.DrawImage (
				Selected ? image.WithStyles ("sel") : image,
				x,
				cellArea.Top - (imageSize - cellArea.Height) * .5,
				imageSize,
				imageSize);
		}

		void UpdateInformationTextColor (Context ctx)
		{
			if (Selected) {
				ctx.SetColor (Styles.CellTextSelectionColor);
			} else {
				//TODO: this is not the correct colour we need a light grey colour
				ctx.SetColor (Ide.Gui.Styles.TabBarInactiveTextColor);
			}
		}

		const double MoreLinkSpacing = 3;

		protected override Size OnGetRequiredSize ()
		{
			var buildOutputNode = GetValue (BuildOutputNodeField);
			var status = GetViewStatus (buildOutputNode);

			TextLayout layout = new TextLayout ();
			layout.Text = buildOutputNode.Message;
			var textSize = layout.GetSize ();

			// When in expanded mode, the height of the row depends on the width. Since we don't know the width,
			// let's use the last width that was used for rendering.

			if (status.Expanded && status.LastRenderWidth != 0 && layout.GetSize ().Width > status.LastRenderWidth) {
				layout.Width = status.LastRenderWidth - BuildExpandIcon.Width - MoreLinkSpacing;
				textSize = layout.GetSize ();
			} 

			status.LastCalculatedHeight = textSize.Height;

			//var layout = new TextLayout ();
			//layout.Text = "W";
			//layout.Font = layout.Font.WithSize (FontSize);
			//defaultFontLayout = layout.Font;
			//fontHeight = layout.GetSize ().Height;
			//return new Size (30, fontHeight * LinesDisplayedCount + DescriptionPaddingHeight + 
			                 //(buildOutputNode?.NodeType == BuildOutputNodeType.Build ? 12 : 3));

			return new Size (30, textSize.Height);
		}

		Color GetSelectedColor ()
		{
			if (UseStrongSelectionColor) {
				return StrongSelectionColor;
			}
			return SelectionColor;
		}

		void UpdateTextColor (Context ctx, BuildOutputNode buildOutputNode)
		{
			if (UseStrongSelectionColor && Selected) {
				if (buildOutputNode.NodeType == BuildOutputNodeType.TargetSkipped) {
					ctx.SetColor (Styles.CellTextSkippedSelectionColor);
				} else {
					ctx.SetColor (Styles.CellTextSelectionColor);
				}
			} else {
				if (buildOutputNode.NodeType == BuildOutputNodeType.TargetSkipped) {
					ctx.SetColor (Styles.CellTextSkippedColor);
				} else {
					ctx.SetColor (Styles.CellTextColor);
				}
			}
		}

		bool IsBackgroundColorFieldSet ()
		{
			return GetValue (HasBackgroundColorField, false);
		}

		void FillCellBackground (Context ctx)
		{
			if (Selected) {
				FillCellBackground (ctx, GetSelectedColor ());
			} else if (IsBackgroundColorFieldSet ()) {
				FillCellBackground (ctx, BackgroundColor);
			}
		}

		void FillCellBackground (Context ctx, Color color)
		{
			ctx.Rectangle (BackgroundBounds);
			ctx.SetColor (color);
			ctx.Fill ();
		}
	}
}
