# Copyright (c) 2014, Guillermo López-Anglada. Please see the AUTHORS file for details.
# All rights reserved. Use of this source code is governed by a BSD-style
# license that can be found in the LICENSE file.)

import sublime

import os

from .sublime import after


class OutputPanel(object):
    """Manages an ST output panel.

    Can be used as a file-like object.
    """

    def __init__(self, name,
                 base_dir=None,
                 syntax='Packages/Text/Plain text.tmLanguage',
                 **kwargs):
        """
        @name
          This panel's name.
        @base_dir
          Directory used to look files matched by regular expressions.
        @syntax:
          This panel's syntax.
        @kwargs
          Any number of settings to set in the underlying view via `.set()`.

          Common settings:
            - result_file_regex
            - result_line_regex
            - word_wrap
            - line_numbers
            - gutter
            - scroll_past_end
        """

        self.name = name
        self.window = sublime.active_window()

        if not hasattr(self, 'view'):
            # Try not to call get_output_panel until the regexes are assigned
            self.view = self.window.create_output_panel(self.name)

        # Default to the current file directory
        if (not base_dir and
                self.window.active_view() and
                self.window.active_view().file_name()):
            base_dir = os.path.dirname(self.window.active_view().file_name())

        self.set('result_base_dir', base_dir)
        self.set('syntax', syntax)

        self.set('result_file_regex', '')
        self.set('result_line_regex', '')
        self.set('word_wrap', False)
        self.set('line_numbers', False)
        self.set('gutter', False)
        self.set('scroll_past_end', False)

    def set(self, name, value):
        self.view.settings().set(name, value)

    def _clean_text(self, text):
        return text.replace('\r', '')

    def write(self, text):
        assert isinstance(text, str), 'must pass decoded text data'
        text = self._clean_text(text)
        fun = lambda: self.view.run_command('append', {'characters': text, 'force': True, 'scroll_to_end': True})
        after(0, fun)

    def flush(self):
        pass

    def show(self):
        # Call create_output_panel a second time after assigning the above
        # settings, so that it'll be picked up as a result buffer
        self.window.create_output_panel(self.name)
        self.window.run_command('show_panel', {
            'panel': 'output.' + self.name})

    def close(self):
        pass


class ErrorPanel(object):
    def __init__(self):
        self.panel = OutputPanel('dart.info')
        self.panel.write('=' * 80)
        self.panel.write('\n')
        self.panel.write("Dart - Something's not quite right\n")
        self.panel.write('=' * 80)
        self.panel.write('\n')
        self.panel.write('\n')

    def write(self, text):
        self.panel.write(text)

    def show(self):
        self.panel.show()
