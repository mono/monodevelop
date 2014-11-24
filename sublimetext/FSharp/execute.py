# Copyright (c) 2014, Guillermo López-Anglada. Please see the AUTHORS file for details.
# All rights reserved. Use of this source code is governed by a BSD-style
# license that can be found in the LICENSE file.)

'''Our own ST `exec` command.

Mostly lifted from Default.exec.py
'''

import sublime
import sublime_plugin

import os
import functools
import time

from Default.exec import ProcessListener
from Default.exec import AsyncProcess
from FSharp.sublime_plugin_lib.sublime import after

from FSharp.sublime_plugin_lib.panels import OutputPanel


class fs_exec(sublime_plugin.WindowCommand, ProcessListener):
    def run(self,
            cmd=None,
            shell_cmd=None,
            file_regex="",
            line_regex="",
            working_dir="",
            encoding="utf-8",
            env={},
            quiet=False,
            kill=False,
            word_wrap=True,
            syntax="Packages/Text/Plain text.tmLanguage",
            preamble='',
            panel_name='fs.out',
            # Catches "path" and "shell"
            **kwargs):

        if kill:
            if hasattr(self, 'proc') and self.proc:
                self.proc.kill()
                self.proc = None
                self.append_string(None, "[Cancelled]")
            return

        # TODO(guillermooo): We cannot have multiple processes running at the
        # same time, or processes that use separate output panels.
        if not hasattr(self, 'out_panel'):
            # Try not to call get_output_panel until the regexes are assigned
            self.out_panel = OutputPanel(panel_name)

        # Default to the current files directory if no working directory was given
        if (not working_dir and
            self.window.active_view() and
            self.window.active_view().file_name()):
                working_dir = os.path.dirname(
                                        self.window.active_view().file_name())

        self.out_panel.set("result_file_regex", file_regex)
        self.out_panel.set("result_line_regex", line_regex)
        self.out_panel.set("result_base_dir", working_dir)
        self.out_panel.set("word_wrap", word_wrap)
        self.out_panel.set("line_numbers", False)
        self.out_panel.set("gutter", False)
        self.out_panel.set("scroll_past_end", False)
        self.out_panel.view.assign_syntax(syntax)

        self.encoding = encoding
        self.quiet = quiet

        self.proc = None
        if not self.quiet:
            if shell_cmd:
                print("Running " + shell_cmd)
            else:
                print("Running " + " ".join(cmd))
            sublime.status_message("Building")

        if preamble:
            self.append_string(self.proc, preamble)

        show_panel_on_build = sublime.load_settings(
              "Dart - Plugin Settings.sublime-settings").get("show_panel_on_build", True)
        if show_panel_on_build:
            self.out_panel.show()

        merged_env = env.copy()
        if self.window.active_view():
            user_env = self.window.active_view().settings().get('build_env')
            if user_env:
                merged_env.update(user_env)

        # Change to the working dir, rather than spawning the process with it,
        # so that emitted working dir relative path names make sense
        if working_dir:
            os.chdir(working_dir)

        self.debug_text = ""
        if shell_cmd:
            self.debug_text += "[shell_cmd: " + shell_cmd + "]\n"
        else:
            self.debug_text += "[cmd: " + str(cmd) + "]\n"
        self.debug_text += "[dir: " + str(os.getcwd()) + "]\n"
        if "PATH" in merged_env:
            self.debug_text += "[path: " + str(merged_env["PATH"]) + "]"
        else:
            self.debug_text += "[path: " + str(os.environ["PATH"]) + "]"

        try:
            # Forward kwargs to AsyncProcess
            self.proc = AsyncProcess(cmd, shell_cmd, merged_env, self,
                                     **kwargs)
        except Exception as e:
            self.append_string(None, str(e) + "\n")
            self.append_string(None, self.debug_text + "\n")
            if not self.quiet:
                self.append_string(None, "[Finished]")

    def append_data(self, proc, data):
        if proc != self.proc:
            # a second call to exec has been made before the first one
            # finished, ignore it instead of intermingling the output.
            if proc:
                proc.kill()
            return

        try:
            str_ = data.decode(self.encoding)
        except UnicodeEncodeError:
            str_ = "[Decode error - output not " + self.encoding + "]\n"
            proc = None
            return

        # Normalize newlines, Sublime Text always uses a single \n separator
        # in memory.
        str_ = str_.replace('\r\n', '\n').replace('\r', '\n')

        self.out_panel.write(str_)

    def append_string(self, proc, str):
        self.append_data(proc, str.encode(self.encoding))

    def finish(self, proc):
        if not self.quiet:
            elapsed = time.time() - proc.start_time
            exit_code = proc.exit_code()
            if (exit_code == 0) or (exit_code == None):
                self.append_string(proc, "[Finished in %.1fs]" % (elapsed))
            else:
                self.append_string(proc,
                    "[Finished in %.1fs with exit code %d]\n" %
                                                        (elapsed, exit_code))
                self.append_string(proc, self.debug_text)

        if proc != self.proc:
            return

        # XXX: What's this for?
        errs = self.out_panel.view.find_all_results()
        if len(errs) == 0:
            sublime.status_message("Build finished")
        else:
            sublime.status_message(("Build finished with %d errors") % len(errs))

    def on_data(self, proc, data):
        after(0, functools.partial(self.append_data, proc, data))

    def on_finished(self, proc):
        after(0, functools.partial(self.finish, proc))