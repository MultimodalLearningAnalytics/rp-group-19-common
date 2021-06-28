using System;
using System.Windows;
using System.Windows.Controls;
using Multimodal_Learning_Analytics_for_Sustained_Attention.Experiment1;
using Multimodal_Learning_Analytics_for_Sustained_Attention.Experiment2;
using Multimodal_Learning_Analytics_for_Sustained_Attention.Experiment3;

namespace Multimodal_Learning_Analytics_for_Sustained_Attention
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            TimeSync.SyncTime();
            this.dateEnvironmentIdTextbox.Text = DateTime.Now.ToString("yyyyMMdd") + "-??";
        }

        private void startExperiment1Btn_Click(object sender, RoutedEventArgs e)
        {
            Experiment1MainWindow window = new Experiment1MainWindow(dateEnvironmentIdTextbox.Text, participantIdTextbox.Text);
            window.Show();
            this.Close();
        }

        private void startExperiment2Btn_Click(object sender, RoutedEventArgs e)
        {
            Experiment2MainWindow window = new Experiment2MainWindow(dateEnvironmentIdTextbox.Text, participantIdTextbox.Text);
            window.Show();
            this.Close();
        }

        private void startExperiment3Btn_Click(object sender, RoutedEventArgs e)
        {
            Experiment3MainWindow window = new Experiment3MainWindow(dateEnvironmentIdTextbox.Text, participantIdTextbox.Text);
            window.Show();
            this.Close();
        }

        private void updateButtonsEnabled()
        {
            if (participantIdTextbox != null && dateEnvironmentIdTextbox != null) // Weird check is needed by WPF
            {
                if (participantIdTextbox.Text.Length > 0 && dateEnvironmentIdTextbox.Text.Length > 0 && !dateEnvironmentIdTextbox.Text.Contains("?") && !participantIdTextbox.Text.Contains("?"))
                {
                    startExperiment1Btn.IsEnabled = true;
                    startExperiment2Btn.IsEnabled = true;
                    startExperiment3Btn.IsEnabled = true;
                }
                else
                {
                    startExperiment1Btn.IsEnabled = false;
                    startExperiment2Btn.IsEnabled = false;
                    startExperiment3Btn.IsEnabled = false;
                }
            }
        }

        private void participantIdTextbox_Changed(object sender, TextChangedEventArgs e)
        {
            updateButtonsEnabled();
        }

        private void dateEnvironmentIdTextbox_Changed(object sender, TextChangedEventArgs e)
        {
            updateButtonsEnabled();
        }

        private void btnPAJurriaan_Click(object sender, RoutedEventArgs e)
        {
            PostAnalysis.Jurriaan.PAJurriaan window = new PostAnalysis.Jurriaan.PAJurriaan();
            window.Show();
            this.Close();
        }
    }
}
