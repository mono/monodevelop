
using System;
using Cairo;
using Mono.TextEditor;
using MonoDevelop.Components;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Highlighting;
using MonoDevelop.Ide;

namespace MonoDevelop.SourceEditor
{
	enum SmartTagSeverity
	{
		OnlyActions,
		Fixes,
		ErrorFixes
	}

	class SmartTagMarginMarker : MarginMarker, ITextLineMarker
	{
		public IDocumentLine Line => base.LineSegment;

		public event EventHandler ShowPopup;
		public event EventHandler CancelPopup;

		public SmartTagSeverity SmartTagSeverity { get; set; }

		public Xwt.Point PopupPosition {
			get;
			private set;
		}

		public int Height {
			get;
			private set;
		}


		public override bool CanDrawForeground (Margin margin)
		{
			return margin is QuickFixMargin;
		}

		public override void DrawForeground (MonoTextEditor editor, Context cr, MarginDrawMetrics metrics)
		{
			double size = metrics.Margin.Width;
			double borderLineWidth = cr.LineWidth;

			double x = Math.Floor (metrics.Margin.XOffset - borderLineWidth / 2);
			double y = Math.Floor (metrics.Y + (metrics.Height - size) / 2);
			var icon = GetIcon (SmartTagSeverity);
			var deltaX = size / 2 - icon.Width / 2 + 0.5f;
			var deltaY = size / 2 - icon.Height / 2 + 0.5f;
			cr.DrawImage (editor, icon, Math.Round (x + deltaX), Math.Round (y + deltaY));
			Height = (int)(Math.Round (deltaY) + icon.Height);
			if (editor.TextArea.QuickFixMargin.HoveredSmartTagMarker == this) {
				const int triangleWidth = 8;
				const int triangleHeight = 4;

				cr.SetSourceColor (SyntaxHighlightingService.GetColor (editor.EditorTheme, EditorThemeColors.LineNumbers));
				cr.MoveTo (metrics.X + metrics.Width - triangleWidth, metrics.Y + metrics.Height - triangleHeight);
				cr.LineTo (metrics.X + metrics.Width, metrics.Y + metrics.Height - triangleHeight);
				cr.LineTo (metrics.X + metrics.Width - triangleWidth / 2, metrics.Y + metrics.Height);
				cr.LineTo (metrics.X + metrics.Width - triangleWidth, metrics.Y + metrics.Height - triangleHeight);
				cr.Fill ();
			}
			PopupPosition = new Xwt.Point (metrics.X, metrics.Y);
		}

		public static Xwt.Drawing.Image GetIcon (SmartTagSeverity severity)
		{
			switch (severity) {
			case SmartTagSeverity.Fixes:
				return ImageService.GetIcon ("md-lightbulb", Gtk.IconSize.Menu);
			case SmartTagSeverity.ErrorFixes:
				return ImageService.GetIcon ("md-lightbulb-error", Gtk.IconSize.Menu);
			default:
				return ImageService.GetIcon ("md-lightbulb-screwdriver", Gtk.IconSize.Menu);
			}
		}

		public override void InformMousePress (MonoTextEditor editor, Margin margin, MarginMouseEventArgs args)
		{
			if (!(margin is QuickFixMargin))
				return;
			ShowPopup?.Invoke (this, EventArgs.Empty);
		}

		public override void InformMouseHover (MonoTextEditor editor, Margin margin, MarginMouseEventArgs args)
		{
			editor.TextArea.QuickFixMargin.HoveredSmartTagMarker = this;
		}
	}
}