# -*- coding: utf-8 -*-
#
# Author:
#   Thomas Wiest <twiest@novell.com>
#
# Copyright (c) 2008 Novell, Inc (http://www.novell.com)
#
# Permission is hereby granted, free of charge, to any person obtaining a copy
# of this software and associated documentation files (the "Software"), to deal
# in the Software without restriction, including without limitation the rights
# to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
# copies of the Software, and to permit persons to whom the Software is
# furnished to do so, subject to the following conditions:
#
# The above copyright notice and this permission notice shall be included in
# all copies or substantial portions of the Software.
#
# THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
# IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
# FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
# AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
# LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
# OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
# THE SOFTWARE.


'Application wrapper for MonoDevelop'

from strongwind import *

import os

def launchMonoDevelop(exe=None):
    'Launch MonoDevelop with accessibility enabled and return a MonoDevelop object'

    # This makes it so we don't have to enable accessibility on our desktops
    os.environ["GTK_MODULES"] = "gail:atk-bridge:gnomebreakpad"
    sleep(config.SHORT_DELAY)

    if exe is None:
        exe = '/usr/bin/monodevelop'

    args = [exe, "--debug"]
    (app, subproc) = cache.launchApplication(args=args, name="MonoDevelop")

    monodevelop = MonoDevelop(app, subproc)
    cache.addApplication(monodevelop)

    # monoDevelopFrame's assertClosed() calls self.app.assertClosed(), but if the
    # app has closed already, self.app will return None.  Normally, we would
    # cache self.app in the constructor of the monoDevelopFrame class, but at the
    # time the monoDevelopFrame's constructor is run, cache.getApplication(self._app_id) 
    # resolves to an accessible.Application().  We promote the application to
    # a MonoDevelop object here, so we must set monoDevelopFrame.app immediately
    # afterward.
    monodevelop.monodevelopFrame.app = monodevelop

    return monodevelop

class MonoDevelop(accessibles.Application):
    def __init__(self, accessible, subproc=None):
        'Get a reference to the monodevelop window'
        super(MonoDevelop, self).__init__(accessible, subproc)

        self.findFrame(re.compile('.*MonoDevelop$'), logName='Monodevelop')
