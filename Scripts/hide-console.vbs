' taken from FreeFileSync - www.freefilesync.org

set argIn = WScript.Arguments
num = argIn.Count

if num = 0 then
    WScript.Echo "Call a Windows batch file (*.cmd, *.bat) without showing the console window" & VbCrLf & VbCrLf &_
                 "Command line:" & VbCrLf & "WScript HideConsole.vbs MyBatchfile.cmd <command line arguments>"
    WScript.Quit 1
end if

argOut = ""
for i = 0 to num - 1
    argOut = argOut & """" & argIn.Item(i) & """ "
next

set WshShell = WScript.CreateObject("WScript.Shell")

WshShell.Run argOut, 0, True