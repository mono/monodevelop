# -*- coding: utf-8 -*-
#
# Strongwind
# Copyright (C) 2007 Medsphere Systems Corporation
# 
# This program is free software; you can redistribute it and/or modify it under
# the terms of the GNU General Public License version 2 as published by the
# Free Software Foundation.
# 
# This program is distributed in the hope that it will be useful, but WITHOUT
# ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
# FOR A PARTICULAR PURPOSE.  See the GNU General Public License for more
# details.
# 
# You should have received a copy of the GNU General Public License along with
# this program; if not, write to the Free Software Foundation, Inc.,
# 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
# 

'''
Log test procedures in a human-readable format

The basic pattern for a strongwind test is to do the following:

    procedurelogger.action('Do something.')
    app.widget.findOtherWidget('Other widget').doAction('something')

    procedurelogger.expectedResult('The application reacts to the action.')
    app.widget.assertWidgetReacted()

Notice that calls to the procedure logger occur *before* actually performing
the actions.  This is intentional - if an exception is encountered performing
the action, or asserting the result, the resulting log will show the attempted
action/result, then the error that occurred.

In many cases, the call to action() is done by the widgets themselves or the
application wrapper, so action() calls are not always necessary.  For example, 
a push button's click() method will automatically log the click.  Also, some 
assert methods will automatically log the expected result, so expectedResult()
calls are not always necessary, either.
'''

import os
import sys

try:
    import yaml
    gotYaml = True
except ImportError:
    print 'Error importing yaml module; tags will not be parsed'
    gotYaml = False

import atexit
import traceback

try:
    import xml.etree.ElementTree as ET # python 2.5
except ImportError:
    try:
        import cElementTree as ET # cElementTree is faster
    except ImportError:
        import elementtree.ElementTree as ET # fallback on regular ElementTree

import datetime
import pyatspi

import config
import cache
import utils
import watchdog



_procedures = []
_actionBuffer = ''
_expectedResultBuffer = ''

_oldParents = []

# roles we want to search up the tree for
_containerParentRoles = [
    pyatspi.ROLE_APPLICATION,
    pyatspi.ROLE_FRAME,
    pyatspi.ROLE_ALERT,
    pyatspi.ROLE_DIALOG,
    pyatspi.ROLE_FILE_CHOOSER,
    pyatspi.ROLE_PAGE_TAB,
    pyatspi.ROLE_PANEL,
    pyatspi.ROLE_TOOL_BAR]

_windowLikeParentRoles = [
    pyatspi.ROLE_FRAME,
    pyatspi.ROLE_DIALOG]

# roles of child which can be set as parent but is not searched for up the tree
# only child is checked against this list
_childRoles = [
     pyatspi.ROLE_APPLICATION,
     pyatspi.ROLE_FRAME,
     pyatspi.ROLE_ALERT,
     pyatspi.ROLE_DIALOG,
     pyatspi.ROLE_FILE_CHOOSER,
     pyatspi.ROLE_PAGE_TAB,
     pyatspi.ROLE_COMBO_BOX,
     pyatspi.ROLE_TABLE,
     pyatspi.ROLE_TOOL_BAR]



def action(action, child=None):
    '''
    Log an action, e.g., Click Cancel

    If a child is given ... FIXME: decipher mysterious code below

    Multiple calls to action() (without a call to expectedResult() in between)
    will cause the message from each call to be concatenated to the message from
    previous calls.
    '''

    _flushBuffers()

    global _actionBuffer
    global _oldParents

    prefix = ''

    # TODO: benchmark the performance hit we receive from logparent using lists of parents
    if child is not None:
        def getValidParents(child, validRoles, checkParents=True):
                'Grab parents whose role is in validRoles'
        
                validParents = []
                current = child
                while current is not None:
                    if current.role in validRoles:
                        validParents.append(current)
                    if not checkParents:
                        return validParents
                    if current.role == pyatspi.ROLE_APPLICATION:
                        break
                    current = current.parent
                return validParents

        def isDifferent(new):
            'Relies on role and name to differentiate widgets'

            try:
                for old in _oldParents:
                    if old.role == new.role and old.name == new.name:
                        if new.role == pyatspi.ROLE_ALERT:
                            oldText = old.message
                            newText = new.message
                            if oldText == newText:
                                return False
                        else:
                            return False
            except (LookupError, KeyError, pyatspi.ORBit.CORBA.COMM_FAILURE):
                pass
            return True

        newParents = getValidParents(child, _childRoles, False) + getValidParents(child.parent, _containerParentRoles)
        application = None
        windowLike = None
        container = None

        #app
        if len(_oldParents) > 0:
            old = _oldParents[-1]
            new = newParents[-1]
            try:
                if old.id != new.id:
                    application = cache.getApplicationById(new.id)
            except (LookupError, KeyError, pyatspi.ORBit.CORBA.COMM_FAILURE):
                application = cache.getApplicationById(new.id)

        #container
        for x in newParents:
            if (container is None) and (x.role in _containerParentRoles) and isDifferent(x):
                if x.name != "":
                    container = x
                continue
            #windowLike
            #only execute this line if container is found
            if (container is not None) and (x.role in _windowLikeParentRoles) and isDifferent(x):
                windowLike = x

        if application is not None:
            prefix += "Switch to %s. " % application
        if container is not None:
            prefix += "In the %s" % container
            if windowLike is not None:
                prefix += " of the %s, " % windowLike
            else:
                prefix += ", "

            action = action[0].lower() + action[1:]

        _oldParents = newParents

    _actionBuffer += prefix + action + '  '
    print 'Action:', prefix + action

    # after each action, reset the watchdog timeout
    watchdog.resetTimeout()

def expectedResult(expectedResult):
    '''
    Log an expected result, e.g., The dialog closes

    Multiple calls to expectedResult() (without a call to action() in between)
    will cause the message from each call to be concatenated to the message from
    previous calls.
    '''

    global _expectedResultBuffer

    _expectedResultBuffer += expectedResult + '  '
    print 'Expected result:', expectedResult

def _flushBuffers():
    '''
    Append (_actionBuffer, _expectedResultBuffer) to the _procedures list, then reset _actionBuffer and _expectedResultBuffer

    After a call to expectedResult() and before the next call to action(),
    (after an action/expectedResult pair), we want to append the pair to the
    _procedures list and possibly take a screenshot.
    '''

    global _actionBuffer
    global _expectedResultBuffer

    if _actionBuffer and _expectedResultBuffer:
        if config.TAKE_SCREENSHOTS:
            filename = 'screen%02d.png' % (len(_procedures) + 1)
            utils.takeScreenshot(os.path.join(config.OUTPUT_DIR, filename))
            print 'Screenshot:', filename
            _procedures.append((_actionBuffer.rstrip(), _expectedResultBuffer.rstrip(), filename))
        else:
            _procedures.append((_actionBuffer.rstrip(), _expectedResultBuffer.rstrip()))

        _actionBuffer = ''
        _expectedResultBuffer = ''
        print ''

def save():
    'Save logged actions and expected results to an XML file'

    global _expectedResultBuffer

    try:
        _expectedResultBuffer += ''.join(traceback.format_exception(sys.last_type, sys.last_value, sys.last_traceback))
    except AttributeError:
        # sys.last_* may not be defined if there was no unhandled exception
        pass

    _flushBuffers()

    root = ET.Element('test')

    ET.SubElement(root, 'name').text = testName
    ET.SubElement(root, 'description').text = testDescription

    parameters = ET.SubElement(root, 'parameters')

    # FIXME: replace with a general YAML => XML function
    environments = ET.SubElement(parameters, 'environments')
    if testParameters.has_key('Environments'):
        for e in testParameters['Environments']:
            environment = ET.SubElement(environments, 'environment')
            for key, value in e.items():
                if type(value) == type(datetime.date(2000, 1, 1)):
                    value = value.ctime()
                ET.SubElement(environment, key.lower()).text = value

    procedures = ET.SubElement(root, 'procedures')
    for p in _procedures:
        step = ET.SubElement(procedures, 'step')
        ET.SubElement(step, 'action'        ).text = p[0]
        ET.SubElement(step, 'expectedResult').text = p[1]

        if config.TAKE_SCREENSHOTS:
            ET.SubElement(step, 'screenshot').text = p[2]

    assert os.path.isdir(config.OUTPUT_DIR)

    file = open(os.path.join(config.OUTPUT_DIR, 'procedures.xml'), 'w')
    file.write('<?xml version="1.0" encoding="UTF-8"?>')
    file.write('<?xml-stylesheet type="text/xsl" href="procedures.xsl"?>')
    ET.ElementTree(root).write(file)
    file.close()



def _getTestInfo():
    "Inspect the file being executed to determine the test's name, description, and parameters"

    name = ''

    try:
       name = utils.getBasenameWithoutExtension(sys.modules['__main__'].__file__)
    except AttributeError:
       pass

    desc = ''
    therest = None

    try:
        # try parsing the test script's docblock assuming it is in the form:
        # """
        # Test description
        #
        # YAML tags
        # """
        desc, therest = sys.modules['__main__'].__doc__.split('\n\n', 1)
        desc = desc.lstrip()
    except Exception:
        try:
            # try parsing the test script's docblock assuming it is in the form:
            # """Test description"""
            desc = sys.modules['__main__'].__doc__.lstrip()
        except Exception:
            pass

    if desc is None: # how does this happen?
        desc = ''

    parameters = {}

    if therest:
        try:
            parameters = yaml.load(therest)
        except NameError:
            pass

    return (name, desc, parameters)

testName, testDescription, testParameters = _getTestInfo()

if not os.path.isdir(config.OUTPUT_DIR):
    os.mkdir(config.OUTPUT_DIR)

if testName != '':
    print testName + ':', testDescription

# prevent reference errors at exit
def _cleanup():
    global _oldParents
    del _oldParents

atexit.register(_cleanup)
atexit.register(save)

