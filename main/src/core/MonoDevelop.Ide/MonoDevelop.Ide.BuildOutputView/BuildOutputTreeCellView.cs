using System;
using System.Collections.Generic;
using MonoDevelop.Ide.Gui;
using Xwt;
using Xwt.Drawing;

namespace MonoDevelop.Ide.BuildOutputView
{
	internal static class Styles
	{
		public static Xwt.Drawing.Color LineBorderColor { get; internal set; }
		public static Xwt.Drawing.Color BackgroundColor { get; internal set; }
		public static Xwt.Drawing.Color PackageInfoBackgroundColor { get; internal set; }
		public static Xwt.Drawing.Color CellBackgroundColor { get; internal set; }
		public static Xwt.Drawing.Color CellSelectionColor { get; internal set; }
		public static Xwt.Drawing.Color CellStrongSelectionColor { get; internal set; }
		public static Xwt.Drawing.Color CellTextColor { get; internal set; }
		public static Xwt.Drawing.Color CellTextSelectionColor { get; internal set; }
		public static Xwt.Drawing.Color PackageSourceUrlTextColor { get; internal set; }
		public static Xwt.Drawing.Color PackageSourceUrlSelectedTextColor { get; internal set; }
		public static Xwt.Drawing.Color PackageSourceErrorTextColor { get; internal set; }
		public static Xwt.Drawing.Color PackageSourceErrorSelectedTextColor { get; internal set; }
		public static Xwt.Drawing.Color ErrorBackgroundColor { get; internal set; }
		public static Xwt.Drawing.Color ErrorForegroundColor { get; internal set; }

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

			BackgroundColor = Ide.Gui.Styles.PrimaryBackgroundColor;

			CellTextColor = Ide.Gui.Styles.BaseForegroundColor;
			CellStrongSelectionColor = Ide.Gui.Styles.BaseSelectionBackgroundColor;
			CellSelectionColor = Ide.Gui.Styles.BaseSelectionBackgroundColor;
			CellTextSelectionColor = Ide.Gui.Styles.BaseSelectionTextColor;

			PackageInfoBackgroundColor = Ide.Gui.Styles.SecondaryBackgroundLighterColor;
			PackageSourceErrorTextColor = Ide.Gui.Styles.ErrorForegroundColor;
			PackageSourceUrlTextColor = Ide.Gui.Styles.SecondaryTextColor;

			PackageSourceErrorSelectedTextColor = PackageSourceErrorTextColor;

			PackageSourceUrlSelectedTextColor = Xwt.Drawing.Color.FromName ("#ffffff");

			LineBorderColor = Ide.Gui.Styles.SeparatorColor;

			ErrorBackgroundColor = Ide.Gui.Styles.StatusWarningBackgroundColor;
			ErrorForegroundColor = Ide.Gui.Styles.StatusWarningTextColor;
		}
	}


	class BuildOutputTreeCellView : CanvasCellView
	{
		
		public double CellWidth { get; set; }

		public Color BackgroundColor { get; set; }
		public Color StrongSelectionColor { get; set; }
		public Color SelectionColor { get; set; }

		public bool UseStrongSelectionColor { get; set; }

		public IDataField<bool> HasBackgroundColorField { get; set; }

		public BuildOutputTreeCellView () 
		{
			BackgroundColor = Styles.CellBackgroundColor;
			StrongSelectionColor = Styles.CellStrongSelectionColor;
			SelectionColor = Styles.CellSelectionColor;
			UseStrongSelectionColor = true;
		}

		int fontSize = 13;

		const int descriptionPaddingHeight = 5;
		const int linesDisplayedCount = 1;

		//Size maxBuildOutputImageSize = new Size (48, 48);
		WidgetSpacing packageDescriptionPadding = new WidgetSpacing (5, 5, 5, 10);
		WidgetSpacing packageImagePadding = new WidgetSpacing (0, 0, 0, 5);
		WidgetSpacing checkBoxPadding = new WidgetSpacing (10, 0, 0, 10);

		FilteredBuildOutputNode buildOutputNode;

		Size fontRequiredSize;

		int imageSide;
		const int imagePadding = 2;
		int imageX => imageSide + imagePadding + 5;

		protected override void OnDraw(Context ctx, Xwt.Rectangle cellArea)
		{
			FillCellBackground (ctx);
			UpdateTextColor (ctx);

			imageSide = (int)cellArea.Height - (2 * imagePadding);

			//if (Selected)
			//image = image.WithStyles ("sel");

			ctx.DrawImage (
				buildOutputNode.GetImage (),
				cellArea.Left + imagePadding,
				cellArea.Top + imagePadding,
				imageSide,
				imageSide);

			// Package description.
			var descriptionTextLayout = new TextLayout ();
			descriptionTextLayout.Font = descriptionTextLayout.Font.WithSize (fontSize);
			descriptionTextLayout.Width = cellArea.Width - imageX;
			descriptionTextLayout.Height = cellArea.Height;
			descriptionTextLayout.Text = buildOutputNode.Message;

			ctx.DrawTextLayout (
				descriptionTextLayout,
				cellArea.Left + imageX,
				cellArea.Top + ((cellArea.Height - fontRequiredSize.Height)*.5));
		}

		protected override Size OnGetRequiredSize ()
		{
			var layout = new TextLayout ();
			layout.Text = "W";
			layout.Font = layout.Font.WithSize (fontSize);
			fontRequiredSize = layout.GetSize ();
			return new Size (CellWidth, fontRequiredSize.Height * linesDisplayedCount + descriptionPaddingHeight);
		}

		protected override void OnDataChanged()
		{
			base.OnDataChanged();
			var backEnd = (Xwt.GtkBackend.CellViewBackend) this.BackendHost.Backend;
			buildOutputNode = (FilteredBuildOutputNode) backEnd.TreeModel.GetValue (backEnd.CurrentIter, 0);
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
