using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using WXM.Media;

namespace WXM.NET
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnEncode_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(this.lbFileName.Text))
                return;

            string infile = this.lbFileName.Text;
            string outpath = Path.GetDirectoryName(infile);

            Thread th = new Thread(new ThreadStart(() =>
            {

                int i = 128;

                string outfile = outpath + "\\" + System.IO.Path.GetFileNameWithoutExtension(infile) + "_" + i + ".mp3";

                this.convertToMp3(infile, outfile, i);

            }));

            th.Start();
        }

        private void convertToMp3(string infile, string outfile, int p)
        {
            string retmsg = "";

            WxLame wx = new WxLame(44100, 2, 160, p);

            wx.OnProcess += lame_OnProcess;

            try
            {
                wx.Encode(infile, outfile);

                retmsg = "Create :" + outfile;
            }
            catch (Exception ex)
            {
                retmsg = ex.Message;
            }
            finally
            {
                wx.Dispose();
            }

            this.Dispatcher.Invoke(() =>
            {
                this.txtLog.Text = retmsg;
                this.progressbar.Value = 0;
            });
        }

        private void lame_OnProcess(object sender, double e)
        {
            this.Dispatcher.Invoke(() =>
            {
                this.progressbar.Value = e * 100;
            });
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();

            dialog.Filter = "PCM WAV files |*.wav|All files (*.*)|*.*";
            dialog.InitialDirectory = @"c:\";

            if (dialog.ShowDialog() == true)
            {
                this.lbFileName.Text = dialog.FileName;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void convertLame(string wavfile, string mp3file)
        {
            LibMp3Lame lame = new LibMp3Lame();

            lame.OnProcess += lame_OnProcess;

            string retmsg = "";

            ID3Tag tag = new ID3Tag();

            tag.Artist = "AOA";
            tag.Album = "1st Album ANGEL'S KNOCK";
            tag.Title = "Excuse Me";
            tag.Track = "1";
            try
            {
                lame.InputSampleRate = 44100;
                lame.NumChannels = 2;
                
                lame.VBR = VBRMode.Default;
                lame.VBRMaxBitrateKbps = 320;
                lame.VBRMinBitrateKbps = 128;
            
                lame.WriteVBRTag = true;

                lame.SetOptimization(ASMOptimizations.SSE, true);
                 
                lame.InitParams();

                lame.ApplyID3Tag(tag);

                lame.Encode(wavfile, mp3file);
                                               
                retmsg = "done.";
            }
            catch (Exception ex)
            {
                retmsg = ex.Message;
            }
            finally
            {
                lame.Dispose();
            }

            this.Dispatcher.Invoke(() =>
            {
                this.txtLog.Text = retmsg;
                this.progressbar.Value = 0;
            });
        }

        private void btnEncode2_Click(object sender, RoutedEventArgs e)
        {
            if (!LibMp3Lame.Available)
            {
                this.txtLog.Text = "Not found libmp3lame.dll";

                return;
            }

            if (string.IsNullOrEmpty(this.lbFileName.Text))
                return;

            string wavfile = this.lbFileName.Text;

            string outpath = Path.GetDirectoryName(wavfile);          

            Thread th = new Thread(new ThreadStart(() =>
            {
                int dt = DateTime.Now.Minute + DateTime.Now.Second;

                string mp3file = outpath + "\\" + System.IO.Path.GetFileNameWithoutExtension(wavfile) + "_" + dt + ".mp3";

                this.convertLame(wavfile, mp3file);
            }));

            th.Start(); 
        }

    }
}
