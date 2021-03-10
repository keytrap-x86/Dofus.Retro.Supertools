using Dofus.Retro.Supertools.Controls;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

using MessageBox = System.Windows.MessageBox;

namespace Dofus.Retro.Supertools.Core.Helpers
{
    public static class ProcessHelper
    {
        #region Managed IP Helper API

        public class TcpTable : IEnumerable<TcpRow>
        {
            #region Private Fields

            #endregion

            #region Constructors

            public TcpTable(IEnumerable<TcpRow> tcpRows)
            {
                Rows = tcpRows;
            }

            #endregion

            #region Public Properties

            public IEnumerable<TcpRow> Rows { get; }

            #endregion

            #region IEnumerable<TcpRow> Members

            public IEnumerator<TcpRow> GetEnumerator()
            {
                return Rows.GetEnumerator();
            }

            #endregion

            #region IEnumerable Members

            IEnumerator IEnumerable.GetEnumerator()
            {
                return Rows.GetEnumerator();
            }

            #endregion
        }

        public class TcpRow
        {
            #region Private Fields

            #endregion

            #region Constructors

            public TcpRow(IpHelper.TcpRow tcpRow)
            {
                State = tcpRow.state;
                ProcessId = tcpRow.owningPid;

                var localPort = (tcpRow.localPort1 << 8) + (tcpRow.localPort2) + (tcpRow.localPort3 << 24) + (tcpRow.localPort4 << 16);
                long localAddress = tcpRow.localAddr;
                LocalEndPoint = new IPEndPoint(localAddress, localPort);

                var remotePort = (tcpRow.remotePort1 << 8) + (tcpRow.remotePort2) + (tcpRow.remotePort3 << 24) + (tcpRow.remotePort4 << 16);
                long remoteAddress = tcpRow.remoteAddr;
                RemoteEndPoint = new IPEndPoint(remoteAddress, remotePort);
            }

            #endregion

            #region Public Properties

            public IPEndPoint LocalEndPoint { get; }

            public IPEndPoint RemoteEndPoint { get; }

            public TcpState State { get; }

            public int ProcessId { get; }

            #endregion
        }

        public static class ManagedIpHelper
        {
            #region Public Methods

            public static TcpTable GetExtendedTcpTable(bool sorted)
            {
                var tcpRows = new List<TcpRow>();

                var tcpTable = IntPtr.Zero;
                var tcpTableLength = 0;

                if (IpHelper.GetExtendedTcpTable(tcpTable, ref tcpTableLength, sorted, IpHelper.AF_INET,
                        IpHelper.TcpTableType.OwnerPidAll, 0) == 0) 
                    return new TcpTable(tcpRows);
                try
                {
                    tcpTable = Marshal.AllocHGlobal(tcpTableLength);
                    if (IpHelper.GetExtendedTcpTable(tcpTable, ref tcpTableLength, true, IpHelper.AF_INET, IpHelper.TcpTableType.OwnerPidAll, 0) == 0)
                    {
                        var table = (IpHelper.TcpTable)Marshal.PtrToStructure(tcpTable, typeof(IpHelper.TcpTable));

                        var rowPtr = (IntPtr)((long)tcpTable + Marshal.SizeOf(table.length));
                        for (var i = 0; i < table.length; ++i)
                        {
                            tcpRows.Add(new TcpRow((IpHelper.TcpRow)Marshal.PtrToStructure(rowPtr, typeof(IpHelper.TcpRow))));
                            rowPtr = (IntPtr)((long)rowPtr + Marshal.SizeOf(typeof(IpHelper.TcpRow)));
                        }
                    }
                }
                finally
                {
                    if (tcpTable != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(tcpTable);
                    }
                }

                return new TcpTable(tcpRows);
            }

            #endregion
        }

        #endregion

        #region P/Invoke IP Helper API

        /// <summary>
        /// <see cref="http://msdn2.microsoft.com/en-us/library/aa366073.aspx"/>
        /// </summary>
        public static class IpHelper
        {
            #region Public Fields

            public const string DLL_NAME = "iphlpapi.dll";
            public const int AF_INET = 2;

            #endregion

            #region Public Methods

            /// <summary>
            /// <see cref="http://msdn2.microsoft.com/en-us/library/aa365928.aspx"/>
            /// </summary>
            [DllImport(DLL_NAME, SetLastError = true)]
            public static extern uint GetExtendedTcpTable(IntPtr tcpTable, ref int tcpTableLength, bool sort, int ipVersion, TcpTableType tcpTableType, int reserved);

            #endregion

            #region Public Enums

            /// <summary>
            /// <see cref="http://msdn2.microsoft.com/en-us/library/aa366386.aspx"/>
            /// </summary>
            public enum TcpTableType
            {
                BasicListener,
                BasicConnections,
                BasicAll,
                OwnerPidListener,
                OwnerPidConnections,
                OwnerPidAll,
                OwnerModuleListener,
                OwnerModuleConnections,
                OwnerModuleAll
            }

            #endregion

            #region Public Structs

            /// <summary>
            /// <see cref="http://msdn2.microsoft.com/en-us/library/aa366921.aspx"/>
            /// </summary>
            [StructLayout(LayoutKind.Sequential)]
            public struct TcpTable
            {
                public uint length;
                public TcpRow row;
            }

            /// <summary>
            /// <see cref="http://msdn2.microsoft.com/en-us/library/aa366913.aspx"/>
            /// </summary>
            [StructLayout(LayoutKind.Sequential)]
            public struct TcpRow
            {
                public TcpState state;
                public uint localAddr;
                public byte localPort1;
                public byte localPort2;
                public byte localPort3;
                public byte localPort4;
                public uint remoteAddr;
                public byte remotePort1;
                public byte remotePort2;
                public byte remotePort3;
                public byte remotePort4;
                public int owningPid;
            }

            #endregion
        }

        #endregion

        public static string GetCharacterNameFromWindow(Process dofusProcess)
        {
            const string characterNameRegex = "^(.*?)\\- Dofus Retro";
            return Regex.Match(dofusProcess.MainWindowTitle, characterNameRegex).Groups[1].Value.Trim();
        }

        public static string GetLocalIpUsedByProcess(this Process process)
        {
            var tcpRow = ManagedIpHelper.GetExtendedTcpTable(true)
                .FirstOrDefault(t => t.RemoteEndPoint.Address.ToString() == Static.Datacenter.Servers[0].Ip && t.ProcessId == process.Id);

            return tcpRow?.LocalEndPoint.ToString();
        }

        public static List<Process> GetDofusRetroProcesses()
        {
            return Process.GetProcessesByName("Dofus Retro").Where(p => !string.IsNullOrEmpty(p.MainWindowTitle)).ToList();
        }

        public static async Task<Process> StartDofusProcessAsync(this CharacterController persoInList)
        {
            if (!File.Exists(Static.Datacenter.DofusRetroExePath))
            {
                

                    MessageBox.Show("Le chemin de l'exécutable Dofus Retro n'est pas défini. " +
                        "Veuillez le définir avant de pouvour utiliser la fonctionnalité de connexion automatique", "Chemin Dofus Rétro", MessageBoxButton.OK, MessageBoxImage.Information);
                    var opf = new OpenFileDialog
                    {
                        Filter = "Dofus Retro.exe|Dofus Retro.exe",
                        Title = "Choisir l'emplacement de Dofus Retro.exe"
                    };

                    var result = opf.ShowDialog();
                    if(result != DialogResult.OK)
                    {
                        return null;
                    }

                    Static.Datacenter.DofusRetroExePath = opf.FileName;


            }

            return await Task.Run(() => {
                var pi = new ProcessStartInfo(Static.Datacenter.DofusRetroExePath);
                var p = new Process { StartInfo = pi, EnableRaisingEvents = true };
                p.Exited += (sender, args) =>
                {
                    persoInList.Character.IsConnected = false;
                    persoInList.Character.Process = null;
                    persoInList.Character.IsWindowFocused = false;

                    if (persoInList.MouseHook == null)
                        return;

                    persoInList.MouseHook.Uninstall();
                    persoInList.MouseHook.Dispose();
                    persoInList.MouseHook = null;

                };
                p.Start();
                return p;
            });

            
        }

        public static async Task RunProgram(string filename, string args = null, bool noWindow = false, bool waitForExit = true)
        {
            var pi = new ProcessStartInfo(filename, args)
            {
                CreateNoWindow = noWindow,
                RedirectStandardOutput = false,
                UseShellExecute = true
            };

            var proc = new Process
            {
                StartInfo = pi
            };

            await Task.Run(() =>
            {
                proc.Start();
                if (waitForExit)
                    proc.WaitForExit();
            });
        }
    }
}
