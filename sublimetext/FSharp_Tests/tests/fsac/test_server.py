import unittest
import os

from FSharp.fsac.server import Server
from FSharp import const

import sublime


THIS_DIR = os.path.dirname(os.path.dirname(__file__))
DATA_DIR = os.path.join(sublime.packages_path(), 'FSharp_Tests/data')


class ServerTests(unittest.TestCase):
    def __init__(self, *args, **kwargs):
        super().__init__(*args, **kwargs)

        if sublime.platform() in ('osx', 'linux'):
            self.cmd_line = ('mono', (const.path_to_fs_ac_binary(),))
        else:
            assert sublime.platform() == 'windows'
            self.cmd_line = (const.path_to_fs_ac_binary(), ())

    def testCanInstantiate(self):
        try:
            cmd, args = self.cmd_line
            s = Server(cmd, *args)
            self.assertEqual(None, s.proc)
        finally:
            s.stop()

    def testCanStart(self):
        try:
            cmd, args = self.cmd_line
            s = Server(cmd, *args)
            s.start()
            self.assertTrue(s.proc.stdin)
        finally:
            s.stop()

    def testCanGetHelp(self):
        try:
            cmd, args = self.cmd_line
            s = Server(cmd, *args)
            s.start()
            s.help()
            data = s.read_all(eof=bytes('    \n', 'ascii'))
            self.assertEqual(data['Kind'], '_UNPARSED')
            self.assertEqual(data['Data'].strip()[:len('Supported')], 'Supported')
        finally:
            s.stop()

    # def testCanSetProject(self):
    #     try:
    #         cmd, args = self.cmd_line
    #         s = Server(cmd, *args)
    #         s.start()
    #         p = os.path.join(DATA_DIR, 'FindDecl.fsproj')
    #         self.assertTrue(os.path.exists(p))
    #         s.project(p)
    #         response = s.read_line()
    #         self.assertEqual(response['Kind'], 'project')
    #     finally:
    #         s.stop()

    # def testCanParseFile(self):
    #     try:
    # cmd, args = self.cmd_line
    #         s = Server(cmd, *args)
    #         s.start()
    #         response = s.parse('./tests/data/FileTwo.fs')
    #         # XXX: Why in all caps?
    #         self.assertEqual(response['Kind'], 'INFO')
    #     finally:
    #         s.stop()

    # def testCanRetrieveErrors(self):
    #     try:
    # cmd, args = self.cmd_line
    #         s = Server(cmd, *args)
    #         s.start()
    #         response = s.parse('./tests/data/FileTwo.fs')
    #         response = s.errors()
    #         self.assertEqual(response['Kind'], 'errors')
    #     finally:
    #         s.stop()

    # def testCanRetrieveDeclarations(self):
    #     try:
    # cmd, args = self.cmd_line
    #         s = Server(cmd, *args)
    #         s.start()
    #         response = s.parse('./tests/data/FileTwo.fs')
    #         response = s.declarations('./tests/data/FileTwo.fs')
    #         self.assertEqual(response['Kind'], 'declarations')
    #     finally:
    #         s.stop()

    # def testCanRetrieveCompletions(self):
    #     try:
    # cmd, args = self.cmd_line
    #         s = Server(cmd, *args)
    #         s.start()
    #         response = s.parse('./tests/data/FileTwo.fs')
    #         helptext, completions = s.completions('./tests/data/FileTwo.fs', 12, 9)
    #         self.assertEqual(helptext['Kind'], 'helptext')
    #         self.assertEqual(completions['Kind'], 'completion')
    #     finally:
    #         s.stop()

    # def testCanRetrieveTooltip(self):
    #     try:
    # cmd, args = self.cmd_line
    #         s = Server(cmd, *args)
    #         s.start()
    #         response = s.parse('./tests/data/FileTwo.fs')
    #         response = s.tooltip('./tests/data/FileTwo.fs', 12, 9)
    #         self.assertEqual(response['Kind'], 'tooltip')
    #     finally:
    #         s.stop()

    # def testCanFindDeclaration(self):
    #     try:
    # cmd, args = self.cmd_line
    #         s = Server(cmd, *args)
    #         s.start()
    #         s.project('./tests/data/FindDecl.fsproj')
    #         s.parse('./tests/data/FileTwo.fs')
    #         s.parse('./tests/data/Script.fsx')
    #         s.parse('./tests/data/Program.fs')
    #         response = s.find_declaration('./tests/data/Program.fs', 5, 15)
    #         self.assertEqual(response['Kind'], 'finddecl')
    #     finally:
    #         s.stop()
