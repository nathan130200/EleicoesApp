using DSharpPlus.Entities;
using Serilog;

namespace EleicoesBot;

public static class Util
{
    public static async Task NoExcept(this Task t)
    {
        try
        {
            await t;
        }
        catch (Exception ex)
        {
            Log.Error(ex.ToString());
        }
    }

    public static async Task ExpiresOn(this Task<DiscordMessage> task, double time, string reason = default)
    {
        var timeout = TimeSpan.FromSeconds(time);

        {
            var msg = await task;

            _ = Task.Run(async () =>
            {
                await Task.Delay(timeout);
                await msg.DeleteAsync();
            });
        }
    }

    public static async Task<TResult> NoExcept<TResult>(this Task<TResult> t)
    {
        try
        {
            return await t;
        }
        catch (Exception ex)
        {
            Log.Error(ex.ToString());
        }

        return default;
    }
}