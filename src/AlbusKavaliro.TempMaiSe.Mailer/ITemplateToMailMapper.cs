using FluentEmail.Core;
using AlbusKavaliro.TempMaiSe.Models;

namespace AlbusKavaliro.TempMaiSe.Mailer;

public interface ITemplateToMailMapper
{
    IFluentEmail Map(TemplateData configuredTemplate, IFluentEmail email);
}
