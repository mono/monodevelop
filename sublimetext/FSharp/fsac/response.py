# Copyright (c) 2014, Guillermo López-Anglada. Please see the AUTHORS file for details.
# All rights reserved. Use of this source code is governed by a BSD-style
# license that can be found in the LICENSE file.)


class CompilerLocationResponse (object):
    def __init__(self, content):
        self.content = content

    @property
    def compilers_path(self):
       return self.content['Data']


class ProjectResponse (object):
    def __init__(self, content):
        self.content = content

    @property
    def files(self):
       return self.content['Data']['Files']

    @property
    def framework(self):
       return self.content ['Data']['Framework']

    @property
    def output(self):
       return self.content ['Data']['Output']

    @property
    def output(self):
       return self.content ['Data']['References']


class TopLevelDeclaration(object):
  def __init__(self, data):
    self.data = data

  @property
  def first_location(self):
    col = self.data['BodyRange']['Item1']['Column']
    row = self.data['BodyRange']['Item1']['Line']
    return (row - 1, col)

  @property
  def name(self):
    return self.data['Name']

  def __str__(self):
    return 'Declaration({0})<{1},{2}>'.format(self.name, *self.first_location)

  def to_menu_data(self):
    return [self.name, 'fs_go_to_location', {'loc': list(self.first_location)}]


class DeclarationsResponse(object):
    def __init__(self, data):
        self.data = data

    @property
    def declarations(self):
       for decl in self.data['Data'][0]['Nested']:
          yield TopLevelDeclaration (decl)

    def __str__(self):
       return '<DeclarationsList>'

