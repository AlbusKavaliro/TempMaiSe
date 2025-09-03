using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

using AlbusKavaliro.TempMaiSe.Models;

namespace AlbusKavaliro.TempMaiSe.Samples.Api;

public class TemplateContext(DbContextOptions<TemplateContext> options) : IdentityDbContext(options)
{
    public virtual DbSet<Template> Templates { get; set; }

    public virtual DbSet<Partial> Partials { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ApplyConfiguration(new TemplateConfiguration());
        builder.ApplyConfiguration(new PartialConfiguration());
        base.OnModelCreating(builder);
    }
}
