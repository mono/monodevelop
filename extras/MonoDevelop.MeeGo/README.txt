To prepare a Moblin machine as a target
======================================

These instructions are intended to be run as root on a SUSE Moblin machine.
If you have a different setup, you will have to adapt them.

1) Enable Avahi Server
    a) Edit /etc/avahi/avahi-daemon.conf, set host and domain, e.g.
        host-name=monomo
        domain-name=local
    b) (Re)start the Avahi daemon:
        /etc/init.d/avahi-daemon restart
    c) Set Avahi to run at startup:
        insserv avahi-daemon
    d) Check that you can ping the target machine from the developer machine. 
    If they are on the same subnet you can now use $host-name.$domain-name as the
    target address, e.g 
        ping monomo.local

2) Enable SSH Server
    a) (Re)start the SSH daemon:
        /etc/init.d/sshd restart
    b) Set Avahi to run at startup:
        insserv sshd
    c) Check that you can ssh into the target from the dev machine, e.g.
        ssh monomo.local

3) Upgrade Mono to 2.6.1
    a) Add the zypper repo and upgrade
        zypper addrepo http://ftp.novell.com/pub/mono/download-stable/SLE_11 mono-stable
        zypper refresh --repo mono-stable
        zypper dist-upgrade --repo mono-stable
    b) Check the Mono version on the target is 2.6.1+
        mono --version

To build  the MonoDevelop addin on Linux
========================================

1) Build MonoDevelop from SVN:
        http://monodevelop.com/Developers/Articles/Development%3a_Getting_Started

2) Ensure that you include MonoDevelop.MeeGo in your build profile. You can 
   reconfigure with
       ./configure --select

3) Download the SharpSSH binaries from
        http://www.tamirgal.com/blog/page/SharpSSH.aspx
   and extract them to
       extras/MonoDevelop.MeeGo/lib

4) Build with
        make
   then run with
        make run

Instructions for building on Mac and Windows will be added later.

To use the addin
================

1) New Solution -> C# -> MeeGo -> MeeGo GTK# Project

2) Everything should work as normal, build, run, debug, etc

But you can also...

3) Run -> Run with... -> MeeGo Device
   - or -
   Run -> Run with... -> Mono Soft Debugger for MeeGo

4) Enter the mdns address (or IP address) of the device e.g.
   monomo.local, and the user and password

5) See app appear on the device!

Known issues
==========

* Error recovery is very poor e.g. if device address not not found or 
  connection times out you might have to kill MD

* Device address and credentials are stored in plaintext and cannot be
  modified without restarting MD

* No way to configure location that the app is copied to on the target