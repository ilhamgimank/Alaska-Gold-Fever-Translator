// Features/CurrencyConverter.cs (Fitur pengubah mata uang khusus USD ke IDR)
using System.Text.RegularExpressions;

namespace AlaskaGoldFeverTranslator.Features
{
    public static class CurrencyConverter
    {
        private const double USD_TO_IDR_RATE = 17950.0;

        // Menangkap format angka secara bebas (baik pakai titik maupun koma)
        private static readonly Regex CurrencyRegex = new Regex(@"(?:(?:\$|USD)\s*([\d,\.]+))|(?:([\d,\.]+)\s*(?:\$|USD))", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static string Convert(string text)
        {
            // Abaikan jika teks kosong
            if (string.IsNullOrEmpty(text)) return text;

            // Pengecekan kilat
            if (!text.Contains("$") && text.IndexOf("USD", System.StringComparison.OrdinalIgnoreCase) < 0) return text;

            return CurrencyRegex.Replace(text, match =>
            {
                // Mengambil string angka
                string numberStr = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;

                // Membersihkan titik/koma yang mungkin secara tak sengaja terbawa di akhir kalimat (misal: "Harganya $5.")
                numberStr = numberStr.TrimEnd('.', ',');

                // DETEKSI PINTAR: Mencari format Eropa/Indonesia vs Format US
                int lastComma = numberStr.LastIndexOf(',');
                int lastDot = numberStr.LastIndexOf('.');

                // Jika posisi koma ada di belakang titik, ATAU tidak ada titik tapi komanya ada di 2 digit terakhir (misal: "5,00")
                if (lastComma > lastDot && (numberStr.Length - lastComma) <= 3)
                {
                    // Ini pasti Format ID/EU (1.000,50 atau 5,00)
                    numberStr = numberStr.Replace(".", ""); // Hapus pemisah ribuan (titik)
                    numberStr = numberStr.Replace(",", "."); // Ubah koma desimal jadi titik standar komputer
                }
                else
                {
                    // Ini pasti Format US (1,000.50 atau 5.00 atau 5,000)
                    numberStr = numberStr.Replace(",", ""); // Hapus pemisah ribuan (koma)
                }

                if (double.TryParse(numberStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double usdAmount))
                {
                    double idrAmount = usdAmount * USD_TO_IDR_RATE;
                    // Format ke standar Indonesia (contoh: Rp 89.750)
                    return "Rp. " + idrAmount.ToString("N0").Replace(",", ".");
                }

                // Jika gagal di-parse, kembalikan aslinya
                return match.Value;
            });
        }
    }
}