namespace MediView.Web.Components;

public static class Initials
{
    public static string Of(string fullName)
    {
        var letters = fullName
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(word => word.FirstOrDefault(char.IsLetter))
            .Where(char.IsLetter)
            .Select(char.ToUpperInvariant)
            .ToArray();

        return letters.Length switch
        {
            0 => "?",
            1 => letters[0].ToString(),
            _ => new string([letters[0], letters[^1]]),
        };
    }
}
