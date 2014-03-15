import sublime_plugin

import os


class RunFsScriptCommand(sublime_plugin.WindowCommand):
    """
    Executes .fsscript and .fsi files and prints their output.

    This command is meant to be used as a `target` in a .sublime-build file.
    """
    def run(self, path_to_script, **kwargs):
        """
        @path_to_script
          Full path to a .fsx or .fsscript file.
        """

        path_to_fsi = locate_fsi()
        if not path_to_fsi:
            exe_name = 'fsharpi' if os.name != 'nt' else 'fsi.exe'
            print("FSharp: Cannot locate {0}".format(exe_name))
            return

        # FIXME: We should obtain this from the .sublime-build file via
        #        $file, but I can't get it to work.
        path_to_script = self.window.active_view().file_name()
        # Forward parameters to built-in `exec` command.
        self.window.run_command('exec', {"cmd": [path_to_fsi,
                                                 path_to_script]})


def locate_fsi():
    """
    Returns the path to fsi.exe, or `fsharpi`.
    """
    if os.name != 'nt':
        return 'fsharpi'

    # TODO: Obtain latest version? Use registry?
    # Windows
    usual_path = 'Microsoft F#\\v4.0'
    sdk_path =  'Microsoft SDKs\\F#\\3.1\\Framework\\v4.0'

    is64bit = 'PROGRAMFILES(X86)' in os.environ
    head = (os.environ['PROGRAMFILES(X86)'] if is64bit
                                            else os.environ['PROGRAMFILES'])

    tail = 'fsi.exe'

    paths = [os.path.join(head, usual_path, tail),
             os.path.join(head, sdk_path, tail)]

    for path in paths:
        if os.path.exists(path):
            return path
