#! /usr/bin/python

import re
import sys
import time
import getopt
import subprocess
#from pprint import pprint


filetwofs = open("FileTwo.fs")
filetwostr = filetwofs.read()
filetwofs.close()

programfs = open("Program.fs")
programstr = programfs.read()
programfs.close()

test1fs = open("Test1.fsx")
test1str = test1fs.read()
test1fs.close()

text = """project "Test1.fsproj"
parse "FileTwo.fs"
""" + filetwostr + """
<<EOF>>
parse "Test1.fsx"
""" + test1str + """
<<EOF>>
parse "Program.fs"
""" + programstr + """
<<EOF>>
completion "Test1.fsx" 5 13
"""

text = text + """completion "Program.fs" 7 19
completion "Program.fs" 3 22
completion "Program.fs" 5 13
"""

text = text + """errors
quit
"""

# n,n,g,g

def main():
  try:
    opts, args = getopt.getopt(sys.argv[1:], "nva:w:")
  except getopt.GetoptError, err:
    # print help information and exit:
    print str(err) # will print something like "option -a not recognized"
    sys.exit(2)
  upload   = True
  verbose  = False
  wait     = 0
  attempts = 1
  for o, a in opts:
    if o == "-n":
      upload = False
    elif o == "-v":
      verbose = True
    elif o == "-a":
      attempts = int(a)
    elif o == "-w":
      wait = int(a)
    else:
      assert False, "unhandled option"


  child = subprocess.Popen(['mono', '../../bin/Debug//fsautocomplete.exe'], stdin=subprocess.PIPE, stdout=subprocess.PIPE)

  out, err = child.communicate(text)

  print "%s" % out


if __name__ == "__main__":
  main()
