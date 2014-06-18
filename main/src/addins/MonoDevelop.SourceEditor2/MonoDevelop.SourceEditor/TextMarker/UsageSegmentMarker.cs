using System;
using System.Linq;
using Mono.TextEditor;
using Mono.TextEditor.Highlighting;
using MonoDevelop.Ide.Editor;
using Mono.CSharp.Linq;

namespace MonoDevelop.Debugger
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
	}
}

//public class UsageMarker : TextLineMarker
//{
//	List<UsageSegment> usages = new List<UsageSegment> ();
//
//	public List<UsageSegment> Usages {
//		get { return usages; }
//	}
//
//	public bool Contains (int offset)
//	{
//		return usages.Any (u => u.TextSegment.Offset <= offset && offset <= u.TextSegment.EndOffset);
//	}
//
//	public override bool DrawBackground (TextEditor editor, Context cr, double y, LineMetrics metrics)
//	{
//		if (metrics.SelectionStart >= 0 || editor.CurrentMode is TextLinkEditMode || editor.TextViewMargin.SearchResultMatchCount > 0)
//			return false;
//		foreach (var usage in Usages) {
//			int markerStart = usage.TextSegment.Offset;
//			int markerEnd = usage.TextSegment.EndOffset;
//
//			if (markerEnd < metrics.TextStartOffset || markerStart > metrics.TextEndOffset) 
//				return false; 
//
//			double @from;
//			double to;
//
//			if (markerStart < metrics.TextStartOffset && metrics.TextEndOffset < markerEnd) {
//				@from = metrics.TextRenderStartPosition;
//				to = metrics.TextRenderEndPosition;
//			} else {
//				int start = metrics.TextStartOffset < markerStart ? markerStart : metrics.TextStartOffset;
//				int end = metrics.TextEndOffset < markerEnd ? metrics.TextEndOffset : markerEnd;
//
//				uint curIndex = 0, byteIndex = 0;
//				TextViewMargin.TranslateToUTF8Index (metrics.Layout.LineChars, (uint)(start - metrics.TextStartOffset), ref curIndex, ref byteIndex);
//
//				int x_pos = metrics.Layout.Layout.IndexToPos ((int)byteIndex).X;
//
//				@from = metrics.TextRenderStartPosition + (int)(x_pos / Pango.Scale.PangoScale);
//
//				TextViewMargin.TranslateToUTF8Index (metrics.Layout.LineChars, (uint)(end - metrics.TextStartOffset), ref curIndex, ref byteIndex);
//				x_pos = metrics.Layout.Layout.IndexToPos ((int)byteIndex).X;
//
//				to = metrics.TextRenderStartPosition + (int)(x_pos / Pango.Scale.PangoScale);
//			}
//
//			@from = Math.Max (@from, editor.TextViewMargin.XOffset);
//			to = Math.Max (to, editor.TextViewMargin.XOffset);
//			if (@from < to) {
//				Mono.TextEditor.Highlighting.AmbientColor colorStyle;
//				if ((usage.UsageType & ReferenceUsageType.Write) == ReferenceUsageType.Write) {
//					colorStyle = editor.ColorStyle.ChangingUsagesRectangle;
//				} else {
//					colorStyle = editor.ColorStyle.UsagesRectangle;
//				}
//
//				using (var lg = new LinearGradient (@from + 1, y + 1, to , y + editor.LineHeight)) {
//					lg.AddColorStop (0, colorStyle.Color);
//					lg.AddColorStop (1, colorStyle.SecondColor);
//					cr.SetSource (lg);
//					cr.RoundedRectangle (@from + 0.5, y + 1.5, to - @from - 1, editor.LineHeight - 2, editor.LineHeight / 4);
//					cr.FillPreserve ();
//				}
//
//				cr.SetSourceColor (colorStyle.BorderColor);
//				cr.Stroke ();
//			}
//		}
//		return true;
//	}
//}