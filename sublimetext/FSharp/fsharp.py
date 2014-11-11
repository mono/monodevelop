# Copyright (c) 2014, Guillermo López-Anglada. Please see the AUTHORS file for details.
# All rights reserved. Use of this source code is governed by a BSD-style
# license that can be found in the LICENSE file.)

import sublime
import sublime_plugin

import os

from FSharp.fsac import server
from FSharp.fsac.client import FsacClient
from FSharp.fsac.request import AdHocRequest
from FSharp.fsac.request import CompilerLocationRequest
from FSharp.fsac.request import DataRequest
from FSharp.fsac.request import DeclarationsRequest
from FSharp.fsac.request import ParseRequest
from FSharp.fsac.request import ProjectRequest
from FSharp.fsac.response import CompilerLocationResponse
from FSharp.fsac.response import ProjectResponse
from FSharp.sublime_plugin_lib import PluginLogger
from FSharp.sublime_plugin_lib.panels import OutputPanel


_logger = PluginLogger (__name__)


def plugin_unloaded():
    editor_context.fsac.stop()


class Editor(object):
    """Global editor state.
    """
    def __init__(self, resp_proc):
        _logger.info ('starting fsac server...')
        self.fsac = FsacClient(server.start(), resp_proc)
        self.compilers_path = None
        self.project_file = None
        self.fsac.send_request (CompilerLocationRequest())

    @property
    def compiler_path(self):
        if self.compilers_path is None:
            return None
        return os.path.join(self.compilers_path, 'fsc.exe')

    @property
    def interpreter_path(self):
        if self.compilers_path is None:
            return None
        return os.path.join(self.compilers_path, 'fsi.exe')


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
        panel = OutputPanel (name='fs.out')
        panel.write (str(data))
        panel.write ("\n")
        panel.show()
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