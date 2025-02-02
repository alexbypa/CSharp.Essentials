using CSharpEssentials.HttpHelper;
using CSharpEssentials.SerializerHelper;
using System.Text.Json;

namespace CSharpEssentials.HangFireHelper;
public class HangFireHttpJobRequestHandler: IHangFireJobRequestHandler<HangFireHttpJobRequest> {
    private readonly HttpClient _httpClient;
    public HangFireHttpJobRequestHandler(HttpClient httpClient) {
        _httpClient = httpClient;
    }
    public async Task<bool> ExecuteAsync(HangFireHttpJobRequest request, Func<HttpResponseMessage, bool> conditionForRetry)  {
        //TODO: configurare Hangfire PER visualizzarlo in dashboard tramite Credenziali !
        List<Func<HttpRequestMessage, HttpResponseMessage, Task>> actionsHttp = new List<Func<HttpRequestMessage, HttpResponseMessage, Task>>();
        Func<HttpRequestMessage, HttpResponseMessage, Task> traceRetry = (httpreq, httpres) => {
            JsonElement jsonRoot = httpres.Content.ReadAsStringAsync().GetAwaiter().GetResult().Deserialize<JsonElement>((json) =>
                //TODO: 
                Console.WriteLine("Il testo {json} non è un json valido ", json)
                //loggerExtension.TraceAsync(request, Serilog.Events.LogEventLevel.Error, null, "Il testo {jsonContent} non è un json valido ", jsonContent);
            );
            HangFireRetryOperations RequestToSend = new HangFireRetryOperations {
                ContentType = "Application/Json",
                CreatedDate = DateTime.Now,
                GameSessionID = request.GameSessionID,
                OperationName = request.ActionCommand,
                Payload = httpreq.Content.ReadAsStringAsync().GetAwaiter().GetResult(),
                MustRetry = conditionForRetry(httpres) != true,
                Status = jsonRoot.GetString("Status.ErrDesc", "Error"),
                Url = httpreq.RequestUri.AbsoluteUri.ToString(),
                BodyResponse = httpres.Content.ReadAsStringAsync().GetAwaiter().GetResult(),
            };
            //TODO:
            //loggerExtension.TraceAsync(irequest, Serilog.Events.LogEventLevel.Information, null, "INFO RETRY: {Url}, {Payload}, {AddOnQueueRetry}, {httpStatus} {BodyResponse}",
            //    RequestToSend.Url, RequestToSend.Payload, RequestToSend.MustRetry, RequestToSend.Status, RequestToSend.BodyResponse);
            return Task.CompletedTask;
        };
        actionsHttp.Add(traceRetry);

        httpsClientHelper httpsClientHelper = new httpsClientHelper(actionsHttp);
        httpsClientHelper.JsonData = request.Body;
        Task<HttpResponseMessage> responseMessage = httpsClientHelper
            .addTimeout(TimeSpan.FromSeconds(15))//TODO: Impostazioni 
            .sendAsync(request.Url);

        var responseHttp = responseMessage.GetAwaiter().GetResult();
        string jsonContent = responseHttp.Content.ReadAsStringAsync().GetAwaiter().GetResult();

        return conditionForRetry(responseHttp);
    }
}