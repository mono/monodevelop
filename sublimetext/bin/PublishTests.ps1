$script:thisDir = split-path $MyInvocation.MyCommand.Path -parent

if (-not (test-path variable:STDataPath)) {
    write-error "You must define `$STDataPath"
    exit 1
}

$pathToTests = "$thisDir/../FSharp_Tests"

'publishing tests locally...'
remove-item "$STDataPath/Packages/FSharp_tests/*" -recurse -force -erroraction silentlycontinue
[void](new-item -itemtype d "$STDataPath/Packages/FSharp_Tests" -erroraction silentlycontinue)

push-location $pathToTests -erroraction stop
    copy-item "test_runner.py" "$STDataPath/Packages/FSharp_Tests" -force
    copy-item "FSharpTests.sublime-commands" "$STDataPath/Packages/FSharp_Tests" -force
pop-location

push-location (join-path $pathToTests 'tests') -erroraction stop
    copy-item "*" "$STDataPath/Packages/FSharp_Tests" -force -recurse
pop-location

'done'
