using System;
using Mono.TextEditor;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.FindInFiles;
using Cairo;
using MonoDevelop.Components;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide.Editor.Highlighting;

namespace MonoDevelop.SourceEditor
{
	class UsageSegmentMarker : TextSegmentMarker, ITextSegmentMarker
	{
		readonly Usage usage;

		public Usage Usage {
			get {
				return usage;
			}
		}

		public UsageSegmentMarker (Usage usage) : base (usage.Offset, usage.Length)
		{
			this.usage = usage;
		}

		event EventHandler<TextMarkerMouseEventArgs> ITextSegmentMarker.MousePressed {
			add {
			}
			remove {
			}
		}

		event EventHandler<TextMarkerMouseEventArgs> ITextSegmentMarker.MouseHover {
			add {
			}
			remove {
			}
		}

		object ITextSegmentMarker.Tag {
			get;
			set;
		}

		public override void DrawBackground (Mono.TextEditor.MonoTextEditor editor, Context cr, LineMetrics metrics, int startOffset, int endOffset)
		{
			int markerStart = usage.Offset;
			int markerEnd = usage.EndOffset;

			if (markerEnd < startOffset || markerStart > endOffset) 
				return; 

			double @from;
			double to;
			var startXPos = metrics.TextRenderStartPosition;
			var endXPos = metrics.TextRenderEndPosition;
			var y = metrics.LineYRenderStartPosition;
			if (markerStart < startOffset && endOffset < markerEnd) {
				@from = startXPos;
				to = endXPos;
			} else {
				int start = startOffset < markerStart ? markerStart : startOffset;
				int end = endOffset < markerEnd ? endOffset : markerEnd;

				uint curIndex = 0, byteIndex = 0;
				TextViewMargin.TranslateToUTF8Index (metrics.Layout.LineChars, (uint)(start - startOffset), ref curIndex, ref byteIndex);

				int x_pos = metrics.Layout.IndexToPos ((int)byteIndex).X;

				@from = startXPos + (int)(x_pos / Pango.Scale.PangoScale);

				TextViewMargin.TranslateToUTF8Index (metrics.Layout.LineChars, (uint)(end - startOffset), ref curIndex, ref byteIndex);
				x_pos = metrics.Layout.IndexToPos ((int)byteIndex).X;

				to = startXPos + (int)(x_pos / Pango.Scale.PangoScale);
			}

			@from = Math.Max (@from, editor.TextViewMargin.XOffset);
			to = Math.Max (to, editor.TextViewMargin.XOffset);
			if (@from < to) {
				HslColor colorStyle;
				if ((usage.UsageType & ReferenceUsageType.Write) == ReferenceUsageType.Write ||
					(usage.UsageType & ReferenceUsageType.Declariton) == ReferenceUsageType.Declariton) {
					
					colorStyle = SyntaxHighlightingService.GetColor (editor.EditorTheme, EditorThemeColors.ChangingUsagesRectangle);
					if (colorStyle.Alpha == 0.0)
						colorStyle = SyntaxHighlightingService.GetColor (editor.EditorTheme, EditorThemeColors.UsagesRectangle);
				} else {
					colorStyle = SyntaxHighlightingService.GetColor (editor.EditorTheme, EditorThemeColors.UsagesRectangle);
				}

				using (var lg = new LinearGradient (@from + 1, y + 1.5, to , y + editor.LineHeight - 1)) {
					lg.AddColorStop (0, colorStyle);
					lg.AddColorStop (1, colorStyle);
					cr.SetSource (lg);
					cr.RoundedRectangle (@from - 0.5, y + 0.5, to - @from + 1, editor.LineHeight - 1, 2);
					cr.FillPreserve ();
				}
				// TODO: EditorTheme : do we need the border ?!
				cr.SetSourceColor (colorStyle);
				cr.Stroke ();
			}
		}
	}
}