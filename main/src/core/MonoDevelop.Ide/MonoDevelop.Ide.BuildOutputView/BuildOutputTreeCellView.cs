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

		const int FontSize = 11;
		const int DescriptionPaddingHeight = 0;
		const int LinesDisplayedCount = 1;

		WidgetSpacing packageDescriptionPadding = new WidgetSpacing (5, 5, 5, 10);
		WidgetSpacing packageImagePadding = new WidgetSpacing (0, 0, 0, 5);
		WidgetSpacing checkBoxPadding = new WidgetSpacing (10, 0, 0, 10);

		FilteredBuildOutputNode buildOutputNode;

		Size fontRequiredSize;
		const int InformationRightDistance = 400;
		const int ImageSide = 20;
		const int ImageLeftPadding = 2;
		int imageX => ImageSide + ImageLeftPadding + 5;

		bool IsFirstNode () => buildOutputNode.Parent == null;

		protected override void OnDraw(Context ctx, Xwt.Rectangle cellArea)
		{
			FillCellBackground (ctx);

			DrawImage (ctx, cellArea);
			DrawNodeText (ctx, cellArea);

			if (IsFirstNode ()) {
				DrawBuildInformation (ctx, cellArea);
			} else {
				DrawNodeInformation (ctx, cellArea);
			}
		}

		void DrawNodeInformation (Context ctx, Xwt.Rectangle cellArea)
		{
			if (!buildOutputNode.HasChildren)
				return;

			UpdateInformationTextColor (ctx);

			var descriptionTextLayout = new TextLayout ();
			descriptionTextLayout.Width = InformationRightDistance;
			descriptionTextLayout.Height = cellArea.Height;
			descriptionTextLayout.Font = descriptionTextLayout.Font
					.WithSize (FontSize)
					.WithWeight (FontWeight.Light);

			descriptionTextLayout.Text = buildOutputNode.GetDurationAsString ();

			ctx.DrawTextLayout (
				descriptionTextLayout,
				BackgroundBounds.Width - descriptionTextLayout.Width,
				cellArea.Top + ((cellArea.Height - fontRequiredSize.Height) * .5));
		}

		void DrawBuildInformation (Context ctx, Xwt.Rectangle cellArea) 
		{
			UpdateInformationTextColor (ctx);

			var descriptionTextLayout = new TextLayout ();
			descriptionTextLayout.Width = InformationRightDistance;
			descriptionTextLayout.Height = cellArea.Height;
			//TODO: this needs to be changed to a read data
			descriptionTextLayout.Text = "Debug | iPhoneSimulator   Started at 5:04 pm on April 12, 2018";
			descriptionTextLayout.Font = descriptionTextLayout.Font
					.WithSize (FontSize)
					.WithWeight (FontWeight.Light);

			ctx.DrawTextLayout (
				descriptionTextLayout,
				BackgroundBounds.Width - descriptionTextLayout.Width,
				cellArea.Top + ((cellArea.Height - fontRequiredSize.Height) * .5));
		}

		void DrawNodeText (Context ctx, Xwt.Rectangle cellArea)
		{
			UpdateTextColor (ctx);

			// NodeText
			var descriptionTextLayout = new TextLayout ();
			descriptionTextLayout.Width = cellArea.Width - imageX;
			descriptionTextLayout.Height = cellArea.Height;
			descriptionTextLayout.Text = buildOutputNode.Message;
			descriptionTextLayout.Trimming = TextTrimming.Word;

			if (IsFirstNode ()) {
				descriptionTextLayout.Font = descriptionTextLayout.Font
					.WithSize (FontSize)
					.WithWeight (FontWeight.Bold);
			} else {
				descriptionTextLayout.Font = descriptionTextLayout.Font
					.WithSize (FontSize)
					.WithWeight (FontWeight.Light);
			}

			ctx.DrawTextLayout (
				descriptionTextLayout,
				cellArea.Left + imageX,
				cellArea.Top + ((cellArea.Height - fontRequiredSize.Height) * .5));
		}

		void DrawImage (Context ctx, Xwt.Rectangle cellArea)
		{
			var image = buildOutputNode.GetImage ()
									   .WithSize (ImageSide);
			ctx.DrawImage (
				Selected ? image.WithStyles ("sel") : image,
				cellArea.Left + ImageLeftPadding,
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
			fontRequiredSize = layout.GetSize ();
			return new Size (CellWidth, fontRequiredSize.Height * LinesDisplayedCount + DescriptionPaddingHeight + 
			                 (buildOutputNode.NodeType == BuildOutputNodeType.Build ? 12 : 3));
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
