using InterfacesUWP.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace InterfacesUWP
{
    /// <summary>
    /// Adds hyperlink and email address detection to text. From https://blogs.u2u.be/diederik/post/An-auto-hyperlinking-RichTextBlock-for-Windows-81-Store-apps
    /// </summary>
    public class TextBlockExtensions : DependencyObject
    {
        /// <summary>
        /// The raw text property.
        /// </summary>
        public static readonly DependencyProperty RawTextProperty =
            DependencyProperty.RegisterAttached("RawText", typeof(string), typeof(TextBlockExtensions), new PropertyMetadata("", OnRawTextChanged));

        /// <summary>
        /// Gets the raw text.
        /// </summary>
        public static string GetRawText(DependencyObject obj)
        {
            return obj.GetValue(RawTextProperty) as string;
        }

        /// <summary>
        /// Sets the raw text.
        /// </summary>
        public static void SetRawText(DependencyObject obj, string value)
        {
            obj.SetValue(RawTextProperty, value);
        }

        /// <summary>
        /// Called when raw text changed.
        /// </summary>
        private static void OnRawTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            string rawText = GetRawText(d);
            TextBlock tb = d as TextBlock;
            if (tb == null)
            {
                throw new InvalidOperationException("Object must be TextBlock");
            }

            try
            {
                tb.Inlines.Clear();

                if (rawText == null || rawText.Length == 0)
                {
                    return;
                }

                foreach (var inline in TextToRichInlinesHelper.Convert(rawText))
                {
                    tb.Inlines.Add(inline);
                }
            }
            catch (Exception ex)
            {
                tb.Text = ex.ToString();
            }
        }
    }
}
