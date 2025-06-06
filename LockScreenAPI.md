# LockScreen API

This documentation list methods of changing the Windows lock screen.

## Windows 7

Windows 7 has an [OEMBackground](https://www.askvg.com/windows-7-supports-login-screen-customization-without-3rd-party-software-how-to-instructions-inside/) feature in registry allowing custom Logon Screen background:

````
[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Authentication\LogonUI\Background]
"OEMBackground"=dword:00000001
````

Then the image has to be placed in `C:\Windows\System32\oobe\info\backgrounds\backgroundDefault.jpg`.

Windows 7 enforces a limit of 250 KiB, so SpotlightDL will recompress the image to the highest quality fitting in that limit. This results in lower quality lock screen images than Windows 8 and greater, but works without patching anything on the system so the feature is safe to use.

## Windows 8 and greater

### User LockScreen API

Setting a lockscreen image for the current user account is straightforward using the .NET API:

```C#
var file = await StorageFile.GetFileFromPathAsync(lockScreenImageFilePath);
await LockScreen.SetImageFileAsync(file);
```

Reference: [LockScreen.SetImageFileAsync](https://learn.microsoft.com/en-us/uwp/api/windows.system.userprofile.lockscreen.setimagefileasync?view=winrt-26100)

### System LockScreen Policy (GPO)

_Require administrator privileges. Works will all editions **except Home and Pro**._

**`gpedit.msc`**

```
Computer Configuration
 +- Policies
     +- Administrative Templates
         +- Control Panel
             +- Personalization
```

Set `Force a specific default lock screen image` to the desired image path.

**Registry**

```
Windows Registry Editor Version 5.00

[HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Windows\Personalization]
"LockscreenImage"="C:\\Path\\To\\lockscreen.png"
```

References:
* [Configure the desktop and lock screen backgrounds](https://learn.microsoft.com/en-us/windows/configuration/background/?tabs=gpo#configure-the-lock-screen-background)
* [How to Change the Default Lock Screen Image using GPO](https://www.cloudtechadmin.com/how-to-change-the-default-lock-screen-image-using-gpo/)

### System LockScreen Policy (CSP)

_Require administrator privileges. Works will all editions **except Home**. Windows 10 1709 or greater._

> [!NOTE]
> This policy will lock down the lockscreen settings in personalization panel.
> Remove the "PersonalizationCSP" registry key to unlock.

```
Windows Registry Editor Version 5.00

[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\PersonalizationCSP]
"LockScreenImagePath"="C:\\Path\\To\\lockscreen.png"
```

References:
* [Configure the desktop and lock screen backgrounds](https://learn.microsoft.com/en-us/windows/configuration/background/?tabs=intune#configure-the-lock-screen-background)
* [LockScreenImageUrl](https://learn.microsoft.com/en-us/windows/client-management/mdm/personalization-csp#lockscreenimageurl)
* [Set Desktop Wallpaper and Logon Screen Background via Group Policy](https://woshub.com/setting-desktop-wallpapers-background-using-group-policy/)

### By manipulating system files

_That was the method implemented in Spotlight Downloader v1.x._
_Require administrator privileges. Works with any Windows edition, but might break things._

The global lock screen images for Windows 8+ are stored as `C:\Windows\Web\Screen\imgXXX.jpg`.
SpotlightDL v1.x backups each image as `imgXXX.jpg.bak` if it does not already exists, then overwrite this file.

The lock screen image cache, located at `C:\ProgramData\Microsoft\Windows\SystemData\S-1-5-18\ReadOnly\LockScreen_*`, must be cleared for the change to take effect.

SpotlightDL gets around NTFS permissions on these folders [being locked down to TrustedInstaller](https://helpdeskgeek.com/windows-7/windows-7-how-to-delete-files-protected-by-trustedinstaller/)
by setting the local `Administrators` group as new owner of the relevant files and folders, and granting full control to this group.
Then, programs running as administrator can overwrite the lockscreen image and clear the cache.

This way of replacing the lockscreen is basically a C# implementation of [this script](https://www.reddit.com/r/PowerShell/comments/5fglby/powershell_to_set_windows_10_lockscreen/daoepvj/),
avoiding the use of the `takeown` and `iacls` commands which are not reliable due to a [localization issue](http://community.idera.com/powershell/ask_the_experts/f/powershell_for_windows-12/10227/trying-to-make-a-takeown-exe-cmdlet-but-locales-is-causing-a-problem).
