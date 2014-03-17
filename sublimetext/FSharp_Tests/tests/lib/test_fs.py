import unittest

from FSharp.lib import fs


class Test_Fs_Helpers(unittest.TestCase):
    def test_can_detect_fs_project_file(self):
        self.assertTrue(fs.is_fsharp_project('foo.fsproj'))

    def test_is_fsharp_project_can_fail(self):
        self.assertFalse(fs.is_fsharp_project('foo.txt'))

    def test_can_detect_fs_code_file(self):
        self.assertTrue(fs.is_fsharp_code('one.fs'))
        self.assertTrue(fs.is_fsharp_code('two.fsx'))
        self.assertTrue(fs.is_fsharp_code('three.fsi'))

    def test_can_detect_fs_script_file(self):
        self.assertTrue(fs.is_fsharp_script('foo.fsx'))
        self.assertTrue(fs.is_fsharp_script('foo.fsscript'))

    def test_is_fsharp_script_can_fail(self):
        self.assertFalse(fs.is_fsharp_script('foo.fs'))

    def test_is_fsharp_code_can_fail(self):
        self.assertFalse(fs.is_fsharp_code('three.txt'))

    def test_can_detect_fsharp_file(self):
        self.assertTrue(fs.is_fsharp_file('foo.fs'))
        self.assertTrue(fs.is_fsharp_file('foo.fsproj'))

    def test_is_fsharp_file_can_fail(self):
        self.assertFalse(fs.is_fsharp_file('foo.txt'))
