using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SpelunkyDeathCounter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const int PROCESS_WM_READ = 0x0010;
        const int PROCESS_QUERY_INFORMATION = 0x0400;

        const int MEMORY_OFFSET = 0x22DA6F4C;

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(IntPtr pHandle, IntPtr Address, byte[] Buffer, int Size, IntPtr NumberofBytesRead);

        public static IntPtr spelunkyHandle = IntPtr.Zero;
        public static IntPtr spelunkyDeathAddress = IntPtr.Zero;
        public static Thread spelunkyThread;

        static int offset = 0;

        static bool isOpen = true;

        public MainWindow()
        {
            InitializeComponent();
        }

        public void ConnectToGame()
        {
            Process? process = Process.GetProcessesByName("Spel2").FirstOrDefault();
            if (process is null)
            {
                MessageBox.Show("Spelunky is not running!");
                return;
            }

            IntPtr handle = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_WM_READ, false, process.Id);
            if (handle == IntPtr.Zero)
            {
                MessageBox.Show("Failed to open process!");
                return;
            }

            // Get the base address of process
            IntPtr baseAddress = process.MainModule!.BaseAddress;

            // Get the address where deaths are stored
            IntPtr deathsAddress = baseAddress + MEMORY_OFFSET;

            spelunkyHandle = handle;
            spelunkyDeathAddress = deathsAddress;

            spelunkyThread = new(new ThreadStart(UpdateDeaths));
            spelunkyThread.Start();
        }

        private void UpdateDeaths()
        {
            while (isOpen)
            {
                Dispatcher.InvokeAsync(() =>
                    {
                        CounterBlock.Text = "Deaths: " + (GetDeaths() - offset).ToString();
                    });
                Thread.Sleep(1000);
            }
        }

        public static int GetDeaths()
        {
            byte[] buffer = new byte[4];

            ReadProcessMemory(spelunkyHandle, spelunkyDeathAddress, buffer, buffer.Length, IntPtr.Zero);

            return BitConverter.ToInt32(buffer);
        }

        private void ConnectToGame_Click(object sender, RoutedEventArgs e)
        {
            ConnectToGame();
        }

        private void ToggleCounterMode_Click(object sender, RoutedEventArgs e)
        {
            if (offset == 0)
            {
                offset = GetDeaths();
            }
            else
            {
                offset = 0;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            isOpen = false;
            spelunkyThread.Join();
        }
    }
}
