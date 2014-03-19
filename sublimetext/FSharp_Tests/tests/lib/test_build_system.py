import unittest
import os

from FSharp.build_system import locate_fsi


class Test_Fs_Helpers(unittest.TestCase):
    @unittest.skipUnless(os.name == 'nt', 'requires Windows')
    def test_can_detect_fs_project_file(self):
        self.assertTrue(os.path.exists(locate_fsi()))
        self.assertTrue(locate_fsi().endswith('fsi.exe'))

    @unittest.skipUnless(os.name != 'nt', 'requires mono')
    def test_can_detect_fs_project_file(self):
        self.assertTrue(os.path.exists(locate_fsi()))
        self.assertTrue(locate_fsi().endswith('fsharpi'))
