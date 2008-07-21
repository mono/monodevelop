# -*- coding: utf-8 -*-
#
# Author:
#   Thomas Wiest <twiest@novell.com>
#
# Copyright (c) 2008 Novell, Inc (http://www.novell.com)
#
# Permission is hereby granted, free of charge, to any person obtaining a copy
# of this software and associated documentation files (the "Software"), to deal
# in the Software without restriction, including without limitation the rights
# to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
# copies of the Software, and to permit persons to whom the Software is
# furnished to do so, subject to the following conditions:
#
# The above copyright notice and this permission notice shall be included in
# all copies or substantial portions of the Software.
#
# THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
# IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
# FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
# AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
# LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
# OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
# THE SOFTWARE.


from strongwind import *
from monodevelop import *
from mdstrings import *
import random
import re


class MonodevelopFrame(accessibles.Frame):
    logName = 'Monodevelop'

    def __init__(self, accessible):
        super(MonodevelopFrame, self).__init__(accessible)

        # get a reference to commonly-used child widgets
        self.menuBar = self.findMenuBar('')

    def getRandomNumber(self):
        return str(random.randint(1,99999))

    def quit(self):
        'Quit MonoDevelop'
        self.menuBar.select([menuFile, menuOptionQuit])
        self.assertClosed()

    def _openFileOpenDialog(self):
        self.menuBar.select([menuFile, menuOptionOpen])
        return self.app.findDialog(None, logName='File to Open')

    def openFile(self, filename):
        dialog = self._openFileOpenDialog()
        toggle = dialog.findToggleButton("Type a file name")
        if (not toggle.checked):
            toggle.click()
        dialog.keyCombo('<Alt>l')
        dialog.typeText(filename)
        dialog.findPushButton("Open").click()
        #sleep(config.MEDIUM_DELAY)

    def buildSolution(self):
        self.menuBar.select([menuBuild, menuOptionBuildSolution])
        sleep(config.MEDIUM_DELAY)

    def assertClosed(self):
        'Assert that MD is closed'
        super(MonodevelopFrame, self).assertClosed()
        # if the MonoDevelop window closes, the entire app should close.
        # assert that this is true
        self.app.assertClosed()

    def assertBuildSucceeded(self):
        ico = self.findIcon('md-build-combine')
        ico.mouseClick()
        bld = self.findText('')
        matchStr = '.*Build successful\..*'
        procedurelogger.expectedResult('%s contains %s.' % (bld.text, matchStr))
        p = re.compile(matchStr)
        match = p.search(bld.text)

        def resultMatches():
           if match:
               return True
           else:
               return False
        assert retryUntilTrue(resultMatches)

    def _openFileNewSolutionDialog(self):
        self.menuBar.select([menuFile, menuOptionNew, menuOptionSolution])
        sleep(config.MEDIUM_DELAY)
        return self.app.findDialog('New Solution', logName='New Solution')

    def _createNewSolution(self, list, name):
        dialog = self._openFileNewSolutionDialog()
        dialog.expandAllCategories()
        dialog.selectProject(list)
        dialog.setProjectName(name)
        return dialog

    def _createNewCsSolution(self, list, name):
        dialog = self._createNewSolution(list, name)
        dialog.pushButton("Forward")
        sleep(config.MEDIUM_DELAY)
        dialog.pushButton("OK")
        sleep(config.MEDIUM_DELAY)

    def _createNewCSolution(self, list, name):
        dialog = self._createNewSolution(list, name)
        dialog.pushButton("OK")
        sleep(config.MEDIUM_DELAY)

    def createNewCEmptyProjectSolution(self, name):
        self._createNewCSolution([categoryC, projectEmptyC], name)

    def createNewCSharedLibraryProjectSolution(self, name):
        self._createNewCSolution([categoryC, projectSharedLibrary], name)

    def createNewCStaticLibraryProjectSolution(self, name):
        self._createNewCSolution([categoryC, projectStaticLibrary], name)

    def createNewCConsoleProjectSolution(self, name):
        self._createNewCSolution([categoryC, projectConsole], name)

    def createNewCppEmptyProjectSolution(self, name):
        self._createNewCSolution([categoryC, categoryCpp, projectEmptyCpp], name)

    def createNewCppSharedLibraryProjectSolution(self, name):
        self._createNewCSolution([categoryC, categoryCpp, projectSharedLibrary], name)

    def createNewCppStaticLibraryProjectSolution(self, name):
        self._createNewCSolution([categoryC, categoryCpp, projectStaticLibrary], name)

    def createNewCppConsoleProjectSolution(self, name):
        self._createNewCSolution([categoryC, categoryCpp, projectConsole], name)

    def createNewCsConsoleProjectSolution(self, name):
        self._createNewCsSolution([categoryCs, projectConsole], name)

    def createNewCsEmptyProjectSolution(self, name):
        self._createNewCsSolution([categoryCs, projectEmpty], name)

    def createNewCsGtkProjectSolution(self, name):
        self._createNewCsSolution([categoryCs, projectGtk], name)

    def createNewCsLibraryProjectSolution(self, name):
        self._createNewCsSolution([categoryCs, projectLibrary], name)

    def createNewCsAspWebApplicationProjectSolution(self, name):
        self._createNewCsSolution([categoryCs, categoryAsp, projectWebApplication], name)

    def createNewCsAspEmptyWebApplicationProjectSolution(self, name):
        self._createNewCsSolution([categoryCs, categoryAsp, projectEmptyWebApplication], name)

    def createNewCsMoonlightProjectSolution(self, name):
        self._createNewCsSolution([categoryCs, categoryMoonlight, projectMoonlight], name)

    def createNewCsEmptyMoonlightProjectSolution(self, name):
        self._createNewCsSolution([categoryCs, categoryMoonlight, projectEmptyMoonlight], name)
