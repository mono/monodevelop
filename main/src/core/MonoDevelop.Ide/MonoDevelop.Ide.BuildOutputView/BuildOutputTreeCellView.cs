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
		public static readonly Xwt.Drawing.Image EmptyIcon = ImageService.GetIcon (Ide.Gui.Stock.Empty);
	}

	enum TextSelectionState
	{
		Clicked,
		Selecting,
		Selected //selection is finished but we want continue drawing the selection
	}

	class TextSelection<T> where T : class
	{
		const int SelectionDistancePixel = 1;

		Point selectionStartingPoint;
		int selectionStart;
		int selectionEnd;

		public T Content { get; private set; }

		public TextSelectionState State { get; private set; } = TextSelectionState.Clicked;
		public int Index => Math.Min (selectionStart, selectionEnd);
		public int Length => Math.Abs (selectionEnd - selectionStart) + 1;

		public bool IsStarting (T currentContent) => State != TextSelectionState.Selected && currentContent == Content;

		public bool IsShown (T currentContent) => (State == TextSelectionState.Selecting || State == TextSelectionState.Selected) && currentContent == Content;

		public void Stop ()
		{
			State = TextSelectionState.Selected;
		}

		public void Set (int pos, Point position)
		{
			selectionEnd = pos;

			if (State == TextSelectionState.Clicked && Away (selectionStartingPoint, position, SelectionDistancePixel)) {
				State = TextSelectionState.Selecting;
			}
		}

		static bool Away (Point first, Point second, double maxPixel)
		{
			if (Math.Abs (first.X - second.X) > maxPixel)
				return true;
			if (Math.Abs (first.Y - second.Y) > maxPixel)
				return true;
			return false;
		}

		public void Start (Point position, int pos, T content)
		{
			selectionStart = selectionEnd = pos;
			State = TextSelectionState.Clicked;
			selectionStartingPoint = position;
			Content = content;
		}
	}

	class BuildOutputTreeCellView : CanvasCellView
	{
		static readonly Image BuildExpandIcon = ImageService.GetIcon (Ide.Gui.Stock.BuildExpand, Gtk.IconSize.Menu)
			.WithSize (10);
		static readonly Image BuildCollapseIcon = ImageService.GetIcon (Ide.Gui.Stock.BuildCollapse, Gtk.IconSize.Menu)
			.WithSize (10);

		public EventHandler<BuildOutputNode> GoToTask;
		public EventHandler<BuildOutputNode> ExpandErrors;
		public EventHandler<BuildOutputNode> ExpandWarnings;

		class ViewStatus
		{
			internal TextLayout layout = new TextLayout ();

			bool expanded;
			public Rectangle LastRenderBounds = Rectangle.Zero;
			public Rectangle LastRenderLayoutBounds = Rectangle.Zero;
			public Rectangle LastRenderImageBounds = Rectangle.Zero;
			public Rectangle LastRenderExpanderBounds = Rectangle.Zero;
			public double CollapsedRowHeight = -1;
			public double CollapsedLayoutHeight = -1;
			public double LayoutYPadding = 0;
			public double ExpanderYPadding = 0;
			public double IconYPadding = 0;

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

						layout.Width = layout.Height = -1;
						Reload ();
						CalculateLayout (LastRenderBounds);
					}
				}
			}

			internal bool CalculateLayout (Rectangle cellArea)
			{
				// Relayouting is expensive and not required if the size didn't change. Just update the locations in that case.
				if (!LastRenderBounds.IsEmpty && LastRenderBounds.Contains (LastRenderLayoutBounds) && cellArea.Size == LastRenderBounds.Size) {
					LastRenderExpanderBounds = LastRenderExpanderBounds.Offset (-LastRenderBounds.X, -LastRenderBounds.Y).Offset (cellArea.X, cellArea.Y);
					LastRenderLayoutBounds = LastRenderLayoutBounds.Offset (-LastRenderBounds.X, -LastRenderBounds.Y).Offset (cellArea.X, cellArea.Y);
					LastRenderImageBounds = LastRenderImageBounds.Offset (-LastRenderBounds.X, -LastRenderBounds.Y).Offset (cellArea.X, cellArea.Y);

					LastRenderBounds = cellArea;
					return true; // no resize is required
				}

				var imageRect = Rectangle.Zero;
				var expanderRect = Rectangle.Zero;

				var textLayout = GetUnconstrainedLayout ();
				var textSize = textLayout.GetSize ();

				var maxWidth = cellArea.Width - DefaultInformationContainerWidth;
				if (HasIcon) {
					maxWidth -= ImageSize + ImagePadding;
				}

				var startX = cellArea.X;
				var needsExpander = textSize.Width > maxWidth || NewLineCharIndex > -1;
				if (needsExpander) {
					expanderRect = new Rectangle (startX, cellArea.Y + ExpanderYPadding, ImageSize, ImageSize);
					startX += ImageSize;
				}

				if (HasIcon) {
					imageRect = new Rectangle (startX, cellArea.Y, ImageSize, ImageSize);
					startX += ImageSize + ImagePadding;
				}

				var layoutBounds = cellArea;
				layoutBounds.X = startX;

				layoutBounds.Width -= startX - cellArea.X + DefaultInformationContainerWidth;

				textLayout.Width = layoutBounds.Width;

				if (needsExpander) {
					textLayout.Height = Expanded ? -1 : cellArea.Height;
					textSize = textLayout.GetSize ();
				} else {
					layoutBounds.Height = textSize.Height;
				}

				layoutBounds.Height = textSize.Height;
				layoutBounds.Y += LayoutYPadding;

				LastRenderImageBounds = imageRect;
				LastRenderExpanderBounds = expanderRect;
				LastRenderLayoutBounds = layoutBounds;
				LastRenderBounds = cellArea;

				// check that the text still fits into the cell
				if (!cellArea.Contains (layoutBounds))
					return false; // resize required
								  // if the cell is too large, we need to resize it
				if (Expanded && Math.Abs (layoutBounds.Height - LayoutYPadding - cellArea.Height) > 1)
					return false; // resize required
				return true;
			}

			public BuildOutputNode Node { get; private set; }
			readonly public Image Icon;

			public bool IsRootNode => Node.Parent == null;

			public bool HasIcon => Icon != Resources.EmptyIcon;

			public ViewStatus (BuildOutputNode node)
			{
				if (node == null)
					throw new ArgumentNullException (nameof (node));
				Node = node;
				Icon = Node.GetImage ();
				layout.Font = GetFont (node);
			}

			Font GetFont (BuildOutputNode node)
			{
				if (IsRootNode) {
					return defaultBoldFont;
				}
				if (node.IsCommandLine) {
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
						ExpanderYPadding = (CollapsedRowHeight - BuildExpandIcon.Height) * .5;
						IconYPadding = (CollapsedRowHeight - ImageSize) * .5;
					}
				}
				layout.Trimming = Expanded ? TextTrimming.Word : TextTrimming.WordElipsis;
			}

			public TextLayout GetUnconstrainedLayout ()
			{
				layout.Width = layout.Height = -1;
				return layout;
			}

			internal string Duration { get; private set; }

			internal void Initialize (bool isShowingDiagnostics)
			{
				DrawsBottomLine = Node.Next == null || !(Node.Next.NodeType == BuildOutputNodeType.Error || Node.Next.NodeType == BuildOutputNodeType.Warning);
				DrawsTopLine = Node.Previous == null || !(Node.Previous.NodeType == BuildOutputNodeType.Error || Node.Previous.NodeType == BuildOutputNodeType.Warning);
				Duration = Node.GetDurationAsString (isShowingDiagnostics);
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
		const int ImagePadding = 4;
		const int FontSize = 11;
		const int MinLayoutWidth = 30;

		const int DefaultExpandClickDelay = 250;

		DateTime lastExpanderClick = DateTime.Now;

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

		bool IsRowExpanded (BuildOutputNode buildOutputNode) => ((Xwt.TreeView)ParentWidget)?.IsRowExpanded (buildOutputNode) ?? false;

		string GetInformationMessage (BuildOutputNode buildOutputNode) => GettextCatalog.GetString ("{0} | {1}     Started at {2}", buildOutputNode.Configuration, buildOutputNode.Platform, buildOutputNode.StartTime.ToString ("h:m tt on MMM d, yyyy"));

		internal bool IsViewClickable (BuildOutputNode node, Point position)
		{
			var view = GetViewStatus (node);
			return !view.LastRenderExpanderBounds.Contains (position) && view.LastRenderBounds.Contains (position);
		}

		static Font defaultFont;
		static Font defaultBoldFont;
		static Font monospaceFont;

		double lastErrorPanelStartX;

		public TextSelection<BuildOutputNode> TextSelection { get; private set; }

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

		protected override void OnDraw (Context ctx, Xwt.Rectangle cellArea)
		{
			var buildOutputNode = GetValue (BuildOutputNodeField);
			var isSelected = Selected;

			var status = GetViewStatus (buildOutputNode);

			//Draw the node background
			FillCellBackground (ctx, buildOutputNode, status);

			// If the height required by the text is not the same as what was calculated in OnGetRequiredSize(), it means that
			// the required height has changed and CalcLayout will return false. In that case call QueueResize(),
			// so that OnGetRequiredSize() is called again and the row is properly resized.
			if (!status.CalculateLayout (cellArea))
				QueueResize ();

			status.LastRenderBounds = cellArea;

			var layout = status.layout;
			var layoutBounds = status.LastRenderLayoutBounds;

			//Draw the image row
			if (status.HasIcon) {
				//Draw the image row
				DrawImage (ctx, cellArea, status.Icon, status.LastRenderImageBounds.X, ImageSize, isSelected, status.IconYPadding);
			}

			ctx.SetColor (Styles.GetTextColor (buildOutputNode, UseStrongSelectionColor && isSelected));

			HighlightSearchResults (layout, contextProvider.SearchString, Styles.GetTextColor (buildOutputNode, false), Styles.GetSearchMatchBackgroundColor (isSelected));

			// Render the selection
			if (TextSelection?.IsShown (buildOutputNode) ?? false) {
				layout.SetBackground (Styles.CellTextSelectionColorSecundary, TextSelection.Index, TextSelection.Length);
			}

			// Draw the text
			ctx.DrawTextLayout (layout, layoutBounds.X, layoutBounds.Y);

			// Draw right hand expander
			if (!status.LastRenderExpanderBounds.IsEmpty) {
				// Draw the image
				Image icon;
				if (status.Expanded)
					icon = BuildCollapseIcon;
				else
					icon = BuildExpandIcon;
				ctx.DrawImage (icon, status.LastRenderExpanderBounds.X, status.LastRenderExpanderBounds.Y);
			}

			//Information section
			if (!status.IsRootNode) {
				DrawNodeInformation (ctx, cellArea, buildOutputNode, status.LayoutYPadding, isSelected, ImageSize, ImagePadding, status);
			} else if (buildOutputNode.NodeType == BuildOutputNodeType.BuildSummary) {
				// For build summary, display error/warning summary
				var startX = layoutBounds.X + layout.GetSize ().Width + 24;

				status.ErrorsRectangle.X = startX;
				status.ErrorsRectangle.Y = cellArea.Y;
				DrawImage (ctx, cellArea, Resources.ErrorIconSmall, startX, ImageSize, isSelected);

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

				DrawImage (ctx, cellArea, Resources.WarningIconSmall, startX, ImageSize, isSelected);

				var warnings = GettextCatalog.GetString ("{0} warnings", buildOutputNode.WarningCount.ToString ());
				startX += ImageSize + 2;
				layout = DrawText (ctx, cellArea, startX, warnings, status.LayoutYPadding, font: defaultFont);

				size = layout.GetSize ();
				status.WarningsRectangle.Width = size.Width + ImageSize + 2;
				status.WarningsRectangle.Height = size.Height;

				//Right area // based in our DefaultInformationContainerWidth
				startX = cellArea.X + (cellArea.Width - DefaultInformationContainerWidth);

				//Draw of duration based in startX position
				DrawNodeDuration (ctx, cellArea, status.Duration, startX, ImagePadding);

			} else if (buildOutputNode.NodeType == BuildOutputNodeType.Build) {
				var textStartX = layoutBounds.X + layout.GetSize ().Width + 24;
				DrawText (ctx, cellArea, textStartX, GetInformationMessage (buildOutputNode), status.LayoutYPadding, defaultFont, cellArea.Width - textStartX);
			}
		}

		double DrawNodeDuration (Context ctx, Rectangle cellArea, string duration, double textStartX, double padding)
		{
			var size = DrawText (ctx, cellArea, textStartX, duration, padding, defaultFont, DefaultInformationContainerWidth).GetSize ();
			textStartX += size.Width + 10;
			return textStartX;
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

					//TODO: we can do a cache of the text layout and only resize
					//Our link text layoud needs to be created with real size
					var layout = CreateTextLayout (cellArea, text, defaultFont, trimming: TextTrimming.WordElipsis, underline: true);
					status.TaskLinkRenderRectangle.Size = layout.GetSize ();

					//Now we calculate if fits the content and readjust
					var maxSize = cellArea.Width + cellArea.X + padding - status.TaskLinkRenderRectangle.X;
					status.TaskLinkRenderRectangle.Width = layout.Width = maxSize;

					DrawText (ctx, layout, cellArea, status.TaskLinkRenderRectangle.X, padding);
					return;
				}
				return;
			}

			UpdateInformationTextColor (ctx, isSelected);

			//Right area // based in our DefaultInformationContainerWidth
			var textStartX = cellArea.X + (cellArea.Width - DefaultInformationContainerWidth);

			//Duration text
			var duration = status.Duration;
			if (duration != "") {
				textStartX = DrawNodeDuration (ctx, cellArea, duration, textStartX, padding);
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
					DrawImage (ctx, cellArea, Resources.ErrorIconSmall, textStartX, imageSize, isSelected);
					textStartX += ImageSize + 2;
					var errors = buildOutputNode.ErrorCount.ToString ();

					var layout = DrawText (ctx, cellArea, textStartX, errors, padding, defaultFont, trimming: TextTrimming.Word);
					textStartX += layout.GetSize ().Width;
				}

				if (buildOutputNode.WarningCount > 0) {
					DrawImage (ctx, cellArea, Resources.WarningIconSmall, textStartX, imageSize, isSelected);
					textStartX += ImageSize + 2;
					DrawText (ctx, cellArea, textStartX, buildOutputNode.WarningCount.ToString (), padding, defaultFont, 10, trimming: TextTrimming.Word);
				}
			}
		}

		static TextLayout CreateTextLayout (Xwt.Rectangle cellArea, string text, Font font, TextTrimming trimming = TextTrimming.WordElipsis, bool underline = false, double width = 0) 
		{
			var descriptionTextLayout = new TextLayout {
				Font = font,
				Text = text,
				Trimming = trimming
			};

			if (underline) {
				descriptionTextLayout.SetUnderline (0, text.Length);
			}

			if (width != 0) {
				descriptionTextLayout.Width = width;
			}

			descriptionTextLayout.Height = cellArea.Height;

			return descriptionTextLayout;
		}

		static TextLayout DrawText (Context ctx, Xwt.Rectangle cellArea, double x, string text, double padding, Font font, double width = 0, TextTrimming trimming = TextTrimming.WordElipsis, bool underline = false) 
		{
			if (width < 0) {
				throw new Exception ("width cannot be negative");
			}

			var textLayout = CreateTextLayout (cellArea, text, font, trimming, underline);
			DrawText (ctx, textLayout, cellArea, x, padding);
			return textLayout;
		}

		static void DrawText (Context ctx, TextLayout textLayout, Xwt.Rectangle cellArea, double x, double padding)
		{
			ctx.DrawTextLayout (textLayout, x, cellArea.Y + padding);
		}

		void DrawImage (Context ctx, Xwt.Rectangle cellArea, Image image, double x, int imageSize, bool isSelected)
		{
			DrawImage (ctx, cellArea, image, x, imageSize, isSelected, (cellArea.Height / 2 - imageSize / 2));
		}

		void DrawImage (Context ctx, Xwt.Rectangle cellArea, Image image, double x, int imageSize, bool isSelected, double topPadding)
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

			var layout = status.GetUnconstrainedLayout ();
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
				status.Initialize (contextProvider.IsShowingDiagnostics);
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

			var containsClickableElement = status.TaskLinkRenderRectangle.Contains (args.Position) || status.ErrorsRectangle.Contains (args.Position) || status.WarningsRectangle.Contains (args.Position);
			if (containsClickableElement) {
				ParentWidget.Cursor = CursorType.Hand;
			} else {
				ParentWidget.Cursor = CursorType.Arrow;
			}

			status.CalculateLayout (status.LastRenderBounds);

			var insideText = status.LastRenderLayoutBounds.Contains (args.Position);
			if ((TextSelection?.IsStarting (node) ?? false)) {
				var pos = status.layout.GetIndexFromCoordinates (args.Position.X - status.LastRenderLayoutBounds.X, args.Position.Y - status.LastRenderLayoutBounds.Y);
				if (pos != -1) {
					TextSelection.Set (pos, args.Position);
					QueueDraw ();
				}
			} else if (insideText && !containsClickableElement)  {
				ParentWidget.Cursor = CursorType.IBeam;
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

			status.CalculateLayout (status.LastRenderBounds);

			if (status.LastRenderExpanderBounds != Rectangle.Zero && status.LastRenderExpanderBounds.Contains (args.Position)) {
				if (DateTime.Now.Subtract (lastExpanderClick).TotalMilliseconds < DefaultExpandClickDelay) {
					return;
				}
				status.Expanded = !status.Expanded;
				lastExpanderClick = DateTime.Now;
				QueueResize ();
				return;
			}

			if (args.Button == PointerButton.Left && status.LastRenderLayoutBounds.Contains (args.Position)) {
				var pos = status.layout.GetIndexFromCoordinates (args.Position.X - status.LastRenderLayoutBounds.X, args.Position.Y - status.LastRenderLayoutBounds.Y);
				if (pos != -1) {
					if (TextSelection == null) {
						TextSelection = new TextSelection<BuildOutputNode> ();
					}
					TextSelection.Start (args.Position, pos, node);
				} else {
					TextSelection = null;
				}

				QueueDraw ();
			}

			//HACK: to avoid automatic scroll behaviour in Gtk (we handle the behaviour)
			//we only want break the normal click behaviour of treeview, in cases when label size is bigger than tree height
			var treeView = ((TreeView)ParentWidget);
			if (status.Expanded && status.LastRenderBounds.Height > treeView.Size.Height) {
				args.Handled = true;
				treeView.SelectRow (node);
			}
			base.OnButtonPressed (args);
		}

		protected override void OnButtonReleased (ButtonEventArgs args)
		{
			if (TextSelection != null) {
				if (TextSelection.State == TextSelectionState.Selecting) {
					TextSelection.Stop ();
					QueueDraw ();
				} else if (TextSelection.State == TextSelectionState.Clicked) {
					ClearSelection ();
				}
			}
			base.OnButtonReleased (args);
		}

		public void ClearSelection ()
		{
			TextSelection = null;
			QueueDraw ();
		}

		protected override void OnMouseExited ()
		{
			ParentWidget.Cursor = CursorType.Arrow;
			base.OnMouseExited ();
		}

		#endregion
	}
}
