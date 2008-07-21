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
Application and widget cache

Caches are typically used to increase performance, but the application and
widget caches in this module do not improve performance (in fact, there is a
slight performance penalty).

The reason we cache applications is so that we can resolve a single instance of
a strongwind.accessible.Application object (or subclass) associated with an
application id.

Consider the following example: we launch gcalctool, then immediately create an
instance of an application wrapper, gcalctool.Gcalctool, which extends
strongwind.accessibles.Application.  Later in the test, one of the widgets
calls its getApplication() method, hoping to receive an instance of the
galctool.Gcacltool, but instead receives a new instance of
strongwind.accessibles.Application.  Calls to methods and properties defined in
galctool.Gcacltool but not in strongwind.accessibles.Application will fail.

With the application cache, when we first create the instance of the
application wrapper, we cache that object.  Later, the widget can call
getApplication(), then look up the resulting
strongwind.accessibles.Application's id in the application cache to get
a handle to the cached gcalctool.Gcalctool object. 

The widget cache exists for similar reasons.
'''

import os
import pyatspi
import re
import subprocess
import weakref
import atexit

from time import sleep

import config
import errors
import accessibles
import procedurelogger
import utils

_desktop = accessibles.Desktop(pyatspi.Registry.getDesktop(0))
_applications = weakref.WeakValueDictionary()
_widgets = {}

def addWidget(widget):
    '''
    Add a strongwind.accessibles.Accessible (or a subclass) to the widget cache

    If a widget with the same key has already been added, a subsequent call to 
    this method will replace the previous widget in the cache.

    The key is comprised of the widget's role, the widget's name, and the hash
    of the widget's _accessible.  Since the widget's name is part of the key, 
    if a widget is re-used in an application but it changes names, the cache
    will treat the widget as a different widget.
    '''

    if not isinstance(widget, accessibles.Accessible):
        raise TypeError, "Cannot add %s instance to the widget cache" % widget.__class__.__name__

    key = (widget._accessible.getRole(), widget._accessible.name, hash(widget._accessible))

    if _widgets.has_key(key):
        del _widgets[key]

    _widgets[key] = widget

def getWidget(widget):
    '''
    Retrieve a widget from the widget cache

    If this method is called with a dead widget, an exception will be raised.
    A widget is considered dead if its parent is None.
    '''

    if isinstance(widget, pyatspi.Accessibility.Accessible):
        accessible = widget
    elif isinstance(widget, accessibles.Accessible):
        accessible = widget._accessible
    else:
        raise KeyError

    key = (accessible.getRole(), accessible.name, hash(accessible))

    if _widgets.has_key(key):
        # make sure the widget is still alive
        if _widgets[key]._accessible.parent is None:
            del _widgets[key]

    return _widgets[key]

def addApplication(app):
    '''
    Add a strongwind.accessibles.Application (or a subclass) to the application cache

    If an application with the same id has already been added, a subsequent call to
    this method will replace the previous application in the cache.
    '''

    if not isinstance(app, accessibles.Application):
        raise TypeError, "Cannot add %s instance to the application cache" % app.__class__.__name__

    # if this application already exists in the cache, remove it first
    if _applications.has_key(app.id):
        del _applications[app.id]

    _applications[app.id] = app

def getApplicationById(id):
    '''
    Retrieve an application from the application cache

    If this method is called with the id of a closed application, an
    exception will be raised.  An application is considered closed if querying
    for its id results in a COMM_FAILURE exception.
    '''

    if _applications.has_key(id):
        try:
            # poke the application first to make sure it's not stale
            _applications[id]._accessible.id
        except (LookupError, pyatspi.ORBit.CORBA.COMM_FAILURE):
            del _applications[id]

    return _applications[id]

def getApplicationsList():
    '''
    Returns all of the applications in the application cache

    Closed applications are pruned from the cache before returning the
    contents of the cache.  An application is considered closed if querying
    for its id results in a COMM_FAILURE exception.
    '''

    for k,v in _applications.items():
        try:
            # poke each application in the cache and remove stale applications
            v._accessible.id
        except (LookupError, pyatspi.ORBit.CORBA.COMM_FAILURE):
            del _applications[k]

    return _applications.values()

def launchApplication(args=[], name=None, find=None, cwd=None, env=None, wait=config.MEDIUM_DELAY, cache=True, logString=None):
    '''
    Launch an application with accessibility enabled

    args, cwd, and env are passed to subprocess.Popen.  If cwd is not specified, it
    defaults to os.cwd().  If env is not specified, it defaults to os.environ, plus
    GTK_MODULES='gail:atk-bridge'

    After launching the application, a reference to the
    strongwind.accessibles.Application is cached.  The "name" argument to this
    method is used to find the accessible that should be promoted to a
    strongwind.accessibles.Application.  The name is also used to refer to the
    application in the test procedures log.  If name is not specified, it defaults
    to the basename of args[0] with any file extension stripped.  

    If the accessible name of the application is not fixed, the "find" argument can
    be used to search for a pattern.  If find is not specified, it defaults to
    re.compile('^' + name)

    Returns a tuple containing a strongwind.accessibles.Application 
    object and a Popen object.
    '''

    # if a name for the application is not specified, try to guess it
    if name is None:
        name = utils.getBasenameWithoutExtension(args[0])

    if logString is None:
        logString = 'Launch %s.' % name

    procedurelogger.action(logString)

    if env is None:
        env = os.environ

    # enable accessibility for this application
    if not env.has_key('GTK_MODULES'):
        env['GTK_MODULES'] = 'gail:atk-bridge'

    if find is None:
        find = re.compile('^' + name)

    if cwd is None:
        cwd = os.getcwd()

    def findAppWithLargestId(desktop, find):
        '''
        Find the application with the largest id whose name matches find

        If ids are not recycled (i.e., ids always increment and never start
        over again at 1), the application with the highest id will be the last
        launched.  We're making this assumption.
        '''
    
        appWithLargestId = None

        apps = utils.findAllDescendants(desktop, lambda x: pyatspi.ROLE_APPLICATION == x.role and find.search(x.name), False)

        if len(apps) > 0:
            appWithLargestId = apps[0]

        for a in apps:
            if a._accessible.id > appWithLargestId._accessible.id:
                appWithLargestId = a

        return appWithLargestId

    # before we launch the application, check to see if there is another
    # instance of the application already open
    existingApp = findAppWithLargestId(_desktop, find)

    # launch the application
    subproc = subprocess.Popen(args, cwd=cwd, env=env)

    # wait for the application to launch and for the applications list to
    # settle.  if we try to list the desktop's applications too soon, we get
    # crashes sometimes. 
    sleep(wait)

    def findNewApplication():
        '''
        Find the application we just launched

        If there is an existing application, make sure the app we find here has
        an id larger than the existing application.

        If no application is found, wait and retry a number of times before
        returning None. 
        '''
        for i in xrange(config.RETRY_TIMES):
            app = findAppWithLargestId(_desktop, find)
            try:
                if existingApp is None or existingApp.id < app.id:
                    return app
            except (LookupError, pyatspi.ORBit.CORBA.COMM_FAILURE):
                return app
            sleep(config.RETRY_INTERVAL)

        raise errors.SearchError

    app = findNewApplication()

    if cache:
        addApplication(app)

    return (app, subproc)

# prevent reference errors at exit
def _cleanup():
    global _desktop
    del _desktop
    _widgets.clear()
    _applications.clear()

atexit.register(_cleanup)

