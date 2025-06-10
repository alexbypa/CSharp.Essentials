using LoggerHelperDemo.Entities;
using LoggerHelperDemo.Models;
using LoggerHelperDemo.Repositories;

namespace LoggerHelperDemo.Services;
public interface IUserService {
    Task<IEnumerable<User>> SyncUsersAsync(int page);
}

public class UserService : IUserService {
    private readonly HttpClient _http;
    private readonly IUserRepository _repo;

    public UserService(HttpClient http, IUserRepository repo) {
        _http = http;
        _repo = repo;
    }

    public async Task<IEnumerable<User>> SyncUsersAsync(int page) {
        var resp = await _http.GetFromJsonAsync<ReqResResponse>($"{_http.BaseAddress}/users?page={page}");
        if (resp?.Data == null)
            return Enumerable.Empty<User>();

        var saved = new List<User>();
        foreach (var dto in resp.Data) {
            var user = new User {
                ExternalId = dto.Id,
                Email = dto.Email,
                FirstName = dto.First_name,
                LastName = dto.Last_name,
                Avatar = dto.Avatar
            };
            await _repo.AddAsync(user);
            saved.Add(user);
        }
        await _repo.SaveChangesAsync();
        return saved;
    }
}

