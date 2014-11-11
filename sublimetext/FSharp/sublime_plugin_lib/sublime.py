# Copyright (c) 2014, Guillermo López-Anglada. Please see the AUTHORS file for details.
# All rights reserved. Use of this source code is governed by a BSD-style
# license that can be found in the LICENSE file.)

'''Utilities based on the Sublime Text api.
'''
import sublime


# TODO(guillermooo): make an *_async version too?
def after(timeout, f, *args, **kwargs):
    '''Runs @f after @timeout delay in milliseconds.

    @timeout
      Delay in milliseconds.

    @f
      Function to run passing it @*args and @*kwargs.
    '''
    sublime.set_timeout(lambda: f(*args, **kwargs), timeout)
