# Copyright (c) 2014, Guillermo López-Anglada. Please see the AUTHORS file for details.
# All rights reserved. Use of this source code is governed by a BSD-style
# license that can be found in the LICENSE file.)

'''Wraps a process to make it act as a pipe server. Takes care of supressing
console windows under Windows and other housekeeping.
'''

import subprocess
from subprocess import PIPE
from subprocess import Popen
import os
import threading
from contextlib import contextmanager


@contextmanager
def pushd(to):
    old = os.getcwd()
    try:
        os.chdir(to)
        # TODO(guillermooo): makes more sense to return 'old'
        yield to
    finally:
        os.chdir(old)


def supress_window():
    """Returns a STARTUPINFO structure configured to supress windows.
    Useful, for example, to supress console windows.

    Works only on Windows.
    """
    if os.name == 'nt':
        startupinfo = subprocess.STARTUPINFO()
        startupinfo.dwFlags |= subprocess.STARTF_USESHOWWINDOW
        startupinfo.wShowWindow = subprocess.SW_HIDE
        return startupinfo
    return None


# _logger = PluginLogger(__name__)


class PipeServer(object):
    '''Starts as process and communicates with it via pipes.
    '''
    status_lock = threading.RLock()

    def __init__(self, args):
        self.proc = None
        self.args = args

    @property
    def is_running(self):
        '''Returns `True` if the server seems to be responsive.
        '''
        try:
            with PipeServer.status_lock:
                return not self.proc.stdin.closed
        except AttributeError:
            # _logger.debug('PipeServer not started yet')
            return

    def start(self, working_dir='.'):
        with PipeServer.status_lock:
            if self.is_running:
                # _logger.debug(
                    # 'tried to start an already running PipeServer; aborting')
                return

            with pushd(working_dir):
                # _logger.debug('starting PipeServer with args: %s', self.args)
                self.proc = Popen(self.args,
                                        stdout=PIPE,
                                        stdin=PIPE,
                                        stderr=PIPE,
                                        startupinfo=supress_window())

    def stop(self):
        # _logger.debug('stopping PipeServer...')
        self.proc.stdin.close()
        self.proc.stdout.close()
        self.proc.kill()
