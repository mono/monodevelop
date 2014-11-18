# Copyright (c) 2014, Guillermo López-Anglada. Please see the AUTHORS file for details.
# All rights reserved. Use of this source code is governed by a BSD-style
# license that can be found in the LICENSE file.)

import os

from FSharp.sublime_plugin_lib.path import find_file_by_extension
from FSharp.sublime_plugin_lib.path import extension_equals


def find_fsproject (start):
    '''Find a .fsproject file starting at @start path.

    Returns the path to the file or `None` if not found.
    '''
    return find_file_by_extension(start, 'fsproj')


class FSharpFile (object):
    '''Inspects a file for interesting properties from the plugin's POV.
    '''
    def __init__(self, view_or_fname):
        """
        @view_or_fname
          A Sublime Text view or a file name.
        """
        assert view_or_fname, 'wrong arg: %s' % view_or_fname
        self.view_or_fname = view_or_fname

    @property
    def path(self):
        try:
            return self.view_or_fname.file_name()
        except AttributeError:
            return self.view_or_fname

    @property
    def is_fsharp_file(self):
        return any((self.is_code_file,
                   self.is_script_file,
                   self.is_project_file))

    @property
    def is_code(self):
        return (self.is_code_file or self.is_script_file)

    @property
    def is_code_file(self):
        return extension_equals(self.view_or_fname, '.fs')

    @property
    def is_script_file(self):
        return (extension_equals(self.view_or_fname, '.fsx') or
                extension_equals(self.view_or_fname, '.fsi'))

    @property
    def is_project_file(self):
        return extension_equals(self.view_or_fname, '.fsproj')


class FSharpProjectFile (object):
    def __init__(self, path):
        assert path.endswith('fsproj'), 'wrong fsproject path: %s' % path
        self.path = path
        self.parent = os.path.dirname (self.path)

    def __eq__(self, other):
        # todo: improve comparison
        return os.path.normpath(self.path) == os.path.normpath(other.path)

    def governs(self, fname):
        return fname.startswith(self.parent)

    @classmethod
    def from_path(cls, path):
        '''
        @path
          A path to a file or directory.
        '''
        fs_project = find_fsproject (path)
        if not fs_project:
            return None
        return FSharpProjectFile (fs_project)