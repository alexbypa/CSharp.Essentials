namespace CSharpEssentials.HangFireHelper;
public interface IHangFireJobRequestHandler<TRequest> {
    Task<bool> ExecuteAsync(TRequest request, Func<HttpResponseMessage, bool> conditionForRetry);
}