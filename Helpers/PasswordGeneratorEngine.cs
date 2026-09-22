using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace PasswordGenerator.Helpers
{
    /// <summary>
    /// Silnik generowania bezpiecznych haseł z użyciem kryptograficznie bezpiecznego RNG.
    /// </summary>
    public static class PasswordGeneratorEngine
    {
        public const string Lowercase = "abcdefghijklmnopqrstuvwxyz";
        public const string Uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        public const string Digits = "0123456789";
        // Dokładnie znaki specjalne wymagane przez użytkownika: !@#$%^&*
        public const string SpecialChars = "!@#$%^&*";

        private const string AmbiguousChars = "0OIl1";

        /// <summary>
        /// Generuje hasło o podanej długości z wybranymi zestawami znaków.
        /// </summary>
        public static string Generate(
            int length,
            bool includeUppercase,
            bool includeLowercase,
            bool includeDigits,
            bool includeSpecial,
            bool excludeAmbiguous = false)
        {
            if (length <= 0)
                throw new ArgumentException("Długość hasła musi być większa od zera.");

            string lower = includeLowercase ? FilterAmbiguous(Lowercase, excludeAmbiguous) : string.Empty;
            string upper = includeUppercase ? FilterAmbiguous(Uppercase, excludeAmbiguous) : string.Empty;
            string digits = includeDigits ? FilterAmbiguous(Digits, excludeAmbiguous) : string.Empty;
            string special = includeSpecial ? SpecialChars : string.Empty;

            var charPool = new StringBuilder();
            if (!string.IsNullOrEmpty(lower)) charPool.Append(lower);
            if (!string.IsNullOrEmpty(upper)) charPool.Append(upper);
            if (!string.IsNullOrEmpty(digits)) charPool.Append(digits);
            if (!string.IsNullOrEmpty(special)) charPool.Append(special);

            if (charPool.Length == 0)
                throw new ArgumentException("Zaznacz co najmniej jeden zestaw znaków.");

            string pool = charPool.ToString();
            char[] password = new char[length];

            // Gwarantujemy obecność co najmniej jednego znaku z każdej wybranej kategorii
            var guaranteedCategories = new List<string>();
            if (!string.IsNullOrEmpty(lower)) guaranteedCategories.Add(lower);
            if (!string.IsNullOrEmpty(upper)) guaranteedCategories.Add(upper);
            if (!string.IsNullOrEmpty(digits)) guaranteedCategories.Add(digits);
            if (!string.IsNullOrEmpty(special)) guaranteedCategories.Add(special);

            // Wypełniamy całe hasło losowo z pełnej puli
            for (int i = 0; i < length; i++)
            {
                password[i] = pool[RandomNumberGenerator.GetInt32(pool.Length)];
            }

            // Losowe pozycje dla gwarantowanych znaków (o ile długość na to pozwala)
            var chosenPositions = new HashSet<int>();
            for (int i = 0; i < guaranteedCategories.Count && i < length; i++)
            {
                int pos;
                do
                {
                    pos = RandomNumberGenerator.GetInt32(length);
                } while (!chosenPositions.Add(pos) && chosenPositions.Count < length);

                string categoryPool = guaranteedCategories[i];
                password[pos] = categoryPool[RandomNumberGenerator.GetInt32(categoryPool.Length)];
            }

            return new string(password);
        }

        private static string FilterAmbiguous(string source, bool exclude)
        {
            if (!exclude) return source;
            var sb = new StringBuilder();
            foreach (char c in source)
            {
                if (!AmbiguousChars.Contains(c))
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Oblicza entropię hasła w bitach: L * log2(PoolSize).
        /// </summary>
        public static double CalculateEntropy(string password)
        {
            if (string.IsNullOrEmpty(password)) return 0;

            int poolSize = 0;
            bool hasLower = false, hasUpper = false, hasDigit = false, hasSpecial = false;

            foreach (char c in password)
            {
                if (char.IsLower(c)) hasLower = true;
                else if (char.IsUpper(c)) hasUpper = true;
                else if (char.IsDigit(c)) hasDigit = true;
                else hasSpecial = true;
            }

            if (hasLower) poolSize += 26;
            if (hasUpper) poolSize += 26;
            if (hasDigit) poolSize += 10;
            if (hasSpecial) poolSize += 8; // !@#$%^&* to 8 znaków

            if (poolSize == 0) poolSize = 1;

            return Math.Round(password.Length * Math.Log2(poolSize), 1);
        }

        /// <summary>
        /// Oblicza siłę hasła w skali 0-100 na podstawie entropii i długości.
        /// </summary>
        public static int CalculateStrength(string password)
        {
            if (string.IsNullOrEmpty(password)) return 0;

            double entropy = CalculateEntropy(password);

            // Skalowanie entropii do wartości 0-100
            // < 28 bitów: bardzo słabe (0 - 25)
            // 28 - 45 bitów: słabe (25 - 50)
            // 45 - 65 bitów: średnie (50 - 75)
            // 65 - 85 bitów: silne (75 - 90)
            // >= 85 bitów: bardzo silne (90 - 100)
            int score = (int)Math.Min(100, Math.Max(5, (entropy / 90.0) * 100));

            // Jeśli hasło ma mniej niż 8 znaków, maksymalnie 35%
            if (password.Length < 8 && score > 35) score = 35;
            // Jeśli hasło ma mniej niż 6 znaków, maksymalnie 20%
            if (password.Length < 6 && score > 20) score = 20;

            return score;
        }

        /// <summary>
        /// Zwraca polski opis siły hasła.
        /// </summary>
        public static string GetStrengthLabel(int strength)
        {
            return strength switch
            {
                < 30 => "Bardzo słabe",
                < 55 => "Słabe",
                < 75 => "Umiarkowane",
                < 90 => "Silne",
                _ => "Bardzo silne"
            };
        }

        /// <summary>
        /// Szacunkowy czas złamania hasła metodą brute force przy 10 mld prób/s.
        /// </summary>
        public static string EstimateCrackTime(double entropy)
        {
            if (entropy <= 0) return "Natychmiast";

            double totalCombinations = Math.Pow(2, entropy);
            double guessesPerSecond = 10_000_000_000.0; // 10 miliardów prób na sekundę
            double seconds = (totalCombinations / 2.0) / guessesPerSecond;

            if (seconds < 1) return "Mniej niż sekunda";
            if (seconds < 60) return $"{Math.Max(1, (int)seconds)} sekund";
            if (seconds < 3600) return $"{Math.Max(1, (int)(seconds / 60))} minut";
            if (seconds < 86400) return $"{Math.Max(1, (int)(seconds / 3600))} godzin";
            if (seconds < 31536000) return $"{Math.Max(1, (int)(seconds / 86400))} dni";
            if (seconds < 315360000) return $"{Math.Max(1, (int)(seconds / 31536000))} lat";
            if (seconds < 31536000000000L) return $"{Math.Max(1, (int)(seconds / 3153600000L))} tysięcy lat";
            return "Miliony lat";
        }
    }
}
