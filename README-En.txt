==============================================================
==== SpotlightDL v2.0.0 - By ORelio & GitHub contributors ====
======= https://github.com/ORelio/Spotlight-Downloader =======
==============================================================

Thanks for dowloading SpotlightDL!

This program can retrieve Windows Spotlight images by requesting the Microsoft Spotlight API.
SpotlightDL can also define images as wallpaper and system-wide lockscreen image.

It is useful in the following use cases:
 - Download most of the Spotlight library with maximum image resolution and metadata
 - Define Spotlight images as ad-less lockscreen or wallpaper
 - Chain SpotlightDL with your own scripts and apps using the url mode

============
 How to use
============

Extract the archive if not already extracted, then call SpotlightDownloader.exe from the command line.
If you are not used to the command prompt, a few Batch files are offered for your convenience:

spotlight-download-archive
  This script downloads as much images as possible from the Spotlight API,
  with maximum res and metadata. Please note that there is no actual way
  of listing *all* images so SpotlightDownloader will make many API calls
  to discover and download new images, and stop when no new images are
  discovered. It may miss a few images but you should get most of them.

update-archive-and-wallpaper
  This script calls once the Spotlight API and adds the result to the
  Spotlight archive, then randomly defines a Spotlight image as wallpaper.
  This allows to gradually download images without hammering the Spotlight API.

update-archive-and-lockscreen
  Same as update-archive-and-wallpaper but updates the lockscreen image for the current user.

update-archive-and-lockscreen-all-users
  Same as update-archive-and-wallpaper but defines images as system-wide lockscreen.
  Requires Admin privileges and relies on Intune policy, which is not supported on the Home edition.

update-wallpaper
  This script maintains a cache of several Spotlight pictures tailored to your screen
  resolution and randomly defines a new Spotlight image as wallpaper. The cache allows
  a few updates without Internet access, and oldest images are deleted from the cache.

update-lockscreen
  Same as update-wallpaper but defines lockscreen image for the current user.

update-lockscreen-all-users
  Same as update-wallpaper but defines images as system-wide lockscreen.
  Requires Admin privileges and relies on Intune policy, which is not supported on the Home edition.

restore-lockscreen
  This script restores the default system-wide lockscreen image.
  This will unlock user lockscreen settings locked by the Intune policy set by some other scripts above.

generate-manual
  This script saves usage info as a text file for your convenience,
  in order to help writing your own batch files or PowerShell scripts.

hide-console
  This script launches another script without showing the console window.
  Mostly useful if you plan to schedule an update script on logon.
  The path passed as argument should not contain special characters.

===========================
 Scheduling a batch script
===========================

If you wish to periodically update your wallpaper or lockscreen,
you can schedule one of the provided scripts by following these instructions:

= If you are not administrator =
= Startup shortcut method =

Use Win+R keyboard shortcut and specify:
  %appdata%\Microsoft\Windows\Start Menu\Programs\Startup

Perform a right click inside the Startup folder > New > Shortcut
  wscript "C:\Path\To\hide-console.vbs" "C:\Path\To\desired-script.bat"
  Next > Type a meaningful name for that shortcut > Finish

The shortcut will launch on each logon and run the script.
Note: lockscreen-related scripts won't work with this method.

= If you are administrator =
= Task scheduler method =

Use Win+R and specify:
  taskschd.msc

Click "Create a new task"
  General tab
    - Define the task name
    - Check "Run with highest privileges" to run the script as admin (lockscreen...)
  Triggers tab
    - Click New and add a trigger: "At log on" or "On a schedule" for instance
  Actions tab
    - Click New, choose Start a program
    - Program/Script: wscript
    - Add arguments: "C:\Path\To\hide-console.vbs" "C:\Path\To\desired-script.bat"
  Conditions tab
    - You may want to uncheck "Start the task only if the computer is on AC power"
  Settings tab
    - If your task has a defined schedule, e.g. everyday at 10am but your computer
      is powered off at 10am, the task will not run. You may want to enable the
      "Run task as soon as possible after a sheduled start is missed" feature.

Click OK to save your task.

=====
 FAQ
=====

Q: How many images are downloaded when using default mode? (i.e. without using --single or --many)
R: Default mode downloads a list of images returned by a single API call: currently 4.

Q: Some images do not have a title or copyright in their metadata?
R: Those fields are not provided for all images by the Spotlight API.

Q: The lockscreen image does not appear when I am NOT logged on?
R: You may want to define lockscreen image for all users using the appropriate script.

Q: Windows says that my Lockscreen settings are managed by my organization!?
R: Use the restore-lockscreen script to remove policy set by the update-lockscreen-all-users script

Q: I do not want to have the image title inside my wallpaper or lockscreen. How to remove it?
R: Edit the batch file you want to use and remove --embed-meta inside the corresponding command.

Q: The lock screen appears, but I still see various texts on it?
R: Disable "Get fun facts, tips, tricks, and more on your lock screen" in your Lockscreen settings

Q: I would like to retrieve metadata/images for a specific language. How to do it?
R: Edit the batch file you want to use and add --locale en-US or any other locale code

Q: When downloading images using spotlight-download-archive.bat, I get very few images. Why?
R: This may be related to the current locale set on your computer. Try en-US as decribed above.

Q: How to download more images by trying all languages at once? I do not need image metadata.
R: Edit spotlight-download-archive.bat to replace --metadata with --all-locales in the download command.

Q: Does SpotlightDL consume bandwidth on networks that I defined as metered?
R: No. Batch files will reuse previously downloaded images, except spotlight-download-archive.

Q: How to enable image download on metered networks?
R: Edit the batch file you want to use and remove the whole line containing "check-metered.ps1"

Q: Images are 1080p, how to get 4K images?
A: 4K resolution is only available via API v4, supported by SpotlightDL v1.5.0 or higher

=========
 Credits
=========

The Spotlight Downloader program has been created using the following resources:

 - Bing / Spotlight API by Microsoft Corporation
 - Spotlight API studies by KoalaBR, Biswa96 & ORelio
 - Agency FB font family by Microsoft Corporation (Logo)

+------------------------------------------+
| © 2018-2025 ORelio & GitHub contributors |
+------------------------------------------+