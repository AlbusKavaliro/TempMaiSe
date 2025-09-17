using FluentEmail.Core;

namespace AlbusKavaliro.TempMaiSe.Mailer;

public interface IMailInformationToMailMapper
{
    IFluentEmail Map(MailInformation mailInformation, IFluentEmail email);
}
