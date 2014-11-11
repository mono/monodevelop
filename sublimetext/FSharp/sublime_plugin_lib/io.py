# Copyright (c) 2014, Guillermo López-Anglada. Please see the AUTHORS file for details.
# All rights reserved. Use of this source code is governed by a BSD-style
# license that can be found in the LICENSE file.)

import threading

import os


class AsyncStreamReader(threading.Thread):
    '''Reads a process stream from an alternate thread.
    '''
    def __init__(self, stream, on_data, *args, **kwargs):
        '''
        @stream
          Stream to read from.

        @on_data
          Callback to call with bytes read from @stream.
        '''
        super().__init__(*args, **kwargs)
        self.stream = stream
        self.on_data = on_data
        assert self.on_data, 'wrong call: must provide callback'

    def run(self):
        while True:
            data = self.stream.readline()
            if not data:
                return

            self.on_data(data)


def touch(path):
    with open(path, 'wb') as f:
        f.close()
