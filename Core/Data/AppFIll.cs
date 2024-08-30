using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Data;
public class AppFIll
{
    /// <summary>
    /// Telegram user Id
    /// </summary>
    public long Id { get; set; }

    public string Stage { get; set; } = string.Empty;

    public string? Value { get; set; } 
}
