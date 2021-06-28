using System;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace Multimodal_Learning_Analytics_for_Sustained_Attention
{
    public static class BlurElementExtension
    {
        /// <summary>
        /// Blurs element gradually during 'duriation', starting at 'beginTime'.
        /// </summary>
        /// <param name="element">The element to blur</param>
        /// <param name="blurRadius">Blur radius / intensity</param>
        /// <param name="duration">The duration of the blur animation</param>
        /// <param name="beginTime">Delay to start blur</param>
        public static void BlurApply(this UIElement element, double blurRadius, TimeSpan duration, TimeSpan beginTime)
        {
            BlurEffect blur = new BlurEffect() {
                Radius = 0,
                RenderingBias = RenderingBias.Quality,
            };
            DoubleAnimation blurEnable = new DoubleAnimation(0, blurRadius, duration)
            { BeginTime = beginTime };
            element.Effect = blur;
            blur.BeginAnimation(BlurEffect.RadiusProperty, blurEnable);
        }
        /// <summary>
        /// Deblurs element gradually during 'duriation', starting at 'beginTime'.
        /// </summary>
        /// <param name="element">The element to deblur</param>
        /// <param name="duration">The duration of the deblur</param>
        /// <param name="beginTime">Delay to start deblur</param>
        public static void BlurDisable(this UIElement element, TimeSpan duration, TimeSpan beginTime)
        {
            BlurEffect blur = element.Effect as BlurEffect;
            if (blur == null || blur.Radius == 0)
            {
                return;
            }
            DoubleAnimation blurDisable = new DoubleAnimation(blur.Radius, 0, duration) { BeginTime = beginTime };
            blur.BeginAnimation(BlurEffect.RadiusProperty, blurDisable);
        }
    }
}
