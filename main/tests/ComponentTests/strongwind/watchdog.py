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
Detect and terminate hung test scripts

The watchdog runs in a separate thread and increments a counter.  
Callbacks can be registered with the watchdog to be run every n 
seconds.  Calling resetTimeout() will reset to the counter to 0;  
this is done automatically by procedurelogger.action().  If the 
counter is allowed to reach config.WATCHDOG_TIMEOUT, test script 
execution is aborted.

Typically, application failures will cause some other aspect of 
the test to fail before the watchdog kicks in.  (e.g., a dialog 
is supposed to appear, but it cannot be found, so the test fails)  
However, applications can sometimes hang in a way that causes a 
pyatspi call to hang.  In this case, the main Strongwind thread 
will hang waiting for the pyatspi call to return - this is
when the watchdog would step in and kill the test script.

The watchdog is most useful when running a batch of Strongwind 
tests unattended.  If one test hangs, it will eventually be killed 
by the watchdog so that other tests can be run.
'''

import os
import sys
import threading
import atexit
from time import sleep

import config
import procedurelogger

_watchdogThread = None

_counter = 0
_counterLock = threading.Lock()

_terminate = False
_terminateLock = threading.Lock()

_callbacks = {} 
_callbacksLock = threading.Lock()

def resetTimeout():
    '''
    Reset the watchdog counter to 0

    The watchdog counter is incremented every second.  If it reaches
    config.WATCHDOG_TIMEOUT, script execution is aborted.

    This method is called by procedurelogger.action(), so most test 
    scripts will not need to call this manually.
    '''

    global _counter

    _counterLock.acquire()
    _counter = 0
    _counterLock.release()

def _watchdog():
    '''
    The body of the watchdog thread

    Every second, run any registered callbacks and increment the watchdog
    counter.  If a callback returns False, the script will terminate.
    '''

    global _counter
    global _terminate
    global _callbacks

    def exit(message):
        sys.stderr.write(message + '\n')

        # _exit() exits "without calling cleanup handlers, flushing stdio buffers, 
        # etc.", so we have to tell procedurelogger to save its log explicitly
        procedurelogger.save()
        os._exit(1)

    _terminateLock.acquire()
    _counterLock.acquire()
    while _terminate is False and _counter < config.WATCHDOG_TIMEOUT:
        _terminateLock.release()

        _callbacksLock.acquire()
        for timeout in _callbacks:
            if _counter is not 0 and (_counter % timeout) == 0:
                for callback,args in _callbacks[timeout]:
                    if not callback(*args):
                        _counterLock.release()
                        _callbacksLock.release()
                        exit('Watchdog callback returned False; exiting')
        _callbacksLock.release()

        _counter += 1
        _counterLock.release()

        sleep(1)

        # we sometimes get the following exception when the script is exiting:
        #
        # Exception in thread Thread-1 (most likely raised during interpreter shutdown):
        # Traceback (most recent call last):
        #   File "threading.py", line 460, in __bootstrap
        #   File "threading.py", line 440, in run
        #   File "/tmp/strongwind-node/tests/strongwind/watchdog.py", line 91, in _watchdog
        # <type 'exceptions.AttributeError'>: 'NoneType' object has no attribute 'acquire'
        # Unhandled exception in thread started by 
        # Error in sys.excepthook:
        # 
        # Original exception was:
        #
        # so try-catch the next statement and abort if we get an AttributeError
        try:
            _terminateLock.acquire()
        except exceptions.AttributeError:
            return

        _counterLock.acquire()

    _counterLock.release()

    if _terminate is False:
        _terminateLock.release()
        exit('Watchdog timeout reached; exiting')

    _terminateLock.release()

def start():
    '''
    Start the watchdog thread

    Set the terminate flag to false, create the watchdog thread, start it, and
    arrange for it to stop when the script exits.  
    '''

    global _watchdogThread
    global _terminate

    _terminateLock.acquire()
    _terminate = False
    _terminateLock.release()

    if _watchdogThread is None:
        _watchdogThread = threading.Thread(target=_watchdog)
        _watchdogThread.setDaemon(True)

    if not _watchdogThread.isAlive():
        _watchdogThread.start()
        atexit.register(stop)

def stop():
    '''
    Stop the watchdog thread

    Set the terminate flag to True; the watchdog thread should notice within one 
    second and exit.
    '''

    global _terminate

    _terminateLock.acquire()
    _terminate = True
    _terminateLock.release()

def addCallback(timeout, callback, args={}):
    '''
    Add a watchdog callback

    callback will be called with args every timeout seconds,  If callback returns False, 
    the script will terminate.
    '''

    global _callbacks

    _callbacksLock.acquire()
    if _callbacks.has_key(timeout):
        _callbacks[timeout].append((callback, args))
    else:
        _callbacks[timeout] = [(callback, args)]
    _callbacksLock.release()

def removeCallback(timeout, callback, args={}):
    'Remove a watchdog callback'

    global _callbacks

    _callbacksLock.acquire()
    _callbacks[timeout].__delitem__((callback, args))
    _callbacksLock.release()


# start the watchdog
start()

