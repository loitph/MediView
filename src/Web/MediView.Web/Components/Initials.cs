namespace MediView.Web.Components;

public static class Initials
{
    public static string Of(string fullName)
    {
        var words = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            return "?";
        }

        var first = char.ToUpperInvariant(words[0][0]);
        if (words.Length == 1)
        {
            return first.ToString();
        }

        var last = char.ToUpperInvariant(words[^1][0]);
        return $"{first}{last}";
    }
}
