import subprocess

def addopt(opts):
    '''add option to hide the window; Windows only'''
    if getattr(subprocess, 'STARTUPINFO', 0) != 0:
        si = subprocess.STARTUPINFO()
        si.dwFlags = 1 # STARTF_USESHOWWINDOW
        si.dwShowWindow = 0 # SW_HIDE
        opts['startupinfo'] = si
