import sublime
import sublime_plugin

import os
import io
import unittest


class PrintTestResults(sublime_plugin.TextCommand):
    def run(self, edit, content):
        view = sublime.active_window().new_file()
        view.insert(edit, 0, content)
        view.set_scratch(True)


class RunFsharpTests(sublime_plugin.WindowCommand):
    def run(self):
        path_to_tests = os.path.join(sublime.packages_path(), 'FSharp_Tests')
        tests = unittest.TestLoader().discover(path_to_tests)

        # TODO: Print results as they become available.
        bucket = io.StringIO()
        unittest.TextTestRunner(stream=bucket, verbosity=1).run(tests)

        # TODO: Print to an output panel instead.
        self.window.run_command('print_test_results', {'content': bucket.getvalue()})

        # XXX: Is this needed still?
        # Hack to return focus to the results view.
        self.window.run_command('show_panel', {'panel': 'console', 'toggle': True})
        self.window.run_command('show_panel', {'panel': 'console', 'toggle': True})
