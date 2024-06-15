using LC_Update;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        private DateTime lastUpdateTime; //ตัวแปร เวลา
        public string extractPath = LC_Update.Properties.Settings.Default.Path_Location; // เพิ่มตัวแปรสำหรับเก็บเส้นทางที่เลือก
        public Form1()
        {
            InitializeComponent();
            this.FormBorderStyle = FormBorderStyle.None;
            // เพิ่ม MouseDown event handler ให้กับ Form
            this.MouseDown += new MouseEventHandler(Form1_MouseDown);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            btn_start.Enabled = false;
            btn_start.Hide();
            Check_Install_Path(); // ตรวจสอบตำแหน่งไฟล์
            string versionUrl = "http://25.64.193.152/update/version.xml"; //ที่อยู่ของไฟล์เวอร์ชัน
            string localVersionPath = "version.xml"; //ชื่อไฟล์เวอร์ชัน

            try
            {
                DownloadFile(versionUrl, localVersionPath, CheckVersion);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error 001: {ex.Message}");
            }
        }

        private void Check_Install_Path()
        {
            if (!LC_Update.Properties.Settings.Default.Install_First_Time)
            {
                using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
                {
                    if (folderDialog.ShowDialog() == DialogResult.OK)
                    {
                        extractPath = folderDialog.SelectedPath;
                        LC_Update.Properties.Settings.Default.Path_Location = extractPath;
                        LC_Update.Properties.Settings.Default.Install_First_Time = true;
                        LC_Update.Properties.Settings.Default.Save();
                    }
                    else
                    {
                        MessageBox.Show("กรุณาเลือกที่อยู่ติดตั้งก่อน", "LC_Update");
                        Application.Exit();
                    }
                }
            }
            else
            {
                extractPath = LC_Update.Properties.Settings.Default.Path_Location;
            }
        }

        private void DownloadFile(string url, string localPath, Action callback)
        {
            using (WebClient client = new WebClient())
            {
                client.DownloadFileCompleted += (sender, e) =>
                {
                    if (e.Error != null)
                    {
                        MessageBox.Show($"Error 002: {e.Error.Message}");
                        return;
                    }
                    if (e.Cancelled)
                    {
                        MessageBox.Show("Error 003: ยกเลิกการดาวน์โหลด !");
                        return;
                    }
                    callback?.Invoke();
                };
                client.DownloadFileAsync(new Uri(url), localPath);
            }
        }

        private void CheckVersion()
        {
            string localVersionPath = "version.xml"; //ชื่อไฟล์เวอร์ชัน
            if (!File.Exists(localVersionPath))
            {
                MessageBox.Show("Error 004: ไม่พบไฟล์เวอร์ชัน");
                return;
            }

            string installedVersion = LC_Update.Properties.Settings.Default.InstalledVersion;
            string availableVersion = GetVersionFromXml(localVersionPath);

            if (string.Compare(installedVersion, availableVersion) < 0)
            {
                string url = "http://25.64.193.152/update/update.zip"; //ที่อยู่ของไฟล์
                string localPath = "update.zip"; //ชื่อไฟล์
                try
                {
                    DownloadFile(url, localPath, () => OnDownloadCompleted(availableVersion));
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error 001: {ex.Message}");
                }
            }
            else
            {
                btn_start.Enabled = true;
                btn_start.Show();
                progressBar1.Hide();
                label1.Hide();
                MessageBox.Show("ไม่มีการอัปเดตใหม่ในเวลานี้ โปรดรอแพทช์ใหม่ <3","xorbit256");
                Application.Exit();
            }
        }

        private string GetVersionFromXml(string filePath)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(filePath);
            XmlNode node = xmlDoc.SelectSingleNode("//version");
            return node?.InnerText ?? string.Empty;
        }

        private void OnDownloadCompleted(string newVersion)
        {
            string zipPath = "update.zip"; //ชื่อไฟล์ที่อยู่ในระบบฝั่ง Server
            if (string.IsNullOrWhiteSpace(extractPath))
            {
                MessageBox.Show("Error 007: Path cannot be the empty string or all whitespace.");
                return;
            }

            string extractPathToFolder = extractPath;
            if (File.Exists(zipPath))
            {
                try
                {
                    // ตรวจสอบเวอร์ชันก่อนการติดตั้ง
                    if (Directory.Exists(extractPathToFolder))
                    {
                        string installedVersion = LC_Update.Properties.Settings.Default.InstalledVersion;
                        if (string.Compare(installedVersion, newVersion) >= 0)
                        {
                            MessageBox.Show("The installed version is up-to-date.");
                            return;
                        }
                    }

                    // ลบไฟล์ในที่อยู่ปลายทางหากมีอยู่แล้ว
                    if (Directory.Exists(extractPathToFolder))
                    {
                        Directory.Delete(extractPathToFolder, true);
                    }

                    ZipFile.ExtractToDirectory(zipPath, extractPathToFolder);
                    File.Delete(zipPath);

                    // อัปเดตเวอร์ชันที่ติดตั้งในระบบ
                    LC_Update.Properties.Settings.Default.InstalledVersion = newVersion;
                    LC_Update.Properties.Settings.Default.Save();

                    btn_start.Enabled = true;
                    btn_start.Show();
                    progressBar1.Hide();
                    label1.Hide();
                    MessageBox.Show("Download and extraction completed!");
                }
                catch (FileNotFoundException ex)
                {
                    MessageBox.Show($"Error 004: {ex.Message}");
                    Application.Exit();
                }
                catch (InvalidDataException ex)
                {
                    MessageBox.Show($"Error 005: {ex.Message}");
                    Application.Exit();
                }
                catch (UnauthorizedAccessException ex)
                {
                    MessageBox.Show($"Error 006: {ex.Message}");
                    Application.Exit();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error 007: {ex.Message}");
                    LC_Update.Properties.Settings.Default.Path_Location = "";
                    LC_Update.Properties.Settings.Default.Install_First_Time = false;
                    LC_Update.Properties.Settings.Default.Save();
                    Application.Exit();
                }
            }
        }

        // Import ฟังก์ชัน ReleaseCapture และ SendMessage จาก user32.dll
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        // ค่าคงที่สำหรับการส่งข้อความ
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;
        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, (IntPtr)HT_CAPTION, IntPtr.Zero);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = MessageBox.Show("คุณกำลังจะออกจากโปรแกรม การดำเนินการนี้จะยกเลิกการดาวน์โหลด \r\nต้องการดำเนินการต่อหรือไม่ ?", "xorbit256", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if (dialogResult == DialogResult.Yes)
            {
                Application.Exit();

            }
            else if (dialogResult == DialogResult.No)
            {
                //พื้นที่สำหรับเรียกใช้ ฟังชั่น (เมื่อกด No จะดำเนินการ)
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized; //ปุ่มย่อโปรแกรม
        }

        private void btn_start_Click(object sender, EventArgs e)
        {
            //Process.Start("Launcher.exe"); //เปิด Process ที่ตั้งไว้
            Application.Exit();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
            {
                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    extractPath = folderDialog.SelectedPath;
                    LC_Update.Properties.Settings.Default.Path_Location = extractPath;
                    LC_Update.Properties.Settings.Default.Save();
                }
            }
        }
    }
}
