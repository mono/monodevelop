import unittest

import sublime

from FSharp.sublime_plugin_lib.testing import SyntaxTest


class FSharpSyntaxTest(SyntaxTest):
    def setUp(self):
        super().setUp()
        self._setSyntax('Packages/FSharp/FSharp.tmLanguage')