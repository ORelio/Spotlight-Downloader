# Source: https://github.com/wiltaylor/NetMetered/blob/master/NetMetered.psm1

function Test-NetMetered
{
	[void][Windows.Networking.Connectivity.NetworkInformation, Windows, ContentType = WindowsRuntime]
	$networkprofile = [Windows.Networking.Connectivity.NetworkInformation]::GetInternetConnectionProfile()

	if ($networkprofile -eq $null)
	{
		Write-Warning "Can't find any internet connections!"
		return $false
	}

	$cost = $networkprofile.GetConnectionCost()

	if ($cost -eq $null)
	{
		Write-Warning "Can't find any internet connections with a cost!"
		return $false
	}

	if ($cost.Roaming -or $cost.OverDataLimit)
	{
		return $true
	}

	if ($cost.NetworkCostType -eq [Windows.Networking.Connectivity.NetworkCostType]::Fixed -or
	$cost.NetworkCostType -eq [Windows.Networking.Connectivity.NetworkCostType]::Variable)
	{
		return $true
	}

	if ($cost.NetworkCostType -eq [Windows.Networking.Connectivity.NetworkCostType]::Unrestricted)
	{
		return $false
	}

	throw "Network cost type is unknown!"
}

# Source: https://weblogs.asp.net/soever/returning-an-exit-code-from-a-powershell-script

function ExitWithCode
{
    param($exitcode)
    $host.SetShouldExit($exitcode)
    exit $exitcode
} 

# Main: Check if metered, and return 1 if metered

try {
	if (Test-NetMetered) {
		echo 'Metered connection detected.'
		ExitWithCode -exitcode 1
	}
} catch { }

# Usage: powershell -ExecutionPolicy Bypass -File check-metered.ps1 && <COMMAND>
# <COMMAND> will run only if no metered connection was detected