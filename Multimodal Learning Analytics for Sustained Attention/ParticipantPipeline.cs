using Microsoft.Psi;
using Microsoft.Psi.Data;
using System;
using System.IO;

namespace Multimodal_Learning_Analytics_for_Sustained_Attention
{
    static class ParticipantPipeline
    {
        // Video
        public static readonly string VIDEO_STORE_NAME = "VideoRawData";
        public static readonly string WEBCAM_STREAM_NAME = "Webcam";

        // Temperature
        public static readonly string TEMPERATURE_STORE_NAME = "TemperatureRawData";
        public static readonly string AMBIENT_TEMPERATURE_STREAM_NAME = "Ambient temperature";
        public static readonly string PARTICIPANT_TEMPERATURE_STREAM_NAME = "Participant temperature";

        // Audio
        public static readonly string LAV_AUDIO_RAW_STREAM_NAME = "LavAudioRaw";
        public static readonly string LAV_AUDIO_FREQ_STREAM_NAME = "LavAudioFreq";
        public static readonly string LAV_AUDIO_SYNC_STREAM_NAME = "LavAudioSync";
        public static readonly string EP_AUDIO_RAW_STREAM_NAME = "EpAudioRaw";
        public static readonly string EP_AUDIO_FREQ_STREAM_NAME = "EpAudioFreq";
        public static readonly string AUDIO_PLAYER_STREAM_NAME = "AudioPlayer";
        public static readonly string AUDIO_STORE_NAME = "AudioRawData";

        // Distraction
        public static readonly string DISTRACTION_STREAM_NAME = "Distraction";
        public static readonly string DEBLUR_TIMES_STREAM_NAME = "DeblurTimes";
        public static readonly string INATTENTIVE_STREAM_NAME = "Inattentive";
        public static readonly string DISTRACTION_STORE_NAME = "Distraction";

        // Diagnostics
        public static readonly string DIAGNOSTICS_STREAM_NAME = "Diagnostics";

        public static (Pipeline pipeline, PsiExporter videoStore, PsiExporter temperatureStore, PsiExporter audioStore, PsiExporter distractionStore) CreateRecordingPipelineAndStores(string dateEnvironmentId, string participantId, MLAExperiment experiment, bool containsDistractionStore)
        {
            string storePath = GetStorePath(experiment, dateEnvironmentId, participantId);

            // Create PSI pipeline and create new PSI store files
            Pipeline pipeline = Pipeline.Create(enableDiagnostics: MLAConfig.ENABLE_DIAGNOSTICS);
            PsiExporter videoStore = PsiStore.Create(pipeline, VIDEO_STORE_NAME, storePath);
            PsiExporter temperatureStore = PsiStore.Create(pipeline, TEMPERATURE_STORE_NAME, storePath);
            PsiExporter audioStore = PsiStore.Create(pipeline, AUDIO_STORE_NAME, storePath);
            PsiExporter distractionStore = null;
            if (containsDistractionStore)
            {
                distractionStore = PsiStore.Create(pipeline, DISTRACTION_STORE_NAME, storePath);
            }

            if (MLAConfig.ENABLE_DIAGNOSTICS)
            {
                pipeline.Diagnostics.Write(DIAGNOSTICS_STREAM_NAME, videoStore);
            }

            pipeline.PipelineRun += delegate (object sender, PipelineRunEventArgs e)
            {
                Console.WriteLine("Pipeline started");
            };
            pipeline.PipelineCompleted += delegate (object sender, PipelineCompletedEventArgs e)
            {
                Console.WriteLine("Pipeline completed");
            };

            return (pipeline, videoStore, temperatureStore, audioStore, distractionStore);
        }

        public static string GetStorePath(MLAExperiment experiment, string dateEnvironmentId, string participantId)
        {
            return Path.Combine(MLAConfig.PSI_STORES_FOLDER, experiment.Value, dateEnvironmentId, participantId);
        }

        /// <summary>
        /// Transforms a c:\experiment_stores\{experiment}\{date-id}\{participantID} path to one that is e.g: ...\{participantID}\VideoRawData.0001.
        /// In practice only used for replaying data (don't use this for recording of data).
        /// </summary>
        /// <param name="storesPath">The current non versioned store path</param>
        /// <param name="storeName">The storeName</param>
        /// <param name="streamVersion">The version number such as '0002'</param>
        /// <returns>A full path to a specific store including version number, thus: 'c:\experiment_stores\{experiment}\{date-id}\{participantID}\{storeName}.{streamVersion}'</returns>
        public static string AppendStreamVersionToPath(string storesPath, string storeName, string streamVersion)
        {
            if (streamVersion == null || streamVersion.Length == 0) return storeName;

            if (!streamVersion.StartsWith("."))
            {
                streamVersion = $".{streamVersion}";
            }

            return Path.Combine(storesPath, storeName + streamVersion);
        }

        public static (
            Pipeline pipeline,
            PsiImporter videoStore,
            PsiImporter temperatureStore,
            PsiImporter audioStore,
            PsiImporter distractionStore
        ) CreateReadPipelineAndStores(
            string dateEnvironmentId,
            string participantId,
            MLAExperiment experiment,
            bool containsDistractionStore,
            string streamVersion = null
        ) {
            string storesPath = GetStorePath(experiment, dateEnvironmentId, participantId);

            

            Pipeline pipeline = Pipeline.Create(enableDiagnostics: MLAConfig.ENABLE_DIAGNOSTICS);
            PsiImporter videoStore = PsiStore.Open(pipeline, VIDEO_STORE_NAME, AppendStreamVersionToPath(storesPath, VIDEO_STORE_NAME, streamVersion));
            PsiImporter temperatureStore = PsiStore.Open(pipeline, TEMPERATURE_STORE_NAME, AppendStreamVersionToPath(storesPath, TEMPERATURE_STORE_NAME, streamVersion));
            PsiImporter audioStore = PsiStore.Open(pipeline, AUDIO_STORE_NAME, AppendStreamVersionToPath(storesPath, AUDIO_STORE_NAME, streamVersion));
            PsiImporter distractionStore = null;
            if (containsDistractionStore)
            {
                distractionStore = PsiStore.Open(pipeline, DISTRACTION_STORE_NAME, AppendStreamVersionToPath(storesPath, DISTRACTION_STORE_NAME, streamVersion));
            }

            return (pipeline, videoStore, temperatureStore, audioStore, distractionStore);
        }
    }

    public class MLAExperiment
    {
        private MLAExperiment(string value) { Value = value; }

        public string Value;

        public static MLAExperiment ExperimentOne { get { return new MLAExperiment("experiment1"); } }
        public static MLAExperiment ExperimentTwo { get { return new MLAExperiment("experiment2"); } }
        public static MLAExperiment ExperimentThree { get { return new MLAExperiment("experiment3"); } }
        public static MLAExperiment Testing { get { return new MLAExperiment("testing"); } }

        public override string ToString()
        {
            return Value;
        }
    }
}

