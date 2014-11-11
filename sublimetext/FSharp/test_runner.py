# Copyright (c) 2014, Guillermo López-Anglada. Please see the AUTHORS file for details.
# All rights reserved. Use of this source code is governed by a BSD-style
# license that can be found in the LICENSE file.)

import sublime
import sublime_plugin

import os
import unittest
import contextlib
import threading


from FSharp.sublime_plugin_lib.panels import OutputPanel


class RunFsharpTests(sublime_plugin.WindowCommand):
    '''Runs tests and displays the result.

    - Do not use ST while tests are running.

    @working_dir
      Required. Should be the parent of the top-level directory for `tests`.

    @loader_pattern
      Optional. Only run tests matching this glob.

    @active_file_only
      Optional. Only run tests in the active file in ST. Shadows
      @loader_pattern.

    To use this runner conveniently, open the command palette and select one
    of the `Build: Dart - Test *` commands.
    '''
    @contextlib.contextmanager
    def chdir(self, path=None):
        old_path = os.getcwd()
        if path is not None:
            assert os.path.exists(path), "'path' is invalid {}".format(path)
            os.chdir(path)
        yield
        if path is not None:
            os.chdir(old_path)

    def run(self, **kwargs):
        with self.chdir(kwargs.get('working_dir')):
            p = os.path.join(os.getcwd(), 'tests')
            patt = kwargs.get('loader_pattern', 'test*.py',)
            # TODO(guillermooo): I can't get $file to expand in the build
            # system. It should be possible to make the following code simpler
            # with it.
            if kwargs.get('active_file_only') is True:
                patt = os.path.basename(self.window.active_view().file_name())
            suite = unittest.TestLoader().discover(p, pattern=patt)

            file_regex = r'^\s*File\s*"([^.].*?)",\s*line\s*(\d+),.*$'
            display = OutputPanel('fs.tests', file_regex=file_regex)
            display.show()
            runner = unittest.TextTestRunner(stream=display, verbosity=1)

            def run_and_display():
                runner.run(suite)

            threading.Thread(target=run_and_display).start()