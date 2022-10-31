using System.Globalization;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EleicoesBot;

public interface IApuracoesService
{
    TseApiResult ObterResultados();
}

public class ApuracoesService : IApuracoesService
{
    private HttpClient m_client;
    private ILogger<ApuracoesService> m_logger;
    private DateTimeOffset m_nextTime;
    private TseApiResult m_value;

    public ApuracoesService(ILogger<ApuracoesService> logger)
    {
        m_logger = logger;
        m_client = new HttpClient();
        _ = Task.Run(WorkerTask);
    }

    public TseApiResult ObterResultados()
        => m_value;

    async Task WorkerTask()
    {
        while (true)
        {
            try
            {
                var json = await m_client.GetStringAsync("https://resultados.tse.jus.br/oficial/ele2022/545/dados-simplificados/br/br-c0001-e000545-r.json");
                m_value = JsonConvert.DeserializeObject<TseApiResult>(json);
                m_nextTime = DateTimeOffset.UtcNow.AddSeconds(5);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex);
                Console.ResetColor();
            }

            await Task.Delay(5000);
        }
    }
}

public class TseApiResult
{
    [JsonProperty("cand")]
    public IEnumerable<Candidato> Candidatos { get; set; }

    [JsonProperty]
    protected string pst { get; set; }

    public float PercentualSecoesComputadas => float.Parse(pst.Replace(',', '.'), CultureInfo.InvariantCulture);

    [JsonExtensionData]
    internal Dictionary<string, JToken> extra { get; set; }

    public static implicit operator bool(TseApiResult self)
        => self != null;
}

public class Candidato
{
    [JsonProperty]
    protected string n { get; set; }

    [JsonProperty]
    protected string nm { get; set; }

    [JsonProperty]
    protected string vap { get; set; }

    [JsonProperty]
    protected string pvap { get; set; }

    [JsonExtensionData]
    internal Dictionary<string, JToken> extra { get; set; }

    [JsonIgnore]
    public int Numero => int.Parse(n);

    [JsonIgnore]
    public string Nome => nm;

    [JsonIgnore]
    public long VotosApurados => long.Parse(vap);

    [JsonIgnore]
    public float PercentualVotosApurados => float.Parse(pvap.Replace(',', '.'), CultureInfo.InvariantCulture);
}
