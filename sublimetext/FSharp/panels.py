import sublime
import sublime_plugin

from FSharp.lib import fs


_menu_items = {
    'F#: Set as Project File':  ('fs_set_project_file'),
    'F#: Go to Declaration':    ('fs_find_declaration'),
    'F#: Get Tooltip':          ('fs_get_tooltip'),
    'F#: List Declarations':    ('fs_declarations')
}


CANCEL = -1


class FsShowMainMenu(sublime_plugin.WindowCommand):
    """
    Shows the main menu for F# commands.
    """
    ITEMS = list(sorted(_menu_items.keys()))

    def is_enabled(self):
        #Enable only for F# files.
        fname = self.window.active_view().file_name()
        if fname:
            return fs.is_fsharp_file(fname)
        return False

    def run(self):
        self.window.show_quick_panel(self.ITEMS, self.on_done)

    def on_done(self, idx):
        if idx == CANCEL:
            return

        selected_item = self.ITEMS[idx]
        self.window.run_command(_menu_items[selected_item])
