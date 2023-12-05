param(
    [Parameter()]
    [String] $Name,
    [String] $BakPath
)
[string] $dbCommand = "RESTORE DATABASE [$Name] " +
"FROM    DISK = N'$BakPath' " +
"WITH    FILE = 1, " +
"NOUNLOAD, REPLACE, STATS = 5"

sqlcmd -E -Q $dbCommand