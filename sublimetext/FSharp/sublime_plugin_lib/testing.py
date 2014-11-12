# Copyright (c) 2014, Guillermo López-Anglada. Please see the AUTHORS file for details.
# All rights reserved. Use of this source code is governed by a BSD-style
# license that can be found in the LICENSE file.)
'''Provides classes that make it easier to test several aspects of a package.
'''

import unittest

import sublime


class ViewTest(unittest.TestCase):
    def setUp(self):
        self.view = sublime.active_window().new_file()

    def append(self, text):
        self.view.run_command('append', {'characters': text})

    def tearDown(self):
        self.view.set_scratch(True)
        self.view.close()


class SyntaxTest(ViewTest):
    def _setSyntax(self, rel_path):
        self.view.set_syntax_file(rel_path)

    def getScopeNameAt(self, pt):
        return self.view.scope_name(pt)

    def getFinestScopeNameAt(self, pt):
        return self.getScopeNameAt(pt).split()[-1]

    def getScopeNameAtRowCol(self, row, col):
        text_pt = self.view.text_point(row, col)
        return self.getScopeNameAt(text_pt)

    def getFinestScopeNameAtRowCol(self, row, col):
        return self.getScopeNameAtRowCol(row, col).split()[-1]

