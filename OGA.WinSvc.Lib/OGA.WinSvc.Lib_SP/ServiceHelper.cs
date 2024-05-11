using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Management;
using System.Runtime.InteropServices;

namespace OGA.WinSvc
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    static public class ServiceHelper
    {
        private const int STANDARD_RIGHTS_REQUIRED = 0xF0000;
        private const int SERVICE_WIN32_OWN_PROCESS = 0x00000010;
        private const uint SERVICE_NO_CHANGE = 0xFFFFFFFF;
        const int SERVICE_CONFIG_DESCRIPTION = 0x01;

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private class SERVICE_STATUS
        {
            public int dwServiceType = 0;
            public ServiceState dwCurrentState = 0;
            public int dwControlsAccepted = 0;
            public int dwWin32ExitCode = 0;
            public int dwServiceSpecificExitCode = 0;
            public int dwCheckPoint = 0;
            public int dwWaitHint = 0;
        }

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        public struct SERVICE_STATUS_PROCESS
        {
            public int serviceType;
            public int currentState;
            public int controlsAccepted;
            public int win32ExitCode;
            public int serviceSpecificExitCode;
            public int checkPoint;
            public int waitHint;
            public int processID;
            public int serviceFlags;
        }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        #region DLL References

        #region OpenSCManager
        [System.Runtime.InteropServices.DllImport("advapi32.dll", EntryPoint = "OpenSCManagerW", ExactSpelling = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
        static extern IntPtr OpenSCManager(string machineName, string databaseName, ScmAccessRights dwDesiredAccess);
        #endregion

        #region OpenService
        [System.Runtime.InteropServices.DllImport("advapi32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        static extern IntPtr OpenService(IntPtr hSCManager, string lpServiceName, ServiceAccessRights dwDesiredAccess);
        #endregion

        #region CreateService
        [System.Runtime.InteropServices.DllImport("advapi32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern IntPtr CreateService(IntPtr hSCManager, string lpServiceName, string lpDisplayName, ServiceAccessRights dwDesiredAccess,
                                                    int dwServiceType, ServiceBootFlag dwStartType, ServiceError dwErrorControl, string lpBinaryPathName,
                                                    string lpLoadOrderGroup, IntPtr lpdwTagId, string lpDependencies, string lpServiceStartName, string lpPassword);
        #endregion

        #region CloseServiceHandle
        [System.Runtime.InteropServices.DllImport("advapi32.dll", SetLastError = true)]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        static extern bool CloseServiceHandle(IntPtr hSCObject);
        #endregion

        #region QueryServiceStatus
        [System.Runtime.InteropServices.DllImport("advapi32.dll")]
        private static extern int QueryServiceStatus(IntPtr hService, SERVICE_STATUS lpServiceStatus);

        [System.Runtime.InteropServices.DllImport("advapi32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
        static extern bool QueryServiceStatusEx(IntPtr serviceHandle, int infoLevel, IntPtr buffer, int bufferSize, out int bytesNeeded);
        #endregion

        #region DeleteService
        [System.Runtime.InteropServices.DllImport("advapi32.dll", SetLastError = true)]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool DeleteService(IntPtr hService);
        #endregion

        #region ControlService
        [System.Runtime.InteropServices.DllImport("advapi32.dll")]
        private static extern int ControlService(IntPtr hService, ServiceControl dwControl, SERVICE_STATUS lpServiceStatus);
        #endregion

        #region StartService
        [System.Runtime.InteropServices.DllImport("advapi32.dll", SetLastError = true)]
        private static extern int StartService(IntPtr hService, int dwNumServiceArgs, int lpServiceArgVectors);
        #endregion

        #region Set Service Properties

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        [System.Runtime.InteropServices.DllImport("advapi32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        public static extern Boolean ChangeServiceConfig(
               IntPtr hService,
               UInt32 nServiceType,
               UInt32 nStartType,
               UInt32 nErrorControl,
               String lpBinaryPathName,
               String lpLoadOrderGroup,
               IntPtr lpdwTagId,
               [In] char[] lpDependencies,
               String lpServiceStartName,
               String lpPassword,
               String lpDisplayName);


        [System.Runtime.InteropServices.DllImport("advapi32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        public static extern bool ChangeServiceConfig2(IntPtr hService, int dwInfoLevel, [System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Struct)] ref SERVICE_DESCRIPTION lpInfo);

        #endregion

        #region Get Service Configuration
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        public struct SERVICE_DESCRIPTION
        {
            public string lpDescription;
        }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        #endregion

        #endregion

        /// <summary>
        /// Attempts to stop and uninstall the service by name.
        /// Retursn   1 if the service was successfully stopped and uninstalled.
        /// Returns   0 if the service was not found.
        /// Returns  -1 if the service stop timed out before the service reported as stopped.
        /// Returns  -3 if unable to query the status of the service.
        /// Returns  -4 if unable to tell the SCM to to stop the service.
        /// Returns  -8 if the service could not be delete from the SCM database.
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        static public int Uninstall(string serviceName)
        {
            // Call the uninstall method with a default wait time of 20 seconds.
            return Uninstall(serviceName, 20000);
        }
        /// <summary>
        /// Attempts to stop and uninstall the service by name.
        /// Retursn   1 if the service was successfully stopped and uninstalled.
        /// Returns   0 if the service was not found.
        /// Returns  -1 if the service stop timed out before the service reported as stopped.
        /// Returns  -3 if unable to query the status of the service.
        /// Returns  -4 if unable to tell the SCM to to stop the service.
        /// Returns  -8 if the service could not be delete from the SCM database.
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="waittimeout"></param>
        /// <returns></returns>
        static public int Uninstall(string serviceName, int waittimeout)
        {
            // Get a reference to the SCM.
            IntPtr scm = OpenSCManager(ScmAccessRights.AllAccess);

            // See if we got a reference to the SCM.
            if (scm == IntPtr.Zero)
            {
                // Could not get a reference to the SCM.
                return -1;
            }
            // We have a handle to the SCM.

            try
            {
                // Attempt to get a reference to the service by name.
                IntPtr service = OpenService(scm, serviceName, ServiceAccessRights.AllAccess);
                if (service == IntPtr.Zero)
                {
                    // A service by the given name is not installed.
                    return 0;
                }
                // A service handle was received.

                try
                {
                    // Attempt to stop the service.
                    // Returns   1 if the service was successfully stopped.
                    // Returns   0 if the service was already stopped.
                    // Returns  -1 if the service stop timed out before the service reported as stopped.
                    // Returns  -3 if unable to query the status of the service.
                    // Returns  -4 if unable to tell the SCM to to stop the service.
                    int Result = StopService(service, waittimeout);
                    if(Result < 0)
                    {
                        // An error occurred while attempting to stop the service.
                        // We will return the error to the caller.
                        return Result;
                    }
                    // If here, we stopped the service, or it was already stopped.
                    // Either way, we can uninstall it.

                    // Uninstall the service.
                    if (!DeleteService(service))
                    {
                        // Could not delete the service from the SCM database.
                        return -8;
                    }
                    // If here, we have deleted the service from the SCM database.

                    // Return success.
                    return 1;
                }
                finally
                {
                    // Release the service handle.
                    CloseServiceHandle(service);
                }
            }
            finally
            {
                // Release the SCM handle.
                CloseServiceHandle(scm);
            }
        }

        /// <summary>
        /// Attempts to uninstall any service who's servicename begins with the given rootname.
        /// This handles services that have multiple versions installed.
        /// </summary>
        /// <param name="serviceName_root"></param>
        /// <returns></returns>
        static public int Uninstall_all_Instances_of_ServiceNameRoot(string serviceName_root)
        {
            try
            {
                OGA.SharedKernel.Logging_Base.Logger_Ref?.Debug(
                    "Attempting to uninstall any previous service instances...");

                // Uninstall any previous instance of the service.
                List<string> svcinstances = ServiceHelper.Get_ServiceList_Matching_Rootname(serviceName_root);
                if (svcinstances.Count == 0)
                {
                    OGA.SharedKernel.Logging_Base.Logger_Ref?.Debug(
                        "No previous service instances exist.");
                }
                else
                {
                    OGA.SharedKernel.Logging_Base.Logger_Ref?.Debug(
                        "There are {0} previous service instance(s) to uninstall, with the matching servicenameroot={1}.",
                        svcinstances.Count.ToString(),
                        serviceName_root);

                    // Loop through each previous service instance.
                    foreach (string svcinst in svcinstances)
                    {
                        try
                        {
                            OGA.SharedKernel.Logging_Base.Logger_Ref?.Debug(
                                "Attempting to uninstall previous service instance named, {0}.",
                                svcinst);

                            // Uninstall the service.
                            ServiceHelper.Uninstall(svcinst, 15000);

                            OGA.SharedKernel.Logging_Base.Logger_Ref?.Debug(
                                "Previous service instance named, {0}, was uninstalled.", svcinst);
                        }
                        catch (Exception e)
                        {
                            OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(e,
                                "An exception was caught while uninstalling service, " + svcinst + ".");
                        }
                    }
                }
                // If here, we have uninstalled all previous versions of the given servicename root.

                return 1;
            }
            catch (Exception e)
            {
                OGA.SharedKernel.Logging_Base.Logger_Ref?.Error(e,
                    "An exception was caught while uninstalling instances of the service with the matching servicenameroot={1}.",
                    serviceName_root);

                return -2;
            }
        }


        /// <summary>
        /// Returns  1 if the service is installed.
        /// Returns  0 if not installed.
        /// Returns -1 if SCM access is declined.
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        static public int ServiceIsInstalled(string serviceName)
        {
            // Get a reference to the SCM.
            IntPtr scm = OpenSCManager(ScmAccessRights.AllAccess);

            // See if we got a reference to the SCM.
            if (scm == IntPtr.Zero)
            {
                // Could not get a reference to the SCM.
                return -1;
            }
            // We have a handle to the SCM.

            try
            {
                // Attempt to get a reference to the service by name.
                IntPtr service = OpenService(scm, serviceName, ServiceAccessRights.AllAccess);
                if (service == IntPtr.Zero)
                {
                    // A service by the given name is not installed.
                    // Return zero for not installed.
                    return 0;
                }
                // A service handle was received.
                // This means the service is installed.

                // Close the service handle and return success.
                CloseServiceHandle(service);
                return 1;
            }
            finally
            {
                // Release the SCM handle.
                CloseServiceHandle(scm);
            }
        }

        /// <summary>
        /// Service installer that configures the service with the Local_System account.
        /// The binary path can include arguments that are passed to main.
        /// The binary path can include arguments that are passed to main.
        /// Returns   1 if the service is installed.
        /// Returns   0 if the service is already running.
        /// Returns  -1 if SCM access is declined.
        /// Returns  -2 if unable to install the service.
        /// Returns  -3 if unable to query the status of the service.
        /// Returns  -4 if unable to tell the SCM to to start the service.
        /// Returns  -6 if the service stop timed out before the service reported as running.
        /// Returns  -7 if unable to change the service description.
        /// Returns -10 if service executable is not found in the filesystem.
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="displayName"></param>
        /// <param name="binpath"></param>
        /// <param name="args"></param>
        /// <param name="description"></param>
        /// <param name="waittime"></param>
        /// <returns></returns>
		static public int InstallAndStart(string serviceName, string displayName, string binpath, string args, string description, int waittime)
        {
            // Call the install and start method with NULL for username and password.
            // This will register the service as the Local_System user.
            return InstallAndStart(serviceName, displayName, binpath, args, description, waittime, null, null);
        }
        /// <summary>
        /// Service installer that configures the service with the Local_System account.
        /// The binary path can include arguments that are passed to main.
        /// The binary path can include arguments that are passed to main.
        /// Returns   1 if the service is installed.
        /// Returns   0 if the service is already running.
        /// Returns  -1 if SCM access is declined.
        /// Returns  -2 if unable to install the service.
        /// Returns  -3 if unable to query the status of the service.
        /// Returns  -4 if unable to tell the SCM to to start the service.
        /// Returns  -6 if the service stop timed out before the service reported as running.
        /// Returns  -7 if unable to change the service description.
        /// Returns -10 if service executable is not found in the filesystem.
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="displayName"></param>
        /// <param name="binpath"></param>
        /// <param name="args"></param>
        /// <param name="description"></param>
        /// <returns></returns>
		static public int InstallAndStart(string serviceName, string displayName, string binpath, string args, string description)
        {
            // Call the install and start method with NULL for username and password.
            // This will register the service as the Local_System user.
            return InstallAndStart(serviceName, displayName, binpath, args, description, 20000, null, null);
        }
        /// <summary>
        /// Service installer that configures the service with the Local_System account.
        /// The binary path can include arguments that are passed to main.
        /// The binary path can include arguments that are passed to main.
        /// Returns   1 if the service is installed.
        /// Returns   0 if the service is already running.
        /// Returns  -1 if SCM access is declined.
        /// Returns  -2 if unable to install the service.
        /// Returns  -3 if unable to query the status of the service.
        /// Returns  -4 if unable to tell the SCM to to start the service.
        /// Returns  -6 if the service stop timed out before the service reported as running.
        /// Returns  -7 if unable to change the service description.
        /// Returns -10 if service executable is not found in the filesystem.
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="displayName"></param>
        /// <param name="binpath"></param>
        /// <param name="args"></param>
        /// <param name="description"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
		static public int InstallAndStart(string serviceName, string displayName, string binpath, string args, string description, string username, string password)
        {
            // Call the install and start method with NULL for username and password.
            // This will register the service as the Local_System user.
            return InstallAndStart(serviceName, displayName, binpath, args, description, 20000, username, password);
        }
        /// <summary>
        /// Service installer that accepts a username and password.
        /// The binary path can include arguments that are passed to main.
        /// Returns   1 if the service is installed.
        /// Returns   0 if the service is already running.
        /// Returns  -1 if SCM access is declined.
        /// Returns  -2 if unable to install the service.
        /// Returns  -3 if unable to query the status of the service.
        /// Returns  -4 if unable to tell the SCM to to start the service.
        /// Returns  -6 if the service stop timed out before the service reported as running.
        /// Returns  -7 if unable to change the service description.
        /// Returns -10 if service executable is not found in the filesystem.
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="displayName"></param>
        /// <param name="binpath"></param>
        /// <param name="args"></param>
        /// <param name="description"></param>
        /// <param name="waittime"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        static public int InstallAndStart(string serviceName, string displayName, string binpath, string args, string description, int waittime, string username, string password)
        {
            string binpath_with_args = "";
            IntPtr scm = IntPtr.Zero;
            IntPtr service = IntPtr.Zero;

            try
            {
                if (System.IO.File.Exists(binpath) == false)
                {
                    // Service executable is not accessible.
                    return -10;
                }
                // The executable exists.

                // Get a reference to the SCM.
                scm = OpenSCManager(ScmAccessRights.AllAccess);

                // See if we got a reference to the SCM.
                if (scm == IntPtr.Zero)
                {
                    // Could not get a reference to the SCM.
                    return -1;
                }
                // We have a handle to the SCM.

                try
                {
                    if (args == null)
                        args = "";

                    // Compose the full command line argument.
                    if (args.Length > 0)
                        binpath_with_args = binpath + " " + args;
                    else
                        binpath_with_args = binpath;

                    // Attempt to get a reference to the service by name.
                    service = OpenService(scm, serviceName, ServiceAccessRights.AllAccess);

                    // See if we got a handle to a service.
                    if (service == IntPtr.Zero)
                    {
                        // No handle.
                        // The service is not installed.

                        // Install it.
                        service = CreateService(scm,                            // IntPtr hSCManager
                                                serviceName,                    // string lpServiceName
                                                displayName,                    // string lpDisplayName
                                                ServiceAccessRights.AllAccess,  // ServiceAccessRights dwDesiredAccess
                                                SERVICE_WIN32_OWN_PROCESS,      // int dwServiceType
                                                ServiceBootFlag.AutoStart,      // ServiceBootFlag dwStartType
                                                ServiceError.Normal,            // ServiceError dwErrorControl
                                                binpath_with_args,              // string lpBinaryPathName
                                                null,                           // string lpLoadOrderGroup
                                                IntPtr.Zero,                    // IntPtr lpdwTagId
                                                null,                           // string lpDependencies
                                                username,                       // string lpServiceStartName
                                                password);                      // string lpPassword

                        // See if the service handle is still null.
                        if (service == IntPtr.Zero)
                        {
                            // Unable to install the service.
                            return -2;
                        }
                        // We installed the service successfully.

                        // Update the description of our new service.
                        // This is done in a secondary call since the create service API call has no input for service description.
                        var pinfo = new SERVICE_DESCRIPTION
                        {
                            lpDescription = description
                        };

                        // Tell the SCM to change the service description for us.
                        if (ChangeServiceConfig2(service, SERVICE_CONFIG_DESCRIPTION, ref pinfo) == false)
                        {
                            // Unable to change the service description.
                            return -7;
                        }
                    }
                    // If here, the service name exists, or we have created the new service and set its description.
                    // Either way, the service is installed.
                    // We need to attempt to start it.

                    // Attempt to start the service.
                    // Returns   1 if the service was successfully started.
                    // Returns   0 if the service was already running.
                    // Returns  -3 if unable to query the status of the service.
                    // Returns  -4 if unable to tell the SCM to to start the service.
                    // Returns  -6 if the service stop timed out before the service reported as running.
                    int res = StartService(service, waittime);
                    return res;
                }
                finally
                {
                    // Release the service handle.
                    CloseServiceHandle(service);
                }
            }
            finally
            {
                // Release the SCM handle.
                CloseServiceHandle(scm);
            }
        }

        /// <summary>
        /// Service installer that accepts a username and password.
        /// The binary path can include arguments that are passed to main.
        /// Returns   1 if the service is installed.
        /// Returns   0 if the service is already running.
        /// Returns  -1 if SCM access is declined.
        /// Returns  -2 if unable to install the service.
        /// Returns  -3 if unable to query the status of the service.
        /// Returns  -4 if unable to tell the SCM to to start the service.
        /// Returns  -6 if the service stop timed out before the service reported as running.
        /// Returns  -7 if unable to change the service description.
        /// Returns -10 if service executable is not found in the filesystem.
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="displayName"></param>
        /// <param name="binpath"></param>
        /// <param name="args"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        static public int Install_Service(string serviceName, string displayName, string binpath, string args, string description)
        {
            return Install_Service(serviceName, displayName, binpath, args, description, null, null);
        }
        /// <summary>
        /// Service installer that accepts a username and password.
        /// The binary path can include arguments that are passed to main.
        /// Returns   1 if the service is installed.
        /// Returns   0 if the service is already running.
        /// Returns  -1 if SCM access is declined.
        /// Returns  -2 if unable to install the service.
        /// Returns  -3 if unable to query the status of the service.
        /// Returns  -4 if unable to tell the SCM to to start the service.
        /// Returns  -6 if the service stop timed out before the service reported as running.
        /// Returns  -7 if unable to change the service description.
        /// Returns -10 if service executable is not found in the filesystem.
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="displayName"></param>
        /// <param name="binpath"></param>
        /// <param name="args"></param>
        /// <param name="description"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        static public int Install_Service(string serviceName, string displayName, string binpath, string args, string description, string username, string password)
        {
            string binpath_with_args = "";

            if (System.IO.File.Exists(binpath) == false)
            {
                // Service executable is not accessible.
                return -10;
            }
            // The executable exists.

            // Get a reference to the SCM.
            IntPtr scm = OpenSCManager(ScmAccessRights.AllAccess);

            // See if we got a reference to the SCM.
            if (scm == IntPtr.Zero)
            {
                // Could not get a reference to the SCM.
                return -1;
            }
            // We have a handle to the SCM.

            if (args == null)
                args = "";

            // Compose the full command line argument.
            if(args.Length > 0)
                binpath_with_args = binpath + " " + args;
            else
                binpath_with_args = binpath;

            try
            {
                // Attempt to get a reference to the service by name.
                IntPtr service = OpenService(scm, serviceName, ServiceAccessRights.AllAccess);

                // See if we got a handle to a service.
                if (service == IntPtr.Zero)
                {
                    // No handle.
                    // The service is not installed.

                    // Install it.
                    service = CreateService(scm,                            // IntPtr hSCManager
                                            serviceName,                    // string lpServiceName
                                            displayName,                    // string lpDisplayName
                                            ServiceAccessRights.AllAccess,  // ServiceAccessRights dwDesiredAccess
                                            SERVICE_WIN32_OWN_PROCESS,      // int dwServiceType
                                            ServiceBootFlag.AutoStart,      // ServiceBootFlag dwStartType
                                            ServiceError.Normal,            // ServiceError dwErrorControl
                                            binpath_with_args,              // string lpBinaryPathName
                                            null,                           // string lpLoadOrderGroup
                                            IntPtr.Zero,                    // IntPtr lpdwTagId
                                            null,                           // string lpDependencies
                                            username,                       // string lpServiceStartName
                                            password);                      // string lpPassword

                    // See if the service handle is still null.
                    if (service == IntPtr.Zero)
                    {
                        // Unable to install the service.

                        string errorMessage = new System.ComponentModel.Win32Exception(System.Runtime.InteropServices.Marshal.GetLastWin32Error()).Message;

                        return -2;
                    }
                    // We installed the service successfully.

                    // Update the description of our new service.
                    // This is done in a secondary call since the create service API call has no input for service description.
                    var pinfo = new SERVICE_DESCRIPTION
                    {
                        lpDescription = description
                    };

                    // Tell the SCM to change the service description for us.
                    if (ChangeServiceConfig2(service, SERVICE_CONFIG_DESCRIPTION, ref pinfo) == false)
                    {
                        // Unable to change the service description.
                        return -7;
                    }
                }
                // If here, the service name exists, or we have created the new service and set its description.
                // Either way, the service is installed.

                // Release the service handle.
                CloseServiceHandle(service);

                return 1;
            }
            finally
            {
                // Release the SCM handle.
                CloseServiceHandle(scm);
            }
        }

        /// <summary>
        /// Returns   1 if the service was successfully started.
        /// Returns   0 if the service was already running.
        /// Returns  -1 if SCM access is declined.
        /// Returns  -3 if unable to query the status of the service.
        /// Returns  -4 if unable to tell the SCM to to start the service.
        /// Returns  -6 if the service stop timed out before the service reported as running.
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        static public int StartService(string serviceName)
        {
            return StartService(serviceName, 20000);
        }
        /// <summary>
        /// Returns   1 if the service was successfully started.
        /// Returns   0 if the service was already running.
        /// Returns  -1 if SCM access is declined.
        /// Returns  -3 if unable to query the status of the service.
        /// Returns  -4 if unable to tell the SCM to to start the service.
        /// Returns  -6 if the service stop timed out before the service reported as running.
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="waittime"></param>
        /// <returns></returns>
        static public int StartService(string serviceName, int waittime)
        {
            // Get a reference to the SCM.
            IntPtr scm = OpenSCManager(ScmAccessRights.AllAccess);

            // See if we got a reference to the SCM.
            if (scm == IntPtr.Zero)
            {
                // Could not get a reference to the SCM.
                return -1;
            }
            // We have a handle to the SCM.

            try
            {
                // Attempt to get a reference to the service by name.
                IntPtr service = OpenService(scm, serviceName, ServiceAccessRights.QueryStatus | ServiceAccessRights.Start);
                if (service == IntPtr.Zero)
                {
                    // A service by the given name is not installed, or unable to start the service.
                    return -2;
                }
                // A service handle was received.

                try
                {
                    // Returns   1 if the service was successfully started.
                    // Returns   0 if the service was already running.
                    // Returns  -3 if unable to query the status of the service.
                    // Returns  -4 if unable to tell the SCM to to start the service.
                    // Returns  -6 if the service stop timed out before the service reported as running.
                    return StartService(service, waittime);
                }
                finally
                {
                    // Release the service handle
                    CloseServiceHandle(service);
                }
            }
            finally
            {
                // Release the SCM handle.
                CloseServiceHandle(scm);
            }
        }

        /// <summary>
        /// Returns   1 if the service was successfully stopped.
        /// Returns   0 if the service was already stopped.
        /// Returns  -1 if SCM access is declined.
        /// Returns  -3 if unable to query the status of the service.
        /// Returns  -4 if unable to tell the SCM to to stop the service.
        /// Returns  -6 if the service stop timed out before the service reported as stopped.
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="waittimeout"></param>
        /// <returns></returns>
        static public int StopService(string serviceName, int waittimeout)
        {
            // Get a reference to the SCM.
            IntPtr scm = OpenSCManager(ScmAccessRights.AllAccess);

            // See if we got a reference to the SCM.
            if (scm == IntPtr.Zero)
            {
                // Could not get a reference to the SCM.
                return -1;
            }
            // We have a handle to the SCM.

            try
            {
                // Attempt to get a reference to the service by name.
                IntPtr service = OpenService(scm, serviceName, ServiceAccessRights.QueryStatus | ServiceAccessRights.Stop);
                if (service == IntPtr.Zero)
                {
                    // A service by the given name is not installed, or don't have rights to stop it.
                    return -2;
                }
                // A service handle was received.

                try
                {
                    // Returns   1 if the service was successfully stopped.
                    // Returns   0 if the service was already stopped.
                    // Returns  -3 if unable to query the status of the service.
                    // Returns  -4 if unable to tell the SCM to to stop the service.
                    // Returns  -6 if the service stop timed out before the service reported as stopped.
                    return StopService(service, waittimeout);
                }
                finally
                {
                    // Release the service handle reference.
                    CloseServiceHandle(service);
                }
            }
            finally
            {
                // Release the SCM handle.
                CloseServiceHandle(scm);
            }
        }
        /// <summary>
        /// Attempts to stop the service by service name.
        /// Returns   1 if the service is installed.
        /// Returns  -1 if SCM access is declined.
        /// Returns  -2 if the service name was not found.
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        static public int StopService(string serviceName)
        {
            // Attempt to stop the service with a default wait time of 20 seconds.
            return StopService(serviceName, 20000);
        }

        /// <summary>
        /// Returns   1 if the service is installed.
        /// Returns  -1 if SCM access is declined.
        /// Returns  -2 if service not found.
        /// Returns  -3 if unable to query the status of the service.
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        static public (int res, ServiceState state) GetServiceStatus(string serviceName)
        {
            // Get a reference to the SCM.
            IntPtr scm = OpenSCManager(ScmAccessRights.AllAccess);

            // See if we got a reference to the SCM.
            if (scm == IntPtr.Zero)
            {
                // Could not get a reference to the SCM.
                return (-1, ServiceState.Unknown);
            }
            // We have a handle to the SCM.

            try
            {
                // Attempt to get a reference to the service by name.
                IntPtr service = OpenService(scm, serviceName, ServiceAccessRights.QueryStatus);
                if (service == IntPtr.Zero)
                {
                    // Service not found by name.
                    return (-2, ServiceState.NotFound);
                }
                // A service handle was received.

                try
                {
                    // Attempt to get the status of the service.
                    var resget = GetServiceState(service);
                    if(resget.res != 1)
                    {
                        // Unable to query the status of the service.
                        return (-3, ServiceState.Unknown);
                    }
                    // We have the service status.
                    return (1, resget.state);
                }
                finally
                {
                    CloseServiceHandle(service);
                }
            }
            finally
            {
                // Release the SCM handle.
                CloseServiceHandle(scm);
            }
        }

        /// <summary>
        /// Will determine the filepath of the service binary.
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        static public (int res, string binpath) GetServiceBinPath(string serviceName)
        {
#pragma warning disable CA1416 // Validate platform compatibility
#if (NET452 || NET48)
            Microsoft.Win32.RegistryKey hiveroot = null;
            Microsoft.Win32.RegistryKey key = null;
#else
            Microsoft.Win32.RegistryKey? hiveroot = null;
            Microsoft.Win32.RegistryKey? key = null;
#endif
            string basepath = "System\\CurrentControlSet\\Services\\";
            string servicekeypath = basepath + serviceName;
#if (NET452 || NET48)
            object regpath = "";
#else
            object? regpath = "";
#endif

            try
            {
                // Get a reference to the localmachine hive.
                hiveroot = Microsoft.Win32.Registry.LocalMachine;

                try
                {
                    // Get a reference to the service key.
                    key = hiveroot?.OpenSubKey(servicekeypath);
                    if(key == null)
                    {
                        // No image path defined.
                        return (-1, "");
                    }

                    // Attempt to get the bin path.
                    regpath = key?.GetValue("ImagePath");

                    if(regpath == null)
                    {
                        // No image path defined.
                        return (-1, "");
                    }

                    var binpath = (string)regpath;
                    return (1, binpath);
                }
                catch(Exception)
                {
                    return (-2, "");
                }
                finally
                {
                    try
                    {
                        key?.Close();
                    }
                    catch (Exception) { }
                    try
                    {
                        key?.Dispose();
                    }
                    catch (Exception) { }

                    key = null;
                }
            }
            finally
            {
                try
                {
                    hiveroot?.Close();
                }
                catch (Exception) { }
                try
                {
                    hiveroot?.Dispose();
                }
                catch (Exception) { }

                hiveroot = null;
            }
        }

        
        /// <summary>
        /// Makes a list of all service instances that begin with the given service name root.
        /// </summary>
        /// <param name="servicenameroot"></param>
        /// <returns></returns>
        static public List<string> Get_ServiceList_Matching_Rootname(string servicenameroot)
        {
            System.Collections.Generic.List<string> servicenames = new List<string>();

            try
            {
                // Get a list of all services in the machine.
                System.ServiceProcess.ServiceController[] s = System.ServiceProcess.ServiceController.GetServices();

                // Iterate the list to find all the matches.
                foreach(var t in s)
                {
                    // See if the current service starts with the given rootname.
                    if(t.ServiceName.StartsWith(servicenameroot) == true)
                    {
                        // Add the found service to the list.
                        servicenames.Add(t.ServiceName);
                    }
                }

                // Return the number of found services to the caller.
                return servicenames;
            }
            catch (Exception)
            {
                return servicenames;
            }
        }

        /// <summary>
        /// Call to change a service startup mode.
        /// </summary>
        /// <param name="servicename"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        static public int ChangeStartMode(string servicename, ServiceBootFlag mode)
        {
            IntPtr scm = IntPtr.Zero;
            IntPtr service = IntPtr.Zero;

            try
            {
                // Get a reference to the SCM.
                scm = OpenSCManager(ScmAccessRights.AllAccess);

                // See if we got a reference to the SCM.
                if (scm == IntPtr.Zero)
                {
                    // Could not get a reference to the SCM.
                    return -1;
                }
                // We have a handle to the SCM.

                try
                {
                    // Attempt to get a reference to the service by name.
                    service = OpenService(scm, servicename, ServiceAccessRights.ChangeConfig | ServiceAccessRights.QueryConfig);

                    // See if we got a handle to a service.
                    if (service == IntPtr.Zero)
                    {
                        // No handle.
                        // The service is not installed.

                        return -1;
                    }
                    // If here, the service was found.

                    var result = ChangeServiceConfig(
                        service,                // IntPtr hService,
                        SERVICE_NO_CHANGE,      // UInt32 nServiceType,
                        (uint)mode,             // UInt32 nStartType,
                        SERVICE_NO_CHANGE,      // UInt32 nErrorControl,
                        null,                   // String lpBinaryPathName,
                        null,                   // String lpLoadOrderGroup,
                        IntPtr.Zero,            // IntPtr lpdwTagId,
                        null,                   // [In] char[] lpDependencies,
                        null,                   // String lpServiceStartName,
                        null,                   // String lpPassword,
                        null);                  // String lpDisplayName);

                    if (result == false)
                    {
                        int nError = Marshal.GetLastWin32Error();
                        var win32Exception = new Win32Exception(nError);
                        //throw new ExternalException("Could not change service start type: "
                        //    + win32Exception.Message);

                        // Unable to change the service mode.
                        return -7;
                    }

                    return 1;
                }
                finally
                {
                    CloseServiceHandle(service);
                }
            }
            finally
            {
                CloseServiceHandle(scm);
            }
        }

        /// <summary>
        /// Retrieves the startup mode for a given service.
        /// </summary>
        /// <param name="servicename"></param>
        /// <returns></returns>
        static public ServiceBootFlag GetStartupType_byName(string servicename)
        {
            string wmiQuery = "Select StartMode from Win32_Service where Name='" + servicename + "'";

            ManagementObjectSearcher wmi = new ManagementObjectSearcher(wmiQuery);
            ManagementObjectCollection coll = wmi.Get();

            foreach (var service in coll)
            {
                var st = service["StartMode"].ToString();

                if (st == "Auto")
                    return ServiceBootFlag.AutoStart;
                else if (st == "Manual")
                    return ServiceBootFlag.DemandStart;
                else if (st == "Disabled")
                    return ServiceBootFlag.Disabled;
            }

            return 0;
        }
        /// <summary>
        /// Retrieves the startup mode for a service, by display name.
        /// </summary>
        /// <param name="servicename"></param>
        /// <returns></returns>
        static public ServiceBootFlag GetStartupType_ByDisplayName(string servicename)
        {
            string wmiQuery = "Select StartMode from Win32_Service where DisplayName='" + servicename + "'";

            ManagementObjectSearcher wmi = new ManagementObjectSearcher(wmiQuery);
            ManagementObjectCollection coll = wmi.Get();

            foreach (var service in coll)
            {
                var st = service["StartMode"].ToString();
#pragma warning restore CA1416 // Validate platform compatibility

                if (st == "Auto")
                    return ServiceBootFlag.AutoStart;
                else if (st == "Manual")
                    return ServiceBootFlag.DemandStart;
                else if (st == "Disabled")
                    return ServiceBootFlag.Disabled;
            }

            return 0;
        }

        #region Private Methods

        /// <summary>
        /// Attempts to start the service.
        /// Returns   1 if the service was successfully started.
        /// Returns   0 if the service was already running.
        /// Returns  -3 if unable to query the status of the service.
        /// Returns  -4 if unable to tell the SCM to to start the service.
        /// Returns  -6 if the service stop timed out before the service reported as running.
        /// </summary>
        /// <param name="service"></param>
        /// <returns></returns>
        static private int StartService(IntPtr service)
        {
            // Call the service start method with a default wait time of 20 seconds.
            return StartService(service, 20000);
        }
        /// <summary>
        /// Attempts to start the service.
        /// Returns   1 if the service was successfully started.
        /// Returns   0 if the service was already running.
        /// Returns  -1 if the service stop timed out before the service reported as running.
        /// Returns  -3 if unable to query the status of the service.
        /// Returns  -4 if unable to tell the SCM to to start the service.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="waittime"></param>
        /// <returns></returns>
        static private int StartService(IntPtr service, int waittime)
        {
            int thread_sleepdelay = 200;
            int default_waittime = waittime;
            DateTime starttime = System.DateTime.Now;
            DateTime waitexpiry = starttime.AddMilliseconds(default_waittime);
            // Declare a service status instance that the stop request will populate for us.
            SERVICE_STATUS status = new SERVICE_STATUS();

            // Get the current status of the service.
            if (QueryServiceStatus(service, status) == 0)
            {
                // Unable to get the status of the service.
                return -3;
            }
            // See what the current state is.

            // See if the service is already running.
            if (status.dwCurrentState == ServiceState.Running)
            {
                // The service is already running.
                // Nothing to do.
                return 0;
            }
            // The service is not already running.

            // Loop while the service is in a start pending state.
            // We will hop out after a timeout.
            while (status.dwCurrentState == ServiceState.StartPending)
            {
                // See if a wait hint was given by the service.
                if (status.dwWaitHint == 0)
                {
                    // No wait hint was given.
                    default_waittime = 1000;
                }
                else
                {
                    // A wait hint was given by the service.

                    // Set the wait time to a fraction of the wait hint.
                    default_waittime = status.dwWaitHint / 10;

                    // Apply some boundaries so the wait time is reasonable.
                    if (default_waittime < 1000)
                        // Wait time is less than a second.
                        // Wait a second.
                        default_waittime = 1000;
                    else if (default_waittime > 10000)
                        // Cap it at ten seconds.
                        default_waittime = 10000;
                }

                // Sleep for the wait time.
                System.Threading.Thread.Sleep(thread_sleepdelay);

                // Get the current state.
                status = new SERVICE_STATUS();
                if (QueryServiceStatus(service, status) == 0)
                {
                    // Unable to get the status of the service.
                    return -3;
                }

                // See if the service is now started.
                if (status.dwCurrentState == ServiceState.Running)
                {
                    // The service is now running.
                    return 1;
                }
                // The service is not yet running.

                // See if our timeout has expired.
                if (waitexpiry.CompareTo(System.DateTime.Now) < 0)
                {
                    // The wait expiry has elapsed.
                    // We have waited too long for the service to start.
                    return -6;
                }
                // If here, we have not exceeded our timeout.
            }
            // The service is not running, and is not starting.
            // We will need to send it a start and wait for it.

            try
            {
                // Tell the SCM to initiate a start on the service.
                if (StartService(service, 0, 0) == 0)
                {
                    // An error occurred while attempting to tell the SCM to start the service.
                    return -4;
                }
                // We told the service to start.
            }
            catch (Exception) { }

            // Sleep for the wait time.
            System.Threading.Thread.Sleep(thread_sleepdelay);

            // Get the current state.
            status = new SERVICE_STATUS();
            if (QueryServiceStatus(service, status) == 0)
            {
                // Unable to get the status of the service.
                return -3;
            }
            // We have its latest status.

            // Loop while the service is not running.
            // We will hop out after a timeout.
            while (status.dwCurrentState != ServiceState.Running)
            {
                // The service is not yet running.

                // Sleep for the wait time.
                System.Threading.Thread.Sleep(thread_sleepdelay);

                // Get the current state.
                status = new SERVICE_STATUS();
                if (QueryServiceStatus(service, status) == 0)
                {
                    // Unable to get the status of the service.
                    return -3;
                }
                // We have the latest service status.

                // See if the service is now running.
                if (status.dwCurrentState == ServiceState.Running)
                {
                    // The service is now running.
                    return 1;
                }
                // The service is not yet stopped.

                // See if our timeout has expired.
                if (waitexpiry.CompareTo(System.DateTime.Now) < 0)
                {
                    // The wait expiry has elapsed.
                    // We have waited too long for the service to start.
                    return -6;
                }
                // If here, we have not exceeded our timeout.
            }
            // If here, the service is running.

            // Return success to the caller.
            return 1;
        }

        /// <summary>
        /// Attempts to stop the service.
        /// Returns   1 if the service was successfully stopped.
        /// Returns   0 if the service was already stopped.
        /// Returns  -3 if unable to query the status of the service.
        /// Returns  -4 if unable to tell the SCM to to stop the service.
        /// Returns  -6 if the service stop timed out before the service reported as stopped.
        /// </summary>
        /// <param name="service"></param>
        /// <returns></returns>
        static private int StopService(IntPtr service)
        {
            // Call the service stop method with a default wait time of 20 seconds.
            return StopService(service, 20000);
        }
        /// <summary>
        /// Attempts to stop the service.
        /// Returns   1 if the service was successfully stopped.
        /// Returns   0 if the service was already stopped.
        /// Returns  -1 if the service stop timed out before the service reported as stopped.
        /// Returns  -3 if unable to query the status of the service.
        /// Returns  -4 if unable to tell the SCM to to stop the service.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="waittime"></param>
        /// <returns></returns>
        static private int StopService(IntPtr service, int waittime)
        {
            // The SCM processes services one at a time, serially.
            // It is possible that the SCM is busy with another service when this method is called.
            // A call to ControlService will block for 30 seconds if any service is busy handling a control code.
            // If the busy service still has not returned from its handler function when the timeout expires,
            //  ControlService fails with ERROR_SERVICE_REQUEST_TIMEOUT.
            int controlService_api_call_maxblocking_time = 30000;
            int thread_sleepdelay = 200;
            int default_waittime = waittime;
            DateTime starttime = System.DateTime.Now;
            DateTime waitexpiry = starttime.AddMilliseconds(default_waittime);
            // Declare a service status instance that the stop request will populate for us.
            SERVICE_STATUS status = new SERVICE_STATUS();

            // Get the current status of the service.
            if (QueryServiceStatus(service, status) == 0)
            {
                // Unable to get the status of the service.
                return -3;
            }
            // See what the current state is.

            // See if the service is already stopped.
            if (status.dwCurrentState == ServiceState.Stopped)
            {
                // The service is already stopped.
                // Nothing to do.
                return 0;
            }
            // The service is not already stopped.

            // Loop while the service is in a stop pending state.
            // We will hop out after a timeout.
            while (status.dwCurrentState == ServiceState.StopPending)
            {
                // See if a wait hint was given by the service.
                if (status.dwWaitHint == 0)
                {
                    // No wait hint was given.
                    default_waittime = 1000;
                }
                else
                {
                    // A wait hint was given by the service.

                    // Set the wait time to a fraction of the wait hint.
                    default_waittime = status.dwWaitHint / 10;

                    // Apply some boundaries so the wait time is reasonable.
                    if (default_waittime < 1000)
                        // Wait time is less than a second.
                        // Wait a second.
                        default_waittime = 1000;
                    else if (default_waittime > 10000)
                        // Cap it at ten seconds.
                        default_waittime = 10000;
                }

                // Sleep for the wait time.
                System.Threading.Thread.Sleep(thread_sleepdelay);

                // Get the current state.
                status = new SERVICE_STATUS();
                if (QueryServiceStatus(service, status) == 0)
                {
                    // Unable to get the status of the service.
                    return -3;
                }

                // See if the service is now stopped.
                if (status.dwCurrentState == ServiceState.Stopped)
                {
                    // The service is now stopped.
                    return 1;
                }
                // The service is not yet stopped.

                // See if our timeout has expired.
                if (waitexpiry.CompareTo(System.DateTime.Now) < 0)
                {
                    // The wait expiry has elapsed.
                    // We have waited too long for the service to stop.
                    return -6;
                }
                // If here, we have not exceeded our timeout.
            }
            // The service is not stopped, and is not stopping.
            // We will need to send it a stop and wait for it.

            // Since the controlservice API call can block for 30 seconds,
            //  we will set a timeout in the below loop at that value.
            // Update the expiry to be a full 30 seconds from now.
            waitexpiry = System.DateTime.Now.AddMilliseconds(controlService_api_call_maxblocking_time);

            // Tell the SCM to initiate a stop on the service.
            status = new SERVICE_STATUS();
            if (ControlService(service, ServiceControl.Stop, status) == 0)
            {
                // An error occurred while attempting to tell the SCM to stop the service.

                int error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();

                return -4;
            }
            // We told the service to stop.
            // And, we have its latest status.

            // Loop while the service is not stopped.
            // We will hop out after a timeout.
            while (status.dwCurrentState != ServiceState.Stopped)
            {
                // The service is not yet stopped.

                // Sleep for the wait time.
                System.Threading.Thread.Sleep(thread_sleepdelay);

                // Get the current state.
                status = new SERVICE_STATUS();
                if (QueryServiceStatus(service, status) == 0)
                {
                    // Unable to get the status of the service.
                    return -3;
                }
                // We have the latest service status.

                // See if the service is now stopped.
                if (status.dwCurrentState == ServiceState.Stopped)
                {
                    // The service is now stopped.
                    return 1;
                }
                // The service is not yet stopped.

                // See if our timeout has expired.
                if (waitexpiry.CompareTo(System.DateTime.Now) < 0)
                {
                    // The wait expiry has elapsed.
                    // We have waited too long for the service to stop.
                    return -6;
                }
                // If here, we have not exceeded our timeout.
            }
            // If here, the service has stopped.

            // Return success to the caller.
            return 1;
        }

        /// <summary>
        /// Attempts to retrieve the current status of the service.
        /// Returns 1 if the service status was retrieved.
        /// Returns zero if the service status could not be queried.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        static private (int res, ServiceState state) GetServiceState(IntPtr service)
        {
            // Create a service status instance that we will pass to the query function.
            SERVICE_STATUS status = new SERVICE_STATUS();

            // Attempt to get the status from the SCM.
            if (QueryServiceStatus(service, status) == 0)
            {
                // Failed to query for service status.
                return (0, ServiceState.Unknown);
            }
            // We have the status of the service.

            // Return it to the caller.
            return (1, status.dwCurrentState);
        }

        /// <summary>
        /// Returns a reference to the SCM.
        /// Returns intPtr.Zero if not found.
        /// </summary>
        /// <param name="rights"></param>
        /// <returns></returns>
        static private IntPtr OpenSCManager(ScmAccessRights rights)
        {
            // Call the DLL reference to get the SVM for the local machine.
            IntPtr scm = OpenSCManager(null, null, rights);

            // See if an error occurred.
            string errorMessage = new System.ComponentModel.Win32Exception().Message;
            //int error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();

            //if (scm == IntPtr.Zero)
            //    throw new ApplicationException("Could not connect to service control manager.");

            return scm;
        }

        #endregion
    }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public enum ServiceState
    {
        Unknown = -1, // The state cannot be (has not been) retrieved.
        NotFound = 0, // The service is not known on the host server.
        Stopped = 1,
        StartPending = 2,
        StopPending = 3,
        Running = 4,
        ContinuePending = 5,
        PausePending = 6,
        Paused = 7
    }

    [Flags]
    public enum ScmAccessRights
    {
        Connect = 0x0001,
        CreateService = 0x0002,
        EnumerateService = 0x0004,
        Lock = 0x0008,
        QueryLockStatus = 0x0010,
        ModifyBootConfig = 0x0020,
        StandardRightsRequired = 0xF0000,
        AllAccess = (StandardRightsRequired | Connect | CreateService |
                     EnumerateService | Lock | QueryLockStatus | ModifyBootConfig)
    }

    [Flags]
    public enum ServiceAccessRights
    {
        QueryConfig = 0x1,
        ChangeConfig = 0x2,
        QueryStatus = 0x4,
        EnumerateDependants = 0x8,
        Start = 0x10,
        Stop = 0x20,
        PauseContinue = 0x40,
        Interrogate = 0x80,
        UserDefinedControl = 0x100,
        Delete = 0x00010000,
        StandardRightsRequired = 0xF0000,
        AllAccess = (StandardRightsRequired | QueryConfig | ChangeConfig |
                     QueryStatus | EnumerateDependants | Start | Stop | PauseContinue |
                     Interrogate | UserDefinedControl)
    }

    public enum ServiceBootFlag
    {
        Start = 0x00000000,
        SystemStart = 0x00000001,
        AutoStart = 0x00000002,
        DemandStart = 0x00000003,
        Disabled = 0x00000004
    }

    public enum ServiceControl
    {
        Stop = 0x00000001,
        Pause = 0x00000002,
        Continue = 0x00000003,
        Interrogate = 0x00000004,
        Shutdown = 0x00000005,
        ParamChange = 0x00000006,
        NetBindAdd = 0x00000007,
        NetBindRemove = 0x00000008,
        NetBindEnable = 0x00000009,
        NetBindDisable = 0x0000000A
    }

    public enum ServiceError
    {
        Ignore = 0x00000000,
        Normal = 0x00000001,
        Severe = 0x00000002,
        Critical = 0x00000003
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
