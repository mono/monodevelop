import sublime
import sublime_plugin

from queue import Queue
from threading import Thread
from zipfile import ZipFile
import os

from FSharp.lib import const
from FSharp.lib.fsac import get_server
from FSharp.lib import fs


tasks = Queue()

SIG_STOP = '__STOP__'


def plugin_loaded():
    """
    Initializes plugin.
    """

    # Install binaries if needed.
    if not installation.check_binaries():
        installation.install_binaries()
        print('FSharp: Binaries installed. Everything ok.')
    else:
        print('FSharp: Binaries found. Everything ok.')

    # Start the pipe server.
    AsyncPipe()


def plugin_unloaded():
    tasks.put((SIG_STOP, ()))


class AsyncPipe(object):
    """
    Wraps the fsac server to make it asynchronous.
    """
    def __init__(self):
        self.server = get_server()
        self.tasks = tasks

        writer = Thread(target=self.write, daemon=True)
        reader = Thread(target=self.read, daemon=True)

        writer.start()
        reader.start()

    def write(self):
        while True:
            action, args = self.tasks.get()
            method = getattr(self.server, action, None)

            if not method:
                process_output({'Kind': 'ERROR', 'Data': 'Not a valid call.'})
                continue

            if action == SIG_STOP:
                # Give the other thread a chance to exit.
                self.tasks.put((action, args))
                break

            # Write to server's stdin.
            method(*args)

    def read(self):
        while True:
            data = self.server.read_line()
            process_output(data)

            try:
                # Don't block here so we can read all the remaining output.
                action, args = self.tasks.get(timeout=0.01)
            except:
                continue

            if action == SIG_STOP:
                #  Give the other thread a chance to exit.
                self.tasks.put((action, args))
                break

            self.tasks.put((action, args))


class actions:
    """
    Groups methods that process data received from the autocomplete server.
    """
    @staticmethod
    def generic_action(data=None):
        sublime.status_message("RECEIVED: " + str(data))
        print("RECEIVED: " + str(data))

    @staticmethod
    def show_info(data):
        print(data)

    @staticmethod
    def find_declaration(data):
        data = data['Data']
        fname = data['File']
        row = data['Line'] + 1
        col = data['Column'] + 1
        encoded = "{0}:{1}:{2}".format(fname, row, col)
        sublime.active_window().open_file(encoded, sublime.ENCODED_POSITION)

    @staticmethod
    def declarations(data):
        decls = data['Data']
        print(decls)

    @staticmethod
    def show_completions(data):
        v = sublime.active_window().active_view()
        v.show_popup_menu(data['Data'], None)

    @staticmethod
    def show_tooltip(data):
        v = sublime.active_window().active_view()
        heading = list(data['Data'].keys())[0]
        body = data['Data'][heading]
        v.show_popup_menu([heading, body], None)


def process_output(data):
    action = None
    if data['Kind'] == 'completion':
        # Completions should normally be processed via events.
        # action = actions.show_completions
        pass
    elif data['Kind'] == 'helptext':
        action = actions.show_tooltip
    elif data['Kind'] == 'INFO':
        action = actions.show_info
    elif data['Kind'] == 'finddecl':
        action = actions.find_declaration
    elif data['Kind'] == 'declarations':
        action = actions.declarations
    elif data['Kind'] == 'project':
        for fname in data['Data']:
            tasks.put(('parse', (fname, True)))
    else:
        action = actions.generic_action

    if action:
        # Run action on the main UI thread to make ST happy.
        sublime.set_timeout(lambda: action(data), 0)


class installation:
    @staticmethod
    def check_binaries():
        print('FSharp: Checking installed files')
        return os.path.exists(const.path_to_fs_ac_binary())

    @staticmethod
    def install_binaries():
        print('FSharp: Installing files to Packages/FSharp_Binaries...')
        sublime.status_message('FSharp: Installing files to Packages/FSharp_Binaries...')
        try:
            os.mkdir(const.path_to_fs_binaries())
        except IOError:
            pass

        zipped_bytes = sublime.load_binary_resource('Packages/FSharp/bundled/fsautocomplete.zip')
        target = os.path.join(const.path_to_fs_binaries(), 'fsautocomplete.zip')
        with open(target, 'wb') as f:
            f.write(zipped_bytes)

        with open(target, 'rb') as f:
            ZipFile(f).extractall(path=const.path_to_fs_binaries())
        os.unlink(target)


class FsSetProjectFile(sublime_plugin.WindowCommand):
    def is_enabled(self):
        v = self.window.active_view()
        if v and fs.is_fsharp_project(v.file_name()):
            return True

        msg = 'FSharp: Not a project file.'
        print(msg)
        sublime.status_message(msg)
        return False

    def run(self):
        v = self.window.active_view()
        sublime.status_message('FSharp: Loading project...')
        tasks.put(('project', (v.file_name(),)))


class FsParseFile(sublime_plugin.WindowCommand):
    def is_enabled(self):
        v = self.window.active_view()
        if v and fs.is_fsharp_code(v.file_name()):
            return True

        msg = 'FSharp: Not an F# code file.'
        print(msg)
        sublime.status_message(msg)
        return False

    def run(self):
        v = self.window.active_view()
        tasks.put(('parse', (v.file_name(), True)))


class FsFindDeclaration(sublime_plugin.WindowCommand):
    def is_enabled(self):
        v = self.window.active_view()
        if v and fs.is_fsharp_code(v.file_name()):
            return True

        msg = 'FSharp: Not an F# code file.'
        print(msg)
        sublime.status_message(msg)
        return False

    def run(self):
        v = self.window.active_view()
        row, col = v.rowcol(v.sel()[0].b)
        tasks.put(('parse', (v.file_name(), True)))
        tasks.put(('find_declaration', (v.file_name(), row, col)))


class FsDeclarations(sublime_plugin.WindowCommand):
    def is_enabled(self):
        v = self.window.active_view()
        if v and fs.is_fsharp_code(v.file_name()):
            return True

        msg = 'FSharp: Not an F# code file.'
        print(msg)
        sublime.status_message(msg)
        return False

    def run(self):
        v = self.window.active_view()
        tasks.put(('parse', (v.file_name(), True)))
        tasks.put(('declarations', (v.file_name(),)))


class FsFindCompletions(sublime_plugin.WindowCommand):
    def is_enabled(self):
        v = self.window.active_view()
        if v and fs.is_fsharp_code(v.file_name()):
            return True

        msg = 'FSharp: Not an F# code file.'
        print(msg)
        sublime.status_message(msg)
        return False

    def run(self):
        v = self.window.active_view()
        row, col = v.rowcol(v.sel()[0].b)
        tasks.put(('parse', (v.file_name(), True)))
        tasks.put(('completions', (v.file_name(), row, col)))
