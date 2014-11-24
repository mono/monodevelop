# Copyright (c) 2014, Guillermo López-Anglada. Please see the AUTHORS file for details.
# All rights reserved. Use of this source code is governed by a BSD-style
# license that can be found in the LICENSE file.)

import sublime
import sublime_plugin

import json
import os
import queue
import logging

from FSharp import editor_context
from FSharp.fsac.request import AdHocRequest
from FSharp.fsac.request import CompletionRequest
from FSharp.fsac.request import DataRequest
from FSharp.fsac.request import DeclarationsRequest
from FSharp.fsac.request import FindDeclRequest
from FSharp.fsac.request import ParseRequest
from FSharp.fsac.request import ProjectRequest
from FSharp.fsac.request import TooltipRequest
from FSharp.fsac.response import CompilerLocationResponse
from FSharp.fsac.response import CompilerLocationResponse
from FSharp.fsac.response import DeclarationsResponse
from FSharp.fsac.response import ErrorInfo
from FSharp.fsac.response import ProjectResponse
from FSharp.lib.project import FSharpFile
from FSharp.lib.project import FSharpFile
from FSharp.lib.response_processor import process_resp
from FSharp.lib.response_processor import add_listener
from FSharp.lib.response_processor import raise_event
from FSharp.lib.response_processor import ON_COMPLETIONS_REQUESTED
from FSharp.sublime_plugin_lib.context import ContextProviderMixin
from FSharp.sublime_plugin_lib.panels import OutputPanel
from FSharp.sublime_plugin_lib.panels import OutputPanel
from FSharp.fsac.server import completions_queue


_logger = logging.getLogger(__name__)


def erase_status(view, key):
    view.erase_status(key)


class fs_dot(sublime_plugin.WindowCommand):
    '''Inserts the dot character and opens the autocomplete list.
    '''
    def run(self):
        view = self.window.active_view()
        pt = view.sel()[0].b
        view.run_command('insert', {'characters': '.'})
        view.sel().clear()
        view.sel().add(sublime.Region(pt + 1))
        self.window.run_command('fs_run_fsac', { "cmd": "completion" })


class fs_run_fsac(sublime_plugin.WindowCommand):
    '''Runs an fsautocomplete.exe command.
    '''
    def run(self, cmd):
        _logger.debug ('running fsac action: %s', cmd)
        if not cmd:
            return

        if cmd == 'project':
            self.do_project()
            return

        if cmd == 'parse':
            self.do_parse()
            return

        if cmd == 'declarations':
            self.do_declarations()
            return

        if cmd == 'compilerlocation':
            self.do_compiler_location()
            return

        if cmd == 'finddecl':
            self.do_find_decl()
            return

        if cmd == 'completion':
            self.do_completion()
            return

        if cmd == 'tooltip':
            self.do_tooltip()
            return

        if cmd == 'run-file':
            self.do_run_file()

    def get_active_file_name(self):
        try:
            fname = self.window.active_view ().file_name ()
        except AttributeError as e:
            return
        return fname

    def get_insertion_point(self):
        view = self.window.active_view()
        if not view:
            return None
        try:
            sel = view.sel()[0]
        except IndexError as e:
            return None
        return view.rowcol(sel.b)

    def do_project(self):
        fname = self.get_active_file_name ()
        if not fname:
            return
        editor_context.fsac.send_request (ProjectRequest(fname))

    def do_parse(self):
        fname = self.get_active_file_name ()
        if not fname:
            return
        v = self.window.active_view ()
        content = v.substr(sublime.Region(0, v.size()))
        editor_context.fsac.send_request(ParseRequest(fname, content=content))

    def do_declarations(self):
        fname = self.get_active_file_name ()
        if not fname:
            return
        editor_context.fsac.send_request(DeclarationsRequest(fname))

    def do_compiler_location(self):
        editor_context.fsac.send_request(CompilerLocationRequest())

    def do_find_decl(self):
        fname = self.get_active_file_name ()
        if not fname:
            return

        try:
            (row, col) = self.get_insertion_point()
        except TypeError as e:
            return
        else:
            editor_context.fsac.send_request(FindDeclRequest(fname, row + 1, col))

    def do_completion(self):
        fname = self.get_active_file_name ()
        if not fname:
            return

        try:
            (row, col) = self.get_insertion_point()
        except TypeError as e:
            return
        else:
            # raise first, because the event listener drains the completions queue
            raise_event(ON_COMPLETIONS_REQUESTED, {})
            editor_context.fsac.send_request(CompletionRequest(fname, row + 1, col))
            self.window.run_command('auto_complete')

    def do_tooltip(self):
        fname = self.get_active_file_name ()
        if not fname:
            return

        try:
            (row, col) = self.get_insertion_point()
        except TypeError as e:
            return
        else:
            editor_context.fsac.send_request(TooltipRequest(fname, row + 1, col))

    def do_run_file(self):
        try:
            fname = self.window.active_view().file_name()
        except AttributeError:
            return
        else:
            self.window.run_command('fs_run_interpreter', {
                'fname': fname
                })


class fs_go_to_location (sublime_plugin.WindowCommand):
    def run(self, loc):
        v = self.window.active_view()
        pt = v.text_point(*loc)
        v.sel().clear()
        v.sel().add(sublime.Region(pt))
        v.show_at_center(pt)


class fs_show_menu(sublime_plugin.WindowCommand):
    '''Generic command to show a menu.
    '''
    def run(self, items):
        '''
        @items
          A list of items following this structure:
          item 0: name
          item 1: Sublime Text command name
          item 2: dictionary of arguments for the command
        '''
        self.items = items
        self.names = names = [name for (name, _, _) in items]
        self.window.show_quick_panel(self.names, self.on_done)

    def on_done(self, idx):
        if idx == -1:
            return
        _, cmd, args = self.items[idx]
        if cmd:
            self.window.run_command (cmd, args or {})


class fs_show_data(sublime_plugin.WindowCommand):
    '''A simple command to use the quick panel as a data display.
    '''
    def run(self, data):
        self.window.show_quick_panel(data, None, sublime.MONOSPACE_FONT)


# TODO: move this to the command palette.
class fs_show_options(sublime_plugin.WindowCommand):
    """Displays the main menu for F# commands.
    """
    ITEMS = {
        'F#: Show Declarations': 'declarations',
        'F#: Show Tooltip': 'tooltip',
        'F#: Run File': 'run-file',
    }

    def run(self):
        self.window.show_quick_panel(
            list(sorted(fs_show_options.ITEMS.keys())),
            self.on_done)

    def on_done(self, idx):
        if idx == -1:
            return
        key = list(sorted(fs_show_options.ITEMS.keys()))[idx]
        cmd = fs_show_options.ITEMS[key]
        self.window.run_command('fs_run_fsac', {'cmd': cmd})


class fs_run_interpreter(sublime_plugin.WindowCommand):
    def run(self, fname):
        assert fname, 'bad argument'

        f  = FSharpFile (fname)
        if not os.path.exists(f.path):
            _logger.debug('file must be saved first: %s', f.path)
            return

        if not f.is_script_file:
            _logger.debug('not a script file: %s', f.path)
            return

        self.window.run_command('fs_exec', {
            'shell_cmd': '"{}" "{}"'.format(editor_context.interpreter_path, f.path),
            'working_dir': os.path.dirname(f.path)
            })
