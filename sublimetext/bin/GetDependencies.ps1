<#
.DESCRIPTION
    Downloads and installs dependencies.
#>

[CmdletBinding()]
param()

function IsVerbose {
    return $VerbosePreference -eq 'Continue'
}

function unzip {
    param($Source, $Destination)
    $shellApp= new-object -com "Shell.Application"
    $archive = $shellApp.namespace($Source)
    $dest = $shellApp.namespace($Destination)
    $dest.copyhere($archive.items())
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

    write-verbose 'extracting files...'
    unzip (get-item 'fsac.zip').fullname (get-location).providerpath
    write-verbose 'cleaning up...'
    remove-item 'fsac.zip'
pop-location

write-verbose 'done'
