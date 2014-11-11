# Copyright (c) 2014, Guillermo López-Anglada. Please see the AUTHORS file for details.
# All rights reserved. Use of this source code is governed by a BSD-style
# license that can be found in the LICENSE file.)

'''Helper functions related to platform-specific issues.
'''

import sublime

from os.path import join
import subprocess


def is_windows():
    """Returns `True` if ST is running on Windows.
    """
    return sublime.platform() == 'windows'


def supress_window():
    """Returns a STARTUPINFO structure configured to supress windows.
    Useful, for example, to supress console windows.

    Works only on Windows.
    """
    if is_windows():
        startupinfo = subprocess.STARTUPINFO()
        startupinfo.dwFlags |= subprocess.STARTF_USESHOWWINDOW
        startupinfo.wShowWindow = subprocess.SW_HIDE
        return startupinfo
    return None
