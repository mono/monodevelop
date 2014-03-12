import sublime
import os


# We cannot call the ST api from the top-level, so wrap in functions.
def path_to_fs_binaries():
    return os.path.join(sublime.packages_path(), 'FSharp_Binaries')

def path_to_fs_ac_binary():
    return os.path.join(path_to_fs_binaries(), 'fsautocomplete.exe')