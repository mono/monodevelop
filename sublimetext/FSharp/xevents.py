# Copyright (c) 2014, Guillermo López-Anglada. Please see the AUTHORS file for details.
# All rights reserved. Use of this source code is governed by a BSD-style
# license that can be found in the LICENSE file.)

import logging

from collections import defaultdict
import json
import sublime
import sublime_plugin
import threading

from FSharp.fsac.server import completions_queue
from FSharp.fsharp import editor_context
from FSharp.lib.project import FSharpFile
from FSharp.lib.response_processor import add_listener
from FSharp.lib.response_processor import ON_COMPLETIONS_REQUESTED
from FSharp.sublime_plugin_lib.context import ContextProviderMixin
from FSharp.sublime_plugin_lib.sublime import after


_logger = logging.getLogger(__name__)


class ProjectTracker (sublime_plugin.EventListener):
    '''Tracks events.
    '''
    edits = defaultdict(int)
    edits_lock = threading.Lock()
    parsed = {}
    parsed_lock = threading.Lock()

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

        with ProjectTracker.parsed_lock:
            view_id = view.file_name() or view.id()
            if ProjectTracker.parsed.get(view_id):
                return

        editor_context.parse_view(view)
        self.set_parsed(view, True)

    def on_load_async(self, view):
        self.on_activated_async(view)

    def set_parsed(self, view, value):
        with ProjectTracker.parsed_lock:
            view_id = view.file_name() or view.id()
            ProjectTracker.parsed[view_id] = value

    def on_idle(self, view):
        editor_context.parse_view(view)
        self.set_parsed(view, True)

    def on_modified_async(self, view):
        _logger.debug('modified file: %s', view.file_name())
        if not FSharpFile(view).is_code_file:
            return
        self.add_edit(view)
        self.set_parsed(view, False)


class ContextProvider(sublime_plugin.EventListener, ContextProviderMixin):
    '''Implements contexts for .sublime-keymap files.
    '''
    def on_query_context(self, view, key, operator, operand, match_all):
        if key == 'fs_is_code_file':
            value = FSharpFile(view).is_code
            return self._check(value, operator, operand, match_all)


class FSharpAutocomplete(sublime_plugin.EventListener):
    WAIT_ON_COMPLETIONS = False

    @staticmethod
    def on_completions_requested(data):
        FSharpAutocomplete.WAIT_ON_COMPLETIONS = True

    def on_query_completions(self, view, prefix, locations):
        if not FSharpAutocomplete.WAIT_ON_COMPLETIONS:
            return []

        try:
            data = completions_queue.get(block=True, timeout=.75)
            data = json.loads(data.decode('utf-8'))
            return [[item, item] for item in data['Data']]
        except:
            return []
        finally:
            FSharpAutocomplete.WAIT_ON_COMPLETIONS = False


add_listener(ON_COMPLETIONS_REQUESTED, FSharpAutocomplete.on_completions_requested)
