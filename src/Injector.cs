namespace Aetopia.UWP;

using System;
using System.IO;
using System.Threading;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Security.Principal;
using System.Security.AccessControl;
using System.Runtime.InteropServices;

public static class Injector
{
    static readonly ApplicationActivationManager manager = new();

    static readonly SecurityIdentifier sid = new("S-1-15-2-1");

    static readonly nint lpStartAddress;

    static Injector()
    {
        var hModule = Unmanaged.LoadLibraryEx("Kernel32", default, 0x00000800);
        lpStartAddress = Unmanaged.GetProcAddress(hModule, "LoadLibraryW");
        Unmanaged.FreeLibrary(hModule);
    }

    static void SetAccessControl(string path)
    {
        FileInfo info = new(path);
        FileSecurity security = info.GetAccessControl();
        security.AddAccessRule(new(sid, FileSystemRights.ReadAndExecute, AccessControlType.Allow));
        info.SetAccessControl(security);
    }

    static void CreateRemoteThread(int processId, string path)
    {
        nint hProcess = default, lpBaseAddress = default, hThread = default;
        try
        {
            hProcess = Unmanaged.OpenProcess(0X1FFFFF, false, processId);
            if (hProcess == default) throw new Win32Exception(Marshal.GetLastWin32Error());

            var nSize = sizeof(char) * (path.Length + 1);

            lpBaseAddress = Unmanaged.VirtualAllocEx(hProcess, default, nSize, 0x00001000 | 0x00002000, 0x40);
            if (lpBaseAddress == default) throw new Win32Exception(Marshal.GetLastWin32Error());

            if (!Unmanaged.WriteProcessMemory(hProcess, lpBaseAddress, Marshal.StringToHGlobalUni(path), nSize, out _)) throw new Win32Exception(Marshal.GetLastWin32Error());

            hThread = Unmanaged.CreateRemoteThread(hProcess, default, 0, lpStartAddress, lpBaseAddress, 0, out _);
            if (hThread == default) throw new Win32Exception(Marshal.GetLastWin32Error());
            _ = Unmanaged.WaitForSingleObject(hThread, Timeout.Infinite);
        }
        finally
        {
            Unmanaged.VirtualFreeEx(hProcess, lpBaseAddress, 0, 0x00008000);
            Unmanaged.CloseHandle(hThread);
            Unmanaged.CloseHandle(hProcess);
        }
    }

    public static void Inject(string appUserModelId, string path)
    {
        if (Unmanaged.PathIsRelative(path))
            throw new Exception("The specified path must be absolute.");
        SetAccessControl(path);
        try
        {
            manager.ActivateApplication(appUserModelId, null, 0x00000002, out var processId);
            CreateRemoteThread(processId, path);
        }
        catch (ArgumentException) { throw new Win32Exception(Marshal.GetLastWin32Error()); }
    }

    public static async Task InjectAsync(string appdUserModelId, string path) => await Task.Run(() => Inject(appdUserModelId, path)).ConfigureAwait(false);
}