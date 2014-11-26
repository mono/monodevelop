import contextlib
import glob
import os
import tempfile
import time
import unittest

import sublime

from FSharp.lib.project import find_fsproject
from FSharp.lib.project import FSharpFile
from FSharp.lib.project import FSharpProjectFile
from FSharp.sublime_plugin_lib.io import touch


@contextlib.contextmanager
def make_directories(dirs):
    tmp_dir = tempfile.TemporaryDirectory()
    current = tmp_dir.name
    for dd in dirs:
        for d in dd:
            current = os.path.join(current, d)
            os.mkdir(current)
        current = tmp_dir.name
    yield tmp_dir.name
    tmp_dir.cleanup()


class Test_find_fsproject(unittest.TestCase):
    def testCanFind(self):
        with make_directories([["foo", "bar", "baz"]]) as tmp_root:
            fs_proj_file = os.path.join(tmp_root, 'hey.fsproj')
            touch(fs_proj_file)
            found = find_fsproject (os.path.join (tmp_root, 'foo/bar/baz'))
            self.assertEquals(found, fs_proj_file)


class Test_FSharpProjectFile (unittest.TestCase):
    def testCanCreateFromPath(self):
        with tempfile.TemporaryDirectory () as tmp:
            f = os.path.join (tmp, 'foo.fsproj')
            touch (f)
            fs_project = FSharpProjectFile.from_path(f)
            self.assertEquals(fs_project.path, f)

    def testCanReturnParent(self):
        with tempfile.TemporaryDirectory () as tmp:
            f = os.path.join (tmp, 'foo.fsproj')
            touch (f)
            fs_project = FSharpProjectFile.from_path(f)
            self.assertEquals(fs_project.parent, tmp)

    def testCanBeCompared(self):
        with tempfile.TemporaryDirectory () as tmp:
            f = os.path.join (tmp, 'foo.fsproj')
            touch (f)
            fs_project_1 = FSharpProjectFile.from_path(f)
            fs_project_2 = FSharpProjectFile.from_path(f)
            self.assertEquals(fs_project_1, fs_project_2)

    def test_governs_SameLevel(self):
        with tempfile.TemporaryDirectory () as tmp:
            f = os.path.join (tmp, 'foo.fsproj')
            f2 = os.path.join (tmp, 'foo.fs')
            touch (f)
            touch (f2)
            fs_proj = FSharpProjectFile.from_path(f)
            self.assertTrue(fs_proj.governs (f2))


class Test_FSharpFile (unittest.TestCase):
    def setUp(self):
        self.win = sublime.active_window()

    def tearDown(self):
        self.win.run_command ('close')

    def testCanDetectCodeFile(self):
        with tempfile.TemporaryDirectory () as tmp:
            f = os.path.join (tmp, 'foo.fs')
            touch (f)
            v = self.win.open_file(f)
            time.sleep(0.01)
            fs_file = FSharpFile (v)
            self.assertTrue (fs_file.is_code_file)

    def testCanDetectScriptFile(self):
        with tempfile.TemporaryDirectory () as tmp:
            f = os.path.join (tmp, 'foo.fsx')
            touch (f)
            v = self.win.open_file(f)
            time.sleep(0.01)
            fs_file = FSharpFile (v)
            self.assertTrue (fs_file.is_script_file)

    def testCanDetectCodeForCodeFile(self):
        with tempfile.TemporaryDirectory () as tmp:
            f = os.path.join (tmp, 'foo.fs')
            touch (f)
            v = self.win.open_file(f)
            time.sleep(0.01)
            fs_file = FSharpFile (v)
            self.assertTrue (fs_file.is_code)

    def testCanDetectCodeForScriptFile(self):
        with tempfile.TemporaryDirectory () as tmp:
            f = os.path.join (tmp, 'foo.fsx')
            touch (f)
            v = self.win.open_file(f)
            time.sleep(0.01)
            fs_file = FSharpFile (v)
            self.assertTrue (fs_file.is_code)

    def testCanDetectProjectFile(self):
        with tempfile.TemporaryDirectory () as tmp:
            f = os.path.join (tmp, 'foo.fsproj')
            touch (f)
            v = self.win.open_file(f)
            time.sleep(0.01)
            fs_file = FSharpFile (v)
            self.assertTrue (fs_file.is_project_file)
