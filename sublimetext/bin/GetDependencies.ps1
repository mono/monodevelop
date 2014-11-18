<#
.DESCRIPTION
    Downloads and installs dependencies.
#>

[CmdletBinding()]
param()

function IsVerbose {
    return $VerbosePreference -eq 'Continue'
}

$script:thisDir = split-path $MyInvocation.MyCommand.Path -parent
$script:serverDir = resolve-path((join-path $thisDir '../FSharp/fsac/fsac'))
$script:bundledDir = resolve-path((join-path $thisDir '../FSharp/fsac/fsac'))

# clean up
remove-item (join-path $bundledDir '*.*') -erroraction silentlycontinue

push-location $bundledDir
    write-verbose 'downloading fsautocomplete.zip...'
    $client = new-object System.Net.WebClient
    $client.DownloadFile('https://bitbucket.org/guillermooo/fsac/downloads/fsac.zip',
                         "$bundledDir\fsac.zip" )

    if (IsVerbose) {
        & '7z' 'x' 'fsac.zip' '-o.'
    }
    else {
        & '7z' 'x' 'fsac.zip' '-o.' > $null
    }

    remove-item 'fsac.zip'
pop-location
write-verbose 'done'
