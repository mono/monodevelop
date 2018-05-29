﻿using System;
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
		public static Xwt.Drawing.Color CellTextSelectionColorSecundary { get; internal set; }


		public static Color CellErrorBackgroundColor { get; set; }
		public static Color CellErrorLineBackgroundColor { get; set; }

		public static Color CellWarningBackgroundColor { get; set; } 
		public static Color CellWarningLineBackgroundColor { get; set; }

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

				CellErrorBackgroundColor = Color.FromBytes (245, 109, 79, 20);
				CellErrorLineBackgroundColor = Color.FromBytes (245, 109, 79, 100);
				CellWarningBackgroundColor = Color.FromBytes (241, 196, 15, 20);
				CellWarningLineBackgroundColor = Color.FromBytes (241, 196, 15, 100);

			} else {
				CellBackgroundColor = Xwt.Drawing.Color.FromName ("#3c3c3c");
				SearchMatchUnfocusedBackgroundColor = Xwt.Drawing.Color.FromName ("#a2a53f");

				CellErrorBackgroundColor = Color.FromBytes (255, 113, 82, 30); ;
				CellErrorLineBackgroundColor = Color.FromBytes (255, 113, 82, 175);
				CellWarningBackgroundColor = Color.FromBytes (255, 207, 15, 30);
				CellWarningLineBackgroundColor = Color.FromBytes (255, 207, 15, 175);
			}

			// Shared
			CellTextColor = Ide.Gui.Styles.BaseForegroundColor;
			CellStrongSelectionColor = Ide.Gui.Styles.BaseSelectionBackgroundColor;
			CellSelectionColor = Ide.Gui.Styles.BaseSelectionBackgroundColor;
			CellTextSelectionColor = Ide.Gui.Styles.BaseSelectionTextColor;
			CellTextSkippedColor = Ide.Gui.Styles.SecondaryTextColor;
			CellTextSkippedSelectionColor = Ide.Gui.Styles.SecondarySelectionTextColor;
			LinkForegroundColor = Xwt.Drawing.Color.FromName ("#999999");
			SearchMatchFocusedBackgroundColor = Xwt.Drawing.Color.FromName ("#fcff54");
			CellTextSelectionColorSecundary = Colors.LightBlue;
		}

		public static Xwt.Drawing.Color GetTextColor (BuildOutputNode buildOutputNode, bool isSelected)
		{
			if (isSelected) {
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

		public static Color GetSearchMatchBackgroundColor (bool focused)
		{
			return focused ? Styles.SearchMatchFocusedBackgroundColor : Styles.SearchMatchUnfocusedBackgroundColor;
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
		public static readonly Xwt.Drawing.Image TaskSuccessIcon = ImageService.GetIcon (Ide.Gui.Stock.BuildTaskSuccess, Gtk.IconSize.Menu);
		public static readonly Xwt.Drawing.Image TaskFailedIcon = ImageService.GetIcon (Ide.Gui.Stock.BuildTaskFailed, Gtk.IconSize.Menu);
		public static readonly Xwt.Drawing.Image WarningIcon = ImageService.GetIcon (Ide.Gui.Stock.BuildWarning, Gtk.IconSize.Menu);
		public static readonly Xwt.Drawing.Image WarningIconSmall = ImageService.GetIcon (Ide.Gui.Stock.BuildWarningSmall, Gtk.IconSize.Menu);
		public static readonly Xwt.Drawing.Image FolderIcon = ImageService.GetIcon (Ide.Gui.Stock.OpenFolder, Gtk.IconSize.Menu);
	}

	class BuildOutputCellSelection
	{
		public int SelectionStart { get; private set; }
		public int SelectionEnd { get; private set; }
		public BuildOutputNode Node { get; private set; }

		public BuildOutputCellSelection (BuildOutputNode node, int selectionStart, int selectionEnd)
		{
			SelectionStart = selectionStart;
			SelectionEnd = selectionEnd;
			Node = node;
		}
	}

	class BuildOutputTreeCellView : CanvasCellView
	{
		static readonly Image BuildExpandIcon = ImageService.GetIcon (Ide.Gui.Stock.BuildExpand, Gtk.IconSize.Menu).WithSize (16);
		static readonly Image BuildCollapseIcon = ImageService.GetIcon (Ide.Gui.Stock.BuildCollapse, Gtk.IconSize.Menu).WithSize (16);
		static readonly Image BuildExpandIconSel = BuildExpandIcon.WithStyles ("sel");
		static readonly Image BuildCollapseIconSel = BuildCollapseIcon.WithStyles ("sel");

		public EventHandler<BuildOutputNode> GoToTask;
		public EventHandler<BuildOutputNode> ExpandErrors;
		public EventHandler<BuildOutputNode> ExpandWarnings;

		class ViewStatus
		{
			TextLayout layout = new TextLayout ();

			bool expanded;
			public Rectangle LastRenderBounds = Rectangle.Zero;
			public Rectangle LastRenderLayoutBounds = Rectangle.Zero;
			public Rectangle LastRenderExpanderBounds = Rectangle.Zero;
			public double CollapsedRowHeight = -1;
			public double CollapsedLayoutHeight = -1;
			public double LayoutYPadding = 0;
			public int NewLineCharIndex = -1;

			public Rectangle TaskLinkRenderRectangle = Rectangle.Zero;
			public Rectangle ErrorsRectangle = Rectangle.Zero;
			public Rectangle WarningsRectangle = Rectangle.Zero;

			public Rectangle Task = Rectangle.Zero;

			public bool DrawsTopLine { get; set; }
			public bool DrawsBottomLine { get; set; }

			public bool Expanded {
				get { return expanded; }
				set {
					if (expanded != value) {
						expanded = value;
						Reload ();
						LastRenderBounds = Rectangle.Zero;
						LastRenderLayoutBounds = Rectangle.Zero;
						LastRenderExpanderBounds = Rectangle.Zero;
						layout.Width = layout.Height = -1;
					}
				}
			}

			public BuildOutputNode Node { get; private set; }

			public bool IsRootNode => Node.Parent == null;

			public ViewStatus (BuildOutputNode node)
			{
				if (node == null)
					throw new ArgumentNullException (nameof (node));
				Node = node;
				layout.Font = GetFont (node);
			}

			Font GetFont (BuildOutputNode node)
			{
				if (IsRootNode) {
					return defaultBoldFont;
				} else if (node.IsCommandLine) {
					return monospaceFont;
				}
				return defaultFont;
			}

			public void Reload ()
			{
				var message = Node.Message;
				NewLineCharIndex = message.IndexOf ('\n');
				if (!Expanded && NewLineCharIndex > -1)
					message = message.Substring (0, NewLineCharIndex);
				if (layout.Text != message) {
					layout.Text = message;

					// PERF: calculate the height in collapsed state only once
					// The layout height of the first line is always the same and we want
					// the first line to be always aligned to the left icon in all states.
					// The heights calculated here will be used to always report the static
					// height in collapsed state and to calculate the padding.
					if (!Expanded || CollapsedLayoutHeight < 0) {
						layout.Trimming = TextTrimming.WordElipsis;
						var textSize = layout.GetSize ();
						CollapsedLayoutHeight = textSize.Height;
						CollapsedRowHeight = Math.Max (textSize.Height, ImageSize);
						LayoutYPadding = (CollapsedRowHeight - CollapsedLayoutHeight) * .5;
					}
				}
				layout.Trimming = Expanded ? TextTrimming.Word : TextTrimming.WordElipsis;
			}

			public TextLayout GetUnconstrainedLayout ()
			{
				layout.Width = layout.Height = -1;
				return layout;
			}

			internal void Initialize ()
			{
				DrawsBottomLine = Node.Next == null || !(Node.Next.NodeType == BuildOutputNodeType.Error || Node.Next.NodeType == BuildOutputNodeType.Warning);
				DrawsTopLine = Node.Previous == null || !(Node.Previous.NodeType == BuildOutputNodeType.Error || Node.Previous.NodeType == BuildOutputNodeType.Warning);

				Reload ();
			}
		}

		const int BuildTypeRowContentPadding = 6;
		const int RowContentPadding = 3;
		const int BuildConfigurationInformationLeftPadding = 32;

		const int DefaultRowHeight = 20;
		const int LinesDisplayedCount = 1;
		const int DefaultInformationContainerWidth = 370;
		const int ImageSize = 16;
		const int ImagePadding = 0;
		const int FontSize = 11;
		const int MinLayoutWidth = 30;

		public Color StrongSelectionColor { get; set; }
		public Color SelectionColor { get; set; }

		public bool UseStrongSelectionColor { get; set; }

		public IDataField<bool> HasBackgroundColorField { get; set; }
		public IDataField<BuildOutputNode> BuildOutputNodeField { get; set; }

		// Used to track the selection
		int selectionStart;
		int selectionEnd;
		BuildOutputNode selectionRow;
		bool clicking;

		IBuildOutputContextProvider contextProvider;

		// This could also be stored in the data store. In this example we keep it in
		// an internal dictionary to clearly separate the data model from the view model.
		// This is a simple implementation, it doesn't take into account that nodes could
		// be removed
		Dictionary<BuildOutputNode, ViewStatus> viewStatus = new Dictionary<BuildOutputNode, ViewStatus> ();

		bool IsRowExpanded (BuildOutputNode buildOutputNode) => ((Xwt.TreeView)ParentWidget)?.IsRowExpanded (buildOutputNode) ?? false;

		string GetInformationMessage (BuildOutputNode buildOutputNode) => GettextCatalog.GetString ("{0} | {1}     Started at {2}", buildOutputNode.Configuration, buildOutputNode.Platform, buildOutputNode.StartTime.ToString ("h:m tt on MMM d, yyyy"));

		public BuildOutputCellSelection GetCurrentSelection () => new BuildOutputCellSelection (selectionRow, selectionStart, selectionEnd);

		static Font defaultFont;
		static Font defaultBoldFont;
		static Font monospaceFont;

		double lastErrorPanelStartX;

		static BuildOutputTreeCellView ()
		{
			var fontName = Font.FromName (Gui.Styles.DefaultFontName)
			                   .WithSize (FontSize);
			defaultBoldFont = fontName.WithWeight (FontWeight.Bold);
			defaultFont = fontName.WithWeight (FontWeight.Normal);
			monospaceFont = FontService.MonospaceFont.ToXwtFont ().WithSize (FontSize);
		}

		public BuildOutputTreeCellView (IBuildOutputContextProvider context)
		{
			StrongSelectionColor = Styles.CellStrongSelectionColor;
			SelectionColor = Styles.CellSelectionColor;
			UseStrongSelectionColor = true;
			contextProvider = context;
		}

		internal void OnBoundsChanged (object sender, EventArgs args) 
		{
			lastErrorPanelStartX = 0;
		}

		static void HighlightSearchResults (TextLayout layout, string search, Color foreground, Color background)
		{
			string text = null;
			if (layout != null) {
				text = layout.Text;
				layout.ClearAttributes ();
			}
			if (string.IsNullOrEmpty (text) || string.IsNullOrEmpty (search))
				return;
			int index = 0;
			while ((index = text.IndexOf (search, index, StringComparison.OrdinalIgnoreCase)) > -1) {
				layout.SetForeground (foreground, index, search.Length);
				layout.SetBackground (background, index, search.Length);
				index++;
			}
		}


		void FillCellBackground (Context ctx, BuildOutputNode buildOutputNode, ViewStatus status)
		{
			if (!buildOutputNode.HasChildren) {
				if (buildOutputNode.NodeType == BuildOutputNodeType.Error) {
					FillCellBackground (ctx, Styles.CellErrorBackgroundColor);

					if (status.DrawsTopLine) {
						DrawTopLine (ctx, Styles.CellErrorLineBackgroundColor);
					}

					if (status.DrawsBottomLine) {
						DrawBottomLine (ctx, Styles.CellErrorLineBackgroundColor);
					}

				} else if (buildOutputNode.NodeType == BuildOutputNodeType.Warning) {
					FillCellBackground (ctx, Styles.CellWarningBackgroundColor);

					if (status.DrawsTopLine) {
						DrawTopLine (ctx, Styles.CellWarningLineBackgroundColor);
					}

					if (status.DrawsBottomLine) {
						DrawBottomLine (ctx, Styles.CellWarningLineBackgroundColor);
					}
				}
			}
		}

		void DrawBottomLine (Context ctx, Color color) => DrawLine (ctx, color, BackgroundBounds.BottomLeft, BackgroundBounds.BottomRight);

		void DrawTopLine (Context ctx, Color color) => DrawLine (ctx, color, BackgroundBounds.TopLeft, BackgroundBounds.TopRight);

		void DrawLine (Context ctx, Color color, Point init, Point end)
		{
			ctx.SetColor (color);
			ctx.SetLineWidth (2);
			ctx.MoveTo (init);
			ctx.LineTo (end);
			ctx.Stroke ();
		}

		void FillCellBackground (Context ctx, Color color)
		{
			ctx.Rectangle(BackgroundBounds);
			ctx.SetColor(color);
			ctx.Fill();
		}

		protected override void OnDraw(Context ctx, Xwt.Rectangle cellArea)
		{
			var buildOutputNode = GetValue (BuildOutputNodeField);
			var isSelected = Selected;

			var status = GetViewStatus (buildOutputNode);

			//Draw the node background
			FillCellBackground (ctx, buildOutputNode, status);

			//Draw the image row
			DrawImage (ctx, cellArea, buildOutputNode.GetImage (), (cellArea.Left - 3), ImageSize, isSelected, ImagePadding);

			// If the height required by the text is not the same as what was calculated in OnGetRequiredSize(), it means that
			// the required height has changed and CalcLayout will return false. In that case call QueueResize(),
			// so that OnGetRequiredSize() is called again and the row is properly resized.
			if (!CalcLayout (status, cellArea, out var layout, out var layoutBounds, out var expanderRect))
				QueueResize ();

			status.LastRenderBounds = cellArea;
			status.LastRenderLayoutBounds = layoutBounds;
			status.LastRenderExpanderBounds = expanderRect;

			ctx.SetColor (Styles.GetTextColor (buildOutputNode, UseStrongSelectionColor && isSelected));

			HighlightSearchResults (layout, contextProvider.SearchString, Styles.GetTextColor (buildOutputNode, false), Styles.GetSearchMatchBackgroundColor (isSelected));

			// Render the selection
			if (selectionRow == buildOutputNode && selectionStart != selectionEnd) {
				layout.SetBackground (Styles.CellTextSelectionColorSecundary, Math.Min (selectionStart, selectionEnd), Math.Abs (selectionEnd - selectionStart));
			}

			// Draw the text
			ctx.DrawTextLayout (layout, layoutBounds.X, layoutBounds.Y);

			// Draw right hand expander
			if (!expanderRect.IsEmpty) {
				// Draw the image
				Image icon;
				if (status.Expanded)
					icon = isSelected ? BuildCollapseIconSel : BuildCollapseIcon;
				else
					icon = isSelected ? BuildExpandIconSel : BuildExpandIcon;
				ctx.DrawImage (icon, expanderRect.X, expanderRect.Y);
			}

			//Information section
			if (!status.IsRootNode) {
				DrawNodeInformation (ctx, cellArea, buildOutputNode, status.LayoutYPadding, isSelected, ImageSize, ImagePadding, status);
			} else if (buildOutputNode.NodeType == BuildOutputNodeType.BuildSummary) {
				// For build summary, display error/warning summary
				var startX = layoutBounds.X + layout.GetSize ().Width + 24;

				status.ErrorsRectangle.X = startX;
				status.ErrorsRectangle.Y = cellArea.Y;
				DrawImage (ctx, cellArea, Resources.ErrorIconSmall, startX, ImageSize, isSelected, ImagePadding);

				startX += ImageSize + 2;
				var errors = GettextCatalog.GetString ("{0} errors", buildOutputNode.ErrorCount.ToString ());
				layout = DrawText (ctx, cellArea, startX, errors, status.LayoutYPadding, defaultFont, layoutBounds.Width);

				var size = layout.GetSize ();
				//Our error rectangle includes text + image + margin
				status.ErrorsRectangle.Width = size.Width + ImageSize + 2;
				status.ErrorsRectangle.Height = size.Height;

				startX += size.Width + 24;

				status.WarningsRectangle.X = startX;
				status.WarningsRectangle.Y = cellArea.Y;

				DrawImage (ctx, cellArea, Resources.WarningIconSmall, startX, ImageSize, isSelected, ImagePadding);

				var warnings = GettextCatalog.GetString ("{0} warnings", buildOutputNode.WarningCount.ToString ());
				startX += ImageSize + 2;
				layout = DrawText (ctx, cellArea, startX, warnings, status.LayoutYPadding, font: defaultFont);

				size = layout.GetSize ();
				status.WarningsRectangle.Width = size.Width + ImageSize + 2;
				status.WarningsRectangle.Height = size.Height;

			} else if (buildOutputNode.NodeType == BuildOutputNodeType.Build) {
				var textStartX = layoutBounds.X + layout.GetSize ().Width + 24;
				DrawText (ctx, cellArea, textStartX, GetInformationMessage (buildOutputNode), status.LayoutYPadding, defaultFont, cellArea.Width - textStartX);
			}
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

					var layout = DrawText (ctx, cellArea, status.TaskLinkRenderRectangle.X, text, padding, font: defaultFont, trimming: TextTrimming.Word, underline: true);
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
				size = DrawText (ctx, cellArea, textStartX, duration, padding, defaultFont, DefaultInformationContainerWidth).GetSize ();
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

				if (buildOutputNode.ErrorCount > 0) {
					DrawImage (ctx, cellArea, Resources.ErrorIconSmall, textStartX, imageSize, isSelected, imagePadding);
					textStartX += ImageSize + 2;
					var errors = buildOutputNode.ErrorCount.ToString ();

					var layout = DrawText (ctx, cellArea, textStartX, errors, padding, defaultFont, trimming: TextTrimming.Word);
					textStartX += layout.GetSize ().Width;
				}

				if (buildOutputNode.WarningCount > 0) {
					DrawImage (ctx, cellArea, Resources.WarningIconSmall, textStartX, imageSize, isSelected, imagePadding);
					textStartX += ImageSize + 2;
					DrawText (ctx, cellArea, textStartX, buildOutputNode.WarningCount.ToString (), padding, defaultFont, 10, trimming: TextTrimming.Word);
				}
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

		void DrawImage (Context ctx, Xwt.Rectangle cellArea, Image image, double x, int imageSize, bool isSelected, double topPadding = 0)
		{
			ctx.DrawImage (isSelected ? image.WithStyles ("sel") : image, x, cellArea.Top + (cellArea.Height / 2 - imageSize / 2), imageSize, imageSize);
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
			layout.Width = maxLayoutWidth;
			var textSize = layout.GetSize ();
			var height = Math.Max (textSize.Height + 2 * status.LayoutYPadding, DefaultRowHeight);

			return new Size (minWidth, height);
		}

		bool IsBackgroundColorFieldSet ()
		{
			return GetValue (HasBackgroundColorField, false);
		}

		ViewStatus GetViewStatus (BuildOutputNode node)
		{
			if (!viewStatus.TryGetValue (node, out var status))
				status = viewStatus [node] = new ViewStatus (node);
			return status;
		}

		protected override void OnDataChanged ()
		{
			base.OnDataChanged ();
			var node = GetValue (BuildOutputNodeField);
			if (node != null) {
				var status = GetViewStatus (node);
				status.Initialize ();
			}
		}

		internal void OnDataSourceChanged () 
		{
			viewStatus.Clear ();
		}

		#region Mouse Events

		protected override void OnMouseMoved (MouseMovedEventArgs args)
		{
			var node = GetValue (BuildOutputNodeField);
			var status = GetViewStatus (node);

			if (status.TaskLinkRenderRectangle.Contains (args.Position) || status.ErrorsRectangle.Contains (args.Position) || status.WarningsRectangle.Contains (args.Position)) {
				ParentWidget.Cursor = CursorType.Hand;
			} else {
				ParentWidget.Cursor = CursorType.Arrow;
			}

			CalcLayout (status, Bounds, out var layout, out var layoutBounds, out var expanderRect);

			var insideText = layoutBounds.Contains (args.Position);
			if (clicking && insideText && selectionRow == node) {
				var pos = layout.GetIndexFromCoordinates (args.Position.X - layoutBounds.X, args.Position.Y - layoutBounds.Y);
				if (pos != -1) {
					selectionEnd = pos;
					QueueDraw ();
				}
			} else {
				if (insideText) {
					ParentWidget.Cursor = CursorType.IBeam;
				}
			}
		}

		protected override void OnButtonPressed (ButtonEventArgs args)
		{
			var node = GetValue (BuildOutputNodeField);
			var status = GetViewStatus (node);

			if (args.Button == PointerButton.Left && args.MultiplePress == 0) {

				if (status.TaskLinkRenderRectangle.Contains (args.Position)) {
					GoToTask?.Invoke (this, node);
					return;
				}

				if (status.ErrorsRectangle.Contains (args.Position)) {
					ExpandErrors?.Invoke (this, node);
					return;
				}

				if (status.WarningsRectangle.Contains (args.Position)) {
					ExpandWarnings?.Invoke (this, node);
					return;
				}

			}

			CalcLayout (status, Bounds, out var layout, out var layoutBounds, out var expanderRect);

			if (args.Button == PointerButton.Left) {
				if (expanderRect != Rectangle.Zero && expanderRect.Contains (args.Position)) {
					status.Expanded = !status.Expanded;
					QueueResize ();
					return;
				}

				if (layoutBounds.Contains (args.Position)) {
					var pos = layout.GetIndexFromCoordinates (args.Position.X - layoutBounds.X, args.Position.Y - layoutBounds.Y);
					if (pos != -1) {
						selectionStart = selectionEnd = pos;
						selectionRow = node;
						clicking = true;
					} else {
						selectionRow = null;
					}
					QueueDraw ();
				}
			}

			base.OnButtonPressed (args);
		}

		protected override void OnButtonReleased (ButtonEventArgs args)
		{
			if (clicking) {
				clicking = false;
				QueueDraw ();
			}
			base.OnButtonReleased (args);
		}

		bool CalcLayout (ViewStatus status, Rectangle cellArea, out TextLayout layout, out Rectangle layoutBounds, out Rectangle expanderRect)
		{
			// Relayouting is expensive and not required if the size didn't change. Just update the locations in that case.
			if (!status.LastRenderBounds.IsEmpty && status.LastRenderBounds.Contains (status.LastRenderLayoutBounds) && cellArea.Size == status.LastRenderBounds.Size) {
				expanderRect = status.LastRenderExpanderBounds.Offset (-status.LastRenderBounds.X, -status.LastRenderBounds.Y).Offset (cellArea.X, cellArea.Y);
				layoutBounds = status.LastRenderLayoutBounds.Offset (-status.LastRenderBounds.X, -status.LastRenderBounds.Y).Offset (cellArea.X, cellArea.Y);
				layout = status.GetUnconstrainedLayout ();
				layout.Width = layoutBounds.Width;
				return true; // no resize is required
			}

			expanderRect = Rectangle.Zero;
			layoutBounds = cellArea;
			layoutBounds.X += ImageSize - 3;
			layoutBounds.Width -= (ImageSize - 3) + DefaultInformationContainerWidth;

			layout = status.GetUnconstrainedLayout ();
			var textSize = layout.GetSize ();

			if (textSize.Width > layoutBounds.Width || status.NewLineCharIndex > -1) {
				layoutBounds.Width -= (ImageSize + ImagePadding);
				layoutBounds.Width = Math.Max (MinLayoutWidth, layoutBounds.Width);
				layout.Width = layoutBounds.Width;
				layout.Height = status.Expanded ? -1 : cellArea.Height;
				
				textSize = layout.GetSize ();

				var expanderX = layoutBounds.Right + ImagePadding;
				if (expanderX > 0)
					expanderRect = new Rectangle (expanderX, cellArea.Y + ((status.CollapsedLayoutHeight - BuildExpandIcon.Height) * .5), BuildExpandIcon.Width, BuildExpandIcon.Height);
			}

			layoutBounds.Height = textSize.Height;
			layoutBounds.Y += status.LayoutYPadding;
			expanderRect.Y += status.LayoutYPadding;

			// check that the text still fits into the cell
			if (!cellArea.Contains (layoutBounds))
				return false; // resize required
			// if the cell is too large, we need to resize it
			else if (status.Expanded && Math.Abs (layoutBounds.Height - status.LayoutYPadding - cellArea.Height) > 1)
				return false; // resize required
			return true;
		}

		protected override void OnMouseExited ()
		{
			ParentWidget.Cursor = CursorType.Arrow;
			base.OnMouseExited ();
		}

		#endregion
	}
}
