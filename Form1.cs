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

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        private DateTime lastUpdateTime; //ตัวแปร เวลา
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
            string url = "http://25.64.193.152/update/update.zip"; //ที่อยู่ของไฟล์
            string localPath = "update.zip"; //ชื่อไฟล์
            try
            {
                DownloadFile(url, localPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error 001: {ex.Message}");
            }
        }
        private void DownloadFile(string url, string localPath)
        {
            using (WebClient client = new WebClient())
            {
                client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(client_DownloadProgressChanged);
                client.DownloadFileCompleted += new AsyncCompletedEventHandler(client_DownloadFileCompleted);
                client.DownloadFileAsync(new Uri(url), localPath);
            }
        }

        private void client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            // อัปเดต UI ทุก ๆ 1500 มิลลิวินาที ( 1.5 วินาที )
            if ((DateTime.Now - lastUpdateTime).TotalMilliseconds >= 1500)
            {
                progressBar1.Value = e.ProgressPercentage;
                label1.Text = $"Downloaded {e.BytesReceived / 1024.0 / 1024.0:F2} MB of {e.TotalBytesToReceive / 1024.0 / 1024.0:F2} MB ({e.ProgressPercentage}%)";
                lastUpdateTime = DateTime.Now;
            }
        }

        private void client_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MessageBox.Show($"Error 002: {e.Error.Message}");
                return;
            }
            if (e.Cancelled)
            {
                MessageBox.Show("Error 003: Download cancelled");
                return;
            }

            string zipPath = "update.zip"; //ชื่อไฟล์
            string extractPath = AppDomain.CurrentDomain.BaseDirectory; //ที่อยู่ของโปรแกรม ควรจะอยู่ที่เดียวกันกับ Launcher

            // ตรวจสอบว่ามีไฟล์ .zip อยู่แล้วหรือไม่
            if (File.Exists(zipPath))
            {
                try
                {
                    ZipFile.ExtractToDirectory(zipPath, extractPath);
                    File.Delete(zipPath);
                    btn_start.Enabled = true;
                    btn_start.Show();
                    progressBar1.Hide();
                    label1.Hide();
                    //MessageBox.Show("Download and extraction completed!");
                }
                catch (FileNotFoundException ex)
                {
                    MessageBox.Show($"Error 004: {ex.Message}");
                }
                catch (InvalidDataException ex)
                {
                    MessageBox.Show($"Error 005: {ex.Message}");
                }
                catch (UnauthorizedAccessException ex)
                {
                    MessageBox.Show($"Error 006: {ex.Message}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error 007: {ex.Message}");
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
            MessageBox.Show("Error 003: Download cancelled");
            Application.Exit();//ปุ่มปิดโปรแกรม
            /*
             * จริงๆสามารถเติมแต่งส่วนนี้ได้ เผื่อจะถาม User ว่าต้องการปิดโปรแกรมตอนนี้เลยหรือไม่ ?
             * */
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized; //ปุ่นย่อโปรแกรม
        }

        private void btn_start_Click(object sender, EventArgs e)
        {
            Process.Start("Launcher.exe"); //เปิด Process ที่ตั้งไว้
            Application.Exit();
        }
    }
}
