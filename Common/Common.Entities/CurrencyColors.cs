using Windows.UI;

namespace Common.Entities
{
    public class CurrencyColors
    {
        public Color BaseColor
        {
            get;
        }
        public Color QuoteColor
        {
            get;
        }

        public CurrencyColors(Color baseColor, Color quoteColor)
        {
            BaseColor = baseColor;
            QuoteColor = quoteColor;
        }
    }
}