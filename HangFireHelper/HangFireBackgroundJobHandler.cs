using CSharpEssentials.SerializerHelper;
using Hangfire;
using Hangfire.Console;
using Hangfire.Server;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;

namespace CSharpEssentials.HangFireHelper;
public class BackgroundJobHandler : HangFireDashboardTracert {
    private readonly IBackgroundJobClient _jobClient;
    private readonly IServiceProvider _serviceProvider;

    public BackgroundJobHandler(IBackgroundJobClient jobClient, IServiceProvider serviceProvider) {
        _jobClient = jobClient;
        _serviceProvider = serviceProvider;
    }
    private void _RaiseMessage(byte levelLog, PerformContext performcontext, WriteTextOnDashboard e) {
        performcontext.WriteLine(e.Text);
    }
    public void EnqueueWithRetry<TRequest>(TRequest request, string Action, string IdTransaction, TimeSpan retryInterval, int maxRetries, int retryCount, HttpStatusCode httpStatusCode, string jsonPathToValidate, int jsonValuetoValidate/*, Func<IRequest, string> WriteOnDashBoardWhenIsSuccess, Func<IRequest, int, double, string, string> WriteOnDashBoardWhenIsFailure*/) {
        if (retryCount >= maxRetries) {
            Console.WriteLine("Retry limite raggiunto.");
            return;
        }
        var jobId = _jobClient.Enqueue(() => ExecuteWithRetry(request, Action, IdTransaction, retryInterval, maxRetries, retryCount, httpStatusCode, jsonPathToValidate, jsonValuetoValidate, /*WriteOnDashBoardWhenIsSuccess, WriteOnDashBoardWhenIsFailure, */null));
    }
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    /// <param name="request"></param>
    /// <param name="irequest"></param>
    /// <param name="retryInterval"></param>
    /// <param name="maxRetries"></param>
    /// <param name="retryCount"></param>
    /// <param name="httpStatusCode"></param>
    /// <param name="jsonPathToValidate"></param>
    /// <param name="jsonValuetoValidate"></param>
    /// <param name="performcontext"></param>
    /// <returns></returns>
    [DisableConcurrentExecution(timeoutInSeconds: 30)] //FUTURE: Da parametrizzare
    public async Task ExecuteWithRetry<TRequest>(TRequest request, string Action, string IdTransaction, TimeSpan retryInterval, int maxRetries, int retryCount, HttpStatusCode httpStatusCode, string jsonPathToValidate, int jsonValuetoValidate/*, Func<IRequest, string> WriteOnDashBoardWhenIsSuccess, Func<IRequest, int, double, string, string> WriteOnDashBoardWhenIsFailure*/, PerformContext performcontext) {
        OnWriteText += _RaiseMessage;
        using var scope = _serviceProvider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IHangFireJobRequestHandler<TRequest>>();

        RaiseMessage(1, $"Inizio chiamata HTTP", performcontext);
        //RaiseMessage(1, $"Chiamata in corso verso {.RequestMessage.RequestUri}", performcontext);
        bool isSuccess = await handler.ExecuteAsync(request, (res) => {
            var statusCode = res.StatusCode;
            string jsonContent = res.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            RaiseMessage(1, $"Risposta HTTP ottenuta : {jsonContent}", performcontext);
            JsonElement jsonRoot = jsonContent.Deserialize<JsonElement>((json) => Console.WriteLine("Risposta non conforme : ", json));
            int Validate = jsonRoot.GetInt(jsonPathToValidate, 500);
            if (statusCode == httpStatusCode && Validate == jsonValuetoValidate) {
                //TODO:
                //RaiseMessage(1, WriteOnDashBoardWhenIsSuccess(irequest), performcontext);
                RaiseMessage(1, $"Condizione soddisfatta, job completato per comando {Action} con chiave {IdTransaction}, Risposta ottenuta con successo.", performcontext);
                return true;
            } else {
                //TODO:
                //RaiseMessage(statusCode != HttpStatusCode.OK ? (byte)3 : (byte)2, WriteOnDashBoardWhenIsFailure(irequest, retryCount + 1, retryInterval.TotalSeconds, jsonContent), performcontext);
                RaiseMessage(statusCode != HttpStatusCode.OK ? (byte)3 : (byte)2, $"Retry numero {retryCount + 1} fallito per comando {Action} con chiave {IdTransaction}. Ritento tra {retryInterval.TotalSeconds} seconds. Risposta ottenuta {jsonContent}", performcontext);
                return false;  //FUTURE: dobbiamo aggiungere un sistema di mail ing per notificare quando dei job vanno in retry più di n volte
            }
        });
        //TODO: non vengono inseriti i tracciati sulla tabella e non si vedono i messaggi su hangfire
        if (isSuccess) {
            //TODO:
            //loggerExtension.TraceAsync(irequest, Serilog.Events.LogEventLevel.Information, null, $"Messaggio {irequest.ActionLog} scodato correttamente , GameSessionId : {irequest.GameSessionID}");
        } else {
            //TODO:
            //loggerExtension.TraceAsync(irequest, Serilog.Events.LogEventLevel.Warning, null, $"Retry numero {retryCount + 1} fallito. Ritento tra {retryInterval.TotalSeconds} seconds. Messaggio {irequest.ActionLog}, GameSessionId {irequest.GameSessionID} ");
            _jobClient.Schedule(
                () => ExecuteWithRetry(request, Action, IdTransaction, retryInterval, maxRetries, retryCount + 1, httpStatusCode, jsonPathToValidate, jsonValuetoValidate/*, WriteOnDashBoardWhenIsSuccess, WriteOnDashBoardWhenIsFailure*/, null),
                retryInterval
            );
        }
    }
}