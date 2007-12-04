using System;
using System.Drawing;
using System.Text;

using MonoDevelop.EditorBindings.FormattingStrategy;
using MonoDevelop.EditorBindings.Properties;

namespace MonoDevelop.TextEditor.Document
{
	public interface ITextEditorProperties
	{
		bool AutoInsertCurlyBracket { // is wrapped in text editor control
			get;
			set;
		}
		
		bool HideMouseCursor { // is wrapped in text editor control
			get;
			set;
		}
		
		bool IsIconBarVisible { // is wrapped in text editor control
			get;
			set;
		}
		
		bool AllowCaretBeyondEOL {
			get;
			set;
		}
		
		bool ShowMatchingBracket { // is wrapped in text editor control
			get;
			set;
		}
		
		bool UseAntiAliasedFont { // is wrapped in text editor control
			get;
			set;
		}
		
		bool MouseWheelScrollDown {
			get;
			set;
		}
		
		string LineTerminator {
			get;
			set;
		}
		
		bool CreateBackupCopy { // is wrapped in text editor control
			get;
			set;
		}
		
		LineViewerStyle LineViewerStyle { // is wrapped in text editor control
			get;
			set;
		}
		
		bool ShowInvalidLines { // is wrapped in text editor control
			get;
			set;
		}
		
		int VerticalRulerRow { // is wrapped in text editor control
			get;
			set;
		}
		
		bool ShowSpaces { // is wrapped in text editor control
			get;
			set;
		}
		
		bool ShowTabs { // is wrapped in text editor control
			get;
			set;
		}
		
		bool ShowEOLMarker { // is wrapped in text editor control
			get;
			set;
		}
		
		bool ConvertTabsToSpaces { // is wrapped in text editor control
			get;
			set;
		}
		
		bool ShowHorizontalRuler { // is wrapped in text editor control
			get;
			set;
		}
		
		bool ShowVerticalRuler { // is wrapped in text editor control
			get;
			set;
		}
		
		Encoding Encoding {
			get;
			set;
		}
		
		bool EnableFolding { // is wrapped in text editor control
			get;
			set;
		}
		
		bool ShowLineNumbers { // is wrapped in text editor control
			get;
			set;
		}
		
		int TabIndent { // is wrapped in text editor control
			get;
			set;
		}
		
		IndentStyle IndentStyle { // is wrapped in text editor control
			get;
			set;
		}
		
		DocumentSelectionMode DocumentSelectionMode {
			get;
			set;
		}
		
		Pango.FontDescription Font { // is wrapped in text editor control
			get;
			set;
		}
	}
}
