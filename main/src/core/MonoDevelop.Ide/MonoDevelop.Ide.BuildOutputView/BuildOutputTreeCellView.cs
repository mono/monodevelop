using System;
using System.Collections.Generic;
using MonoDevelop.Ide.Gui;
using Xwt;
using Xwt.Drawing;

namespace MonoDevelop.Ide.BuildOutputView
{
	static class Styles
	{
		public static Xwt.Drawing.Color CellBackgroundColor { get; internal set; }
		public static Xwt.Drawing.Color CellSelectionColor { get; internal set; }
		public static Xwt.Drawing.Color CellStrongSelectionColor { get; internal set; }
		public static Xwt.Drawing.Color CellTextColor { get; internal set; }
		public static Xwt.Drawing.Color CellTextSelectionColor { get; internal set; }

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
		const int FontSize = 11;
		const int DescriptionPaddingHeight = 0;
		const int LinesDisplayedCount = 1;
		const int DefaultIformationContainerWidth = 400;
		const int ImageSide = 20;
		const int ImageLeftPadding = 2;

		public double CellWidth { get; set; }

		public Color BackgroundColor { get; set; }
		public Color StrongSelectionColor { get; set; }
		public Color SelectionColor { get; set; }

		public bool UseStrongSelectionColor { get; set; }

		public IDataField<bool> HasBackgroundColorField { get; set; }

		WidgetSpacing packageDescriptionPadding = new WidgetSpacing (5, 5, 5, 10);
		WidgetSpacing packageImagePadding = new WidgetSpacing (0, 0, 0, 5);
		WidgetSpacing checkBoxPadding = new WidgetSpacing (10, 0, 0, 10);

		public IDataField<BuildOutputNode> BuildOutputNodeField { get; set; }

		BuildOutputNode buildOutputNode;
		Font defaultFontLayout;
		Size fontRequiredSize;
		int informationContainerWidth;
		int imageX => ImageSide + ImageLeftPadding + 5;
	
		bool IsFirstNode () => buildOutputNode.Parent == null;

		public BuildOutputTreeCellView ()
		{
			BackgroundColor = Styles.CellBackgroundColor;
			StrongSelectionColor = Styles.CellStrongSelectionColor;
			SelectionColor = Styles.CellSelectionColor;
			UseStrongSelectionColor = true;
			informationContainerWidth = DefaultIformationContainerWidth;
		}

		protected override void OnDraw(Context ctx, Xwt.Rectangle cellArea)
		{
			FillCellBackground (ctx);

			DrawImageRow (ctx, cellArea);
			DrawNodeText (ctx, cellArea);

			if (!IsFirstNode ()) {
				DrawNodeInformation (ctx, cellArea);
			}
		}

		void DrawNodeInformation (Context ctx, Xwt.Rectangle cellArea)
		{
			if (!buildOutputNode.HasChildren)
				return;

			var duration = buildOutputNode.GetDurationAsString ();
			if (duration != "") {
				
				UpdateInformationTextColor (ctx);

				var textStartX = BackgroundBounds.Width - informationContainerWidth;
				DrawText (ctx, cellArea, textStartX, informationContainerWidth, duration);
			}
		}

		void DrawText (Context ctx, Xwt.Rectangle cellArea, double x, double width, string text, Font font = null) 
		{
			if (Math.Max (width, 0) == 0) {
				return;
			}

			var descriptionTextLayout = new TextLayout ();
			descriptionTextLayout.Width = width;
			descriptionTextLayout.Height = cellArea.Height;
			descriptionTextLayout.Trimming = TextTrimming.WordElipsis;

			if (font == null) {
				descriptionTextLayout.Font = defaultFontLayout.WithWeight (FontWeight.Light);
			} else {
				descriptionTextLayout.Font = font;
			}

			descriptionTextLayout.Text = text;

			ctx.DrawTextLayout (descriptionTextLayout, x, cellArea.Top + ((cellArea.Height - fontRequiredSize.Height) * .5));
		}

		void DrawNodeText (Context ctx, Xwt.Rectangle cellArea)
		{
			UpdateTextColor (ctx);

			Font font;
			if (IsFirstNode ()) {
				font = defaultFontLayout.WithWeight (FontWeight.Bold);
			} else {
				font = defaultFontLayout.WithWeight (FontWeight.Light);
			}

			var startX = cellArea.Left + imageX;
			var width = BackgroundBounds.Width - informationContainerWidth - startX;

			DrawText (ctx, cellArea, startX, width, buildOutputNode.Message, font);
		}

		void DrawImageRow (Context ctx, Xwt.Rectangle cellArea)
		{
			var image = buildOutputNode.GetImage ().WithSize (ImageSide);
			DrawImage (ctx, cellArea, image, cellArea.Left + ImageLeftPadding);
		}

		void DrawImage (Context ctx, Xwt.Rectangle cellArea, Image image, double x)
		{
			ctx.DrawImage (
				Selected ? image.WithStyles ("sel") : image,
				x,
				cellArea.Top - (ImageSide - cellArea.Height) * .5,
				ImageSide,
				ImageSide);
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

		protected override Size OnGetRequiredSize ()
		{
			var layout = new TextLayout ();
			layout.Text = "W";
			layout.Font = layout.Font.WithSize (FontSize);
			defaultFontLayout = layout.Font;
			fontRequiredSize = layout.GetSize ();
			return new Size (CellWidth, fontRequiredSize.Height * LinesDisplayedCount + DescriptionPaddingHeight + 
			                 (buildOutputNode.NodeType == BuildOutputNodeType.Build ? 12 : 3));
		}

		protected override void OnDataChanged()
		{
			base.OnDataChanged();
			buildOutputNode = GetValue (BuildOutputNodeField);
		}

		Color GetSelectedColor ()
		{
			if (UseStrongSelectionColor) {
				return StrongSelectionColor;
			}
			return SelectionColor;
		}

		void UpdateTextColor (Context ctx)
		{
			if (UseStrongSelectionColor && Selected) {
				ctx.SetColor (Styles.CellTextSelectionColor);
			} else {
				ctx.SetColor (Styles.CellTextColor);
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
