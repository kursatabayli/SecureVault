using FluentAssertions;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using SecureVault.Shared.Result;
using SecureVault.Vault.Application.Features.CQRS.VaultItems.Commands;
using SecureVault.Vault.Application.Features.CQRS.VaultItems.Results;
using SecureVault.Vault.Domain.Entities;
using SecureVault.Vault.Domain.Enums;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;

namespace SecureVault.Vault.Api.Tests.Controllers
{
    public class VaultItemControllerTests : IClassFixture<VaultApiFactory>
    {
        private readonly HttpClient _client;
        private readonly VaultApiFactory _factory;

        public VaultItemControllerTests(VaultApiFactory factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        private string GenerateTestToken(Guid userId)
        {
            Claim[] claims = 
                [
                new(ClaimTypes.NameIdentifier, userId.ToString()), 
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                ];

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("SuperSecretKeyForTestingPurposes123!"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: "TestIssuer",
                audience: "TestAudience",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [Fact]
        public async Task GetUserVaultItems_Should_ReturnUnauthorized_WhenNoTokenIsProvided()
        {
            var response = await _client.GetAsync("/api/VaultItem/vault-items/type/Password");
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetUserVaultItems_Should_ReturnOkAndItems_WhenTokenIsValid()
        {
            var userId = Guid.NewGuid();
            var token = GenerateTestToken(userId);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var collection = _factory.Database.GetCollection<VaultItem>("vaultItems");
            await collection.InsertOneAsync(VaultItem.Create(userId, ItemType.Password, []));

            var response = await _client.GetAsync($"/api/VaultItem/vault-items/type/{ItemType.Password}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var items = await response.Content.ReadFromJsonAsync<List<VaultItemResult>>();
            items.Should().NotBeNull();
            items.Should().HaveCount(1);
        }

        [Fact]
        public async Task Create_Should_ReturnOk_WhenRequestIsValid()
        {
            var userId = Guid.NewGuid();
            var token = GenerateTestToken(userId);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var command = new CreateVaultItemCommand(Guid.Empty, ItemType.Password, [1, 2, 3]);

            var response = await _client.PostAsJsonAsync("/api/VaultItem", command);

            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var itemsInDb = await _factory.Database.GetCollection<VaultItem>("vaultItems")
                                                    .Find(i => i.UserId == userId)
                                                    .ToListAsync();
            itemsInDb.Should().HaveCount(1);
            itemsInDb.First().EncryptedData.Should().BeEquivalentTo(new byte[] { 1, 2, 3 });
        }

        [Fact]
        public async Task Delete_Should_ReturnBadRequest_WhenUserIsNotOwner()
        {
            var ownerId = Guid.NewGuid();
            var attackerId = Guid.NewGuid();
            var token = GenerateTestToken(attackerId);
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var collection = _factory.Database.GetCollection<VaultItem>("vaultItems");

            var itemToDelete = VaultItem.Create(ownerId, ItemType.Password, []);
            await collection.InsertOneAsync(itemToDelete);

            var response = await _client.DeleteAsync($"/api/VaultItem/{itemToDelete.Id}");

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var error = await response.Content.ReadFromJsonAsync<Error>();
            error!.Code.Should().Be(ErrorCodes.UnauthorizedAccess);
        }
    }

}
