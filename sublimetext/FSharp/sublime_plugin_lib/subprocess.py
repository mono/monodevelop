# Copyright (c) 2014, Guillermo López-Anglada. Please see the AUTHORS file for details.
# All rights reserved. Use of this source code is governed by a BSD-style
# license that can be found in the LICENSE file.)

from subprocess import Popen
import os

from . import PluginLogger
from .plat import supress_window


_logger = PluginLogger(__name__)


def killwin32(proc):
    try:
        path = os.path.expandvars("%WINDIR%\\System32\\taskkill.exe")
        GenericBinary(show_window=False).start([path, "/pid", str(proc.pid)])
    except Exception as e:
        _logger.error(e)


class GenericBinary(object):
    '''Starts a process.
    '''
    def __init__(self, *args, show_window=True):
        '''
        @show_window
          Windows only. Whether to show a window.
        '''
        self.args = args
        self.startupinfo = None
        if not show_window:
            self.startupinfo = supress_window()

    def start(self, args=[], env=None, shell=False, cwd=None):
        cmd = self.args + tuple(args)
        _logger.debug('running cmd line (GenericBinary): %s', cmd)
        Popen(cmd, startupinfo=self.startupinfo, env=env, shell=shell,
              cwd=cwd)
