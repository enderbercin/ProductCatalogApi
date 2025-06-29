namespace ProductCatalogApi.Utils
{
    public static class RomanNumeralConverter
    {
        private static readonly Dictionary<int, string> RomanNumerals = new()
        {
            { 1000, "M" },
            { 900, "CM" },
            { 500, "D" },
            { 400, "CD" },
            { 100, "C" },
            { 90, "XC" },
            { 50, "L" },
            { 40, "XL" },
            { 10, "X" },
            { 9, "IX" },
            { 5, "V" },
            { 4, "IV" },
            { 1, "I" }
        };

        public static string ToRoman(int number)
        {
            if (number <= 0)
            {
                return "N/A";
            }

            var result = "";
            var remaining = number;

            foreach (var numeral in RomanNumerals.OrderByDescending(x => x.Key))
            {
                while (remaining >= numeral.Key)
                {
                    result += numeral.Value;
                    remaining -= numeral.Key;
                }
            }

            return result;
        }

        //public static int FromRoman(string roman)
        //{
        //    if (string.IsNullOrEmpty(roman))
        //    {
        //        return 0;
        //    }

        //    var result = 0;
        //    var previousValue = 0;

        //    for (int i = roman.Length - 1; i >= 0; i--)
        //    {
        //        var currentValue = GetRomanValue(roman[i]);
                
        //        if (currentValue >= previousValue)
        //        {
        //            result += currentValue;
        //        }
        //        else
        //        {
        //            result -= currentValue;
        //        }
                
        //        previousValue = currentValue;
        //    }

        //    return result;
        //}

        //private static int GetRomanValue(char romanChar)
        //{
        //    return romanChar switch
        //    {
        //        'I' => 1,
        //        'V' => 5,
        //        'X' => 10,
        //        'L' => 50,
        //        'C' => 100,
        //        'D' => 500,
        //        'M' => 1000,
        //        _ => 0
        //    };
        //}
    }
} 