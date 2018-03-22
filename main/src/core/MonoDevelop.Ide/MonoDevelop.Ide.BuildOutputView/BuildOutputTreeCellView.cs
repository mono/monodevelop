using System;
using System.Collections.Generic;
using MonoDevelop.Ide.Gui;
using Xwt;
using Xwt.Drawing;
using MonoDevelop.Core;
using MonoDevelop.Ide.Fonts;

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
		public static Xwt.Drawing.Color LinkForegroundColor { get; internal set; }
		public static Xwt.Drawing.Color SearchMatchFocusedBackgroundColor { get; internal set; }
		public static Xwt.Drawing.Color SearchMatchUnfocusedBackgroundColor { get; internal set; }

		static Styles ()
		{
			LoadStyles ();
			Ide.Gui.Styles.Changed += (o, e) => LoadStyles ();
		}

		public static void LoadStyles ()
		{
			if (IdeApp.Preferences.UserInterfaceTheme == Theme.Light) {
				CellBackgroundColor = Ide.Gui.Styles.PadBackground;
				SearchMatchUnfocusedBackgroundColor = Xwt.Drawing.Color.FromName ("#fdffa9");
			} else {
				CellBackgroundColor = Xwt.Drawing.Color.FromName ("#3c3c3c");
				SearchMatchUnfocusedBackgroundColor = Xwt.Drawing.Color.FromName ("#a2a53f");
			}

			// Shared
			CellTextColor = Ide.Gui.Styles.BaseForegroundColor;
			CellStrongSelectionColor = Ide.Gui.Styles.BaseSelectionBackgroundColor;
			CellSelectionColor = Ide.Gui.Styles.BaseSelectionBackgroundColor;
			CellTextSelectionColor = Ide.Gui.Styles.BaseSelectionTextColor;
			CellTextSkippedColor = Ide.Gui.Styles.SecondaryTextColor;
			CellTextSkippedSelectionColor = Ide.Gui.Styles.SecondarySelectionTextColor;
			LinkForegroundColor = Ide.Gui.Styles.LinkForegroundColor;
			SearchMatchFocusedBackgroundColor = Xwt.Drawing.Color.FromName ("#fcff54");
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

		public int SelectionStart { get; private set; }
		public int SelectionEnd { get; private set; }
		BuildOutputNode selectionRow;
		bool clicking;

		public EventHandler<BuildOutputNode> GoToTask;

		class ViewStatus
		{
			public bool Expanded;
			public Rectangle LastRenderBounds;
			public Rectangle LastRenderLayoutBounds;
			public double CollapsedRowHeight = -1;
			public double CollapsedLayoutHeight = -1;
			public double LayoutYPadding = 0;
			public double LastCalculatedHeight;
			TextLayout layout = new TextLayout ();
			public TextLayout GetUnconstrainedLayout ()
			{
				layout.Width = layout.Height = -1;
				layout.Trimming = TextTrimming.Word;
				return layout;
			}

			public Rectangle TaskLinkRenderRectangle = Rectangle.Zero;

			public int NewLineCharIndex;
		}

		const int BuildTypeRowContentPadding = 6;
		const int RowContentPadding = 3;
		const int BuildConfigurationInformationLeftPadding = 32;

		const int LinesDisplayedCount = 1;
		const int DefaultInformationContainerWidth = 370;
		const int ImageSize = 20;
		const int ImagePadding = 0;
		const int FontSize = 11;
		const int MinLayoutWidth = 30;

		public Color BackgroundColor { get; set; }
		public Color StrongSelectionColor { get; set; }
		public Color SelectionColor { get; set; }

		public bool UseStrongSelectionColor { get; set; }

		public IDataField<bool> HasBackgroundColorField { get; set; }
		public IDataField<BuildOutputNode> BuildOutputNodeField { get; set; }

		IBuildOutputContextProvider contextProvider;

		// This could also be stored in the data store. In this example we keep it in
		// an internal dictionary to clearly separate the data model from the view model.
		// This is a simple implementation, it doesn't take into account that nodes could
		// be removed
		Dictionary<BuildOutputNode, ViewStatus> viewStatus = new Dictionary<BuildOutputNode, ViewStatus> ();

		bool IsRootNode (BuildOutputNode buildOutputNode) => buildOutputNode.Parent == null;

		bool IsRowExpanded (BuildOutputNode buildOutputNode) => ((Xwt.TreeView)ParentWidget)?.IsRowExpanded (buildOutputNode) ?? false;

		string GetInformationMessage (BuildOutputNode buildOutputNode) => GettextCatalog.GetString ("{0} | {1}     Started at {2}", buildOutputNode.Configuration, buildOutputNode.Platform, buildOutputNode.StartTime.ToString ("h:m tt on MMM d, yyyy"));

		static Font defaultLightFont;
		static Font defaultBoldFont;
		static Font monospaceFont;

		double lastErrorPanelStartX;

		static BuildOutputTreeCellView ()
		{
			var fontName = Font.FromName (Gui.Styles.DefaultFontName)
			                   .WithSize (FontSize);
			defaultBoldFont = fontName.WithWeight (FontWeight.Bold);
			defaultLightFont = fontName.WithWeight (FontWeight.Light);
			monospaceFont = FontService.MonospaceFont.ToXwtFont ().WithSize (FontSize);
		}

		public BuildOutputTreeCellView (IBuildOutputContextProvider context)
		{
			BackgroundColor = Styles.CellBackgroundColor;
			StrongSelectionColor = Styles.CellStrongSelectionColor;
			SelectionColor = Styles.CellSelectionColor;
			UseStrongSelectionColor = true;
			contextProvider = context;
		}

		internal void OnBoundsChanged (object sender, EventArgs args) 
		{
			lastErrorPanelStartX = 0;
		}

		string GetSearchMarkup (string message, Color foreground, Color background)
		{
			return $"<span foreground=\"{foreground.ToHexString ()}\" background=\"{background.ToHexString ()}\">{message}</span>";
		}

		void CreateMarkupText (TextLayout layout, BuildOutputNode buildOutputNode, string message, string search)
		{
			int index = -1;
			if (search == "" || (index = message.IndexOf (search, StringComparison.OrdinalIgnoreCase)) == -1) {
				layout.Markup = message;
				return;
			}
			System.Text.StringBuilder bld = new System.Text.StringBuilder ();
			if (index > 0) {
				bld.Append (message.Substring (0, index));
			}
			bld.Append (GetSearchMarkup (message.Substring (index, search.Length),
			                             GetTextColor (buildOutputNode, false),
			                             HasFocus ? Styles.SearchMatchFocusedBackgroundColor : Styles.SearchMatchUnfocusedBackgroundColor));
			if (message.Length > index + search.Length) {
				bld.Append (message.Substring (index + search.Length));
			}
			layout.Markup = bld.ToString ();
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

			CalcLayout (buildOutputNode, status, Bounds, out var layout, out var layoutBounds, out var expanderRect);

			// Render the selection
			if (selectionRow == buildOutputNode && SelectionStart != SelectionEnd) {
				layout.SetBackground (Colors.LightBlue, Math.Min (SelectionStart, SelectionEnd), Math.Abs (SelectionEnd - SelectionStart));
			}

			ctx.SetColor (GetTextColor (buildOutputNode, isSelected));

			// Draw the text
			ctx.DrawTextLayout (layout, layoutBounds.X, layoutBounds.Y);

			// Draw right hand expander
			if (!expanderRect.IsEmpty) {
				// Draw the image
				Image icon;
				if (status.Expanded)
					icon = ExpanderHovered ? BuildCollapseIcon : BuildCollapseDisabledIcon;
				else
					icon = ExpanderHovered ? BuildExpandIcon : BuildExpandDisabledIcon;
				ctx.DrawImage (icon, expanderRect.X, expanderRect.Y);
			}

			//Information section
			if (!IsRootNode (buildOutputNode)) {
				DrawNodeInformation (ctx, cellArea, buildOutputNode, status.LayoutYPadding, isSelected, ImageSize, ImagePadding, status);
			} else if (buildOutputNode.NodeType == BuildOutputNodeType.BuildSummary) {
				// For build summary, display error/warning summary
				var startX = layoutBounds.Right + 25;
				DrawImage (ctx, cellArea, Resources.ErrorIconSmall, startX, ImageSize, isSelected, ImagePadding);

				startX += ImageSize + 2;
				var errors = GettextCatalog.GetString ("{0} errors", buildOutputNode.ErrorCount.ToString ());
				layout = DrawText (ctx, cellArea, startX, errors, status.LayoutYPadding, defaultLightFont, layoutBounds.Width);

				startX += layout.GetSize ().Width;
				DrawImage (ctx, cellArea, Resources.WarningIconSmall, startX, ImageSize, isSelected, ImagePadding);

				var warnings = GettextCatalog.GetString ("{0} warnings", buildOutputNode.WarningCount.ToString ());
				startX += ImageSize + 2;
				DrawText (ctx, cellArea, startX, warnings, status.LayoutYPadding, font: defaultLightFont);
			} else if (buildOutputNode.NodeType == BuildOutputNodeType.Build) {
				var textStartX = layoutBounds.Right + BuildConfigurationInformationLeftPadding; 
				DrawText (ctx, cellArea, textStartX, GetInformationMessage (buildOutputNode), status.LayoutYPadding, defaultLightFont, cellArea.Width - textStartX);
			}

			status.LastRenderBounds = cellArea;
			status.LastRenderLayoutBounds = layoutBounds;

			// If the height required by the text is not the same as what was calculated in OnGetRequiredSize(), it means that
			// the required height has changed. In that case call QueueResize(), so that OnGetRequiredSize() is called
			// again and the row is properly resized.
			if (status.Expanded && Math.Abs (layoutBounds.Height - status.LastCalculatedHeight) > 1)
				QueueResize ();
		}

		void DrawNodeInformation (Context ctx, Xwt.Rectangle cellArea, BuildOutputNode buildOutputNode, double padding, bool isSelected, int imageSize, int imagePadding, ViewStatus status)
		{
			if (!buildOutputNode.HasChildren) {
				if (buildOutputNode.NodeType == BuildOutputNodeType.Error || buildOutputNode.NodeType == BuildOutputNodeType.Warning) {
					if (isSelected) {
						ctx.SetColor (Styles.CellTextSelectionColor);
					} else {
						ctx.SetColor (Styles.LinkForegroundColor);
					}
					var text = string.Format ("{0}, line {1}", buildOutputNode.File, buildOutputNode.LineNumber);

					status.TaskLinkRenderRectangle.X = lastErrorPanelStartX + 5;
					status.TaskLinkRenderRectangle.Y = cellArea.Y + padding;

					var layout = DrawText (ctx, cellArea, status.TaskLinkRenderRectangle.X, text, padding, font: defaultLightFont, trimming: TextTrimming.Word, underline: true);
					status.TaskLinkRenderRectangle.Size = layout.GetSize ();
					return;
				}
				return;
			}

			UpdateInformationTextColor (ctx, isSelected);

			var textStartX = cellArea.X + (cellArea.Width - DefaultInformationContainerWidth);

			Size size = Size.Zero;

			//Duration text
			var duration = buildOutputNode.GetDurationAsString (contextProvider.IsShowingDiagnostics);
			if (duration != "") {
				size = DrawText (ctx, cellArea, textStartX, duration, padding, defaultLightFont, DefaultInformationContainerWidth).GetSize ();
				textStartX += size.Width + 10;
			}

			if (textStartX > lastErrorPanelStartX) {
				lastErrorPanelStartX = textStartX;
			} else {
				textStartX = lastErrorPanelStartX;
			}

			status.TaskLinkRenderRectangle.X = status.TaskLinkRenderRectangle.Y = status.TaskLinkRenderRectangle.Width = status.TaskLinkRenderRectangle.Height = 0;

			//Error and Warnings count
			if (!IsRowExpanded (buildOutputNode) &&
			    (buildOutputNode.NodeType == BuildOutputNodeType.Task || buildOutputNode.NodeType == BuildOutputNodeType.Target) &&
			    (buildOutputNode.ErrorCount > 0 || buildOutputNode.WarningCount > 0)) {
				
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

		TextLayout DrawText (Context ctx, Xwt.Rectangle cellArea, double x, string text, double padding, Font font, double width = 0, TextTrimming trimming = TextTrimming.WordElipsis, bool underline = false) 
		{
			if (width < 0) {
				throw new Exception ("width cannot be negative");
			}

			var descriptionTextLayout = new TextLayout ();

			descriptionTextLayout.Font = font;
			descriptionTextLayout.Text = text;
			descriptionTextLayout.Trimming = trimming;
		
			if (underline) {
				descriptionTextLayout.SetUnderline (0, text.Length);
			}
		
			if (width != 0) {
				descriptionTextLayout.Width = width;
			}

			descriptionTextLayout.Height = cellArea.Height;

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

		protected override Size OnGetRequiredSize (SizeConstraint widthConstraint)
		{
			var buildOutputNode = GetValue (BuildOutputNodeField);
			var status = GetViewStatus (buildOutputNode);

			double minWidth = ImageSize + ImagePadding + MinLayoutWidth + DefaultInformationContainerWidth;
			if (widthConstraint.IsConstrained)
				minWidth = Math.Max (minWidth, widthConstraint.AvailableSize);

			// in collapsed state we have always the same height and require the minimal width
			// if the layout height has not been calculated yet, use the ImageSize for the height
			if (!status.Expanded) {
				return new Size (minWidth, status.CollapsedRowHeight > -1 ? status.CollapsedRowHeight : ImageSize);
			}

			double maxLayoutWidth;
			if (widthConstraint.IsConstrained)
				maxLayoutWidth = minWidth - ((ImageSize - 3) + ImageSize + ImagePadding + DefaultInformationContainerWidth);
			else
				maxLayoutWidth = status.LastRenderLayoutBounds.Width;

			TextLayout layout = status.GetUnconstrainedLayout ();
			layout.Markup = buildOutputNode.Message;
			layout.Width = maxLayoutWidth;
			var textSize = layout.GetSize ();
			var height = Math.Max (textSize.Height + 2 * status.LayoutYPadding, ImageSize);
			status.LastCalculatedHeight = height;

			return new Size (minWidth, height);
		}

		Color GetSelectedColor ()
		{
			if (UseStrongSelectionColor) {
				return StrongSelectionColor;
			}
			return SelectionColor;
		}

		Xwt.Drawing.Color GetTextColor (BuildOutputNode buildOutputNode, bool isSelected)
		{
			if (UseStrongSelectionColor && isSelected) {
				if (buildOutputNode.NodeType == BuildOutputNodeType.TargetSkipped) {
					return Styles.CellTextSkippedSelectionColor;
				} else {
					return Styles.CellTextSelectionColor;
				}
			} else {
				if (buildOutputNode.NodeType == BuildOutputNodeType.TargetSkipped) {
					return Styles.CellTextSkippedColor;
				} else {
					return Styles.CellTextColor;
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

				// PERF: calculate the height in collapsed state only once
				// The layout height of the first line is always the same and we want
				// the first line to be always aligned to the left icon in all states.
				// The heights calculated here will be used to always report the static
				// height in collapsed state and to calculate the padding.
				var layout = status.GetUnconstrainedLayout ();
				layout.Font = GetFont (node);
				if (status.NewLineCharIndex > -1)
					layout.Markup = node.Message.Substring (0, status.NewLineCharIndex);
				else
					layout.Markup = node.Message;
				var textSize = layout.GetSize ();
				status.CollapsedLayoutHeight = textSize.Height;
				status.CollapsedRowHeight = Math.Max (textSize.Height, ImageSize);
				status.LayoutYPadding = (status.CollapsedRowHeight - status.CollapsedLayoutHeight) * .5;
			}
		}

		internal void OnDataSourceChanged () 
		{
			viewStatus.Clear ();
		}

		#region Mouse Events

		bool expanderHovered;
		bool ExpanderHovered {
			get { return expanderHovered; }
			set {
				if (value != expanderHovered) {
					expanderHovered = value;
					QueueDraw ();
				}
			}
		}

		protected override void OnMouseMoved (MouseMovedEventArgs args)
		{
			var node = GetValue (BuildOutputNodeField);
			var status = GetViewStatus (node);

			if (status.TaskLinkRenderRectangle != Rectangle.Zero && status.TaskLinkRenderRectangle.Contains (args.Position)) {
				ParentWidget.Cursor = CursorType.Hand;
			} else {
				ParentWidget.Cursor = CursorType.Arrow;
			}

			CalcLayout (node, status, Bounds, out var layout, out var layoutBounds, out var expanderRect);

			if (expanderRect != Rectangle.Zero && expanderRect.Contains (args.Position)) {
				ExpanderHovered = true;
			} else {
				ExpanderHovered = false;
			}

			var layoutSize = layout.GetSize ();
			var insideText = new Rectangle (layoutBounds.TopLeft, layoutSize).Contains (args.Position);
			if (clicking && insideText && selectionRow == node) {
				var pos = layout.GetIndexFromCoordinates (args.Position.X - layoutBounds.X, args.Position.Y - layoutBounds.Y);
				if (pos != -1) {
					SelectionEnd = pos;
					QueueDraw ();
				}
			} else {
				ParentWidget.Cursor = insideText ? CursorType.IBeam : CursorType.Arrow;
			}
		}

		protected override void OnButtonPressed (ButtonEventArgs args)
		{
			var node = GetValue (BuildOutputNodeField);
			var status = GetViewStatus (node);

			if (args.Button == PointerButton.Left && args.MultiplePress == 0 && status.TaskLinkRenderRectangle != Rectangle.Zero && status.TaskLinkRenderRectangle.Contains (args.Position) ) {
				GoToTask?.Invoke (this, node);
				return;
			}

			CalcLayout (node, status, Bounds, out var layout, out var layoutBounds, out var expanderRect);

			if (expanderRect != Rectangle.Zero && expanderRect.Contains (args.Position)) {
				status.Expanded = !status.Expanded;
				QueueResize ();
				return;
			}

			if (args.Button != PointerButton.Right) {
				var pos = layout.GetIndexFromCoordinates (args.Position.X - layoutBounds.X, args.Position.Y - layoutBounds.Y);
				if (pos != -1) {
					SelectionStart = SelectionEnd = pos;
					selectionRow = node;
					clicking = true;
				} else {
					selectionRow = null;
				}
				
				QueueDraw ();
			}
		
			base.OnButtonPressed (args);
		}

		void CalcLayout (BuildOutputNode node, ViewStatus status, Rectangle cellArea, out TextLayout layout, out Rectangle layoutBounds, out Rectangle expanderRect)
		{
			expanderRect = Rectangle.Zero;
			layoutBounds = cellArea;
			layoutBounds.X += ImageSize - 3;
			layoutBounds.Width -= (ImageSize - 3) + DefaultInformationContainerWidth;

			layout = status.GetUnconstrainedLayout ();
			if (!status.Expanded && status.NewLineCharIndex > -1)
				CreateMarkupText (layout, node, node.Message.Substring (0, status.NewLineCharIndex), contextProvider.SearchString);
			else
				CreateMarkupText (layout, node, node.Message, contextProvider.SearchString);
			
			var textSize = layout.GetSize ();

			if (textSize.Width > layoutBounds.Width || status.NewLineCharIndex > -1) {
				layoutBounds.Width -= (ImageSize + ImagePadding);
				layout.Width = Math.Max (MinLayoutWidth, layoutBounds.Width);
				if (!status.Expanded)
					layout.Trimming = TextTrimming.WordElipsis;
				textSize = layout.GetSize ();

				var expanderX = layoutBounds.Right + ImagePadding;
				if (expanderX > 0)
					expanderRect = new Rectangle (expanderX, cellArea.Y + ((status.CollapsedLayoutHeight - BuildExpandIcon.Height) * .5), BuildExpandIcon.Width, BuildExpandIcon.Height);
			}

			if (layoutBounds.Height > textSize.Height) {
				var padding = status.LayoutYPadding > 0 ? status.LayoutYPadding : (layoutBounds.Height - textSize.Height) * .5;
				layoutBounds.Y += padding;
				expanderRect.Y += padding;
			}
		}

		protected override void OnButtonReleased (ButtonEventArgs args)
		{
			if (clicking) {
				clicking = false;
				QueueDraw ();
			}
			base.OnButtonReleased (args);
		}

		Font GetFont (BuildOutputNode node)
		{
			if (IsRootNode (node)) {
				return defaultBoldFont;
			} else if (node.IsCommandLine) {
				return monospaceFont;
			}
			return defaultLightFont;
		}

		protected override void OnMouseExited ()
		{
			ExpanderHovered = false;
			ParentWidget.Cursor = CursorType.Arrow;
			base.OnMouseExited ();
		}

		#endregion
	}
}
