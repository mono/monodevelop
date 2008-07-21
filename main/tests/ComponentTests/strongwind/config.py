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

'Strongwind configuration'

# where to write procedure logger output, screenshots, etc.
OUTPUT_DIR = '/tmp/strongwind'

# if a widget is not found in a search, how many times should we try the search again
RETRY_TIMES = 20

# how long to wait between retries
RETRY_INTERVAL = 0.5

# whether or not to take screenshots
TAKE_SCREENSHOTS = True

# how long to wait before taking a screenshot; sometimes apps need some time to finish rendering
SCREENSHOT_DELAY = 0.5

# how long to wait between keystrokes
KEYCOMBO_DELAY = 0.1

# these values are used throughout strongwind.  lower values will cause test
# scripts to complete sooner, but may result in random application crashes or
# random failed tests
SHORT_DELAY = 0.5
MEDIUM_DELAY = 4
LONG_DELAY = 10

# see resetTimeout() in watchdog.py
WATCHDOG_TIMEOUT = 180
#WATCHDOG_TIMEOUT = 99999

