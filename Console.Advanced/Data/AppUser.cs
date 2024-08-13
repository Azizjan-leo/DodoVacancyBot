using System.ComponentModel.DataAnnotations;

namespace Console.Advanced.Data;
public class AppUser
{
    /// <summary>
    /// Telegram User's ID
    /// </summary>
    public long Id { get; set; }

    [Length(2, 2)]
    public string Lang { get; set; } = default!;

    public City? City { get; set; }
}
