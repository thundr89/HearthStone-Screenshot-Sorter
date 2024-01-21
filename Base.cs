/*
 
icon from https://icon-library.com/icon/hearthstone-icon-3.html

 */
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Timers;
using System.Windows.Forms;
using System.IO.Compression;
using Timer = System.Windows.Forms.Timer;

namespace HearthStone_Screenshot_Sorter
{


    class Program : Form
    {
        private NotifyIcon trayIcon;
        private ContextMenu trayMenu;
        private Timer timer;
        private FolderBrowserDialog folderBrowser;
        private StreamWriter logFile;
        private static string DesktopDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        private static string StoreDir = Path.Combine(DesktopDir, "HearthStone Screenshots");
        private static string Log = Path.Combine(StoreDir, "log.txt");
        private static string Zip = Path.Combine(StoreDir, $"Backup_{DateTime.Now:yyyyMMdd_HHmmss}.zip");

        [STAThread]
        public static void Main()
        {
            Application.Run(new Program());
        }

        public Program()
        {
            // Create a simple tray menu with only one item.
            trayMenu = new ContextMenu();
            trayMenu.MenuItems.Add("Rendszerezés most", OnSortNow);
            trayMenu.MenuItems.Add("Mappa kiválasztása", OnSelectFolder);
            trayMenu.MenuItems.Add("Kilépés", OnExit);

            folderBrowser = new FolderBrowserDialog();


            // Create a tray icon.
            trayIcon = new NotifyIcon();
            trayIcon.Text = "Hearthstone képernyőkép rendező";
            trayIcon.Icon = new Icon(Properties.Resources.main, 40, 40);

            // Add menu to tray icon and show it.
            trayIcon.ContextMenu = trayMenu;
            trayIcon.Visible = true;

            timer = new Timer();
            timer.Interval = 1800000; // Fél óránként fut le
            timer.Tick += TimerAction;
            timer.Start();
        }

        protected override void OnLoad(EventArgs e)
        {
            Visible = false; // Hide form window.
            ShowInTaskbar = false; // Remove from taskbar.

            base.OnLoad(e);
        }

        private void OnSortNow(object sender, EventArgs e)
        {
            TimerAction(sender, e);
            trayIcon.ShowBalloonTip(3000, "Hearthstone képernyőkép rendező", "A képernyőképek rendezése megtörtént!", ToolTipIcon.Info);
        }

        private void OnSelectFolder(object sender, EventArgs e)
        {
            if (folderBrowser.ShowDialog() == DialogResult.OK)
            {
                TrayAction(folderBrowser.SelectedPath);
                trayIcon.ShowBalloonTip(3000, "Hearthstone képernyőkép rendező", "A képernyőképek rendezése megtörtént!", ToolTipIcon.Info);
            }
        }

        private void OnExit(object sender, EventArgs e)
        {
            Application.Exit();
        }

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                // Release the icon resource.
                trayIcon.Dispose();
            }

            base.Dispose(isDisposing);
        }
        private void TrayAction(string sourcePath)
        {
            SharedAction(sourcePath, false);
        }

        private void TimerAction(object sender, EventArgs e)
        {
            SharedAction(DesktopDir);
        }

        private void SharedAction(string sourcePath, bool timed = true)
        {

            if (!Directory.Exists(StoreDir))
            {
                Directory.CreateDirectory(StoreDir);
            }
            // Log fájl létrehozása a célmappában
            if (!File.Exists(Log))
            {
                File.Create(Log);
            }

            logFile = new StreamWriter(Log, true);

            if (timed)
            {
                logFile.WriteLine($"Timed sorting started on: {DateTime.Now}");
            }
            else
            {
                logFile.WriteLine($"Manual sorting started on: {DateTime.Now}");
            }
            logFile.Flush();

            var dirInfo = new DirectoryInfo(sourcePath);

            var files = dirInfo.GetFiles("Hearthstone Screenshot *.png")
                   .OrderBy(f => f.Name.Split(' ')[2])
                   .ToList();

            if (files.Count > 0)
            {
                // Biztonsági másolat készítése
                using (var archive = ZipFile.Open(Zip, ZipArchiveMode.Create))
                {
                    foreach (var file in files)
                    {
                        archive.CreateEntryFromFile(file.FullName, file.Name);
                    }
                }

                for (int i = 0; i < files.Count; i++)
                {
                    var creationTime = files[i].CreationTime;
                    var year = creationTime.Year.ToString();
                    var month = creationTime.Month.ToString("D2");
                    var day = creationTime.Day.ToString("D2");
                    var hour = creationTime.Hour.ToString("D2");
                    var minute = creationTime.Minute.ToString("D2");
                    var second = creationTime.Second.ToString("D2");

                    var yearMonthPath = Path.Combine(StoreDir, year, month);

                    // Ha a mappa még nem létezik, akkor létrehozzuk
                    if (!Directory.Exists(yearMonthPath))
                    {
                        Directory.CreateDirectory(yearMonthPath);
                    }

                    var newPath = Path.Combine(yearMonthPath, $"Hearthstone Screenshot {month}-{day}-{year} {hour}.{minute}.{second}.png");
                    if (files[i].FullName != newPath)
                    {
                        File.Move(files[i].FullName, newPath);
                        // Logolás
                        logFile.WriteLine($"[{DateTime.Now}] {files[i].FullName} -> {newPath}");
                        logFile.Flush();
                    }
                }
            }
            else
            {
                logFile.WriteLine($"No file to short");
                logFile.Flush();
            }
            // Log fájl bezárása
            logFile.WriteLine($"Ended on: {DateTime.Now}");
            logFile.Flush();
            logFile.Close();
        }
    }
}