using Microsoft.Psi;
using Microsoft.Psi.Audio;
using Microsoft.Psi.Data;
using Microsoft.Psi.Imaging;
using Microsoft.Psi.Interop.Format;
using Microsoft.Psi.Interop.Transport;
using Microsoft.Psi.Media;
using Multimodal_Learning_Analytics_for_Sustained_Attention.Distraction;
using Multimodal_Learning_Analytics_for_Sustained_Attention.IRTemperature;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Multimodal_Learning_Analytics_for_Sustained_Attention.Experiment1
{
    /// <summary>
    /// Interaction logic for Experiment1MainWindow.xaml
    /// </summary>
    public partial class Experiment1MainWindow : Window
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
        private DistractionEmitter distractionEmitter;
        private DeblurTimeEmitter deblurTimeEmitter;

        private DispatcherTimer blurringTimer;
        private Random blurringRandom;
        private DateTime? blurringStart;
        private List<TimeSpan> blurringTimes = new List<TimeSpan>();

        private bool isDisplayingText;

        public Experiment1MainWindow(string dateEnvironmentId, string participantId)
        {
            this.dateEnvironmentId = dateEnvironmentId;
            this.participantId = participantId;
            this.blurringRandom = new Random();
            this.isDisplayingText = false;
            this.textProvider = new RandomTextFromFolderProvider(MLAConfig.EXPERIMENTS_TEXTS_BASE_FOLDER);
            InitializeComponent();
            this.startBlur();
        }

        private void Grid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true; // Prevent ability to type in textbox

            if (e.Key == Key.Space && this.blurringStart != null)
            {
                // Deblur: save deblur time and deblur text
                TimeSpan deblurTime = DateTime.Now - this.blurringStart.Value;
                this.blurringTimes.Add(deblurTime);
                this.blurringStart = null;
                this.deblur();

                // Start timer again
                this.StartBlurringTimer();
            }
            if (e.Key == Key.Return)
            {
                if (this.textProvider.IsDone)
                {
                    pressSpaceBarWhenReadyLabel.Content = "FINITO!";
                    textBox.Text = "";
                    this.isDisplayingText = false;
                    this.StopBlurringTimer();
                    this.WriteTimesToCSV(this.blurringTimes);
                }
                else if (this.isDisplayingText)
                {
                    this.PSIStopRecoding();
                    textBox.Text = "";
                    pressSpaceBarWhenReadyLabel.Content = "Press enter to view the next text";
                    this.isDisplayingText = false;
                    this.StopBlurringTimer();
                }
                else
                {
                    string text = this.textProvider.GetNextRandomText();
                    textBox.Text = text;
                    pressSpaceBarWhenReadyLabel.Content = "Press enter to indicate you have read the text";
                    this.isDisplayingText = true;
                    this.deblur();
                    this.PSIStartRecording();
                    this.StartBlurringTimer();
                }
            }
        }

        private void WriteTimesToCSV(List<TimeSpan> blurringTimes)
        {
            var filepath = Path.Combine(MLAConfig.EXPERIMENT1_RESULTS_FOLDER, this.participantId + "_experiment1.csv");
            using (StreamWriter writer = new StreamWriter(new FileStream(filepath, FileMode.Create, FileAccess.Write)))
            {
                writer.WriteLine("Tdeblur (ms)");
                blurringTimes.ForEach(t => writer.WriteLine(t.TotalMilliseconds.ToString(CultureInfo.InvariantCulture)));
            }
        }

        private void PSIStartRecording()
        {
            PsiExporter videoStore;
            PsiExporter temperatureStore;
            PsiExporter audioStore;
            PsiExporter distractionStore;
            (this.pipeline, videoStore, temperatureStore, audioStore, distractionStore) = ParticipantPipeline.CreateRecordingPipelineAndStores(dateEnvironmentId, participantId, MLAExperiment.ExperimentOne, true);

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
            }
            else
            {
                this.webcamCapture.Write(ParticipantPipeline.WEBCAM_STREAM_NAME, videoStore, false, MLAConfig.WEBCAM_DELIVERY_POLICY);
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

            this.distractionEmitter = new DistractionEmitter(pipeline);
            this.distractionEmitter.Write(ParticipantPipeline.DISTRACTION_STREAM_NAME, distractionStore);

            this.deblurTimeEmitter = new DeblurTimeEmitter(pipeline);
            this.deblurTimeEmitter.WriteDeblurTimes(ParticipantPipeline.DEBLUR_TIMES_STREAM_NAME, distractionStore);
            this.deblurTimeEmitter.WriteInattentive(ParticipantPipeline.INATTENTIVE_STREAM_NAME, distractionStore);

            WaveFormat audioFormat = WaveFormat.CreatePcm(44100, 16, 1);

            this.lavAudioRaw = new AudioEmitterArray(pipeline, MLAConfig.LAV_RAW_TOPIC, MLAConfig.LAV_AUDIO_SUB_IP, MLAConfig.LAV_SYNC_TIME, audioFormat, 4410, JsonWrapper.LavInstance);
            this.lavAudioRaw.Write(ParticipantPipeline.LAV_AUDIO_RAW_STREAM_NAME, audioStore);

            this.lavAudioFreq = new AudioEmitter(pipeline, MLAConfig.LAV_FREQ_TOPIC, MLAConfig.LAV_AUDIO_SUB_IP, MLAConfig.LAV_SYNC_TIME, JsonWrapper.LavInstance);
            this.lavAudioFreq.Write(ParticipantPipeline.LAV_AUDIO_FREQ_STREAM_NAME, audioStore);

            this.epAudioRaw = new AudioEmitterArray(pipeline, MLAConfig.EP_RAW_TOPIC, MLAConfig.EP_AUDIO_SUB_IP, MLAConfig.EP_SYNC_TIME, audioFormat, 4410, JsonWrapper.EpInstance);
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
            this.distractionEmitter?.Stop(DateTime.UtcNow, () => { });

            this.pipeline?.Dispose();
        }

        /// <summary>(Re-)start the blurring timer. Used when text is displayed or when text got deblurred.</summary>
        private void StartBlurringTimer()
        {
            if (this.blurringTimer == null)
            {
                this.blurringTimer = new DispatcherTimer();
                this.blurringTimer.Tick += new EventHandler(blurringTimer_Tick);
            }

            // Set timer to new random interval
            int randomInterval = this.blurringRandom.Next(2, 6); // Random intervals between 2 and 5 seconds
            this.blurringTimer.Interval = new TimeSpan(0, 0, randomInterval);
            this.blurringTimer.Start();
        }

        /// <summary>Stop the blurring timer. Used when no text is displayed.</summary>
        private void StopBlurringTimer()
        {
            this.blurringTimer.Stop();
            this.deblur();
            this.blurringStart = null;
        }

        private void blurringTimer_Tick(object sender, EventArgs e)
        {
            // Stop timer until text got deblurred
            this.blurringTimer.Stop();

            this.blurringStart = DateTime.Now;
            this.startBlur();
        }

        /// <summary>Start blurring animation.</summary>
        private void startBlur()
        {
            textBox.BlurApply(4, new TimeSpan(0, 0, 4), TimeSpan.Zero);
        }

        /// <summary>Instantly deblur text.</summary>
        private void deblur()
        {
            textBox.BlurDisable(TimeSpan.Zero, TimeSpan.Zero);
        }
    }
}
