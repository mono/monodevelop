<#
.DESCRIPTION
    Publishes the project locally as a directory.
#>

$script:thisDir = split-path $MyInvocation.MyCommand.Path -parent

# todo: use config file for devs
$targetDir = "~/Utilities/Sublime Text 3/Data/Packages/Fsharp"

"restarting Sublime Text..."

try {
    get-process "sublime_text" -erroraction silentlycontinue | stop-process -erroraction silentlycontinue
    start-sleep -milliseconds 250
}
catch [Exception] {
    # ignore
}

# after st has been closed; resources should have been released now
push-location "../Fsharp"
    copy-item * $targetDir -force -recurse -erroraction stop
pop-location

'done'

sublime_text
