using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RepairFileExtension {
    public class FileSignature {
        public FileSignature() { }
        public FileSignature(string extension, int offset, string[] bytes) {
            Extension = extension;
            Offset = offset;
            Bytes = bytes;
        }
        public string Extension { get; set; }
        public int Offset { get; set; }
        public string[] Bytes { get; set; }
        public override string ToString() {
            return Extension;
        }
    }
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private List<FileSignature> Headers;
        private string[] signatures;
        public MainWindow() {
            InitializeComponent();
            Headers = new List<FileSignature>();
            //Headers = new FileSignature() { 
            //    { ".jpg", new byte[] { 0xFF, 0xD8 } },
            //    { ".jpeg", new byte[] { 0xFF, 0xD8 } },
            //    { ".bmp", new byte[] { 0x42, 0x4D } },
            //    { ".png", new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A,0x1A, 0x0A } },
            //    { ".ico", new byte[] { 0x00, 0x00, 0x01, 0x00 } },
            //    { ".gif", new byte[] { 0x47, 0x49, 0x46, 0x38 } },
            //    { ".rar", new byte[] { 0x52, 0x61, 0x72, 0x21, 0x1A, 0x07, 0x00 } },
            //    { ".zip", new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x14, 0x00 } },
            //    { ".pdf", new byte[] { 0x25, 0x50, 0x44, 0x46 } },
            //    { ".doc", new byte[] { 0xd0, 0xcf, 0x11, 0xe0, 0xa1, 0xb1, 0x1a, 0xe1 } },
            //    { ".rtf", new byte[] { 0x7b, 0x5c, 0x72, 0x74, 0x66 } },
            //    { ".mp3", new byte[] { 0x49, 0x44, 0x33, 0x03, 0x00 } },
            //    { ".m4a", new byte[] { 0x00, 0x00, 0x00, 0x20, 0x66, 0x74, 0x79, 0x70, 0x4d, 0x34, 0x41, 0x20 } },
            //    { ".mp4", new byte[] { 0x00, 0x00, 0x00, 0x14, 0x66, 0x74, 0x79, 0x70 } },
            //    { ".avi", new byte[] { 0x52, 0x49, 0x46, 0x46 } },
            //    { ".mpg", new byte[] { 0x00, 0x00, 0x01, 0xBA } },
            //    { ".exe", new byte[] { 0x4D, 0x5A } }
            //};
            signatures = RepairFileExtension.Properties.Resources.Signature.Split('\n');
            foreach (var i in signatures) {
                string ext = "", offs = "";
                try {
                    string sign = i;
                    sign = sign.Replace("\r", "");
                    ext = sign.Substring(0, sign.IndexOf('\t'));
                    sign = sign.Remove(0, ext.Length + 1);
                    offs = sign.Substring(0, sign.IndexOf('\t'));
                    int offset = Convert.ToInt32(offs, 16);
                    sign = sign.Remove(0, offs.Length + 1);
                    string[] bytes = sign.Split(' ');
                    Headers.Add(new FileSignature(ext, offset, bytes));
                } catch (Exception ex) { System.Windows.MessageBox.Show(ex.Message + "[" + offs + "]", ext); }
            }
        }

        private void AddClick(object sender, RoutedEventArgs e) {
            using (OpenFileDialog ofd = new OpenFileDialog()) {
                ofd.Multiselect = true;
                if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                    BackgroundWorker bwkOfd = new BackgroundWorker() {
                        WorkerReportsProgress = true, WorkerSupportsCancellation = true
                    };
                    progress.Value = 0;
                    progress.IsIndeterminate = true;
                    bwkOfd.ProgressChanged += bwkOfd_ProgressChanged;
                    bwkOfd.RunWorkerCompleted += bwkOfd_RunWorkerCompleted;
                    bwkOfd.DoWork += bwkOfd_DoWork;
                    bwkOfd.RunWorkerAsync(ofd.FileNames);
                }
            }
        }

        void bwkOfd_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            progress.IsIndeterminate = false;
            progress.Value = 0;
            progress.Maximum = old.Items.Count;
            start.IsEnabled = true;
            clear.IsEnabled = true;
        }

        void bwkOfd_DoWork(object sender, DoWorkEventArgs e) {
            BackgroundWorker bwk = (BackgroundWorker)sender;
            foreach (var i in (string[])e.Argument) {
                bwk.ReportProgress(0, i);
            }
        }

        void bwkOfd_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            old.Items.Add(e.UserState);
            oldInfo.Text = old.Items.Count + " Elementos";
        }

        private void RepairClick(object sender, RoutedEventArgs e) {
            BackgroundWorker bwk = new BackgroundWorker() {
                WorkerReportsProgress = true, WorkerSupportsCancellation = true
            };
            bwk.ProgressChanged += bwk_ProgressChanged;
            bwk.DoWork += bwk_DoWork;
            _new.Items.Clear();
            newInfo.Text = "0 Elementos";
            bwk.RunWorkerAsync(old.Items);
        }

        void bwk_DoWork(object sender, DoWorkEventArgs e) {
            BackgroundWorker bwk = (BackgroundWorker)sender;
            int k = 0;
            foreach (string i in (ItemCollection)e.Argument) {
                byte[] bytes;
                string ext = System.IO.Path.GetExtension(i), fname = i;
                //using (FileStream fs = new FileStream(i, FileMode.Open)) {
                bytes = File.ReadAllBytes(i);
                //using (BinaryReader br = new BinaryReader(fs)) {
                //bytes = br.ReadBytes((int)fs.Length);
                foreach (var h in Headers) {
                    int c = 0;
                    int offset = Convert.ToInt32(h.Offset);
                    if (offset + h.Bytes.Length < bytes.Length)
                        for (int j = 0; j < h.Bytes.Length; j++) {
                            if (h.Bytes[j] != "??")
                                if (bytes[j + offset] != Convert.ToByte(h.Bytes[j], 16))
                                    break;
                            c++;
                        }
                    if (c == h.Bytes.Length) {
                        ext = "." + h.Extension;
                        break;
                    }
                }
                fname = System.IO.Path.GetDirectoryName(i) + "\\" + System.IO.Path.GetFileNameWithoutExtension(i) + ext;
                bwk.ReportProgress(++k, fname);
                //br.Close();
                //}
                //fs.Close();
                //}
                File.Move(i, fname);
            }
        }

        void bwk_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            _new.Items.Add((string)e.UserState);
            newInfo.Text = _new.Items.Count + " Elementos";
            progress.Value = (double)e.ProgressPercentage / (double)old.Items.Count * 100D;
        }

        private void ClearClick(object sender, RoutedEventArgs e) {
            _new.Items.Clear();
            newInfo.Text = "0 Elementos";
            old.Items.Clear();
            oldInfo.Text = "0 Elementos";
            start.IsEnabled = false;
            clear.IsEnabled = false;
        }

        private void AddFClick(object sender, RoutedEventArgs e) {
            using (FolderBrowserDialog ofd = new FolderBrowserDialog()) {
                if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                    BackgroundWorker bwkOfd = new BackgroundWorker() {
                        WorkerReportsProgress = true, WorkerSupportsCancellation = true
                    };
                    progress.Value = 0;
                    progress.IsIndeterminate = true;
                    bwkOfd.ProgressChanged += bwkOfd_ProgressChanged;
                    bwkOfd.RunWorkerCompleted += bwkOfd_RunWorkerCompleted;
                    bwkOfd.DoWork += bwkOfd_DoWork;
                    bwkOfd.RunWorkerAsync(Directory.GetFiles(ofd.SelectedPath));
                }
            }
        }

        private void helpClick(object sender, RoutedEventArgs e) {

        }
    }
}