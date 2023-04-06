using GameApi;
using GameApi.DTOs;
using GameApi.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net.Http.Json;
using Moq;
using GameApi.Models;
using Newtonsoft.Json;
using System.Text;
using static System.Net.WebRequestMethods;

namespace TestGameApi
{
    [TestClass]
    public class Players 
    {
        private static WebApplicationFactory<Program> _factory = new WebApplicationFactory<Program>();

        private static readonly Guid Guid1 = Guid.Parse("{3023F3E9-B0A4-410D-BC9F-E94F799F876F}");
        private static readonly Guid Guid2 = Guid.Parse("{CB783B7F-6D7D-49BA-A062-FCF51C2BD0A2}");
        private static readonly Guid Guid3 = Guid.Parse("{F231E25C-6234-4861-A498-4ED116A2EA44}");
        private static readonly Guid Guid4 = Guid.Parse("{D48A5996-8AC0-46AC-96EA-CEF86D07108B}");

        private static readonly DateTime Xmas2023_10h00am = new DateTime(2023, 12, 25, 10,  0, 0);
        private static readonly DateTime Xmas2023_10h10am = new DateTime(2023, 12, 25, 10, 10, 0);
        private static readonly DateTime Xmas2023_10h30am = new DateTime(2023, 12, 25, 10, 30, 0);

        private Mock<IClock> NewStubClock(params DateTime[] init)
        {
            var stubClock = new Mock<IClock>();
            var q = new Queue<DateTime>(init);
           
            stubClock
                .Setup(c => c.UtcNow)
                .Returns(() => q.Count > 1 ? q.Dequeue() : q.First());
            return stubClock;
        }

        private HttpClient NewHttpClient(Mock<IClock>? clock=null)
        {
            var stubGuid = Mock.Of<IGuidGenerator>();

            Mock.Get(stubGuid).SetupSequence(c => c.NewGuid())
                .Returns(Guid1).Returns(Guid2)
                .Returns(Guid3).Returns(Guid4)
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

        private async Task<HttpClient> NewHttpClientWithPlayers(params string[] players)
        {
            var http = NewHttpClient();

            foreach (var p in players)
            {
                await http.PostAsJsonAsync<PlayerDto>("/players", new PlayerDto(null, p));
            }
            return http;
        }

        [TestMethod]
        public async Task EmptyPlayerListOnStartup()
        {
            var http = NewHttpClient();

            var actual = await http.GetFromJsonAsync<IEnumerable<PlayerDto>>("/players");

            Assert.IsNotNull(actual);
            CollectionAssert.AreEqual(Array.Empty<PlayerDto>(), actual.ToArray());
        }

        [TestMethod]
        public async Task CreatePlayer()
        {
            var http = NewHttpClient();

            var actual = await http.PostAsJsonAsync<PlayerDto>("/players", new PlayerDto(null, "alice"));
            
            Assert.AreEqual(StatusCodes.Status201Created, (int)actual.StatusCode);
            var player = await actual.Content.ReadFromJsonAsync<Player>();
            Assert.AreEqual("alice", player?.Pseudo);
            Assert.AreEqual(Guid1  , player?.PrivateId);
            Assert.AreEqual(Guid2  , player?.PublicId );
        }

        [TestMethod]
        public async Task CreatePlayerWithoutPseudoFails()
        {
            var http = NewHttpClient();

            var actual = await http.PostAsJsonAsync<PlayerDto>("/players", new PlayerDto(null, null));

            Assert.AreEqual(StatusCodes.Status400BadRequest, (int)actual.StatusCode);

        }
        [TestMethod]
        public async Task CreateTwoPlayers()
        {
            var http = await NewHttpClientWithPlayers("alice", "bob");

            var actual = await http.GetFromJsonAsync<IEnumerable<PlayerDto>>("/players");

            Assert.IsNotNull(actual);
            CollectionAssert.AreEqual(new [] { 
                new PlayerDto(Guid2, "alice"),
                new PlayerDto(Guid4, "bob")
            }, actual.ToArray());
        }
        [TestMethod]
        public async Task DeletePlayer()
        {
            var http = await NewHttpClientWithPlayers("alice", "bob");

            var actual = await http.DeleteAsync($"/players/{Guid2}?privateId={Guid1}");

            Assert.AreEqual(StatusCodes.Status204NoContent, (int)actual.StatusCode);
        }
        [TestMethod]
        public async Task DeletePlayerChangesList()
        {
            var http = await NewHttpClientWithPlayers("alice", "bob");
            await http.DeleteAsync($"/players/{Guid2}?privateId={Guid1}");

            var actual = await http.GetFromJsonAsync<IEnumerable<PlayerDto>>("/players");

            Assert.IsNotNull(actual);
            CollectionAssert.AreEqual(new[] {
                new PlayerDto(Guid4, "bob")
            }, actual.ToArray());
        }
        [TestMethod]
        public async Task DeletePlayerWithBadPrivateKeyFails()
        {
            var http = await NewHttpClientWithPlayers("alice", "bob");

            var actual = await http.DeleteAsync($"/players/{Guid2}?privateId={Guid3}");

            Assert.AreEqual(StatusCodes.Status400BadRequest, (int)actual.StatusCode);
        }
        [TestMethod]
        public async Task DeletePlayerWithPublicKeyFails()
        {
            var http = await NewHttpClientWithPlayers("alice", "bob");

            var actual = await http.DeleteAsync($"/players/{Guid2}?privateId={Guid2}");

            Assert.AreEqual(StatusCodes.Status400BadRequest, (int)actual.StatusCode);
        }
        [TestMethod]
        public async Task DeletePlayerNotFound()
        {
            var http = await NewHttpClientWithPlayers("alice");

            var actual = await http.DeleteAsync($"/players/{Guid4}?privateId={Guid3}");

            Assert.AreEqual(StatusCodes.Status404NotFound, (int)actual.StatusCode);
        }
        [TestMethod]
        public async Task PlayerAliveAfterCreation()
        {
            var http = NewHttpClient(
                NewStubClock(
                    Xmas2023_10h00am,
                    Xmas2023_10h10am
                )
            );
            await http.PostAsJsonAsync<PlayerDto>("/players", new PlayerDto(null, "alice"));

            var actual = await http.GetFromJsonAsync<IEnumerable<PlayerDto>>("/players");

            Assert.IsNotNull(actual);
            CollectionAssert.AreEqual(new[] {
                new PlayerDto(Guid2, "alice")
            }, actual.ToArray());
        }
        [TestMethod]
        public async Task PlayerDeletedAfterTimeout()
        {
            var http = NewHttpClient(
                NewStubClock(
                    Xmas2023_10h00am, 
                    Xmas2023_10h30am
                )
            );
            await http.PostAsJsonAsync<PlayerDto>("/players", new PlayerDto(null, "alice"));
            
            var actual = await http.GetFromJsonAsync<IEnumerable<PlayerDto>>("/players");

            Assert.IsNotNull(actual);
            CollectionAssert.AreEqual(Array.Empty<PlayerDto>(), actual.ToArray());
        }
    }
}