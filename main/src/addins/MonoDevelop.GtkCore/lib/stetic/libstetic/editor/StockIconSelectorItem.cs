
using System;
using Mono.Unix;

namespace Stetic.Editor
{
	public class StockIconSelectorItem: IconSelectorItem
	{
		public StockIconSelectorItem (IntPtr ptr): base (ptr)
		{
		}
		
		public StockIconSelectorItem (): base (Catalog.GetString ("Stock Icons"))
		{
		}
		
		protected override void CreateIcons ()
		{
			foreach (string s in StockIconHelper.StockIcons) {
				if (s != "-" && s != "|") {
					Gdk.Pixbuf pix = WidgetUtils.LoadIcon (s, Gtk.IconSize.Menu);
					if (pix != null) AddIcon (s, pix, s);
				}
				else
					AddSeparator (s);
			}
		}
	}
	
	class StockIconHelper
	{
		public static string[] StockIcons = {
			// Commands
			"gtk-new",
			"gtk-open",
			"gtk-save",
			"gtk-save-as",
			"gtk-revert-to-saved",
			"gtk-quit",
			"gtk-print",
			"gtk-print-preview",
			"gtk-properties",
			"|",
			"gtk-cut",
			"gtk-copy",
			"gtk-paste",
			"gtk-delete",
			"gtk-undelete",
			"gtk-undo",
			"gtk-redo",
			"gtk-preferences",
			"|",
			"gtk-execute",
			"gtk-stop",
			"gtk-refresh",
			"gtk-find",
			"gtk-find-and-replace",
			"|",
			"gtk-spell-check",
			"gtk-convert",
			"gtk-help",
			"|",
			"gtk-add",
			"gtk-remove",
			"gtk-clear",
			"-",

			// Formatting
			"gtk-bold",
			"gtk-italic",
			"gtk-underline",
			"gtk-strikethrough",
			"gtk-select-color",
			"gtk-select-font",
			"|",
			"gtk-indent",
			"gtk-unindent",
			"gtk-justify-center",
			"gtk-justify-fill",
			"gtk-justify-left",
			"gtk-justify-right",
			"|",
			"gtk-sort-ascending",
			"gtk-sort-descending",
			"|",
			"gtk-zoom-100",
			"gtk-zoom-fit",
			"gtk-zoom-in",
			"gtk-zoom-out",
			"-",


			// Dialog
			"gtk-yes",
			"gtk-no",
			"gtk-cancel",
			"gtk-ok",
			"gtk-apply",
			"gtk-close",
			"|",
			"gtk-dialog-error",
			"gtk-dialog-info",
			"gtk-dialog-question",
			"gtk-dialog-warning",
			"-",

			// Navigation
			"gtk-goto-bottom",
			"gtk-goto-first",
			"gtk-goto-last",
			"gtk-goto-top",
			"|",
			"gtk-go-back",
			"gtk-go-down",
			"gtk-go-forward",
			"gtk-go-up",
			"|",
			"gtk-home",
			"gtk-jump-to",
			"-",

			// Misc
			"gtk-cdrom",
			"gtk-floppy",
			"gtk-harddisk",
			"gtk-network",
			"gtk-color-picker",
			"gtk-dnd",
			"gtk-dnd-multiple",
			"gtk-missing-image",
			"gtk-index"
			};
	}
}
