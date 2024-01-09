$BuildPath = "$PSScriptRoot\bld\x64"
$Version = Get-Date -Format "yyyy-MM-dd" # 2020-11-1
$VersionDot = $Version -replace '-','.'
$Project = "ProjectOtter"
$Archive = "$BuildPath\..\$Project-$Version.zip"

# Clean up
if(Test-Path -Path $BuildPath)
{
    Remove-Item $BuildPath -Recurse
}

# Dotnet restore and build
dotnet publish `
	   --runtime win-x64 `
	   --self-contained true `
	   -c Release `
	   -v minimal `
	   -p:Platform=x64 `
	   -p:PublishReadyToRun=true `
	   -p:Version=$VersionDot `
	   -o $BuildPath `

# Archive Build
Compress-Archive -Path "$BuildPath" -DestinationPath $Archive