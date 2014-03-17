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

        bucket = OutputPanel('fsharp.tests')
        bucket.show()

        runner = unittest.TextTestRunner(stream=bucket, verbosity=1)

        sublime.set_timeout_async(lambda: runner.run(tests), 0)


class OutputPanel(object):
    def __init__(self, name, file_regex='', line_regex='', base_dir=None,
                 word_wrap=False, line_numbers=False, gutter=False,
                 scroll_past_end=False,
                 syntax='Packages/Text/Plain text.tmLanguage',
                 ):

        self.name = name
        self.window = sublime.active_window()

        if not hasattr(self, 'output_view'):
            # Try not to call get_output_panel until the regexes are assigned
            self.output_view = self.window.create_output_panel(self.name)

        # Default to the current file directory
        if (not base_dir and self.window.active_view() and
            self.window.active_view().file_name()):
                base_dir = os.path.dirname(
                        self.window.active_view().file_name()
                        )

        self.output_view.settings().set('result_file_regex', file_regex)
        self.output_view.settings().set('result_line_regex', line_regex)
        self.output_view.settings().set('result_base_dir', base_dir)
        self.output_view.settings().set('word_wrap', word_wrap)
        self.output_view.settings().set('line_numbers', line_numbers)
        self.output_view.settings().set('gutter', gutter)
        self.output_view.settings().set('scroll_past_end', scroll_past_end)
        self.output_view.settings().set('syntax', syntax)

        # Call create_output_panel a second time after assigning the above
        # settings, so that it'll be picked up as a result buffer
        self.window.create_output_panel('exec')

    def write(self, s):
        f = lambda: self.output_view.run_command('append', {'characters': s})
        # Call on the UI thread to make ST happy.
        sublime.set_timeout(f, 0)

    def flush(self):
        pass

    def show(self):
        self.window.run_command(
                            'show_panel', {'panel': 'output.' + self.name}
                            )

    def close(self):
        pass