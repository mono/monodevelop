import unittest
import os

from FSharp.fsac.server import Server
from FSharp import const

import sublime


THIS_DIR = os.path.dirname(os.path.dirname(__file__))
DATA_DIR = os.path.join(sublime.packages_path(), 'FSharp_Tests/data')


class ServerTests(unittest.TestCase):
    def testCanInstantiate(self):
        try:
            s = Server(const.path_to_fs_ac_binary())
            self.assertEqual(None, s.proc)
        finally:
            s.stop()

    def testCanStart(self):
        try:
            s = Server(const.path_to_fs_ac_binary())
            s.start()
            self.assertTrue(s.proc.stdin)
        finally:
            s.stop()

    def testCanGetHelp(self):
        try:
            s = Server(const.path_to_fs_ac_binary())
            s.start()
            s.help()
            text = s._read_all()
            self.assertEqual(text.strip()[:len('Supported')], 'Supported')
        finally:
            s.stop()

    def testCanSetProject(self):
        try:
            s = Server(const.path_to_fs_ac_binary())
            s.start()
            p = os.path.join(DATA_DIR, 'FindDecl.fsproj')
            self.assertTrue(os.path.exists(p))
            s.project(p)
            response = s._read()
            self.assertEqual(response['Kind'], 'project')
        finally:
            s.stop()

    # def testCanParseFile(self):
    #     try:
    #         s = Server(const.path_to_fs_ac_binary())
    #         s.start()
    #         response = s.parse('./tests/data/FileTwo.fs')
    #         # XXX: Why in all caps?
    #         self.assertEqual(response['Kind'], 'INFO')
    #     finally:
    #         s.stop()

    # def testCanRetrieveErrors(self):
    #     try:
    #         s = Server(const.path_to_fs_ac_binary())
    #         s.start()
    #         response = s.parse('./tests/data/FileTwo.fs')
    #         response = s.errors()
    #         self.assertEqual(response['Kind'], 'errors')
    #     finally:
    #         s.stop()

    # def testCanRetrieveDeclarations(self):
    #     try:
    #         s = Server(const.path_to_fs_ac_binary())
    #         s.start()
    #         response = s.parse('./tests/data/FileTwo.fs')
    #         response = s.declarations('./tests/data/FileTwo.fs')
    #         self.assertEqual(response['Kind'], 'declarations')
    #     finally:
    #         s.stop()

    # def testCanRetrieveCompletions(self):
    #     try:
    #         s = Server(const.path_to_fs_ac_binary())
    #         s.start()
    #         response = s.parse('./tests/data/FileTwo.fs')
    #         helptext, completions = s.completions('./tests/data/FileTwo.fs', 12, 9)
    #         self.assertEqual(helptext['Kind'], 'helptext')
    #         self.assertEqual(completions['Kind'], 'completion')
    #     finally:
    #         s.stop()

    # def testCanRetrieveTooltip(self):
    #     try:
    #         s = Server(const.path_to_fs_ac_binary())
    #         s.start()
    #         response = s.parse('./tests/data/FileTwo.fs')
    #         response = s.tooltip('./tests/data/FileTwo.fs', 12, 9)
    #         self.assertEqual(response['Kind'], 'tooltip')
    #     finally:
    #         s.stop()

    # def testCanFindDeclaration(self):
    #     try:
    #         s = Server(const.path_to_fs_ac_binary())
    #         s.start()
    #         s.project('./tests/data/FindDecl.fsproj')
    #         s.parse('./tests/data/FileTwo.fs')
    #         s.parse('./tests/data/Script.fsx')
    #         s.parse('./tests/data/Program.fs')
    #         response = s.find_declaration('./tests/data/Program.fs', 5, 15)
    #         self.assertEqual(response['Kind'], 'finddecl')
    #     finally:
    #         s.stop()
