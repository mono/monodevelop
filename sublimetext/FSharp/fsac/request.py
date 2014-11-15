# Copyright (c) 2014, Guillermo López-Anglada. Please see the AUTHORS file for details.
# All rights reserved. Use of this source code is governed by a BSD-style
# license that can be found in the LICENSE file.)


class Request (object):
    def __init__(self, timeout=250, add_newline=True):
        self.add_newline = add_newline
        self.timeout = timeout

    def encode(self):
        data = str(self)
        if self.add_newline:
            data += '\n'
        return data.encode ('utf-8')


class CompilerLocationRequest(Request):
    def __init__(self, *args, **kwargs):
        super ().__init__ (*args, **kwargs)

    def __str__(self):
        return 'compilerlocation'


class ProjectRequest(Request):
    def __init__(self, project_file, *args, **kwargs):
        super ().__init__ (*args, **kwargs)
        self.project_file = project_file

    def __str__(self):
        return 'project "{0}"'.format(self.project_file)


class ParseRequest(Request):
    def __init__(self, file_name, content='', full=True, *args, **kwargs):
        super ().__init__ (*args, add_newline=False, **kwargs)
        self.file_name = file_name
        self.content = content
        self.full = full

    def __str__(self):
        cmd = 'parse "{0}"'.format(self.file_name)
        if self.full:
            cmd += ' full'
        cmd += '\n'
        cmd += self.content + '\n<<EOF>>\n'
        return cmd


class FindDeclRequest(Request):
    def __init__(self, file_name, row, col, *args, **kwargs):
        super().__init__ (*args, **kwargs)
        self.file_name = file_name
        self.row = row
        self.col = col

    def __str__(self):
        return 'finddecl "{0}" {1} {2} {3}'.format(
            self.file_name,
            self.row,
            self.col,
            self.timeout,
            )


class CompletionRequest(Request):
    def __init__(self, file_name, row, col, *args, **kwargs):
        super().__init__ (*args, **kwargs)
        self.file_name = file_name
        self.row = row
        self.col = col

    def __str__(self):
        return 'completion "{0}" {1} {2} {3}'.format(
            self.file_name,
            self.row,
            self.col,
            self.timeout,
            )


class TooltipRequest(Request):
    def __init__(self, file_name, row, col, *args, **kwargs):
        super().__init__ (*args, **kwargs)
        self.file_name = file_name
        self.row = row
        self.col = col

    def __str__(self):
        return 'tooltip "{0}" {1} {2} {3}'.format(
            self.file_name,
            self.row,
            self.col,
            self.timeout,
            )


class DeclarationsRequest(Request):
    def __init__(self, file_name, *args, **kwargs):
        super ().__init__ (*args, **kwargs)
        self.file_name = file_name

    def __str__(self):
        return 'declarations "{0}"'.format(self.file_name)


class DataRequest(Request):
    def __init__(self, *args, content='', add_newline=False, **kwargs):
        super ().__init__ (*args, add_newline=add_newline, **kwargs)
        self.content = content

    def __str__(self):
        return self.content


class AdHocRequest (Request):
    def __init__(self, content, *args, **kwargs):
        super ().__init__ (*args, **kwargs)
        self.content = content

    def __str__(self):
        return self.content

