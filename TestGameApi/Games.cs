using GameApi.DTOs;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Json;

namespace TestGameApi
{
    [TestClass]
    public class Games
    {
        [TestMethod]
        public async Task GetGameListEmptyOnStartup()
        {
            var http = TestEnv.NewHttpClient();

            var actual = await http.GetFromJsonAsync<IEnumerable<GameDto>>("/games");

            Assert.IsNotNull(actual);
            CollectionAssert.AreEqual(Array.Empty<GameDto>(), actual.ToArray());
        }
        [TestMethod]
        public async Task GameDeletedWhenLastPlayerIsTimedout()
        {
            var http = await TestEnv.NewHttpClientWithPlayersAndGame(
                "g1", "alice", 
                TestEnv.NewStubClock(
                    TestEnv.Xmas2023_10h00am, // create alice
                    TestEnv.Xmas2023_10h00am, // alice create g1
                    TestEnv.Xmas2023_10h30am
                )
            );

            var actual = await http.GetFromJsonAsync<IEnumerable<GameDto>>("/games");

            Assert.IsNotNull(actual);
            CollectionAssert.AreEqual(Array.Empty<GameDto>(), actual.ToArray());
        }
        [TestMethod]
        public async Task CreateGame()
        {
            var http = await TestEnv.NewHttpClientWithPlayers("alice,bob");

            var actual = await http.PostAsJsonAsync(
                "/games", 
                new GameDto(Title: "g1", Creator: TestEnv.Guid1)
            );

            Assert.AreEqual(StatusCodes.Status201Created, (int)actual.StatusCode);
            Assert.AreEqual($"/games/{TestEnv.Guid5}", actual.Headers.Location?.OriginalString);
            var game = await actual.Content.ReadFromJsonAsync<GameDto>();
            Assert.AreEqual("g1"         , game?.Title);
            Assert.AreEqual(TestEnv.Guid5, game?.Id);
            Assert.AreEqual(TestEnv.Guid2, game?.Creator);
        }
        [TestMethod]
        public async Task CreateTwoGames()
        {
            var http = await TestEnv.NewHttpClientWithPlayersAndGame("g1", "alice,bob");
            await http.PostAsJsonAsync("/games", new GameDto(Title: "g2", Creator: TestEnv.Guid3));

            var actual = await http.GetFromJsonAsync<IEnumerable<GameDto>>("/games");

            Assert.IsNotNull(actual);
            CollectionAssert.AreEqual(
                new[] { 
                    new GameDto(TestEnv.Guid5, "g1", TestEnv.Guid2),
                    new GameDto(TestEnv.Guid6, "g2", TestEnv.Guid4)
                },
                actual.ToArray()
            );
        }
        [TestMethod]
        public async Task CreateTwoGamesWithSamePlayerFails()
        {
            var http = await TestEnv.NewHttpClientWithPlayersAndGame(
                "g1", "alice,bob",
                TestEnv.NewStubClock(TestEnv.Xmas2023_10h00am, TestEnv.Xmas2023_10h30am)
            );
            var actual = await http.PostAsJsonAsync("/games", new GameDto(Title: "g2", Creator: TestEnv.Guid1));

            Assert.AreEqual(StatusCodes.Status409Conflict, (int)actual.StatusCode);
        }
        [TestMethod]
        public async Task CreateGameWithoutTitleFails()
        {
            var http = await TestEnv.NewHttpClientWithPlayers("alice,bob");

            var actual = await http.PostAsJsonAsync(
                "/games",
                new GameDto(Title: null, Creator: TestEnv.Guid1)
            );
            Assert.AreEqual(StatusCodes.Status400BadRequest, (int)actual.StatusCode);
        }
        [TestMethod]
        public async Task CreateGameWithoutIdFails()
        {
            var http = await TestEnv.NewHttpClientWithPlayers("alice,bob");

            var actual = await http.PostAsJsonAsync(
                "/games",
                new GameDto(Title: "g1", Creator: null)
            );
            Assert.AreEqual(StatusCodes.Status400BadRequest, (int)actual.StatusCode);
        }
        [TestMethod]
        public async Task CreateGameWithPublicIdFails()
        {
            var http = await TestEnv.NewHttpClientWithPlayers("alice,bob");

            var actual = await http.PostAsJsonAsync(
                "/games",
                new GameDto(Title: "g1", Creator: TestEnv.Guid2)
            );
            Assert.AreEqual(StatusCodes.Status404NotFound, (int)actual.StatusCode);
        }
        [TestMethod]
        public async Task CreateGameWithBadIdFails()
        {
            var http = await TestEnv.NewHttpClientWithPlayers("alice");

            var actual = await http.PostAsJsonAsync(
                "/games",
                new GameDto(Title: "g1", Creator: TestEnv.Guid3)
            );
            Assert.AreEqual(StatusCodes.Status404NotFound, (int)actual.StatusCode);
        }
        [TestMethod]
        public async Task GetGameDetails()
        {
            var http = await TestEnv.NewHttpClientWithPlayersAndGame("g1", "alice,bob");

            var actual = await http.GetFromJsonAsync<GameDto>($"/games/{TestEnv.Guid5}");

            Assert.IsNotNull(actual);
            Assert.AreEqual<GameDto>(new GameDto(TestEnv.Guid5, "g1", TestEnv.Guid2), actual);
        }
        [TestMethod]
        public async Task GetGameDetailsWithBadIdFails()
        {
            var http = await TestEnv.NewHttpClientWithPlayersAndGame("g1", "alice,bob");

            var actual = await http.GetAsync($"/games/{TestEnv.Guid6}");

            Assert.AreEqual(StatusCodes.Status404NotFound, (int)actual.StatusCode);
        }
        [TestMethod]
        public async Task DeleteGame()
        {
            var http = await TestEnv.NewHttpClientWithPlayersAndGame("g1", "alice,bob");

            var actual = await http.DeleteAsync($"/games/{TestEnv.Guid5}?creatorPrivateId={TestEnv.Guid1}");

            Assert.AreEqual(StatusCodes.Status204NoContent, (int)actual.StatusCode);
            CollectionAssert.AreEqual(
                Array.Empty<GameDto>(),
                (await http.GetFromJsonAsync<IEnumerable<GameDto>>("/games"))?.ToArray()
            );
        }
        [TestMethod]
        public async Task DeleteGameWithNoCreatorCredentialsFails()
        {
            var http = await TestEnv.NewHttpClientWithPlayersAndGame("g1", "alice,bob");

            var actual = await http.DeleteAsync($"/games/{TestEnv.Guid5}");

            Assert.AreEqual(StatusCodes.Status400BadRequest, (int)actual.StatusCode);
        }
        [TestMethod]
        public async Task DeleteGameWithBadCreatorCredentialsFails()
        {
            var http = await TestEnv.NewHttpClientWithPlayersAndGame("g1", "alice,bob");

            var actual = await http.DeleteAsync($"/games/{TestEnv.Guid5}?creatorPrivateId={TestEnv.Guid6}");

            Assert.AreEqual(StatusCodes.Status401Unauthorized, (int)actual.StatusCode);
        }
        [TestMethod]
        public async Task DeleteGameWithCreatorPublicIdFails()
        {
            var http = await TestEnv.NewHttpClientWithPlayersAndGame("g1", "alice,bob");

            var actual = await http.DeleteAsync($"/games/{TestEnv.Guid5}?creatorPrivateId={TestEnv.Guid2}");

            Assert.AreEqual(StatusCodes.Status401Unauthorized, (int)actual.StatusCode);
        }
        [TestMethod]
        public async Task DeleteUnexistingGameFails()
        {
            var http = await TestEnv.NewHttpClientWithPlayersAndGame("g1", "alice,bob");

            var actual = await http.DeleteAsync($"/games/{TestEnv.Guid6}?creatorPrivateId={TestEnv.Guid1}");

            Assert.AreEqual(StatusCodes.Status404NotFound, (int)actual.StatusCode);
        }
    }
}
