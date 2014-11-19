# Copyright (c) 2014, Guillermo López-Anglada. Please see the AUTHORS file for details.
# All rights reserved. Use of this source code is governed by a BSD-style
# license that can be found in the LICENSE file.)

import logging

import sublime_plugin
import sublime
import threading
from collections import defaultdict

from FSharp.fsharp import editor_context
from FSharp.lib.project import FSharpFile
from FSharp.sublime_plugin_lib.sublime import after

_logger = logging.getLogger(__name__)


class ProjectTracker (sublime_plugin.EventListener):
    edits = defaultdict(int)
    edits_lock = threading.Lock()

    def add_edit(self, view):
        with ProjectTracker.edits_lock:
            view_id = view.file_name() or view.id()
            self.edits[view_id] += 1
        after(1500, lambda: self.subtract_edit(view))

    def subtract_edit(self, view):
        with ProjectTracker.edits_lock:
            view_id = view.file_name() or view.id()
            self.edits[view_id] -= 1
            if self.edits[view_id] == 0:
                self.on_idle(view)

    def on_activated_async(self, view):
        if not FSharpFile(view).is_code_file:
            return
        _logger.debug ('activated file: %s', view.file_name())
        editor_context.parse_view(view)

    def on_idle(self, view):
        editor_context.parse_view(view)

    def on_modified_async(self, view):
        if not FSharpFile(view).is_code_file:
            return
        # _logger.debug ('modified file: %s', view.file_name())
        self.add_edit (view)
