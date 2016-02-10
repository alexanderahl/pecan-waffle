﻿[cmdletbinding()]
param()

$templateInfo = New-Object -TypeName psobject -Property @{
    Name = 'aspnet5-empty'
    Type = 'ProjectTemplate'
    Description = 'ASP.NET 5 empty project'
    DefaultProjectName = 'MyEmptyProject'
    LicenseUrl = 'https://raw.githubusercontent.com/ligershark/pecan-waffle/master/LICENSE'
    ProjectUrl = 'https://github.com/ligershark/pecan-waffle'
    GitUrl = 'https://github.com/ligershark/pecan-waffle.git'
    GitBranch = 'master'
    BeforeInstall = { 'before install' | Write-Host -ForegroundColor Cyan}
    AfterInstall = { 'after install' | Write-Host -ForegroundColor Cyan}
}

replace $templateInfo 'EmptyProject' {"$ProjectName"} {"$DefaultProjectName"}
replace $templateInfo 'SolutionDir' {"$SolutionDir"} {'..\..\'}
replace $templateInfo '..\..\artifacts' {"$ArtifactsDir"} {"$SolutionDir" + 'artifacts'}
replace $templateInfo '97b148d4-829e-4de3-840b-9c6600caa117' {"$ProjectId"} {[System.Guid]::NewGuid()}

# when the template is run any filename with the given string will be updated
Update-FileName $templateInfo 'EmptyProject' {"$ProjectName"}
# excludes files from the template
Exclude-File $templateInfo 'pw-*.*','*.user','*.suo','*.userosscache','project.lock.json','*.vs*scc'
# excludes folders from the template
Exclude-Folder $templateInfo '.vs','artifacts'

beforeinstall = { 'before install' | Write-Host -ForegroundColor Cyan}
afterinstall = { 'after install' | Write-Host -ForegroundColor Cyan}

# This will register the template with pecan-waffle
Set-TemplateInfo -templateInfo $templateInfo
