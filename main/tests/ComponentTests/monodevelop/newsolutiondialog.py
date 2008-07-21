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
from mdstrings import *

class NewSolutionDialog(accessibles.Dialog):
    def __init__(self, accessible):
        super(NewSolutionDialog, self).__init__(accessible)
        self._item = "_item_"
        self.table = self.findTreeTable('')
        self.cells = {}
        for i in range(self.table.childCount):
            curCell = self.table[i]
            curName = curCell.name
            if (len(curCell._accessible.getRelationSet()) > 0):
                curParentName = curCell._accessible.getRelationSet()[0].getTarget(0).name
                if (len(curParentName) == 0):
                    self.cells[curName] = { self._item:curCell }
                else:
                    if (not self.cells.has_key(curParentName)):
                        self.cells[curParentName] = None
                    if (self.cells[curParentName] == None):
                        self.cells[curParentName] = {}
                    self.cells[curParentName][curName] =  { self._item:curCell }

    def selectCategory(self, list):
        curDict = self.cells
        for curKey in list:
            curDict = curDict[curKey]
        curCell = curDict[self._item]
        if (not curCell.selected):
            curCell.select()

    def selectProject(self, list):
        self.selectCategory(list[:-1])
        project = list[-1]
        pane = self.findLayeredPane('')
        projectSelected = False
        for i in range(pane.childCount):
            print "%d. %s" % (i, pane[i].name)
            if (pane[i].name == project):
                pane.selectChild(i)
                projectSelected = True
        if (projectSelected == False):
            raise Exception, "Project [%s] Not Found" % (project)

    def expandCategory(self, category):
        cell = self.cells[category]["_item_"]
        if (not cell.expanded):
            cell._doAction('expand or contract')
            sleep(config.SHORT_DELAY)

    def contractCategory(self, category):
        cell = self.cells[category]["_item_"]
        if (cell.expanded):
            cell._doAction('expand or contract')
            sleep(config.SHORT_DELAY)

    def expandAllCategories(self):
        self.expandCategory(categoryC)
        self.expandCategory(categoryCs)
        self.expandCategory(categoryVb)

    def contractAllCategories(self):
        self.contractCategory(categoryC)
        self.contractCategory(categoryCs)
        self.contractCategory(categoryVb)

    def setProjectName(self, name):
        self.keyCombo('<Alt>a')
        self.typeText(name)

    def setProjectLocation(self, name):
        # For some reason, <Alt>l doesn't work, 
        # even when done manually on this dialog
        self.keyCombo('<Alt>a')
        self.keyCombo('<Tab>')
        self.typeText(name)

    def setSolutionName(self, name):
        self.keyCombo('<Alt>s')
        self.typeText(name)

    def pushButton(self, name):
        btn = self.findPushButton(name)
        btn.click()


    # Extra Test Methods
    def _selectEachCategory(self):
        self.expandAllCategories()
        sleep(config.SHORT_DELAY)
        self.selectCategory([categoryC])
        sleep(config.SHORT_DELAY)
        self.selectCategory([categoryC,categoryCpp])
        sleep(config.SHORT_DELAY)
        self.selectCategory([categoryCs])
        sleep(config.SHORT_DELAY)
        self.selectCategory([categoryCs,categoryAsp])
        sleep(config.SHORT_DELAY)
        self.selectCategory([categoryCs,categoryMoonlight])
        sleep(config.SHORT_DELAY)
        self.selectCategory([categoryCs,categoryNunit])
        sleep(config.SHORT_DELAY)
        self.selectCategory([categoryCs,categorySamples])
        sleep(config.SHORT_DELAY)
        self.selectCategory([categoryIl])
        sleep(config.SHORT_DELAY)
        self.selectCategory([categoryMd])
        sleep(config.SHORT_DELAY)
        self.selectCategory([categoryNunit])
        sleep(config.SHORT_DELAY)
        self.selectCategory([categoryPackaging])
        sleep(config.SHORT_DELAY)
        self.selectCategory([categoryVb])
        sleep(config.SHORT_DELAY)
        self.selectCategory([categoryVb,categoryAsp])
        sleep(config.SHORT_DELAY)
        self.selectCategory([categoryVb,categoryMoonlight])
        sleep(config.SHORT_DELAY)
        self.selectCategory([categoryVb,categoryNunit])
