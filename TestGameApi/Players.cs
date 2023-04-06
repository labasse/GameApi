using GameApi.DTOs;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Json;
using GameApi.Models;

namespace TestGameApi
{
    [TestClass]
    public class Players 
    {
        [TestMethod]
        public async Task GetPlayerEmptyOnStartup()
        {
            var http = TestEnv.NewHttpClient();

            var actual = await http.GetFromJsonAsync<IEnumerable<PlayerDto>>("/players");

            Assert.IsNotNull(actual);
            CollectionAssert.AreEqual(Array.Empty<PlayerDto>(), actual.ToArray());
        }

        [TestMethod]
        public async Task CreatePlayer()
        {
            var http = TestEnv.NewHttpClient();

            var actual = await http.PostAsJsonAsync<PlayerDto>("/players", new PlayerDto(null, "alice"));
            
            Assert.AreEqual(StatusCodes.Status201Created, (int)actual.StatusCode);
            var player = await actual.Content.ReadFromJsonAsync<Player>();
            Assert.AreEqual("alice", player?.Pseudo);
            Assert.AreEqual(TestEnv.Guid1  , player?.PrivateId);
            Assert.AreEqual(TestEnv.Guid2  , player?.PublicId );
        }

        [TestMethod]
        public async Task CreatePlayerWithoutPseudoFails()
        {
            var http = TestEnv.NewHttpClient();

            var actual = await http.PostAsJsonAsync<PlayerDto>("/players", new PlayerDto(null, null));

            Assert.AreEqual(StatusCodes.Status400BadRequest, (int)actual.StatusCode);

        }
        [TestMethod]
        public async Task CreateTwoPlayers()
        {
            var http = await TestEnv.NewHttpClientWithPlayers("alice,bob");

            var actual = await http.GetFromJsonAsync<IEnumerable<PlayerDto>>("/players");

            Assert.IsNotNull(actual);
            CollectionAssert.AreEqual(new [] { 
                new PlayerDto(TestEnv.Guid2, "alice"),
                new PlayerDto(TestEnv.Guid4, "bob")
            }, actual.ToArray());
        }
        [TestMethod]
        public async Task GetPlayerDetail()
        {
            var http = await TestEnv.NewHttpClientWithPlayers("alice,bob");

            var actual = await http.GetFromJsonAsync<PlayerDto>($"/players/{TestEnv.Guid2}");

            Assert.IsNotNull(actual);
            Assert.AreEqual(new PlayerDto(TestEnv.Guid2, "alice"), actual);
        }
        [TestMethod]
        public async Task GetPlayerDetailWithPrivateIdFails()
        {
            var http = await TestEnv.NewHttpClientWithPlayers("alice,bob");

            var actual = await http.GetAsync($"/players/{TestEnv.Guid1}");

            Assert.AreEqual(StatusCodes.Status404NotFound, (int)actual.StatusCode);
        }
        [TestMethod]
        public async Task GetPlayerDetailWithBadIdFails()
        {
            var http = await TestEnv.NewHttpClientWithPlayers("alice");

            var actual = await http.GetAsync($"/players/{TestEnv.Guid4}");

            Assert.AreEqual(StatusCodes.Status404NotFound, (int)actual.StatusCode);
        }
        [TestMethod]
        public async Task DeletePlayer()
        {
            var http = await TestEnv.NewHttpClientWithPlayers("alice,bob");

            var actual = await http.DeleteAsync($"/players/{TestEnv.Guid2}?privateId={TestEnv.Guid1}");

            Assert.AreEqual(StatusCodes.Status204NoContent, (int)actual.StatusCode);
        }
        [TestMethod]
        public async Task DeletePlayerChangesList()
        {
            var http = await TestEnv.NewHttpClientWithPlayers("alice,bob");
            await http.DeleteAsync($"/players/{TestEnv.Guid2}?privateId={TestEnv.Guid1}");

            var actual = await http.GetFromJsonAsync<IEnumerable<PlayerDto>>("/players");

            Assert.IsNotNull(actual);
            CollectionAssert.AreEqual(new[] {
                new PlayerDto(TestEnv.Guid4, "bob")
            }, actual.ToArray());
        }
        [TestMethod]
        public async Task DeletePlayerWithBadPrivateKeyFails()
        {
            var http = await TestEnv.NewHttpClientWithPlayers("alice,bob");

            var actual = await http.DeleteAsync($"/players/{TestEnv.Guid2}?privateId={TestEnv.Guid3}");

            Assert.AreEqual(StatusCodes.Status401Unauthorized, (int)actual.StatusCode);
        }
        [TestMethod]
        public async Task DeletePlayerWithPublicKeyFails()
        {
            var http = await TestEnv.NewHttpClientWithPlayers("alice,bob");

            var actual = await http.DeleteAsync($"/players/{TestEnv.Guid2}?privateId={TestEnv.Guid2}");

            Assert.AreEqual(StatusCodes.Status401Unauthorized, (int)actual.StatusCode);
        }
        [TestMethod]
        public async Task DeletePlayerNotFound()
        {
            var http = await TestEnv.NewHttpClientWithPlayers("alice");

            var actual = await http.DeleteAsync($"/players/{TestEnv.Guid4}?privateId={TestEnv.Guid3}");

            Assert.AreEqual(StatusCodes.Status404NotFound, (int)actual.StatusCode);
        }
        [TestMethod]
        public async Task PlayerAliveAfterCreation()
        {
            var http = await TestEnv.NewHttpClientWithPlayers(
                "alice",
                TestEnv.NewStubClock(
                    TestEnv.Xmas2023_10h00am,
                    TestEnv.Xmas2023_10h10am
                )
            );

            var actual = await http.GetFromJsonAsync<IEnumerable<PlayerDto>>("/players");

            Assert.IsNotNull(actual);
            CollectionAssert.AreEqual(new[] {
                new PlayerDto(TestEnv.Guid2, "alice")
            }, actual.ToArray());
        }
        [TestMethod]
        public async Task PlayerDeletedAfterTimeout()
        {
            var http = await TestEnv.NewHttpClientWithPlayers(
                "alice",
                TestEnv.NewStubClock(
                    TestEnv.Xmas2023_10h00am,
                    TestEnv.Xmas2023_10h30am
                )
            );
            
            var actual = await http.GetFromJsonAsync<IEnumerable<PlayerDto>>("/players");

            Assert.IsNotNull(actual);
            CollectionAssert.AreEqual(Array.Empty<PlayerDto>(), actual.ToArray());
        }
    }
}