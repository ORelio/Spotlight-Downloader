using System;
using System.IO;
using PrivilegeClass;
using System.Security.AccessControl;
using System.Security.Principal;

namespace SharpTools
{
    /// <summary>
    /// File system utilities for applications running as administrator
    /// By ORelio (c) 2018 - CDDL 1.0
    /// </summary>
    public static class FileSystemAdmin
    {
        /// <summary>
        /// Pass ownership and full control of the specified file or directory to the Administrators group. The process must run elevated to do this.
        /// </summary>
        /// <param name="path">File or directory</param>
        /// <param name="recursive">Recursively take ownership of directory contents</param>
        /// <exception cref="System.Security.AccessControl.PrivilegeNotHeldException">Insufficient process privileges to take ownership</exception>
        /// <seealso>https://stackoverflow.com/a/12999567</seealso>
        /// <seealso>https://stackoverflow.com/a/16216587</seealso>
        public static void GrantAll(string path, bool recursive = false)
        {
            Privilege processPrivilege = new Privilege(Privilege.TakeOwnership);
            SecurityIdentifier adminSid = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);

            try
            {
                processPrivilege.Enable();
                FileSecurity fileSecurity = new FileSecurity();
                //fileSecurity.SetOwner(new NTAccount(Environment.UserDomainName, Environment.UserName));
                fileSecurity.SetOwner(adminSid);
                File.SetAccessControl(path, fileSecurity);

                DirectoryInfo dInfo = new DirectoryInfo(path);
                DirectorySecurity dSecurity = dInfo.GetAccessControl();
                dSecurity.AddAccessRule(new FileSystemAccessRule(adminSid,
                    FileSystemRights.FullControl,
                    InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit,
                    PropagationFlags.NoPropagateInherit,
                    AccessControlType.Allow));
                dInfo.SetAccessControl(dSecurity);

                if (recursive)
                {
                    if ((File.GetAttributes(path) & FileAttributes.Directory) == FileAttributes.Directory)
                    {
                        foreach (string element in Directory.GetFiles(path, "*.*", SearchOption.TopDirectoryOnly))
                            GrantAll(element, recursive);
                        foreach (string element in Directory.GetDirectories(path, "*.*", SearchOption.TopDirectoryOnly))
                            GrantAll(element, recursive);
                    }
                }
            }
            finally
            {
                processPrivilege.Revert();
            }
        }

        /// <summary>
        /// Check if the current process is running with elevated/administrator permissions
        /// </summary>
        /// <returns>TRUE if running elevated</returns>
        public static bool IsAdmin()
        {
            AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);
            WindowsIdentity wi = WindowsIdentity.GetCurrent();
            WindowsPrincipal wp = new WindowsPrincipal(wi);
            return wp.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}
