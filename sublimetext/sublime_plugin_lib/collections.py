# Copyright (c) 2014, Guillermo López-Anglada. Please see the AUTHORS file for details.
# All rights reserved. Use of this source code is governed by a BSD-style
# license that can be found in the LICENSE file.)


class CircularArray(list):
    def __init__(self, *args, **kwargs):
        super().__init__(*args, **kwargs)
        self.index = None

    def forward(self):
        if self.index is None:
            self.index = 0
            return self[self.index]

        try:
            self.index += 1
            return self[self.index]
        except IndexError:
            self.index = 0
            return self[self.index]

    def backward(self):
        if self.index is None:
            self.index = -1
            return self[self.index]

        try:
            self.index -= 1
            return self[self.index]
        except IndexError:
            self.index = -1
            return self[self.index]
