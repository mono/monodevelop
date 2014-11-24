# Copyright (c) 2014, Guillermo López-Anglada. Please see the AUTHORS file for details.
# All rights reserved. Use of this source code is governed by a BSD-style
# license that can be found in the LICENSE file.)

import json
import logging
import os
import queue

import sublime
import sublime_plugin

from FSharp.fsac.response import CompilerLocationResponse
from FSharp.fsac.response import DeclarationsResponse
from FSharp.fsac.response import ProjectResponse
from FSharp.fsac.response import ErrorInfo
from FSharp.sublime_plugin_lib.panels import OutputPanel


_logger = logging.getLogger(__name__)


ON_COMPILER_PATH_AVAILABLE = 'OnCompilerPathAvailableEvent'
ON_COMPLETIONS_REQUESTED = 'OnCompletionsRequestedEvent'

_events = {
    ON_COMPILER_PATH_AVAILABLE: [],
    ON_COMPLETIONS_REQUESTED: [],
}


def add_listener(event_name, f):
    '''Registers a listener for the @event_name event.
    '''
    assert event_name, 'must provide "event_name" (actual: %s)' % event_name
    assert event_name in _events, 'unknown event name: %s' % event_name
    if f not in _events:
        _events[event_name].append(f)


def raise_event(event_name=None, data={}):
    '''Raises an event.
    '''
    assert event_name, 'must provide "event_name" (actual: %s)' % event_name
    assert event_name in _events, 'unknown event name: %s' % event_name
    assert isinstance(data, dict), '`data` must be a dict'
    for f in _events[event_name]:
        f(data)


def process_resp(data):
    _logger.debug ('processing response data: %s', data)
    if data ['Kind'] == 'compilerlocation':
        r = CompilerLocationResponse (data)
        raise_event(ON_COMPILER_PATH_AVAILABLE, {'response': r})
        return

    if data['Kind'] == 'project':
        r = ProjectResponse(data)
        _logger.debug('\n'.join(r.files))
        return

    if data['Kind'] == 'errors':
        # todo: enable error navigation via standard keys
        v = sublime.active_window().active_view()
        v.erase_regions ('fs.errs')
        if not data['Data']:
            return
        v.add_regions('fs.errs',
                      [ErrorInfo(e).to_region(v) for e in data['Data']],
                      'invalid.illegal',
                      'dot',
                      sublime.DRAW_SQUIGGLY_UNDERLINE |
                      sublime.DRAW_NO_FILL |
                      sublime.DRAW_NO_OUTLINE
                      )
        return

    if data['Kind'] == 'ERROR':
        _logger.error(str(data))
        return

    if data['Kind'] == 'tooltip' and data['Data']:
        v = sublime.active_window().active_view()
        word = v.substr(v.word(v.sel()[0].b))
        panel = OutputPanel('fs.out')
        panel.write(data['Data'])
        panel.show()
        return

    if data['Kind'] == 'INFO' and data['Data']:
        _logger.info(str(data))
        print("FSharp:", data['Data'])
        return

    if data['Kind'] == 'finddecl' and data['Data']:
        fname = data['Data']['File']
        row = data['Data']['Line']
        col = data['Data']['Column'] + 1
        w = sublime.active_window()
        # todo: don't open file if we are looking at the requested file
        target = '{0}:{1}:{2}'.format(fname, row, col)
        w.open_file(target, sublime.ENCODED_POSITION)
        return

    if data['Kind'] == 'declarations' and data['Data']:
        decls = DeclarationsResponse(data)
        its = [decl.to_menu_data() for decl in decls.declarations]
        w = sublime.active_window()
        w.run_command ('fs_show_menu', {'items': its})
        return

    if data['Kind'] == 'completion' and data['Data']:
        _logger.error('unexpected "completion" results - should be handled elsewhere')
        return
