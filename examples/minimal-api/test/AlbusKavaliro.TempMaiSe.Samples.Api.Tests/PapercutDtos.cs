namespace AlbusKavaliro.TempMaiSe.Samples.Api.Tests;

public record PapercutMailAddress(string Name, string Address);
public record PapercutMailHeader(string Name, string Value);
public record PapercutMessage(string Id, string? Subject, string? TextBody, string? HtmlBody,
                              List<PapercutMailAddress> From, List<PapercutMailAddress> To, List<PapercutMailAddress> Cc,
                              List<PapercutMailAddress> Bcc, List<PapercutMailAddress> Headers, List<PapercutMailSection> Sections);
public record PapercutMessageList(int TotalMessageCount, List<PapercutMessage> Messages);
public record PapercutMailSection(string? Id, string MediaType, string? FileName);
