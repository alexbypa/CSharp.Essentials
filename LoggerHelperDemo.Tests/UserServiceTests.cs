using LoggerHelperDemo.Entities;
using LoggerHelperDemo.Persistence;
using LoggerHelperDemo.Repositories;
using LoggerHelperDemo.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace LoggerHelperDemo.Tests;

using LoggerHelperDemo.Persistence;
using LoggerHelperDemo.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.Protected;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public class UserServiceTests {
    ///[Fact] 
    public async Task SyncUsersAsync_RestituisceIEnumerableDiUser_QuandoApiRitornaDati() {
        // --- Arrange

        // 1) Preparo un HttpMessageHandler finto che intercetta SendAsync
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        var fakeJson = @"{ 
          ""data"": [
            { ""id"": 1, ""email"": ""a@x.com"", ""first_name"": ""Alice"", ""last_name"": ""A"", ""avatar"": ""url"" }
          ]
        }";

        handlerMock
          .Protected()
          .Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
          )
          .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) {
              Content = new StringContent(fakeJson, Encoding.UTF8, "application/json")
          })
          .Verifiable();

        // 2) Costruisco l’HttpClient con il handler finto
        var httpClient = new HttpClient(handlerMock.Object) {
            BaseAddress = new Uri("https://api.test")
        };

        // 3) Preparo un mock di IUserRepository (non tocca DB)
        var repoMock = new Mock<IUserRepository>();
        repoMock.Setup(r => r.AddAsync(It.IsAny<User>()))
                .Returns(Task.CompletedTask);
        repoMock.Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

        // 4) Istanzio il servizio che voglio testare
        var service = new UserService(httpClient, repoMock.Object);

        // --- Act

        var result = await service.SyncUsersAsync(page: 1);

        // --- Assert

        // A) Verifico che il risultato sia un IEnumerable<User>
        var users = Assert.IsAssignableFrom<IEnumerable<User>>(result);

        // B) Verifico che abbia esattamente un elemento
        Assert.Single(users);

        // C) Controllo un campo del singolo user
        var first = users.First();
        Assert.Equal(1, first.ExternalId);
        Assert.Equal("Alice", first.FirstName);

        // Verifica che SendAsync sia stato invocato almeno una volta
        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Get &&
                req.RequestUri == new Uri("https://api.test/users?page=1")
            ),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    //[Fact]
    public async Task SyncUsersAsync_ChiamaEndpointReale_E_RitornaAlmenoUnUser() {
        // 1) Costruisco un HttpClient “reale”
        var httpClient = new HttpClient {
            BaseAddress = new Uri("https://reqres.in/api")
        };
        httpClient.DefaultRequestHeaders.Add("x-api-key", "reqres-free-v1");



        // 2) Per il repository puoi usare un mock minimale o un DB in-memory
        var repoMock = new Mock<IUserRepository>();
        repoMock.Setup(r => r.AddAsync(It.IsAny<User>()))
                .Returns(Task.CompletedTask);
        repoMock.Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

        // 3) Istanzio il servizio con dipendenze “vere”
        var service = new UserService(httpClient, repoMock.Object);

        // 4) Act: chiamo davvero l’API remota
        var users = (await service.SyncUsersAsync(page: 1)).ToList();

        // 5) Assert: almeno un elemento restituito
        Assert.NotEmpty(users);
        Assert.All(users, u => Assert.False(string.IsNullOrEmpty(u.Email)));

        // (Opzionale) Verifico che AddAsync sia stato invocato correttamente
        repoMock.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Exactly(users.Count));
        repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    //[Fact]
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task SyncUsersAsync_SalvaSuPostgresDiTest(int page) {
        // 1) Definisci la connessione al tuo DB PostgreSQL di test

        // 2) Crea il service con il DB di test

        var factory = TestServiceFactory.CreateUserServiceWithPostgres();

        // 3) Esegui la sincronizzazione (la HttpClient interna può essere mockata se vuoi)
        UserService userService = new UserService(factory.httpclient, factory.userRepository);
        var users = (await userService.SyncUsersAsync(page)).ToList();

        // 4) Asserisci sul risultato
        Assert.NotEmpty(users);
        Assert.All(users, u => Assert.True(u.ExternalId > 0));

        var saved = await factory.userRepository.getUserSavedOnLastMinutes(-2);

        Assert.Equal(users.Count, saved.Count);
    }
    public static class TestServiceFactory {
        /// <summary>
        /// Crea un UserService con:
        ///  - un DbContext EF Core con provider Npgsql (PostgreSQL)
        ///  - il repository concreto UserRepository
        ///  - un HttpClient reale (o mockabile) per chiamate esterne
        /// </summary>
        public static (HttpClient httpclient, UserRepository userRepository) CreateUserServiceWithPostgres() {
            string connectionString = "Host=51.178.131.166:1433;Username=postgres;Password=PixPstG!!;Database=HubGamePragmaticCasino;Search Path=dbo,public;ConnectionLifetime=30;";
            // 1) Costruisci le opzioni per il DbContext su PostgreSQL
            var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString, npgsql => {
                // se hai bisogno di specificare la versione di PostgreSQL
                //npgsql.SetPostgresVersion(new Version(12, 3));
            })
            .Options;


            // 3) Istanzia il DbContext “vero” e il repository concreto
            var dbContext = new AppDbContext(options);
            var userRepo = new UserRepository(dbContext);

            // 4) Prepara un HttpClient “reale” (o sostituiscilo in test con un mock)
            var httpClient = new HttpClient {
                BaseAddress = new Uri("https://reqres.in/api")
            };
            httpClient.DefaultRequestHeaders.Add("x-api-key", "reqres-free-v1");

            // 5) Ritorna il service pronto all’uso
            return (httpClient, userRepo);
        }
    }
}