<#
.DESCRIPTION
    Builds and publishes the package.

.PARAMETER -Release
    Use this if you want to exclude tests from the built package.

.PARAMETER -DontUpload
    Use this if you don't want to open a browser window to upload the files.
#>
param([switch]$Release, [switch]$DontUpload)

if (-not (test-path variable:STDataPath)) {
    # Try using the usual location.
    $global:STDataPath = "$env:APPDATA\Sublime Text 3"
}

if (-not (test-path $STDataPath)) {
    write-error "You must define `$STDataPath"
    exit 1
}

$script:thisDir = split-path $MyInvocation.MyCommand.Path -parent
[void] (new-item -itemtype d (join-path $thisDir '../FSharp/dist') -erroraction silentlycontinue)
$script:distDir = resolve-path((join-path $thisDir '../FSharp/dist'))
$script:FSharpDir = resolve-path((join-path $thisDir '../FSharp'))

push-location $FSharpDir
    $typeOfBuild = if ($Release) {'release'} else {'dev'}
    "building $typeOfBuild build..."

    if ((get-command 'py.exe').length -ne 0) {
        # Use the launcher available in Py3k.
        & 'py' '-3.3' (join-path $script:thisDir '../FSharp/builder.py') '--release' $typeOfBuild
    }
    elseif ("$(python.exe -V 2>&1)".startswith('Python 2.7')) {
        & 'python' (join-path $script:thisDir '../FSharp/builder.py') '--release' $typeOfBuild
    }
    else {
        write-error 'could not run python'
        exit 1
    }

    if ($LASTEXITCODE -ne 0) {
       write-error 'could not run builder.py'
       exit 1
    }

    'done'
pop-location

$targetDir = resolve-path "$STDataPath/Installed Packages"

'publishing locally...'
copy-item (join-path $distDir "FSharp.sublime-package") $targetDir -force
'done'

if ($typeOfBuild -eq 'dev') {
    & "$thisDir/PublishTests.ps1"
}

if ($Release -and !$DontUpload) {
    # This should point to a more useful location. Or we should
    # talk to the GH api if possible.
    start-process 'https://github.com/fsharp/fsharpbinding'
    ($distDir).path | clip.exe
}
