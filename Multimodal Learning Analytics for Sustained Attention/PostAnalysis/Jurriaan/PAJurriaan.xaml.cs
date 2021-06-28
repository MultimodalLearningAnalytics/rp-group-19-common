using Microsoft.Psi;
using Microsoft.Psi.Data;
using Microsoft.Psi.Imaging;
using Microsoft.Psi.Media;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Text.RegularExpressions;
using Microsoft.Psi.Diagnostics;
using Microsoft.Psi.Interop.Transport;
using Microsoft.Psi.Interop.Format;
using Rectangle = System.Drawing.Rectangle;

namespace Multimodal_Learning_Analytics_for_Sustained_Attention.PostAnalysis.Jurriaan
{
    /// <summary>
    /// Interaction logic for PAJurriaan.xaml
    /// </summary>
    public partial class PAJurriaan : Window
    {
        private MLAExperiment experiment;
        private string participantID;
        private string dateEnvironmentID;
        private string sessionId;
        private bool? containsDistractionStore;
        private bool? asFastAsPossible;

        private List<string> dateEnvironmentIDList;
        private List<string> participantIDList;
        private List<string> streamVersionList;

        private IProgress<(string, double)> replayProgress;
        DateTime replayStartTime;
        private int sentFrames;
        private int receivedFrames;

        private bool isRunning;

        private WriteableBitmap bitmap;
        private IntPtr bitmapPtr;
        

        public PAJurriaan()
        {
            InitializeComponent();
            
            List<MLAExperiment> comboBoxItems = new List<MLAExperiment>
            {
                MLAExperiment.ExperimentOne,
                MLAExperiment.ExperimentTwo,
                MLAExperiment.ExperimentThree,
                MLAExperiment.Testing
            };
            experimentComboBox.ItemsSource = comboBoxItems;

            replayProgress = new Progress<(string, double)>(pr => {
                TimeSpan deltaTime = DateTime.Now - this.replayStartTime;
                double deltaSeconds = deltaTime.TotalSeconds;
                double expectedSeconds = deltaSeconds / pr.Item2;
                //Console.WriteLine($"Progress: {pr.Item2}, deltaSeconds: {deltaSeconds}, expectedSeconds: {expectedSeconds}");
                DateTime expectedTime = this.replayStartTime.AddSeconds(expectedSeconds);

                this.Dispatcher.Invoke(() => {
                    lblStatus.Content = $"ETA: {expectedTime}";
                    lblSentFrames.Content = $"Sent frames: {this.sentFrames}";
                    lblReceivedFrames.Content = $"Received frames: {this.receivedFrames}";
                });

                progressBar.Value = pr.Item2 * 100;
            });

            isRunning = false;
            Validate();
        }

        private void ComboBoxExperiment_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            experiment = (MLAExperiment)experimentComboBox.SelectedValue;
            comboBoxDateEnvID.SelectedItem = null;
            comboBoxDateEnvID.ItemsSource = null;
            comboBoxParticipantID.SelectedItem = null;
            comboBoxParticipantID.ItemsSource = null;

            try
            {
                dateEnvironmentIDList = new List<string>(Directory.GetDirectories(System.IO.Path.Combine($"{MLAConfig.PSI_STORES_FOLDER}\\{experiment}")));
                comboBoxDateEnvID.ItemsSource = dateEnvironmentIDList;
            } catch (IOException err)
            {
                Console.WriteLine(err.ToString());
            }
            Validate();
        }

        private void ComboBoxDateEnvID_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            dateEnvironmentID = comboBoxDateEnvID.SelectedValue?.ToString();
            comboBoxParticipantID.SelectedItem = null;
            comboBoxParticipantID.ItemsSource = null;

            if (dateEnvironmentID != null)
            {
                try
                {
                    participantIDList = new List<string>(Directory.GetDirectories(dateEnvironmentID));
                    comboBoxParticipantID.ItemsSource = participantIDList;
                }
                catch (IOException err)
                {
                    Console.WriteLine(err.ToString());
                }
            }
            Validate();
        }

        private void ComboBoxParticipantID_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            participantID = comboBoxParticipantID.SelectedValue?.ToString();
            comboBoxSession.SelectedItem = null;
            comboBoxSession.ItemsSource = null;

            if (participantID != null)
            {
                try
                {
                    List<string> allStoreDirectories = new List<string>(Directory.GetDirectories(participantID));
                    HashSet<string> uniqueStreamVersions = new HashSet<string>();

                    Regex rx = new Regex(@"\w+\.(\d+)", RegexOptions.Multiline | RegexOptions.IgnoreCase);

                    foreach (string directory in allStoreDirectories)
                    {
                        foreach (Match match in rx.Matches(directory))
                        {
                            uniqueStreamVersions.Add(match.Groups[1].ToString());
                        }
                    }
                    streamVersionList = uniqueStreamVersions.ToList<string>();
                    comboBoxSession.ItemsSource = streamVersionList;
                } catch (IOException err)
                {
                    Console.WriteLine(err.ToString());
                }
            }

            Validate();
        }

        private void comboBoxSession_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            sessionId = comboBoxSession.SelectedValue?.ToString();
            Validate();
        }

        private void Validate()
        {
            this.Dispatcher.BeginInvoke(
                new Action(() => {
                    comboBoxDateEnvID.IsEnabled = dateEnvironmentIDList?.Count > 0;
                    comboBoxParticipantID.IsEnabled = participantIDList?.Count > 0;
                    comboBoxSession.IsEnabled = streamVersionList?.Count > 0;

                    if (this.isRunning)
                    {
                        BtnDetectFaces.Content = "Running...";
                        BtnDetectFaces.Background = new SolidColorBrush(Color.FromRgb(255, 164, 87));
                        BtnCleanTemps.Background = new SolidColorBrush(Color.FromRgb(255, 164, 87));
                        BtnDetectFaces.IsEnabled = false;
                        BtnCleanTemps.IsEnabled = false;
                    }
                    else
                    {
                        BtnDetectFaces.Content = "Detect Faces";
                        BtnCleanTemps.Content = "Clean temps";
                        BtnDetectFaces.Background = new SolidColorBrush(Color.FromRgb(179, 238, 200));
                        BtnCleanTemps.Background = new SolidColorBrush(Color.FromRgb(179, 238, 200));

                        if (experiment != null && dateEnvironmentID?.Length > 0 && participantID?.Length > 0 && sessionId?.Length > 0)
                        {
                            BtnDetectFaces.IsEnabled = true;
                            BtnCleanTemps.IsEnabled = true;
                        }
                        else
                        {
                            BtnDetectFaces.IsEnabled = false;
                            BtnDetectFaces.IsEnabled = false;
                        }
                    }
                })
            );
        }

        private void CheckBoxContainsDistractionStore_Changed(object sender, RoutedEventArgs e)
        {
            containsDistractionStore = checkBoxContainsDistractionStore.IsChecked;
            Validate();
        }

        private async void BtnDetectFaces_Click(object sender, RoutedEventArgs evnt)
        {
            if (!this.isRunning)
            {
                this.replayStartTime = DateTime.Now;
                string participantStorePath = ParticipantPipeline.GetStorePath(experiment, dateEnvironmentID, participantID);
                string videoStoreWithVersionPath = ParticipantPipeline.AppendStreamVersionToPath(participantStorePath, ParticipantPipeline.VIDEO_STORE_NAME, sessionId);
                string detectedFacesStoreWithVersionPath = System.IO.Path.Combine(participantStorePath, $"{StoreNamesJurriaan.DETECTED_FACES_STORE_NAME}.{sessionId}");

                if (Directory.Exists(detectedFacesStoreWithVersionPath))
                {
                    if (MessageBox.Show(
                        $"Warning, there already exists a store at {detectedFacesStoreWithVersionPath}.\n\nDo you want to override the existing store?",
                        "Overwrite existing store?",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning,
                        MessageBoxResult.No
                    ) != MessageBoxResult.Yes) {
                        return;
                    }
                }

                this.isRunning = true;
                Validate();

                Dataset dataset = Dataset.CreateFromStore(new PsiStoreStreamReader(ParticipantPipeline.VIDEO_STORE_NAME, videoStoreWithVersionPath));

                await dataset.CreateDerivedPartitionAsync((pipeline, importer, exporter) =>
                {
                    pipeline.ProposeReplayTime(TimeInterval.LeftBounded(DateTime.UtcNow));
                    pipeline.PipelineCompleted += (s, e) => { Console.WriteLine($"Pipeline completed by {s} at {e.CompletedOriginatingTime}"); };

                    IProducer<Shared<Image>> webcamStream = importer.OpenStream<Shared<Image>>(ParticipantPipeline.WEBCAM_STREAM_NAME);

                    EmotionsDetector emotionsDetector = new EmotionsDetector(pipeline);

                    this.sentFrames = 0;
                    this.receivedFrames = 0;
                    webcamStream.Do((_, e) => {
                        if (this.sentFrames - this.receivedFrames > 100)
                        {
                            Thread.Sleep(200);
                        }
                        this.sentFrames++;
                    }, DeliveryPolicy.SynchronousOrThrottle)
                    .PipeTo(emotionsDetector, DeliveryPolicy.SynchronousOrThrottle);

                    IProducer<DetectedFace> largestFace = emotionsDetector
                        .Select((e, env) =>
                        {
                            this.receivedFrames++;
                            if (!this.isRunning)
                            {
                                pipeline?.Dispose();
                            }
                            return e.OrderByDescending(e2 => e2.Box.Width * e2.Box.Height).ElementAtOrDefault(0);
                        })
                        .Where(f => f != null);
                    largestFace.Select(f => f.Box).Write(StoreNamesJurriaan.FACE_RECTANGLES_STREAM_NAME, exporter);

                    IProducer<Emotions> emotions = largestFace.Select(f => f.Emotions);
                    emotions.Select(e => e.Angry).Write(StoreNamesJurriaan.ANGRY_STREAM_NAME, exporter);
                    emotions.Select(e => e.Disgust).Write(StoreNamesJurriaan.DISGUST_STREAM_NAME, exporter);
                    emotions.Select(e => e.Fear).Write(StoreNamesJurriaan.FEAR_STREAM_NAME, exporter);
                    emotions.Select(e => e.Happy).Write(StoreNamesJurriaan.HAPPY_STREAM_NAME, exporter);
                    emotions.Select(e => e.Neutral).Write(StoreNamesJurriaan.NEUTRAL_STREAM_NAME, exporter);
                    emotions.Select(e => e.Sad).Write(StoreNamesJurriaan.SAD_STREAM_NAME, exporter);
                    emotions.Select(e => e.Surprise).Write(StoreNamesJurriaan.SURPRISE_STREAM_NAME, exporter);

                    //webcamStream.Do(frame => this.DrawFrame(frame));
                },
                StoreNamesJurriaan.DETECTED_FACES_STORE_NAME,
                false,
                StoreNamesJurriaan.DETECTED_FACES_STORE_NAME,
                detectedFacesStoreWithVersionPath,
                this.asFastAsPossible == true ? ReplayDescriptor.ReplayAll : ReplayDescriptor.ReplayAllRealTime,
                null,
                false,
                this.replayProgress);

                this.isRunning = false;
                Validate();
            }
        }

        private async void BtnCleanTemps_Click(object sender, RoutedEventArgs evnt)
        {
            if (this.isRunning) return;

            string participantStorePath = ParticipantPipeline.GetStorePath(experiment, dateEnvironmentID, participantID);
            string temperatureWithVersionStorePath = ParticipantPipeline.AppendStreamVersionToPath(participantStorePath, ParticipantPipeline.TEMPERATURE_STORE_NAME, sessionId);
            string cleanedTemperatureWithVersionStorePath = System.IO.Path.Combine(participantStorePath, $"{StoreNamesJurriaan.FILTERED_TEMPERATURE_STORE_NAME}.{sessionId}");

            if (Directory.Exists(cleanedTemperatureWithVersionStorePath))
            {
                if (MessageBox.Show(
                    $"Warning, there already exists a store at {cleanedTemperatureWithVersionStorePath}.\n\nDo you want to override the existing store?",
                    "Overwrite existing store?",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning,
                    MessageBoxResult.No
                ) != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            this.isRunning = true;
            Validate();

            Dataset dataset = Dataset.CreateFromStore(new PsiStoreStreamReader(ParticipantPipeline.TEMPERATURE_STORE_NAME, temperatureWithVersionStorePath));

            await dataset.CreateDerivedPartitionAsync((pipeline, importer, exporter) =>
            {
                pipeline.PipelineCompleted += (s, e) => { Console.WriteLine($"Pipeline completed by {s} at {e.CompletedOriginatingTime}"); };
                IProducer<double> participantTempStream = importer.OpenStream<double>(ParticipantPipeline.PARTICIPANT_TEMPERATURE_STREAM_NAME);

                participantTempStream.Where(d => d > 33).Write(StoreNamesJurriaan.FILTERED_PARTICIPANT_TEMPERATURE_STREAM_NAME, exporter);
            },
            StoreNamesJurriaan.FILTERED_TEMPERATURE_STORE_NAME,
            false,
            StoreNamesJurriaan.FILTERED_TEMPERATURE_STORE_NAME,
            cleanedTemperatureWithVersionStorePath,
            this.asFastAsPossible == true ? ReplayDescriptor.ReplayAll : ReplayDescriptor.ReplayAllRealTime,
            null,
            false,
            this.replayProgress);

            this.isRunning = false;
            Validate();
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            MainWindow window = new MainWindow();
            this.Close();
            window.Show();
        }

        private void DrawFrame(Shared<Image> frame)
        {
            // copy the frame image to the display bitmap
            this.UpdateBitmap(frame);

            // redraw on the UI thread
            this.Dispatcher.BeginInvoke(
                new Action(() =>
                {
                    this.UpdateDisplayImage();
                }));
        }

        private void UpdateDisplayImage()
        {
            // invalidate the entire area of the bitmap to cause the display image to be redrawn
            this.bitmap.Lock();
            this.bitmap.AddDirtyRect(new Int32Rect(0, 0, this.bitmap.PixelWidth, this.bitmap.PixelHeight));
            this.bitmap.Unlock();
        }

        private void UpdateBitmap(Shared<Image> image)
        {
            // create a new bitmap if necessary
            if (this.bitmap == null)
            {
                // WriteableBitmap must be created on the UI thread
                this.Dispatcher.Invoke(() =>
                {
                    this.bitmap = new WriteableBitmap(
                        image.Resource.Width,
                        image.Resource.Height,
                        300,
                        300,
                        image.Resource.PixelFormat.ToWindowsMediaPixelFormat(),
                        null);

                    this.image.Source = this.bitmap;
                    this.bitmapPtr = this.bitmap.BackBuffer;
                });
            }

            // update the display bitmap's back buffer
            image.Resource.CopyTo(this.bitmapPtr, image.Resource.Width, image.Resource.Height, image.Resource.Stride, image.Resource.PixelFormat);
        }

        private void checkBoxAsFastAsPossible_Changed(object sender, RoutedEventArgs e)
        {
            this.asFastAsPossible = this.checkBoxAsFastAsPossible.IsChecked;
        }

        private void componentCompleted(object sender, ComponentCompletedEventArgs env)
        {
            Console.WriteLine($"{env.ComponentName} completed at {env.CompletedDateTime} by sender {sender}");
        }
    }
}
