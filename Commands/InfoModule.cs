using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace EleicoesBot.Commands;

public class InfoModule : BaseCommandModule
{
    private IApuracoesService svc;

    public InfoModule(IApuracoesService service)
        => svc = service;

    [Command, Description("Procura dados específicos de um deteminado candidato pelo seu número.")]
    public async Task Candidato(CommandContext ctx,
        [Description("Nº válido do candidato.")]
        int numero)
    {
        await ctx.TriggerTypingAsync();

        var result = svc.ObterResultados();

        if (!result)
        {
            await ctx.RespondAsync($"{ctx.User.Mention} :x: Dados ainda não estão disponíveis.").ExpiresOn(5.0);
            return;
        }

        var candidato = result.Candidatos.FirstOrDefault(x => x.Numero == numero);

        if (candidato == null)
        {
            await ctx.RespondAsync($"{ctx.User.Mention} :x: Candidato não encontrado.").ExpiresOn(5.0);
            return;
        }

        var extra = candidato.extra;

        var embed = new DiscordEmbedBuilder()
            .WithAuthor("ELEIÇÕES - 2022",
                url: "https://resultados.tse.jus.br/oficial/app/index.html#/eleicao/resultados",
                iconUrl: "https://resultados.tse.jus.br/oficial/app/assets/icon/favicon.png");

        var situacao = extra["st"]?.ToString()?.ToLowerInvariant();

        embed.WithThumbnail($"https://resultados.tse.jus.br/oficial/ele2022/545/fotos/br/{extra["sqcand"]}.jpeg")
            .WithDescription(candidato.Nome)
            .AddField("VICE-PRESIDENTE", extra["nv"].ToString())
            .AddField("SITUAÇÃO", extra["st"].ToString())
            .AddField("COLIGAÇÃO / FEDERAÇÃO", extra["cc"].ToString())
            .WithColor(situacao switch
            {
                "eleito" => DiscordColor.White,
                "não eleito" => DiscordColor.Brown,
                "anulado" => DiscordColor.Gray,
                _ => default
            });

        await ctx.Channel.SendMessageAsync(x => x.Embed = embed);
    }

    [Command, Cooldown(1, 10d, CooldownBucketType.User), Description("Resumo dos candidados das eleições para presidente.")]
    public async Task Resultados(CommandContext ctx)
    {
        await ctx.TriggerTypingAsync();

        var result = svc.ObterResultados();

        if (!result)
        {
            await ctx.RespondAsync($"{ctx.User.Mention} :x: Dados ainda não estão disponíveis.").ExpiresOn(5.0);
            return;
        }

        var embed = new DiscordEmbedBuilder();

        var candidatos = result.Candidatos
            .OrderByDescending(x => x.VotosApurados);

        var destaque = candidatos.FirstOrDefault();

        if (result.PercentualSecoesComputadas >= 100f)
        {
            embed.WithColor(DiscordColor.Blurple)
                .AddField(":crown: Novo Presidente da República",
                destaque.Nome + " com " + destaque.VotosApurados + " votos apurados. (**`" + destaque.PercentualVotosApurados + "%`** dos votos)");
        }
        else
        {
            embed.WithColor(DiscordColor.Blurple)
                .WithDescription(":office_worker: Veja em tempo real a apuração dos votos para __Presidente__ no 2º Turno.\n\n- Iniciou <t:1667160000:R>\n- Encerrou <t:1667178000:R>");

            foreach (var c in candidatos)
            {
                embed.AddField(":white_small_square: " + c.Nome,
                    "**" + c.PercentualVotosApurados + "%** dos votos.", false);
            }

            embed.AddField(":scales: TOTAL DE SEÇÕES COMPUTADAS",
                result.PercentualSecoesComputadas.ToString("F2") + '%', true)

                .AddField(":dart: MATEMÁTICAMENTE DEFINIDO", "Eleito", false);
        }

        embed.WithTimestamp(new DateTime(2022, 10, 30, 17, 0, 0))
            .WithFooter("Eleições 2022 - 2º Turno", "https://resultados.tse.jus.br/oficial/app/assets/icon/favicon.png");

        var msg = await ctx.RespondAsync(x => x.Embed = embed);
        await Task.Delay(TimeSpan.FromSeconds(10));
        await msg.DeleteAsync().NoExcept();
    }
}
