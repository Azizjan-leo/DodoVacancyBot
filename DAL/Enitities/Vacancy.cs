namespace DAL.Enitities;

public sealed class Vacancy
{
    public int Id { get; set; }
}

public sealed class LangVacancy
{
    public int Id { get; set; }
    public Vacancy Vacancy { get; set; } = default!;
    public Lang Lang { get; set; }
    public string Text { get; set; } = default!;
}
