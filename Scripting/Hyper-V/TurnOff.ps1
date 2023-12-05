param(
    [Parameter()]
    [String] $Id
)
Import-Module Hyper-V
Stop-VM -Name (Get-VM | Where-Object {{$_.Id -eq $Id}} | Select Name).Name -TurnOff
Remove-VM -Name (Get-VM | Where-Object {{$_.Id -eq $Id}} | Select Name).Name -Force