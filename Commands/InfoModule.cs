using System.Collections.Concurrent;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace EleicoesBot.Commands;

public class InfoModule : BaseCommandModule
{
    private IApuracoesService m_service;

    public InfoModule(IApuracoesService service)
        => m_service = service;

    static readonly ConcurrentDictionary<ulong, CancellationTokenSource> tokens = new();

    [Command]
    public async Task Resultados(CommandContext ctx)
    {
        await ctx.TriggerTypingAsync();

        var (success, result) = await m_service.UpdateAsync();

        if (!success)
        {
            await ctx.RespondAsync($"{ctx.User.Mention} :x: O sistema do bot está iniciando!");
            return;
        }

        var msg = await ctx.Channel.SendMessageAsync(x => x.Content = "\u200b");
        await ctx.Message.DeleteAsync();

        if (tokens.TryRemove(ctx.Guild.Id, out var cts) && !cts.IsCancellationRequested)
            cts.Cancel();

        cts = tokens[ctx.Guild.Id] = new CancellationTokenSource();

        _ = Task.Run(async () => await LateUpdate(msg, cts, ctx.Channel));
    }

    async Task LateUpdate(DiscordMessage msg, CancellationTokenSource cts, DiscordChannel chn)
    {
        var firstTick = false;

        while (!cts.IsCancellationRequested)
        {
            var (success, result) = await m_service.UpdateAsync();

            if (success)
            {
                var embed = new DiscordEmbedBuilder();

                var candidatos = result.Candidatos
                    .OrderByDescending(x => x.VotosApurados);

                var destaque = candidatos.FirstOrDefault();

                if (result.PercentualSecoesComputadas >= 100f && firstTick)
                {
                    embed.WithColor(DiscordColor.Blurple)
                        .AddField(":crown: Novo Presidente da República",
                        destaque.Nome + " com " + destaque.VotosApurados + " votos apurados. (**`" + destaque.PercentualVotosApurados + "%`** dos votos)");
                }
                else
                {
                    firstTick = true;

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

                try
                {
                    bool isEleito = result.PercentualSecoesComputadas >= 100f;

                    if (isEleito)
                    {
                        await Task.Delay(1500);

                        await chn.SendMessageAsync(x =>
                        {
                            x.Content = isEleito ? "@everyone" : null;
                            x.Embed = embed;
                        });

                        break;
                    }
                    else
                    {
                        await msg.ModifyAsync(x => x.Embed = embed);
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(ex);
                    Console.ResetColor();
                }
            }

            await Task.Delay(3000);
        }

        if (tokens.TryRemove(chn.Guild.Id, out var ct))
            ct.Cancel();
    }

    [Command]
    public async Task Limpar(CommandContext ctx, int limit = 32)
    {
        var messages = new List<DiscordMessage>();
        var retry = 5;
        var count = 0;
        var lastCount = 0;

    fetch:
        if (retry == 0)
            goto leave;

        foreach (var msg in await ctx.Channel.GetMessagesBeforeAsync(ctx.Message.Id, 100))
        {
            messages.Add(msg);
            lastCount++;
            count++;
        }

        if (count < limit)
        {
            if (lastCount <= 0)
            {
                lastCount = 0;
                retry--;
            }

            goto fetch;
        }

    leave:

        int failed = 0;

        foreach (var chunk in messages.Chunk(100))
        {
            try
            {
                await ctx.Channel.DeleteMessagesAsync(chunk);
                await Task.Delay(1500);
            }
            catch
            {
                failed++;
            }
        }

        var temp = await ctx.RespondAsync($"Limpou {Math.Abs(count - failed)}/{count} mensagens coletadas.");
        await Task.Delay(3000);
        await temp.DeleteAsync();
    }
}

public static class Extensions
{
    public static string KiloFormat(this long number)
    {
        if (number >= 100000000)
            return (number / 1000000).ToString("#,0M");

        if (number >= 10000000)
            return (number / 1000000).ToString("0.#") + "M";

        if (number >= 100000)
            return (number / 1000).ToString("#,0K");

        if (number >= 10000)
            return (number / 1000).ToString("0.#") + "K";

        return number.ToString("#,0");
    }
}