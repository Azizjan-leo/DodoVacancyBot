namespace Webhook.Controllers.Data.Entities;

public class User
{
    public long Id { get; set; }

    public Lang Lang { get; set; }
}

public enum Lang
{
    RUS = 0,
    KYR = 1,
}
