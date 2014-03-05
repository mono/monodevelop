<#
.DESCRIPTION
    Downloads dependencies.
#>
$script:thisDir = split-path $MyInvocation.MyCommand.Path -parent
$script:serverDir = resolve-path((join-path $thisDir '../FSharp/fsac'))
$script:bundledDir = resolve-path((join-path $thisDir '../FSharp/bundled'))

remove-item (join-path $bundledDir '*.zip') -erroraction silentlycontinue
push-location $bundledDir
    'downloading fsautocomplete.zip...'
    $client = new-object System.Net.WebClient
    $client.DownloadFile('https://bitbucket.org/guillermooo/fsac/downloads/fsautocomplete.zip',
                         "$bundledDir\fsautocomplete.zip" )
pop-location
'done'
