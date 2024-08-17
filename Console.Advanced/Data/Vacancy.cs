namespace Console.Advanced.Data;
public class Vacancy
{
    public int Id { get; set; }
    public Position Position { get; set; } = default!;
    public string Language { get; set; } = default!;
    public string Text { get; set; } = default!;
}
