using Microsoft.EntityFrameworkCore;
using DAL.Enitities;
using DAL;

namespace Webhook.Controllers.Data;

public sealed class Repository(ApplicationDbContext _context)
{
    public async Task<List<LangVacancy>> GetVacancies(Lang lang)
    {
        return await _context.LangVacancies.Where(x => x.Lang == lang).ToListAsync();
    }
    public async Task<User?> GetUser(long userId)
    {
        return await _context.Users.FindAsync(userId);
    }
    public async Task<Lang> SetLang(long userId, string lang)
    {
        var user = await _context.Users.FindAsync(userId);
        
        _ = Enum.TryParse(lang, out Lang newLang);
        
        if (user is null)
        {
            user = new User()
            {
                Id = userId,
                Lang = newLang
            };

            _context.Users.Add(user);
        }
        else
          user.Lang = newLang;

        await _context.SaveChangesAsync();

        return user.Lang;
    }
}
