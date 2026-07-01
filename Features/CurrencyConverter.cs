// Features/CurrencyConverter.cs (Fitur pengubah mata uang khusus USD ke IDR)
using System.Text.RegularExpressions;

namespace AlaskaGoldFeverTranslator.Features
{
    public static class CurrencyConverter
    {
        private const double USD_TO_IDR_RATE = 17950.0;
        // Regex untuk menangkap $10, $ 100.50, $1,000, dll
        private static readonly Regex CurrencyRegex = new Regex(@"\$\s*([0-9,]+(?:\.\d+)?)", RegexOptions.Compiled);

        public static string Convert(string text)
        {
            // Jika teks kosong atau tidak mengandung simbol dollar, kembalikan aslinya untuk menghemat performa
            if (string.IsNullOrEmpty(text) || !text.Contains("$")) return text;

            return CurrencyRegex.Replace(text, match =>
            {
                // Menghapus koma pemisah ribuan gaya US agar bisa di-parse
                string numberStr = match.Groups[1].Value.Replace(",", "");

                if (double.TryParse(numberStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double usdAmount))
                {
                    double idrAmount = usdAmount * USD_TO_IDR_RATE;
                    // Format ke standar Indonesia (contoh: Rp 17.950.000)
                    return "Rp " + idrAmount.ToString("N0").Replace(",", ".");
                }
                return match.Value; // Jika gagal di-parse, biarkan teksnya seperti semula
            });
        }
    }
}