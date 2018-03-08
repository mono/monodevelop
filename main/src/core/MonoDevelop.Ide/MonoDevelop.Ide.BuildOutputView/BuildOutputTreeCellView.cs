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
		static readonly Xwt.Drawing.Image BuildExpandIcon = ImageService.GetIcon (Ide.Gui.Stock.BuildExpand, Gtk.IconSize.Menu).WithSize (16);
		static readonly Xwt.Drawing.Image BuildExpandDisabledIcon = ImageService.GetIcon (Ide.Gui.Stock.BuildExpandDisabled, Gtk.IconSize.Menu).WithSize (16);
		static readonly Xwt.Drawing.Image BuildCollapseIcon = ImageService.GetIcon (Ide.Gui.Stock.BuildCollapse, Gtk.IconSize.Menu).WithSize (16);
		static readonly Xwt.Drawing.Image BuildCollapseDisabledIcon = ImageService.GetIcon (Ide.Gui.Stock.BuildCollapseDisabled, Gtk.IconSize.Menu).WithSize (16);

		class ViewStatus
		{
			public bool Expanded { get; set; }
			public double LastRenderWidth;
			public double LastRenderX;
			public double LastRenderY;
			public double LastCalculatedHeight;

			public int NewLineCharIndex;
		}

		const int BuildTypeRowContentPadding = 6;
		const int RowContentPadding = 3;

		const int LinesDisplayedCount = 1;
		const int DefaultInformationContainerWidth = 370;
		const int ImageSize = 20;
		const int ImagePadding = 0;
		const int FontSize = 11;

		public Color BackgroundColor { get; set; }
		public Color StrongSelectionColor { get; set; }
		public Color SelectionColor { get; set; }

		public bool UseStrongSelectionColor { get; set; }

		public IDataField<bool> HasBackgroundColorField { get; set; }
		public IDataField<BuildOutputNode> BuildOutputNodeField { get; set; }

		//This give us height and width of a character with this font
		int informationContainerWidth => DefaultInformationContainerWidth;

		IBuildOutputContextProvider contextProvider;

		// This could also be stored in the data store. In this example we keep it in
		// an internal dictionary to clearly separate the data model from the view model.
		// This is a simple implementation, it doesn't take into account that nodes could
		// be removed
		Dictionary<BuildOutputNode, ViewStatus> viewStatus = new Dictionary<BuildOutputNode, ViewStatus> ();

		bool IsRootNode (BuildOutputNode buildOutputNode) => buildOutputNode.Parent == null;

		bool IsRowExpanded (BuildOutputNode buildOutputNode) => ((Xwt.TreeView)ParentWidget)?.IsRowExpanded (buildOutputNode) ?? false;

		string GetInformationMessage (BuildOutputNode buildOutputNode) => GettextCatalog.GetString ("{0} | {1}    Started at {2}", buildOutputNode.Configuration, buildOutputNode.Platform, buildOutputNode.StartTime.ToString ("h:m tt on MMMM d, yyyy"));

		//Left includes node level padding
		double GetTextStartX (Xwt.Rectangle cellArea) => cellArea.Left + ImageSize - 3;

		static Font defaultLightFont;
		static Font defaultBoldFont;

		static BuildOutputTreeCellView ()
		{
			var fontName = Font.FromName (Gui.Styles.DefaultFontName)
			                   .WithSize (FontSize);
			defaultBoldFont = fontName.WithWeight (FontWeight.Bold);
			defaultLightFont = fontName.WithWeight (FontWeight.Light);
		}

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
			var buildOutputNode = GetValue (BuildOutputNodeField);
			var isSelected = Selected;

			var status = GetViewStatus (buildOutputNode);

			//Draw the node background
			FillCellBackground (ctx, isSelected);

			//Draw the image row
			DrawImage (ctx, cellArea, GetRowIcon (buildOutputNode), (cellArea.Left - 3), ImageSize, isSelected, ImagePadding);

			TextLayout layout = new TextLayout ();
			layout.Font = GetFont (buildOutputNode);

			var startX = GetTextStartX (cellArea);
			var width = Math.Max (1, (cellArea.Width - informationContainerWidth) - startX);

			// Store the width, it will be used for calculating height in OnGetRequiredSize() when in expanded mode.
			status.LastRenderWidth = width;

			if (!status.Expanded && status.NewLineCharIndex > -1) {
				layout.Text = buildOutputNode.Message.Substring (0, status.NewLineCharIndex);
			} else {
				layout.Text = buildOutputNode.Message;
			}

			UpdateTextColor (ctx, buildOutputNode, isSelected);

			var textSize = layout.GetSize ();
			var padding = GetRowPadding (status.LastCalculatedHeight, textSize.Height, status.Expanded);

			status.LastRenderX = startX;
			status.LastRenderY = cellArea.Y + padding;

			// Text doesn't fit. We need to render the expand icon
			if (textSize.Width > width || status.NewLineCharIndex > -1) {
				
				layout.Width = width;

				if (textSize.Height > cellArea.Height) {
					layout.Height = cellArea.Height;
				}

				if (!status.Expanded)
					layout.Trimming = TextTrimming.WordElipsis;
				else
					textSize = layout.GetSize (); // The height may have changed. We need the real height since we check it at the end of the method

				// Draw the text
				ctx.DrawTextLayout (layout, status.LastRenderX, status.LastRenderY);

				// Draw the image
				var imageRect = new Rectangle (status.LastRenderX + status.LastRenderWidth + ImageSize + ImagePadding, cellArea.Y, ImageSize, ImageSize);
				bool hover = pointerPosition != Point.Zero && imageRect.Contains (pointerPosition);
				Image icon;
				if (status.Expanded)
					icon = hover ? BuildCollapseIcon : BuildCollapseDisabledIcon;
				else
					icon = hover ? BuildExpandIcon : BuildExpandDisabledIcon;
				ctx.DrawImage (icon, imageRect.X, imageRect.Y);
			} else {
				ctx.DrawTextLayout (layout, status.LastRenderX, status.LastRenderY);
			}

			//Information section
			if (!IsRootNode (buildOutputNode)) {
				DrawNodeInformation (ctx, cellArea, buildOutputNode, padding, isSelected, ImageSize, ImagePadding);
			} else if (buildOutputNode.NodeType == BuildOutputNodeType.BuildSummary) {
				// For build summary, display error/warning summary
				startX += layout.GetSize ().Width + 25;
				DrawImage (ctx, cellArea, Resources.ErrorIconSmall, startX, ImageSize, isSelected, ImagePadding);

				startX += ImageSize + 2;
				var errors = GettextCatalog.GetString ("{0} errors", buildOutputNode.ErrorCount.ToString ());
				layout = DrawText (ctx, cellArea, startX, errors, padding, defaultLightFont, width);

				startX += layout.GetSize ().Width;
				DrawImage (ctx, cellArea, Resources.WarningIconSmall, startX, ImageSize, isSelected, ImagePadding);

				var warnings = GettextCatalog.GetString ("{0} warnings", buildOutputNode.WarningCount.ToString ());
				startX += ImageSize + 2;
				DrawText (ctx, cellArea, startX, warnings, padding, font: defaultLightFont);
			} else if (buildOutputNode.NodeType == BuildOutputNodeType.Build) {
				DrawFirstNodeInformation (ctx, cellArea, buildOutputNode, padding, isSelected);
			}

			// If the height required by the text is not the same as what was calculated in OnGetRequiredSize(), it means that
			// the required height has changed. In that case call QueueResize(), so that OnGetRequiredSize() is called
			// again and the row is properly resized.

			if (status.Expanded && textSize.Height != status.LastCalculatedHeight)
				QueueResize ();
		}

		double GetRowPadding (double rowHeight, double currentHeight, bool isExpanded)
		{
			if (isExpanded || currentHeight > rowHeight) {
				return 0;
			}
			return (rowHeight - currentHeight) * .5;
		}

		void DrawFirstNodeInformation (Context ctx, Xwt.Rectangle cellArea, BuildOutputNode buildOutputNode, double padding, bool isSelected)
		{
			UpdateInformationTextColor (ctx, isSelected);
			var textStartX = cellArea.Width - informationContainerWidth;
			DrawText (ctx, cellArea, textStartX, GetInformationMessage (buildOutputNode), padding, defaultLightFont, cellArea.Width - textStartX);
		}

		void DrawNodeInformation (Context ctx, Xwt.Rectangle cellArea, BuildOutputNode buildOutputNode, double padding, bool isSelected, int imageSize, int imagePadding)
		{
			if (!buildOutputNode.HasChildren)
				return;

			UpdateInformationTextColor (ctx, isSelected);

			var textStartX = cellArea.X + (cellArea.Width - informationContainerWidth);

			//Duration text
			var duration = buildOutputNode.GetDurationAsString (contextProvider.IsShowingDiagnostics);
			if (duration != "") {
				DrawText (ctx, cellArea, textStartX, duration, padding, defaultLightFont, informationContainerWidth);
			}

			//Error and Warnings count
			if (!IsRowExpanded (buildOutputNode) &&
			    (buildOutputNode.NodeType == BuildOutputNodeType.Task || buildOutputNode.NodeType == BuildOutputNodeType.Target) &&
			    (buildOutputNode.ErrorCount > 0 || buildOutputNode.WarningCount > 0)) {
				
				textStartX += 55;

				DrawImage (ctx, cellArea, Resources.ErrorIcon, textStartX, imageSize, isSelected, imagePadding);
				textStartX += ImageSize + 2;
				var errors = buildOutputNode.ErrorCount.ToString ();

				var layout = DrawText (ctx, cellArea, textStartX, errors, padding, defaultLightFont, trimming: TextTrimming.Word);
				textStartX += layout.GetSize ().Width;

				DrawImage (ctx, cellArea, Resources.WarningIcon, textStartX, imageSize, isSelected, imagePadding);
				textStartX += ImageSize + 2;
				DrawText (ctx, cellArea, textStartX, buildOutputNode.WarningCount.ToString (), padding, defaultLightFont, 10, trimming: TextTrimming.Word);
			}
		}

		TextLayout DrawText (Context ctx, Xwt.Rectangle cellArea, double x, string text, double padding, Font font, double width = 0, TextTrimming trimming = TextTrimming.WordElipsis) 
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

			descriptionTextLayout.Font = font;
			descriptionTextLayout.Text = text;

			ctx.DrawTextLayout (descriptionTextLayout, x, cellArea.Y + padding);
			return descriptionTextLayout;
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

		void DrawImage (Context ctx, Xwt.Rectangle cellArea, Image image, double x, int imageSize, bool isSelected, double topPadding = 0)
		{
			ctx.DrawImage (isSelected ? image.WithStyles ("sel") : image, x, cellArea.Top + topPadding, imageSize, imageSize);
		}

		void UpdateInformationTextColor (Context ctx, bool isSelected)
		{
			if (isSelected) {
				ctx.SetColor (Styles.CellTextSelectionColor);
			} else {
				//TODO: this is not the correct colour we need a light grey colour
				ctx.SetColor (Ide.Gui.Styles.TabBarInactiveTextColor);
			}
		}

		protected override Size OnGetRequiredSize ()
		{
			var buildOutputNode = GetValue (BuildOutputNodeField);
			var status = GetViewStatus (buildOutputNode);

			TextLayout layout = new TextLayout ();
			layout.Font = GetFont (buildOutputNode);

			if (!status.Expanded && status.NewLineCharIndex > -1) {
				layout.Text = buildOutputNode.Message.Substring (0, status.NewLineCharIndex);
			} else {
				layout.Text = buildOutputNode.Message;
			}

			var textSize = layout.GetSize ();

			// When in expanded mode, the height of the row depends on the width. Since we don't know the width,
			// let's use the last width that was used for rendering.

			if (status.Expanded && status.LastRenderWidth != 0 && textSize.Width > status.LastRenderWidth) {
				layout.Width = status.LastRenderWidth - BuildExpandIcon.Width - 3;
				textSize = layout.GetSize ();
			}

			textSize.Height = Math.Max (textSize.Height, ImageSize); //our minimum size are allways the image size
			status.LastCalculatedHeight = textSize.Height;

			return new Size (30, textSize.Height);
		}

		Color GetSelectedColor ()
		{
			if (UseStrongSelectionColor) {
				return StrongSelectionColor;
			}
			return SelectionColor;
		}

		void UpdateTextColor (Context ctx, BuildOutputNode buildOutputNode, bool isSelected)
		{
			if (UseStrongSelectionColor && isSelected) {
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

		void FillCellBackground (Context ctx, bool isSelected)
		{
			if (isSelected) {
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

		ViewStatus GetViewStatus (BuildOutputNode node)
		{
			if (!viewStatus.TryGetValue (node, out var status))
				status = viewStatus [node] = new ViewStatus ();
			return status;
		}

		protected override void OnDataChanged ()
		{
			base.OnDataChanged ();
			var node = GetValue (BuildOutputNodeField);
			if (node != null) {
				var status = GetViewStatus (node);
				status.NewLineCharIndex = node.Message.IndexOf ('\n');
			}
		}

		internal void OnDataSourceChanged () 
		{
			viewStatus.Clear ();
		}

		#region Mouse Events

		Point pointerPosition;

		protected override void OnMouseMoved (MouseMovedEventArgs args)
		{
			var node = GetValue (BuildOutputNodeField);
		
			CalcLayout (out var layout, out var cellArea, out var expanderRect);

			if (expanderRect != Rectangle.Zero && expanderRect.Contains (args.Position)) {
				pointerPosition = args.Position;
				QueueDraw ();
			} else if (pointerPosition != Point.Zero) {
				pointerPosition = Point.Zero;
				QueueDraw ();
			}
		}

		protected override void OnButtonPressed (ButtonEventArgs args)
		{
			var node = GetValue (BuildOutputNodeField);
			var status = GetViewStatus (node);

			CalcLayout (out var layout, out var cellArea, out var expanderRect);

			if (expanderRect != Rectangle.Zero && expanderRect.Contains (args.Position)) {
				status.Expanded = !status.Expanded;
				QueueResize ();
				return;
			}
		
			base.OnButtonPressed (args);
		}

		void CalcLayout (out TextLayout layout, out Rectangle cellArea, out Rectangle expanderRect)
		{
			var node = GetValue (BuildOutputNodeField);
			var status = GetViewStatus (node);
			expanderRect = Rectangle.Zero;

			cellArea = new Rectangle (status.LastRenderX, status.LastRenderY, status.LastRenderWidth, status.LastCalculatedHeight);

			layout = new TextLayout ();
			layout.Font = GetFont (node);
			layout.Text = node.Message;
			var textSize = layout.GetSize ();

			if (textSize.Width > cellArea.Width || layout.Text.IndexOf ('\n') > -1) {
				layout.Width = Math.Max (1, cellArea.Width);
				if (!status.Expanded)
					layout.Trimming = TextTrimming.WordElipsis;

				var expanderX = cellArea.Right + ImageSize + ImagePadding;
				if (expanderX > 0)
					expanderRect = new Rectangle (expanderX, cellArea.Y, ImageSize, ImageSize);
			}
		}

		Font GetFont (BuildOutputNode node)
		{
			if (IsRootNode (node)) {
				return defaultBoldFont;
			}
			return defaultLightFont;
		}

		#endregion
	}
}
