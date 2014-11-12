# Copyright (c) 2014, Guillermo López-Anglada. Please see the AUTHORS file for details.
# All rights reserved. Use of this source code is governed by a BSD-style
# license that can be found in the LICENSE file.)

import sublime_plugin
import sublime

from FSharp.fsharp import editor_context
from FSharp.lib.project import FSharpFile
from FSharp.sublime_plugin_lib import PluginLogger

_logger = PluginLogger (__name__)


class ProjectTracker (sublime_plugin.EventListener):
    def on_activated(self, view):
        # todo: what about unsaved files?
        fs_file = FSharpFile(view)
        if not fs_file.is_fsharp_file:
            return
        _logger.debug ('activated file: %s', view.file_name())
        editor_context.refresh(fs_file)
        # todo: very inneficient
        if fs_file.is_code:
            content = view.substr(sublime.Region (0, view.size()))
            editor_context.parse_file(fs_file, content)

