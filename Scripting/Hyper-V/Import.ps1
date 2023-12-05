param(
    [Parameter()]
    [String] $Name,
    [String] $VMPath,
	[String] $HDPath
)
Import-Module Hyper-V
Import-VM -Path $VMPath -Copy -GenerateNewId -VhdDestinationPath $HDPath
Get-VM | Where-Object {{$_.CreationTime.DayOfYear -eq (Get-Date).DayOfYear}} | Rename-VM -NewName $Name
$Id = (Get-VM | Where-Object {{$_.Name -eq $Name}} | Where-Object {{$_.CreationTime.DayOfYear -eq (Get-Date).DayOfYear}} | Select Id).Id
Start-VM (Get-VM | Where-Object {{$_.Id -eq $Id}} | Select Name).Name
Write-Host $Id