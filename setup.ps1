# ---
# Safer and More Reliable Bannerlord Project Setup Script (v6)
# ---
# This script applies best practices for setting up a Bannerlord mod project.
# 1. Edits the .csproj file to replace the HarmonyLib NuGet package 
#    with a direct DLL reference, using proper XML namespace handling.
# 2. Edits the .sln file to use a relative path for the project,
#    making the solution portable.
#
# IMPORTANT: Close the solution in Visual Studio before running this script
# to avoid file locking issues.
# ---

try {
    # Stop the script if any command fails
    $ErrorActionPreference = 'Stop'

    # --- PART 1: Update the .csproj file ---

    # ** ROBUST PATHING **
    # This script assumes it is located in the same directory as the .csproj file
    # (e.g., C:\Dev\Enlisted\Enlisted\)
    $scriptDir = $PSScriptRoot
    $csprojPath = Join-Path -Path $scriptDir -ChildPath "Enlisted.csproj"
    
    Write-Host "Loading project file: $csprojPath"

    # Load the .csproj file as an XML document
    [xml]$csproj = Get-Content -Path $csprojPath

    # ** CRITICAL STEP (Based on AI feedback) **
    # Define the MSBuild XML namespace to correctly find elements.
    $ns = New-Object System.Xml.XmlNamespaceManager($csproj.NameTable)
    $msbuildNamespace = "http://schemas.microsoft.com/developer/msbuild/2003"
    $ns.AddNamespace("msb", $msbuildNamespace)

    # Find the HarmonyLib PackageReference node using the namespace
    $harmonyPackageNode = $csproj.SelectSingleNode("//msb:PackageReference[@Include='HarmonyLib']", $ns)

    if ($harmonyPackageNode) {
        Write-Host "Found HarmonyLib PackageReference. Removing it..."
        $itemGroup = $harmonyPackageNode.ParentNode
        $itemGroup.RemoveChild($harmonyPackageNode)

        # If the ItemGroup is now empty, remove it as well
        if (-not $itemGroup.HasChildNodes) {
            $itemGroup.ParentNode.RemoveChild($itemGroup)
        }
    } else {
        Write-Host "HarmonyLib PackageReference not found (this is okay)."
    }

    # Check if the direct 0Harmony reference already exists using the namespace
    $harmonyReferenceNode = $csproj.SelectSingleNode("//msb:Reference[@Include='0Harmony']", $ns)

    if (-not $harmonyReferenceNode) {
        Write-Host "Adding direct reference to 0Harmony.dll..."

        # Find the first ItemGroup that already contains references
        $referenceGroup = $csproj.SelectSingleNode('(//msb:ItemGroup[msb:Reference])[1]', $ns)

        # If no such group exists, create a new one in the correct namespace
        if (-not $referenceGroup) {
            Write-Host "No existing Reference group found. Creating a new one."
            $referenceGroup = $csproj.CreateElement("ItemGroup", $msbuildNamespace)
            $csproj.Project.AppendChild($referenceGroup)
        }

        # Create the new elements IN THE CORRECT NAMESPACE
        $newReference = $csproj.CreateElement("Reference", $msbuildNamespace)
        $newReference.SetAttribute("Include", "0Harmony")

        $hintPath = $csproj.CreateElement("HintPath", $msbuildNamespace)
        # The DOM will correctly handle escaping special characters like '&'
        $hintPath.InnerText = '..\..\..\..\..\..\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\Bannerlord.Harmony\bin\Win64_Shipping_Client\0Harmony.dll'
        $newReference.AppendChild($hintPath)

        $privateNode = $csproj.CreateElement("Private", $msbuildNamespace)
        $privateNode.InnerText = 'False'
        $newReference.AppendChild($privateNode)
        
        $referenceGroup.AppendChild($newReference)

    } else {
        Write-Host "Direct 0Harmony reference already exists."
    }

    # Save the modified .csproj file
    $csproj.Save($csprojPath)
    Write-Host "Project file updated successfully."


    # --- PART 2: Update the .sln file ---

    # The solution file is now assumed to be in the SAME directory as the script.
    $slnPath = Join-Path -Path $scriptDir -ChildPath "Enlisted.sln"

    if (Test-Path $slnPath) {
        Write-Host "Updating solution file: $slnPath to use relative path."
        $slnContent = Get-Content -Path $slnPath -Raw
        
        # This regex finds the project line and replaces the absolute path with a relative one
        $pattern = 'Project\("{[^}]+}"\) = "Enlisted", "[^"]+"'
        $replacement = 'Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Enlisted", "Enlisted.csproj"'
        $newSlnContent = $slnContent -replace $pattern, $replacement

        Set-Content -Path $slnPath -Value $newSlnContent -Encoding UTF8
        Write-Host "Solution file updated successfully."
    } else {
        Write-Host "Solution file not found at expected location: $slnPath" -ForegroundColor Yellow
    }

    Write-Host "Project and solution update complete."

}
catch {
    Write-Host "An error occurred:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
}
