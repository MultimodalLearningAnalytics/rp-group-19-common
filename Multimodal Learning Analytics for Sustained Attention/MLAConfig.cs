using Microsoft.Psi;
using Microsoft.Psi.Imaging;
using System;

namespace Multimodal_Learning_Analytics_for_Sustained_Attention
{
    /// <summary>
    /// This class reads the required environment variables, and shows warnings when required attributes are missing or invalid.
    /// </summary>
    static class MLAConfig
    {
        public static readonly string PSI_STORES_FOLDER = Properties.Settings.Default.PSI_STORES_FOLDER;
        public static readonly bool ENABLE_DIAGNOSTICS = Properties.Settings.Default.ENABLE_DIAGNOSTICS;
        public static readonly int VIDEO_WIDTH = Properties.Settings.Default.VIDEO_WIDTH;
        public static readonly int VIDEO_HEIGHT = Properties.Settings.Default.VIDEO_HEIGHT;
        public static readonly int VIDEO_FRAMERATE = Properties.Settings.Default.VIDEO_FRAMERATE;
        public static readonly string EXPERIMENTS_TEXTS_BASE_FOLDER = Properties.Settings.Default.EXPERIMENTS_TEXTS_BASE_FOLDER;
        public static readonly string EXPERIMENT1_RESULTS_FOLDER = Properties.Settings.Default.EXPERIMENT1_RESULTS_FOLDER;
        public static readonly string LAV_AUDIO_SUB_IP = Properties.Settings.Default.LAV_AUDIO_SUB_IP;
        public static readonly string EP_AUDIO_SUB_IP = Properties.Settings.Default.EP_AUDIO_SUB_IP;
        public static readonly string LAV_RAW_TOPIC = Properties.Settings.Default.LAV_RAW_TOPIC;
        public static readonly string LAV_FREQ_TOPIC = Properties.Settings.Default.LAV_FREQ_TOPIC;
        public static readonly bool LAV_SYNC_TIME = Properties.Settings.Default.LAV_SYNC_TIME;
        public static readonly string EP_RAW_TOPIC = Properties.Settings.Default.EP_RAW_TOPIC;
        public static readonly string EP_FREQ_TOPIC = Properties.Settings.Default.EP_FREQ_TOPIC;
        public static readonly bool EP_SYNC_TIME = Properties.Settings.Default.EP_SYNC_TIME;
        public static readonly string AUDIO_PLAYER_TOPIC = Properties.Settings.Default.AUDIO_PLAYER_TOPIC;
        public static readonly string AUDIO_PLAYER_SUB_IP = Properties.Settings.Default.AUDIO_PLAYER_SUB_IP;
        public static readonly string IR_COM_PORT = Properties.Settings.Default.IR_COM_PORT;
        public static readonly int IR_BAUD_RATE = Properties.Settings.Default.IR_BAUD_RATE;
        public static readonly bool VIDEO_VERTICAL_FLIP = Properties.Settings.Default.VIDEO_VERTICAL_FLIP;
        public static readonly int AUDIO_BLOCK_SIZE = Properties.Settings.Default.AUDIO_BLOCK_SIZE;
        public static readonly DeliveryPolicy<Shared<Image>> WEBCAM_DELIVERY_POLICY = DeliveryPolicy.LatestMessage;

        static MLAConfig()
        {
            if (string.IsNullOrEmpty(PSI_STORES_FOLDER))
            {
                throw new ArgumentNullException("PSI_STORES_FOLDER cannot be empty!");
            }

            if (VIDEO_WIDTH <= 0)
            {
                throw new ArgumentNullException("VIDEO_WIDTH environment variable must be set!");
            }

            if (VIDEO_HEIGHT <= 0)
            {
                throw new ArgumentNullException("VIDEO_HEIGHT environment variable must be set!");
            }

            if (VIDEO_FRAMERATE <= 0)
            {
                throw new ArgumentNullException("VIDEO_FRAMERATE environment variable must be set!");
            }

            if (string.IsNullOrEmpty(EXPERIMENTS_TEXTS_BASE_FOLDER))
            {
                throw new ArgumentNullException("EXPERIMENTS_TEXTS_BASE_FOLDER environment variable must be set!");
            }

            if (string.IsNullOrEmpty(EXPERIMENT1_RESULTS_FOLDER))
            {
                throw new ArgumentNullException("EXPERIMENT1_RESULTS_FOLDER environment variable must be set!");
            }

            if (string.IsNullOrEmpty(IR_COM_PORT))
            {
                throw new ArgumentNullException("IR_COM_PORT environment variable must be set");
            }

            if (IR_BAUD_RATE <= 0)
            {
                throw new ArgumentException("IR_BAUD_RATE must be a valid baud rate");
            }
        }
    }
}
