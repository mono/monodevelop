import sublime
import sublime_plugin


main_items = {
    'F#: Set as Project File':  ('fs_set_project_file'),
    'F#: Go to Declaration':    ('fs_find_declaration'),
    'F#: Get Completions':      ('fs_find_completions'),
    'F#: List Declarations':    ('fs_declarations')
}


CANCEL_SELECTION = -1


class FsShowMainMenu(sublime_plugin.WindowCommand):
    """
    Shows the main menu for F# commands.
    """
    ITEMS = list(sorted(main_items.keys()))

    def is_enabled(self):
        #Enable only for F# files.
        fname = self.window.active_view().file_name()
        if fname:
            return fname.endswith(('.fsproj', '.fs', '.fsx', '.fsi'))


    def run(self):
        self.window.show_quick_panel(self.ITEMS, self.on_done)

    def on_done(self, idx):
        if idx == CANCEL_SELECTION:
            return

        selected_item = self.ITEMS[idx]
        self.window.run_command(main_items[selected_item])
