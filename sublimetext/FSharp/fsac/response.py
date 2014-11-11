# Copyright (c) 2014, Guillermo López-Anglada. Please see the AUTHORS file for details.
# All rights reserved. Use of this source code is governed by a BSD-style
# license that can be found in the LICENSE file.)


class CompilerLocationResponse (object):
    def __init__(self, content):
        self.content = content

    @property
    def compilers_path(self):
       return self.content['Data']


class ProjectResponse (object):
    def __init__(self, content):
        self.content = content

    @property
    def files(self):
       return self.content['Data']['Files']

    @property
    def framework(self):
       return self.content ['Data']['Framework']

    @property
    def output(self):
       return self.content ['Data']['Output']

    @property
    def output(self):
       return self.content ['Data']['References']

