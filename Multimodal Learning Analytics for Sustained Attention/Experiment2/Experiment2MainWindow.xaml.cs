using Microsoft.Psi;
using Microsoft.Psi.Audio;
using Microsoft.Psi.Data;
using Microsoft.Psi.Media;
using Microsoft.Psi.Interop.Format;
using Microsoft.Psi.Interop.Transport;
using Microsoft.Psi.Imaging;
using Multimodal_Learning_Analytics_for_Sustained_Attention.IRTemperature;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Multimodal_Learning_Analytics_for_Sustained_Attention.Experiment2
{
    /// <summary>
    /// Interaction logic for Experiment2MainWindow.xaml
    /// </summary>
    public partial class Experiment2MainWindow : Window
    {
        private readonly string participantId;
        private readonly string dateEnvironmentId;

        private RandomTextFromFolderProvider textProvider;
        private Pipeline pipeline;
        private MediaCapture webcamCapture;
        private IRTemperatureReader temperatureReader;
        private AudioEmitterArray lavAudioRaw;
        private AudioEmitter lavAudioFreq;
        private AudioEmitterArray epAudioRaw;
        private AudioEmitter epAudioFreq;
        private Emitter<string> audioPlayer;


        private bool isDisplayingText;

        public Experiment2MainWindow(string dateEnvironmentId, string participantId)
        {
            this.dateEnvironmentId = dateEnvironmentId;
            this.participantId = participantId;
            this.isDisplayingText = false;
            this.textProvider = new RandomTextFromFolderProvider(MLAConfig.EXPERIMENTS_TEXTS_BASE_FOLDER);
            InitializeComponent();
        }

        private void Grid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true; // Prevent ability to type in textbox

            if (e.Key == Key.Return)
            {
                if (this.textProvider.IsDone)
                {
                    this.PSIStopRecoding();
                    textBox.Text = "";
                    pressSpaceBarWhenReadyLabel.Content = "FINITO!";
                    this.isDisplayingText = false;
                }
                else if (this.isDisplayingText)
                {
                    this.PSIStopRecoding();
                    textBox.Text = "";
                    pressSpaceBarWhenReadyLabel.Content = "Press enter to view the next text";
                    this.isDisplayingText = false;
                }
                else
                {
                    string text = this.textProvider.GetNextRandomText();
                    if (text.Length > 0)
                    {
                        textBox.Text = text;
                        pressSpaceBarWhenReadyLabel.Content = "Press enter to indicate you have read the text";
                        this.isDisplayingText = true;
                        this.PSIStartRecording();
                    }
                }
            }
        }

        private void PSIStartRecording()
        {
            PsiExporter videoStore;
            PsiExporter temperatureStore;
            PsiExporter audioStore;
            (this.pipeline, videoStore, temperatureStore, audioStore, _) = ParticipantPipeline.CreateRecordingPipelineAndStores(dateEnvironmentId, participantId, MLAExperiment.ExperimentTwo, false);

            // Create the webcam component and pipe it to VideoRawData store
            MediaCaptureConfiguration config = new MediaCaptureConfiguration
            {
                CaptureAudio = false,
                Width = MLAConfig.VIDEO_WIDTH,
                Height = MLAConfig.VIDEO_HEIGHT,
                Framerate = MLAConfig.VIDEO_FRAMERATE
            };
            this.webcamCapture = new MediaCapture(this.pipeline, config);
            if (MLAConfig.VIDEO_VERTICAL_FLIP)
            {
                this.webcamCapture.Flip(FlipMode.AlongHorizontalAxis, MLAConfig.WEBCAM_DELIVERY_POLICY).Write(ParticipantPipeline.WEBCAM_STREAM_NAME, videoStore);
            } else
            {
                this.webcamCapture.Write(ParticipantPipeline.WEBCAM_STREAM_NAME, videoStore);
            }

            this.temperatureReader = new IRTemperatureReader(pipeline);
            this.temperatureReader.Select((temps, e) =>
            {
                return temps.AmbientTemperature;
            }).Write(ParticipantPipeline.AMBIENT_TEMPERATURE_STREAM_NAME, temperatureStore);
            this.temperatureReader.Select((temps, e) =>
            {
                return temps.ObjectTemperature;
            }).Write(ParticipantPipeline.PARTICIPANT_TEMPERATURE_STREAM_NAME, temperatureStore);

            WaveFormat audioFormat = WaveFormat.CreatePcm(44100, 16, 1);

            this.lavAudioRaw = new AudioEmitterArray(pipeline, MLAConfig.LAV_RAW_TOPIC, MLAConfig.LAV_AUDIO_SUB_IP, MLAConfig.LAV_SYNC_TIME, audioFormat, MLAConfig.AUDIO_BLOCK_SIZE, JsonWrapper.LavInstance);
            this.lavAudioRaw.Write(ParticipantPipeline.LAV_AUDIO_RAW_STREAM_NAME, audioStore);

            this.lavAudioFreq = new AudioEmitter(pipeline, MLAConfig.LAV_FREQ_TOPIC, MLAConfig.LAV_AUDIO_SUB_IP, MLAConfig.LAV_SYNC_TIME, JsonWrapper.LavInstance);
            this.lavAudioFreq.Write(ParticipantPipeline.LAV_AUDIO_FREQ_STREAM_NAME, audioStore);

            this.epAudioRaw = new AudioEmitterArray(pipeline, MLAConfig.EP_RAW_TOPIC, MLAConfig.EP_AUDIO_SUB_IP, MLAConfig.EP_SYNC_TIME, audioFormat, MLAConfig.AUDIO_BLOCK_SIZE, JsonWrapper.EpInstance);
            this.epAudioRaw.Write(ParticipantPipeline.EP_AUDIO_RAW_STREAM_NAME, audioStore);

            this.epAudioFreq = new AudioEmitter(pipeline, MLAConfig.EP_FREQ_TOPIC, MLAConfig.EP_AUDIO_SUB_IP, MLAConfig.EP_SYNC_TIME, JsonWrapper.EpInstance);
            this.epAudioFreq.Write(ParticipantPipeline.EP_AUDIO_FREQ_STREAM_NAME, audioStore);

            this.audioPlayer = new NetMQSource<string>(pipeline, MLAConfig.AUDIO_PLAYER_TOPIC, $"tcp://{MLAConfig.AUDIO_PLAYER_SUB_IP}", JsonFormat.Instance).Out;
            this.audioPlayer.Write(ParticipantPipeline.AUDIO_PLAYER_STREAM_NAME, audioStore);

            this.pipeline.RunAsync();
        }

        private void PSIStopRecoding()
        {
            this.temperatureReader?.Stop(DateTime.UtcNow, () => { });
            this.webcamCapture?.Stop(DateTime.UtcNow, () => { });
            this.lavAudioRaw?.Stop(DateTime.UtcNow, () => { });
            this.lavAudioFreq?.Stop(DateTime.UtcNow, () => { });
            this.epAudioRaw?.Stop(DateTime.UtcNow, () => { });
            this.epAudioFreq?.Stop(DateTime.UtcNow, () => { });
            this.audioPlayer?.Close(DateTime.UtcNow);

            this.pipeline?.Dispose();
        }
    }
}
