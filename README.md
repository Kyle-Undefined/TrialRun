# TrialRun
Console program to spin up/down VMs and Databases, locally.

Idea from wanting/needing this, built for The Great .Net 8 Hack #HackTogether.

Uses network share to grab custom configurations for each client which holds Database locations, and VM export location.

Uses PowerShell to Import VMs based on a exported Hyper-V VM, restores two databases from backups, and saves the VM Id and the generated folder the Hard Disk for the VM is saved to into a SQL Server.

Once done, can spin down the VM and delete the databases, and cleans out the VHD in the generated folder as it's no longer needed.

All configuration is stored in AppSettings.config and network TOML files.

Requires:
- PowerShell
- Hyper-V
- SQL Server
- .Net 8 (Obviously)

Many more things I would like to add, but for the sake of time, rough proof of concept since I started a little late.
