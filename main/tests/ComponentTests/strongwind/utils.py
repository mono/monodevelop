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

'Utility functions'

import os
import re
from time import sleep

import pyatspi

import config
import errors
import cache

def getBasenameWithoutExtension(path):
    'Takes a path like "/tmp/testscript.py" and returns "testscript"'

    dir, file = os.path.split(path)
    base, ext = os.path.splitext(file)

    return base

def toClassName(str):
    'Takes a string like "push button" or "Patient Select" and returns "PushButton" or "PatientSelect"'

    regex = re.compile('^[0-9]')

    if str.isalnum():
        name = str.capitalize()
    else:
        name = ''
        for word in re.split('[^a-zA-Z0-9]', str):
            name = name + word.capitalize()

    if regex.search(name): name = '_' + name
    return name

def toVarName(str):
    'Takes a string like "push button" or "Patient Select" and returns "pushButton" or "patientSelect"'

    regex = re.compile('^[0-9]')

    if str.isalnum():
        name = str.lower()
    else:
        words = re.split('[^a-zA-Z0-9]', str)

        name = words[0].lower()
        for word in words[1:]:
            name = name + word.capitalize()

    if regex.search(name): name = '_' + name
    return name

def toConstantName(str):
    'Takes a string like "pushButton" and returns "PUSH_BUTTON"'

    name = ''
    for c in str:
        if c.isupper():
            name += '_'
        name += c.upper()

    return name

def equalsOrMatches(str, strOrRegex):
    'Returns True if str is equal to or matches strOrRegex, or if strOrRegex is None'

    if strOrRegex is None:
        return True

    if type(strOrRegex) is type(re.compile('r')):
        return strOrRegex.search(str)

    return str == strOrRegex

def retryUntilTrue(func, args=[], kargs={}):
    'Executes func until either func returns true or the maximum number of tries is exceeded'

    for i in xrange(config.RETRY_TIMES):
        if func(*args,**kargs): return True
        sleep(config.RETRY_INTERVAL)
    return False

def findDescendant(acc, pred, retry=True, recursive=True, breadthFirst=True, raiseException=True):
    '''
    Returns the first descendant of acc matching pred 

    If multiple descendants match the predicate, which descendant is returned 
    depends on the search order, which can be specified by setting breadthFirst 
    to False. 

    If no descendants matching the predicate is found and raiseException is True, 
    a SearchError execption is raised.  If raiseException is False, None is
    returned.
    '''

    tries = (1, config.RETRY_TIMES)[retry]
    for i in xrange(tries):
        if recursive:
            if breadthFirst:
                ret = pyatspi.utils._findDescendantBreadth(acc, pred)
                if ret is not None: return ret
            else:
                for child in acc:
                    try:
                        ret = pyatspi.utils._findDescendantDepth(child, pred)
                    except Exception:
                        ret = None
                        #raise
                    if ret is not None: return ret
        else:
            for child in acc:
                try:
                    if pred(child): return child
                except Exception:
                    continue
                    #raise
        if tries > 1:
            sleep(config.RETRY_INTERVAL)

    if raiseException:
        raise errors.SearchError

    return None

def findAllDescendants(acc, pred, recursive=True):
    'Returns all descendants of acc matching pred'

    matches = []
    for child in acc:
        if pred(child):
            matches.append(child)
        if recursive:
            matches = matches + findAllDescendants(child, pred, recursive)
    return matches

def takeScreenshot(path):
    'Takes a screenshot of the desktop'

    import os.path
    import gtk.gdk
    import gobject

    # pause before taking screenshots, otherwise we get half-drawn widgets
    sleep(config.SCREENSHOT_DELAY)

    assert os.path.isdir(os.path.dirname(path))

    fileExt = os.path.splitext(path)[1][1:]

    rootWindow = gtk.gdk.get_default_root_window()
    geometry = rootWindow.get_geometry()
    pixbuf = gtk.gdk.Pixbuf(gtk.gdk.COLORSPACE_RGB, False, 8, geometry[2], geometry[3])
    gtk.gdk.Pixbuf.get_from_drawable(pixbuf, rootWindow, rootWindow.get_colormap(), 0, 0, 0, 0, geometry[2], geometry[3])

    # gtk.gdk.Pixbuf.save() needs 'jpeg' and not 'jpg'
    if fileExt == 'jpg': fileExt = 'jpeg'

    try:
        pixbuf.save(path, fileExt)
    except gobject.GError:
        raise ValueError, "Failed to save screenshot in %s format" % fileExt

    assert os.path.exists(path)

