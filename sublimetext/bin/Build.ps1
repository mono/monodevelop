<#
    .DESCRIPTION
    Publishes the project locally as a directory.

    .PARAMETER Clean
    Whether to erase the target folder's content first.

    .PARAMETER Restart
    Whether to restart Sublime Text.

    .PARAMETER Full
    Whether to get dependencies too.
#>
[CmdletBinding()]
param([switch]$Clean,
      [switch]$Restart,
      [switch]$Full)


$script:thisDir = split-path $MyInvocation.MyCommand.Path -parent

$msg = @"
You need to set `$global:STPackagesPath to Sublime Text 3's packages path.

To set global variables in PowerShell:

PS> `$STPackagesPath = ...

(Note this is a PowerShell variable, not an environment variable.)

To obtain Sublime Text's packages path, open Sublime Text, open the Python
console and type in the following:

    sublime.packages_path()

For more information on Sublime Text's packages path, see:
http://docs.sublimetext.info/en/latest/basic_concepts.html#the-packages-directory
"@


if (-not (test-path variable:\STPackagesPath) -or
    ($global:STPackagesPath -eq $null) -or
    -not (test-path $global:STPackagesPath)) {
    throw $msg
    exit 1
}

if ($Full) {
    push-location $thisDir
        & .\GetDependencies.ps1
    pop-location
}

# Check that we don't have both FSharp.sublime-package and FSharp at the same time.
# This may cause conflicts.
write-verbose 'checking installed packages...'
$STInstalledPackagesPath = (resolve-path (join-path $global:STPackagesPath "..\Installed Packages"))
$package = get-item "$STInstalledPackagesPath\FSharp.sublime-package" -erroraction silentlycontinue

if ($package) {
    throw "you can't have .../Installed Packages/FSharp.sublime-package and .../Packages/FSharp at the same time"
}

write-debug "path to Sublime Text 3 packages is: '$global:STPackagesPath'"

if ($Restart) {
    try {
        write-verbose "stopping Sublime Text..."
        get-process "sublime_text" -erroraction silentlycontinue | stop-process -erroraction silentlycontinue
        start-sleep -milliseconds 250
    }
    catch [Exception] {
        write-debug "error occurred while stopping sublime text"
        # ignore
    }
}

$fsharpPackageDir = "$global:STPackagesPath\FSharp"
write-debug "target path is: $fsharpPackageDir"
write-verbose "creating '$fsharpPackageDir' directory..."
[void] (new-item -itemtype 'directory' $fsharpPackageDir -force -erroraction stop)

if ($Clean) {
    write-verbose "erasing content of '$fsharpPackageDir'..."
    [void] (remove-item "$fsharpPackageDir/*" -recurse -force -erroraction stop)
}

# after st has been closed; resources should have been released now
push-location "$thisDir/../FSharp"
    write-debug "source directory is: $(get-location)"
    write-verbose "copying files from $(get-location) to '$fsharpPackageDir'..."
    copy-item * $fsharpPackageDir -force -recurse -erroraction stop
pop-location

if ($Restart) {
    write-verbose "restarting Sublime Text..."
    write-debug "path to Sublime Text 3 executable is: $((get-command sublime_text).path)"
    sublime_text
}

write-verbose 'done'
