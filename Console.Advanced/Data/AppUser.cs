using System.ComponentModel.DataAnnotations;

namespace Console.Advanced.Data;
public class AppUser
{
    /// <summary>
    /// Telegram User's ID
    /// </summary>
    public long Id { get; set; }

    public string Language { get; set; } = default!;
    public string? FullName { get; set; } = default!;
    public DateOnly? DateOfBirth { get; set; } = default!;
    public string? PhoneNumber { get; set; } = default!;
    public bool? HasAuto {  get; set; } = false;

    public City? City { get; set; }
}
