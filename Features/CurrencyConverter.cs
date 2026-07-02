// Features/CurrencyConverter.cs (Fitur pengubah mata uang khusus USD ke IDR dengan Live Market Fetcher)
using System;
using System.Net;
using System.IO;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace AlaskaGoldFeverTranslator.Features
{
    public static class CurrencyConverter
    {
        // Nilai fallback (cadangan) jika PC offline atau Google Finance berubah struktur
        private static double _usdToIdrRate = 17972.0;

        // Regex untuk menangkap $10, $ 100.50, $1,000, 10,00$, dll
        private static readonly Regex CurrencyRegex = new Regex(
            @"(?:(?:\$|USD)\s*([-\d,\.]+))|(?:([-\d,\.]+)\s*(?:\$|USD))|^(?:\s*)([-]?\d{1,3}(?:[.,]\d{3})*[.,]\d{2})(?:\s*)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static void Initialize()
        {
            Main.Logger.LogInfo("[CurrencyConverter] Initializing Live Market Fetcher...");
            // Menjalankan pencarian kurs di background agar game tidak nge-freeze
            Task.Run(FetchLiveRateAsync);
        }

        private static async Task FetchLiveRateAsync()
        {
            try
            {
                // Link Google Finance USD to IDR
                string url = "https://www.google.com/finance/quote/USD-IDR";
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)";
                request.Timeout = 10000; // Timeout 10 detik

                using (WebResponse response = await request.GetResponseAsync())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    string html = await reader.ReadToEndAsync();

                    // Mencari nilai di dalam class HTML milik Google Finance (contoh: <div class="YMlKec fxKbKc">16,400.50</div>)
                    Match match = Regex.Match(html, @"class=""YMlKec fxKbKc""[^>]*>([\d,]+\.\d+)");

                    if (match.Success)
                    {
                        // Hapus koma agar bisa dikonversi ke double
                        string rateStr = match.Groups[1].Value.Replace(",", "");
                        if (double.TryParse(rateStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double liveRate))
                        {
                            _usdToIdrRate = liveRate;
                            Main.Logger.LogInfo($"[CurrencyConverter] SUCCESS! Live market rate updated from Google Finance: Rp {_usdToIdrRate:N2}");
                            return;
                        }
                    }

                    // Fallback Regex jika Google mengubah nama class-nya
                    Match matchFallback = Regex.Match(html, @"data-last-price=""([\d\.]+)""");
                    if (matchFallback.Success)
                    {
                        string rateStr = matchFallback.Groups[1].Value;
                        if (double.TryParse(rateStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double liveRate))
                        {
                            _usdToIdrRate = liveRate;
                            Main.Logger.LogInfo($"[CurrencyConverter] SUCCESS! Live market rate updated via fallback attribute: Rp {_usdToIdrRate:N2}");
                            return;
                        }
                    }

                    Main.Logger.LogWarning("[CurrencyConverter] Failed to find the exact rate in HTML. Using offline fallback rate.");
                }
            }
            catch (Exception ex)
            {
                Main.Logger.LogError($"[CurrencyConverter] Internet/Fetch Error: {ex.Message}. Using offline fallback rate.");
            }
        }

        public static string Convert(string text)
        {
            // Abaikan jika teks kosong
            if (string.IsNullOrEmpty(text)) return text;

            return CurrencyRegex.Replace(text, match =>
            {
                // Mengambil string angka dari salah satu grup yang cocok
                string numberStr = match.Groups[1].Success ? match.Groups[1].Value :
                                   match.Groups[2].Success ? match.Groups[2].Value :
                                   match.Groups[3].Value;

                if (string.IsNullOrEmpty(numberStr)) return match.Value;

                numberStr = numberStr.Trim();

                // DETEKSI PINTAR: Mencari format Eropa/Indonesia vs Format US
                int lastComma = numberStr.LastIndexOf(',');
                int lastDot = numberStr.LastIndexOf('.');

                if (lastComma > lastDot && (numberStr.Length - lastComma) <= 3)
                {
                    numberStr = numberStr.Replace(".", ""); // Hapus pemisah ribuan (titik)
                    numberStr = numberStr.Replace(",", "."); // Ubah koma desimal jadi titik
                }
                else
                {
                    numberStr = numberStr.Replace(",", ""); // Hapus pemisah ribuan (koma)
                }

                if (double.TryParse(numberStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double usdAmount))
                {
                    // Menggunakan _usdToIdrRate yang bisa berubah secara Real-time!
                    double idrAmount = usdAmount * _usdToIdrRate;

                    string prefix = usdAmount < 0 ? "-Rp. " : "Rp. ";
                    idrAmount = System.Math.Abs(idrAmount);

                    return prefix + idrAmount.ToString("N0").Replace(",", ".");
                }

                return match.Value;
            });
        }
    }
}