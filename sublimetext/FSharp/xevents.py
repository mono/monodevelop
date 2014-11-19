# Copyright (c) 2014, Guillermo López-Anglada. Please see the AUTHORS file for details.
# All rights reserved. Use of this source code is governed by a BSD-style
# license that can be found in the LICENSE file.)

import logging

import sublime_plugin
import sublime

from FSharp.fsharp import editor_context
from FSharp.lib.project import FSharpFile

_logger = logging.getLogger(__name__)


class ProjectTracker (sublime_plugin.EventListener):
    def on_activated_async(self, view):
        _logger.debug ('activated file: %s', view.file_name())
        editor_context.parse_view(view)

    def on_modified_async(self, view):
        # _logger.debug ('modified file: %s', view.file_name())
        editor_context.parse_view(view)
