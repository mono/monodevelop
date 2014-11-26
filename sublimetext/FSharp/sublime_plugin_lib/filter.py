# Copyright (c) 2014, Guillermo López-Anglada. Please see the AUTHORS file for details.
# All rights reserved. Use of this source code is governed by a BSD-style
# license that can be found in the LICENSE file.)

from subprocess import Popen
from subprocess import PIPE
from subprocess import TimeoutExpired
import threading

from . import PluginLogger
from .plat import supress_window
from .text import clean
from .text import decode


_logger = PluginLogger(__name__)


class TextFilter(object):
    '''Filters text through an external program (sync).
    '''
    def __init__(self, args, timeout=10):
        self.args = args
        self.timeout = timeout
        # Encoding the external program likes to receive.
        self.in_encoding = 'utf-8'
        # Encoding the external program will emit.
        self.out_encoding = 'utf-8'

        self._proc = None

    def encode(self, text):
        return text.encode(self.in_encoding)

    def _start(self):
        try:
            self._proc = Popen(self.args,
                               stdout=PIPE,
                               stderr=PIPE,
                               stdin=PIPE,
                               startupinfo=supress_window())
        except OSError as e:
            _logger.error('while starting text filter program: %s', e)
            return

    def filter(self, input_text):
        self._start()
        try:
            in_bytes = self.encode(input_text)
            out_bytes, err_bytes = self._proc.communicate(in_bytes,
                                                          self.timeout)
            if err_bytes:
                _logger.error('while filtering text: %s',
                    clean(decode(err_bytes, self.out_encoding)))
                return

            return clean(decode(out_bytes, self.out_encoding))

        except TimeoutExpired:
            _logger.debug('text filter program response timed out')
            return

        except Exception as e:
            _logger.error('while running TextFilter: %s', e)
            return
