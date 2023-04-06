using GameApi;
using GameApi.DTOs;
using GameApi.Models;
using GameApi.Utils;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using System.Net.Http.Json;

namespace TestGameApi
{
    public class TestEnv
    {
        private static WebApplicationFactory<Program> _factory = new WebApplicationFactory<Program>();

        public static readonly Guid Guid1 = Guid.Parse("{3023F3E9-B0A4-410D-BC9F-E94F799F876F}");
        public static readonly Guid Guid2 = Guid.Parse("{CB783B7F-6D7D-49BA-A062-FCF51C2BD0A2}");
        public static readonly Guid Guid3 = Guid.Parse("{F231E25C-6234-4861-A498-4ED116A2EA44}");
        public static readonly Guid Guid4 = Guid.Parse("{D48A5996-8AC0-46AC-96EA-CEF86D07108B}");
        public static readonly Guid Guid5 = Guid.Parse("{D184E27B-DB14-4304-9FE6-ADCA1B4C2963}");
        public static readonly Guid Guid6 = Guid.Parse("{53A269E0-B983-4884-8323-3FF89AF71B7D}");
        public static readonly Guid Guid7 = Guid.Parse("{18EDFA39-80E5-4175-97AC-CDBA133E1CA0}");

        public static readonly DateTime Xmas2023_10h00am = new DateTime(2023, 12, 25, 10, 0, 0);
        public static readonly DateTime Xmas2023_10h10am = new DateTime(2023, 12, 25, 10, 10, 0);
        public static readonly DateTime Xmas2023_10h30am = new DateTime(2023, 12, 25, 10, 30, 0);

        public static Mock<IClock> NewStubClock(params DateTime[] init)
        {
            var stubClock = new Mock<IClock>();
            var q = new Queue<DateTime>(init);

            stubClock
                .Setup(c => c.UtcNow)
                .Returns(() => q.Count > 1 ? q.Dequeue() : q.First());
            return stubClock;
        }

        public static HttpClient NewHttpClient(Mock<IClock>? clock = null)
        {
            var stubGuid = Mock.Of<IGuidGenerator>();

            Mock.Get(stubGuid).SetupSequence(c => c.NewGuid())
                .Returns(Guid1).Returns(Guid2)
                .Returns(Guid3).Returns(Guid4)
                .Returns(Guid5).Returns(Guid6)
                .Returns(Guid7)
                .Returns(() => Guid.NewGuid());

            var http = _factory.WithWebHostBuilder(
                builder => builder.ConfigureTestServices(
                    services =>
                    {
                        services.Replace(new ServiceDescriptor(typeof(IClock), (clock ?? NewStubClock(Xmas2023_10h00am)).Object));
                        services.Replace(new ServiceDescriptor(typeof(IGuidGenerator), stubGuid));
                    }
                )
            ).CreateClient();
            return http;
        }

        public async static Task<HttpClient> NewHttpClientWithPlayers(string players, Mock<IClock>? clock=null)
        {
            var http = NewHttpClient(clock);

            foreach (var p in players.Split(","))
            {
                await http.PostAsJsonAsync("/players", new PlayerDto(null, p));
            }
            return http;
        }

        public async static Task<HttpClient> NewHttpClientWithPlayersAndGame(string game, string players, Mock<IClock>? clock = null)
        {
            var http = await NewHttpClientWithPlayers(players, clock);

            await http.PostAsJsonAsync("/games", new GameDto(Title: game, Creator: Guid1));
            return http;
        }

        public static async Task AliceLeaveG1(HttpClient http) =>
            await http.DeleteAsync(
                $"/games/{TestEnv.Guid5}/players/{TestEnv.Guid2}?privateId={TestEnv.Guid1}"
            );
        public static async Task BobJoins(HttpClient http, Guid gameId) =>
            await PlayerJoins(http, gameId, TestEnv.Guid3, TestEnv.Guid4);

        public static async Task PlayerJoins(HttpClient http, Guid gameId, Guid privateGuid, Guid publicGuid) =>
            await http.PostAsJsonAsync(
                $"/games/{gameId}/players",
                new Player()
                {
                    PrivateId = privateGuid,
                    PublicId = publicGuid
                }
            );
    }
}
