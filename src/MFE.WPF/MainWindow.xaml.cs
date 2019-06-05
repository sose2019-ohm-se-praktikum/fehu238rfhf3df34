using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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

namespace MFE.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private Progress<float> progress = new Progress<float>();

        private bool isRunning = false;

        public MainWindow()
        {
            InitializeComponent();

            progress.ProgressChanged += OnProgressChanged;
            cancellationTokenSource.Token.Register(OnCanceled);
        }

        private void OnProgressChanged(object sender, float e)
        {
            pg_progress.Value = e * 100;
        }

        private void Btn_chooseInputDirectory_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.ShowNewFolderButton = true;
                var result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    var directory = new DirectoryInfo(dialog.SelectedPath);
                    tb_inputDirectory.Text = directory.FullName;

                    if (tb_outputDirectory.Text.Length == 0)
                    {
                        tb_outputDirectory.Text = directory.FullName;
                    }

                    updateStates();
                }
            }
        }

        private void Btn_chooseOutputDirectory_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.ShowNewFolderButton = true;
                var result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    var directory = new DirectoryInfo(dialog.SelectedPath);
                    tb_outputDirectory.Text = directory.FullName;

                    updateStates();
                }
            }
        }

        private async void Btn_start_Click(object sender, RoutedEventArgs e)
        {
            var settings = new SoundVolumeEvenerSettings();
            settings.OutputDirectory = tb_outputDirectory.Text;
            isRunning = true;
            updateStates();
            await SoundVolumeEvener.RunSoundVolumeEvening(new DirectoryInfo(tb_inputDirectory.Text).EnumerateFiles().Select(x => x.FullName), settings, progress, cancellationTokenSource.Token);
            isRunning = false;
            updateStates();
        }

        private void Btn_cancel_Click(object sender, RoutedEventArgs e)
        {
            cancellationTokenSource.Cancel();
        }

        private void OnCanceled()
        {
            isRunning = false;

            updateStates();
        }

        private void updateStates()
        {
            if (isRunning)
            {
                btn_start.IsEnabled = false;
                btn_cancel.IsEnabled = true;
                btn_chooseInputDirectory.IsEnabled = false;
                btn_chooseOutputDirectory.IsEnabled = false;
            }
            else
            {
                btn_cancel.IsEnabled = false;
                btn_chooseInputDirectory.IsEnabled = true;
                btn_chooseOutputDirectory.IsEnabled = true;

                if (tb_inputDirectory.Text.Length > 0 && tb_outputDirectory.Text.Length > 0)
                {
                    btn_start.IsEnabled = true;
                }
                else
                {
                    btn_start.IsEnabled = false;
                }
            }
        }
    }
}
