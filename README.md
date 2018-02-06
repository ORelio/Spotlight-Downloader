# SpotlightDL

This program can retrieve Windows Spotlight images by requesting the Microsoft Spotlight API.
SpotlightDL can also define images as wallpaper and system-wide lockscreen image.

It is useful in the following use cases:
 - Download the whole Spotlight library with maximum image resolution and metadata
 - Define Spotlight images as wallpaper, not only on Windows 10 but also on previous versions
 - Define Spotlight images as global lock screen: without ads, and without any user being logged in
 - Chain SpotlightDL with your own scripts and apps by taking advantage of the url mode

# How to use

Simply call `SpotlightDownloader.exe` from the Windows command prompt and see usage.
The download/url modes should also work on Mac/Linux using the Mono framework.

A few Batch files are offered for ease of use for common tasks:

 - spotlight-download: Download Spotlight images
 - update-wallpaper: Set images as desktop wallpaper
 - update-lockscreen: Set images as system-wide lockscreen
 - restore-lockscreen: Restore default system-wide lockscreen
 - generate-manual: Generate a text file with command-line usage

If you wish to periodically update your wallpaper or lockscreen,
you can schedule one of the provided script by following [these instructions](README-En.txt).

# How it works

## Spotlight API

The Spotlight API is located on the following endpoint:

`https://arc.msn.com/v3/Delivery/Cache?pid=209567&fmt=json&rafb=0&ua=WindowsShellClient%2F0&disphorzres=9999&dispvertres=9999&lo=80217&pl=en-US&lc=en-US&ctry=us&time=2017-12-31T23:59:59Z`

Where the expected arguments are:
 - `pid` : Purpose currently unknown
 - `fmt` : Output format, e.g. `json`
 - `rafb` : Purpose currently unknown
 - `ua` : Client user agent string
 - `disphorzres`: Screen width in pixels
 - `dispvertres`: Screen height in pixels
 - `lo` : Purpose currently uknown
 - `pl` : Locale, e.g. `en-US`
 - `lc` : Language, e.g. `en-US`
 - `ctry` : Country, e.g. `us`
 - `time` : Time, e.g. `2017-12-31T23:59:59Z`

The JSON response contains details about 6-7 images including image url, title, sha256, ads, etc.

Spotlight API URL was originally found in this [file](https://github.com/KoalaBR/spotlight/blob/3164a43684dcadb751ce9a38db59f29453acf2fe/spotlightprovider.cpp#L17), thanks to the author for their findings!

## Global lock screen

The global lock screen image is stored as `C:\Windows\Web\Screen\img100.jpg`.
SpotlightDL backups the image as `img200.jpg` if it does not already exists, then overwrite this file.
The lock screen image cache, located at `C:\ProgramData\Microsoft\Windows\SystemData\S-1-5-18\ReadOnly\LockScreen_Z`, must be cleared for the change to take effect.

SpotlightDL gets around NTFS permissions on these folders [being locked down to TrustedInstaller](https://helpdeskgeek.com/windows-7/windows-7-how-to-delete-files-protected-by-trustedinstaller/)
by setting the local `Administrators` group as new owner of the relevant files and folders, and granting full control to this group.
Then, programs running as administrator can overwrite the lockscreen image and clear the cache.

This way of replacing the lockscreen is basically a C# implementation of [this script](https://www.reddit.com/r/PowerShell/comments/5fglby/powershell_to_set_windows_10_lockscreen/daoepvj/),
avoiding the use of the `takeown` and `iacls` commands which are not reliable due to a [localization issue](http://community.idera.com/powershell/ask_the_experts/f/powershell_for_windows-12/10227/trying-to-make-a-takeown-exe-cmdlet-but-locales-is-causing-a-problem).

# License

SpotlightDL is provided under [CDDL-1.0](http://opensource.org/licenses/CDDL-1.0) ([Why?](http://qstuff.blogspot.fr/2007/04/why-cddl.html)).

Basically, you can use it or its source for any project, free or commercial, but if you improve it or fix issues,
the license requires you to contribute back by submitting a pull request with your improved version of the code.
Also, credit must be given to the original project, and license notices may not be removed from the code.