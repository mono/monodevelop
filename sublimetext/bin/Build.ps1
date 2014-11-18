<#
    .DESCRIPTION
    Publishes the project locally as a directory.

    .PARAMETER Clean
    Whether to erase the target folder's content first.

    .PARAMETER Restart
    Whether to restart Sublime Text.
#>
[CmdletBinding()]
param([switch]$Clean, [switch]$Restart)


$script:thisDir = split-path $MyInvocation.MyCommand.Path -parent

$msg = @"
You need to set `$global:STDataPath to Sublime Text 3's data path.

PS> `$STDataPath = ...

(Note this is a PowerShell variable, not an environment variable.)

For more information on Sublime Text's data path, see:
http://docs.sublimetext.info/en/latest/basic_concepts.html#the-data-directory
"@

if (-not (test-path variable:\STDataPath) -or
    ($global:STDataPath -eq $null) -or
    -not (test-path $global:STDataPath)) {
    throw $msg
    exit 1
}

write-debug "path to Sublime Text 3 Data is: '$global:STDataPath'"

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

$fsharpPackageDir = "$global:STDataPath\Packages\FSharp"
write-debug "target path is: $fsharpPackageDir"
write-verbose "creating '$fsharpPackageDir' directory..."
[void] (new-item -itemtype 'directory' $fsharpPackageDir -force -erroraction stop)

if ($Clean) {
    write-verbose "erasing '$fsharpPackageDir''s content..."
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
