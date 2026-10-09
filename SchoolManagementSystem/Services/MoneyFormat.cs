namespace SchoolManagementSystem.Services;

/// <summary>Rupee formatting for Pakistani receipts and challans.</summary>
public static class MoneyFormat
{
    private static readonly string[] Ones =
    [
        "", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten",
        "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen"
    ];

    private static readonly string[] Tens =
        ["", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety"];

    /// <summary>"Rs. 12,500" for whole rupees, "Rs. 12,500.50" when paisa are present.</summary>
    public static string Pkr(decimal amount)
        => "Rs. " + (amount % 1 == 0 ? amount.ToString("N0") : amount.ToString("N2"));

    /// <summary>Amount in words using the lakh/crore grouping Pakistanis use, e.g. "Rupees Twelve Thousand Five Hundred Only".</summary>
    public static string ToWords(decimal amount)
    {
        var rupees = (long)Math.Floor(Math.Abs(amount));
        var paisa = (int)Math.Round((Math.Abs(amount) - rupees) * 100, MidpointRounding.AwayFromZero);

        var text = $"Rupees {IntToWords(rupees)}";
        if (paisa > 0) text += $" and {IntToWords(paisa)} Paisa";
        return text + " Only";
    }

    private static string IntToWords(long n)
    {
        if (n == 0) return "Zero";

        var parts = new List<string>();
        var crore = n / 10_000_000; n %= 10_000_000;
        var lakh = n / 100_000; n %= 100_000;
        var thousand = n / 1_000; n %= 1_000;

        if (crore > 0) parts.Add(IntToWords(crore) + " Crore");
        if (lakh > 0) parts.Add(Below100Or1000(lakh) + " Lakh");
        if (thousand > 0) parts.Add(Below100Or1000(thousand) + " Thousand");
        if (n > 0) parts.Add(Below100Or1000(n));
        return string.Join(" ", parts);
    }

    /// <summary>Words for 1..999.</summary>
    private static string Below100Or1000(long x)
    {
        var s = "";
        if (x >= 100)
        {
            s = Ones[x / 100] + " Hundred";
            x %= 100;
            if (x > 0) s += " ";
        }
        if (x >= 20)
        {
            s += Tens[x / 10];
            if (x % 10 > 0) s += " " + Ones[x % 10];
        }
        else if (x > 0)
        {
            s += Ones[x];
        }
        return s;
    }
}
