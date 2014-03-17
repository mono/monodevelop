import unittest
import os

from FSharp.lib.fsac.server import Server
from FSharp.lib import const

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

    def testCanSetProject(self):
        try:
            cmd, args = self.cmd_line
            s = Server(cmd, *args)
            s.start()
            p = os.path.join(DATA_DIR, 'FindDecl.fsproj')
            self.assertTrue(os.path.exists(p))
            s.project(p)
            response = s.read_line()
            self.assertEqual(response['Kind'], 'project')
        finally:
            s.stop()

    def testCanParseFile(self):
        try:
            cmd, args = self.cmd_line
            s = Server(cmd, *args)
            s.start()
            p = os.path.join(DATA_DIR, 'FileTwo.fs')
            s.parse(p)
            response = s.read_line()
            # XXX: Why in all caps?
            self.assertEqual(response['Kind'], 'INFO')
        finally:
            s.stop()

    def testCanRetrieveErrors(self):
        try:
            cmd, args = self.cmd_line
            s = Server(cmd, *args)
            s.start()
            p = os.path.join(DATA_DIR, 'FileTwo.fs')
            s.parse(p)
            _ = s.read_line()
            s.errors()
            response = s.read_line()
            self.assertEqual(response['Kind'], 'errors')
        finally:
            s.stop()

    def testCanRetrieveDeclarations(self):
        try:
            cmd, args = self.cmd_line
            s = Server(cmd, *args)
            s.start()
            p = os.path.join(DATA_DIR, 'FileTwo.fs')
            s.parse(p)
            _ = s.read_line()
            s.declarations(p)
            response = s.read_line()
            self.assertEqual(response['Kind'], 'declarations')
        finally:
            s.stop()

    def testCanRetrieveCompletions(self):
        try:
            cmd, args = self.cmd_line
            s = Server(cmd, *args)
            s.start()
            p = os.path.join(DATA_DIR, 'FileTwo.fs')
            s.parse(p)
            _ = s.read_line()
            s.completions(p, 12, 9)
            helptext = s.read_line()
            completions = s.read_line()
            self.assertEqual(helptext['Kind'], 'helptext')
            self.assertEqual(completions['Kind'], 'completion')
        finally:
            s.stop()

    def testCanRetrieveTooltip(self):
        try:
            cmd, args = self.cmd_line
            s = Server(cmd, *args)
            s.start()
            p = os.path.join(DATA_DIR, 'FileTwo.fs')
            s.parse(p)
            _ = s.read_line()
            s.tooltip(p, 12, 9)
            response = s.read_line()
            self.assertEqual(response['Kind'], 'tooltip')
        finally:
            s.stop()

    def testCanFindDeclaration(self):
        try:
            cmd, args = self.cmd_line
            s = Server(cmd, *args)
            s.start()
            p = os.path.join(DATA_DIR, 'FileTwo.fs')
            p2 = os.path.join(DATA_DIR, 'Script.fsx')
            p3 = os.path.join(DATA_DIR, 'Program.fs')
            s.project(p)
            _ = s.read_line()
            s.parse(p)
            _ = s.read_line()
            s.parse(p2)
            _ = s.read_line()
            s.parse(p3)
            _ = s.read_line()
            s.find_declaration(p3, 5, 15)
            response = s.read_line()
            self.assertEqual(response['Kind'], 'finddecl')
        finally:
            s.stop()
