param([switch]$Clean)
<#
.DESCRIPTION
    Publishes the project locally as a directory.
#>

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

"restarting Sublime Text..."

try {
    get-process "sublime_text" -erroraction silentlycontinue | stop-process -erroraction silentlycontinue
    start-sleep -milliseconds 250
}
catch [Exception] {
    # ignore
}

$fsharpPackageDir = "$global:STDataPath\Packages\FSharp"
[void] (new-item -itemtype 'directory' $fsharpPackageDir -force -erroraction stop)

if ($Clean) {
    [void] (remove-item "$fsharpPackageDir/*" -recurse -force -erroraction stop)
}

# after st has been closed; resources should have been released now
push-location "$thisDir/../FSharp"
    copy-item * $fsharpPackageDir -force -recurse -erroraction stop
pop-location

'done'

sublime_text
