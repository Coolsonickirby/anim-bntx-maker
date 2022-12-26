using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using Microsoft.Win32;
using System.IO;
using System.Threading;


namespace AnimBNTX
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        OpenFileDialog openVideo;
        OpenFileDialog batchOpenVideo;
        SaveFileDialog saveAnimBNTX;

        public MainWindow()
        {
            InitializeComponent();
            openVideo = new OpenFileDialog();
            batchOpenVideo = new OpenFileDialog();
            batchOpenVideo.Multiselect = true;
            saveAnimBNTX = new SaveFileDialog();
            saveAnimBNTX.Filter = "AnimBNTX | *.animbntx";
            updateLoopSection();
        }

        private void button_Click_1(object sender, RoutedEventArgs e)
        {
            bool? res = openVideo.ShowDialog();

            if (res.HasValue && res.Value)
            {
                txtLoadedFile.Text = openVideo.FileName;
            }
        }

        private void chkEnableLoop_Checked(object sender, RoutedEventArgs e)
        {
            updateLoopSection();
        }

        private void updateLoopSection()
        {
            txtLoopCount.IsEnabled = chkEnableLoop.IsChecked.Value;
            txtStartLoopFrame.IsEnabled = chkEnableLoop.IsChecked.Value;
            txtEndLoopFrame.IsEnabled = chkEnableLoop.IsChecked.Value;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            // AnimBNTX Logic

            if (!File.Exists(txtLoadedFile.Text))
            {
                MessageBox.Show("Please enter/load a filepath!");
                return;
            }

            bool? res = saveAnimBNTX.ShowDialog();

            if(res.HasValue && res.Value)
            {
                AnimationBNTX animBNTX = new AnimationBNTX();
                animBNTX.loop_animation = chkEnableLoop.IsChecked.Value ? (UInt32)1 : (UInt32)0;
                animBNTX.loop_count = Int32.Parse(txtLoopCount.Text);
                animBNTX.starting_frame_loop = UInt32.Parse(txtStartLoopFrame.Text);
                animBNTX.ending_frame_loop = UInt32.Parse(txtEndLoopFrame.Text);
                animBNTX.frame_rate = float.Parse(txtFPS.Text);
                animBNTX.group_number = UInt32.Parse(txtGroupNum.Text);

                animBNTX.max_frames = int.Parse(txtMaxFrames.Text);
                animBNTX.frame_multiplier = int.Parse(txtFrameMultiplier.Text);

                animBNTX.LoadFromVideo(txtLoadedFile.Text, chkAddBorder.IsChecked.Value);
                animBNTX.SaveAnimBNTX(saveAnimBNTX.FileName);
            }
        }

        private void ConvertVideo(string path, AnimationBNTX animBNTX)
        {
            string extension = System.IO.Path.GetExtension(path);
            string output_path = path.Replace(extension, ".animbntx");

            animBNTX.LoadFromVideo(path, true);
            animBNTX.SaveAnimBNTX(output_path);
        }

        private void btnExportBatch_Click(object sender, RoutedEventArgs e)
        {
            bool? res = batchOpenVideo.ShowDialog();
            
            if(res.HasValue && res.Value)
            {
                foreach (String file in batchOpenVideo.FileNames)
                {
                    AnimationBNTX animBNTX = new AnimationBNTX();
                    animBNTX.loop_animation = chkEnableLoop.IsChecked.Value ? (UInt32)1 : (UInt32)0;
                    animBNTX.loop_count = Int32.Parse(txtLoopCount.Text);
                    animBNTX.starting_frame_loop = UInt32.Parse(txtStartLoopFrame.Text);
                    animBNTX.ending_frame_loop = UInt32.Parse(txtEndLoopFrame.Text);
                    animBNTX.frame_rate = float.Parse(txtFPS.Text);
                    animBNTX.group_number = UInt32.Parse(txtGroupNum.Text);

                    animBNTX.max_frames = int.Parse(txtMaxFrames.Text);
                    animBNTX.frame_multiplier = int.Parse(txtFrameMultiplier.Text);

                    Thread file_conversion_thread = new Thread(() => {
                        ConvertVideo(file, animBNTX); 
                    });
                    file_conversion_thread.Start();
                }
            }
        }
    }
}