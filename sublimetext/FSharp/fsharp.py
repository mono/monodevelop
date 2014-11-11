# Copyright (c) 2014, Guillermo López-Anglada. Please see the AUTHORS file for details.
# All rights reserved. Use of this source code is governed by a BSD-style
# license that can be found in the LICENSE file.)

import sublime
import sublime_plugin

import os

from FSharp.fsac.request import AdHocRequest
from FSharp.fsac.request import DataRequest
from FSharp.fsac.request import DeclarationsRequest
from FSharp.fsac.request import ParseRequest
from FSharp.fsac.request import ProjectRequest
from FSharp.fsac.response import CompilerLocationResponse
from FSharp.fsac.response import CompilerLocationResponse
from FSharp.fsac.response import DeclarationsResponse
from FSharp.fsac.response import ProjectResponse
from FSharp.lib.editor import Editor
from FSharp.sublime_plugin_lib import PluginLogger
from FSharp.sublime_plugin_lib.panels import OutputPanel


_logger = PluginLogger (__name__)


def plugin_unloaded():
    editor_context.fsac.stop()


def process_resp(data):
    _logger.debug ('processing response data: %s', data)
    if data ['Kind'] == 'compilerlocation':
        r = CompilerLocationResponse (data)
        editor_context.compilers_path = r.compilers_path
        return

    if data['Kind'] == 'project':
        r = ProjectResponse(data)
        panel = OutputPanel (name='fs.out')
        panel.write ("Files in project:\n")
        panel.write ("\n")
        panel.write ('\n'.join(r.files))
        panel.show()
        return

    if data['Kind'] == 'errors' and data['Data']:
        panel = OutputPanel (name='fs.out')
        panel.write (str(data))
        panel.write ("\n")
        panel.show()
        return

    if data['Kind'] == 'INFO' and data['Data']:
        panel = OutputPanel (name='fs.out')
        panel.write (str(data))
        panel.write ("\n")
        panel.show()
        return

    if data['Kind'] == 'declarations' and data['Data']:
        decls = DeclarationsResponse(data)
        its = [decl.to_menu_data() for decl in decls.declarations]
        w = sublime.active_window()
        w.run_command ('fs_show_menu', {'items': its})
        return

class fs_run_fsac(sublime_plugin.WindowCommand):
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

    def get_active_file_name(self):
        try:
            fname = self.window.active_view ().file_name ()
        except AttributeError as e:
            return
        return fname

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


class fs_go_to_location (sublime_plugin.WindowCommand):
    def run(self, loc):
        v = self.window.active_view ()
        pt = v.text_point(*loc)
        v.sel ().clear ()
        v.sel ().add (sublime.Region (pt))
        v.show_at_center(pt)


class fs_show_menu(sublime_plugin.WindowCommand):
    def run(self, items):
        self.items = items
        self.names = names = [name for (name, _, _) in items]
        self.window.show_quick_panel(self.names, self.on_done)

    def on_done(self, idx):
        if idx == -1:
            return
        _, cmd, args = self.items[idx]
        if cmd:
            self.window.run_command (cmd, args or {})


class fs_show_options(sublime_plugin.WindowCommand):
    """Displays the main menu for F#.
    """
    OPTIONS = {
        'F#: Get Compiler Location': 'compilerlocation',
        'F#: Parse Active File': 'parse',
        'F#: Set Active File as Project': 'project',
        'F#: Show Declarations': 'declarations',
    }
    def run(self):
        self.window.show_quick_panel(
            list(sorted(fs_show_options.OPTIONS.keys())),
            self.on_done)

    def on_done(self, idx):
        if idx == -1:
            return
        key = list (sorted (fs_show_options.OPTIONS.keys()))[idx]
        cmd = fs_show_options.OPTIONS[key]
        self.window.run_command ('fs_run_fsac', {'cmd': cmd})


_logger.debug('starting editor context...')
editor_context = Editor(process_resp)