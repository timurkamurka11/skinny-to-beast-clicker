using System;

namespace SkinnyToBeast.Utils
{
    public static class NumberFormatter
    {
        private static readonly string[] Suffixes = { "", "K", "M", "B", "T", "Qa", "Qi" };

        public static string Format(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return "0";
            }

            double absoluteValue = Math.Abs(value);
            int suffixIndex = 0;

            while (absoluteValue >= 1000 && suffixIndex < Suffixes.Length - 1)
            {
                absoluteValue /= 1000;
                value /= 1000;
                suffixIndex++;
            }

            if (suffixIndex == 0)
            {
                return Math.Floor(value).ToString("0");
            }

            return value.ToString("0.##") + Suffixes[suffixIndex];
        }
    }
}
