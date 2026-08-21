using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace DispatchPal.EndToEndTests;

public sealed class DispatchRequestFlowTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _client;

    public DispatchRequestFlowTests()
    {
        _client = new HttpClient
        {
            BaseAddress = new Uri(
                Environment.GetEnvironmentVariable("DISPATCHPAL_API_URL")
                ?? "http://localhost:5247")
        };
    }

    public async Task InitializeAsync()
    {
        var loginResponse = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                email = "admin@dispatchpal.local",
                password = "DispatchPal123!"
            });

        loginResponse.EnsureSuccessStatusCode();

        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(login);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", login.AccessToken);
    }
    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task CreateDispatchRequest_EventuallyBecomesCompleted()
    {
        var uniqueEmail =
            $"e2e-{Guid.NewGuid():N}@dispatchpal.local";

        var customerResponse = await _client.PostAsJsonAsync(
            "/api/customers",
            new
            {
                Name = "End-to-end Test",
                Email = uniqueEmail,
                Phone = "+381600000000"
            });

        Assert.Equal(
            HttpStatusCode.Created,
            customerResponse.StatusCode);

        var customer = await customerResponse.Content
        .ReadFromJsonAsync<CustomerResponse>(JsonOptions);

        Assert.NotNull(customer);
        Assert.NotEqual(Guid.Empty, customer.Id);

        var requestResponse = await _client.PostAsJsonAsync(
       "/api/dispatch-requests",

        new
        {
            CustomerId = customer.Id,
            PickupAddress = "Test pickup",
            DeliveryAddress = "Test delivery",
            PackageDescription = "Test package"
        });

        Assert.Equal(
            HttpStatusCode.Created,
            requestResponse.StatusCode);

        var createdRequest = await requestResponse.Content
            .ReadFromJsonAsync<DispatchRequestResponse>(JsonOptions);

        Assert.NotNull(createdRequest);
        Assert.Equal("Pending", createdRequest.Status);

        DispatchRequestResponse? completedRequest = null;

        for (var attempt = 0; attempt < 20; attempt++)
        {
            await Task.Delay(TimeSpan.FromSeconds(1));

            completedRequest = await _client.GetFromJsonAsync<
                DispatchRequestResponse>(
                $"/api/dispatch-requests/{createdRequest.Id}",
                JsonOptions);

            if (completedRequest?.Status == "Completed")
            {
                break;
            }
        }

        Assert.NotNull(completedRequest);
        Assert.Equal("Completed", completedRequest.Status);

        Assert.Contains(
            completedRequest.StatusHistory,
            history => history.Status == "Pending");

        Assert.Contains(
            completedRequest.StatusHistory,
            history => history.Status == "Completed");
    }
    [Fact]
    public async Task CreateCustomer_WithInvalidEmail_ReturnsBadRequest()
    {
        var request = new
        {
            name = "Marko Markovic",
            email = "ovo-nije-email",
            phone = "0601234567"
        };

        var response = await _client.PostAsJsonAsync(
            "/api/customers",
            request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();

        Assert.Contains("Email", content);

    }

    [Fact]
    public async Task CreateDispatchRequest_WithEmptyCustomerId_ReturnsBadRequest()
    {
        var request = new
        {
            customerId = Guid.Empty,
            pickupAddress = "Beograd",
            deliveryAddress = "Novi Sad",
            packageDescription = "Laptop"
        };

        var response = await _client.PostAsJsonAsync(
            "/api/dispatch-requests",
            request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();

        Assert.Contains(
            "CustomerId must not be empty.",
            content);
    }

    [Fact]
    public async Task GetCustomers_WithSearchAndPagination_ReturnsCorrectPages()
    {
        var prefix = $"Pagination-{Guid.NewGuid():N}";

        for(var index = 1; index <= 3; index++)
        {
            var createResponse = await _client.PostAsJsonAsync(
                "/api/customers",
                new
                {
                    name = $"{prefix}-{index}",
                    email = $"{prefix}-{index}@test.com",
                    phone = "123456"
                });

            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        }

        var encodedSearch = Uri.EscapeDataString(prefix);

        var firstPage =
            await _client.GetFromJsonAsync<PagedResponse<CustomerResponse>>(
                $"/api/customers?search={encodedSearch}&page=1&pageSize=2");

        Assert.NotNull(firstPage);
        Assert.Equal(1, firstPage.Page);
        Assert.Equal(2, firstPage.PageSize);
        Assert.Equal(3, firstPage.TotalCount);
        Assert.Equal(2, firstPage.TotalPages);
        Assert.Equal(2, firstPage.Items.Count);

        var secondPage =
            await _client.GetFromJsonAsync<PagedResponse<CustomerResponse>>(
                $"/api/customers?search={encodedSearch}&page=2&pageSize=2");

        Assert.NotNull(secondPage);
        Assert.Equal(2, secondPage.Page);
        Assert.Equal(2, secondPage.PageSize);
        Assert.Equal(3, secondPage.TotalCount);
        Assert.Equal(2, secondPage.TotalPages);
        Assert.Single(secondPage.Items);
    }

    [Fact]
    public async Task UpdateDispatchRequest_WhenCompleted_ReturnsConflict()
    {
        var uniqueValue = Guid.NewGuid().ToString("N");

        var customerResponse = await _client.PostAsJsonAsync(
            "/api/customers",
            new
            {
                name = "Update Test Customer",
                email = $"update-{uniqueValue}@dispatchpal.local",
                phone = "+381600000000"
            });

        Assert.Equal(
            HttpStatusCode.Created,
            customerResponse.StatusCode);

        var customer =
            await customerResponse.Content
                .ReadFromJsonAsync<CustomerResponse>();

        Assert.NotNull(customer);

        var createResponse = await _client.PostAsJsonAsync(
            "/api/dispatch-requests",
            new
            {
                customerId = customer.Id,
                pickupAddress = "Original pickup",
                deliveryAddress = "Original delivery",
                packageDescription = "Original package"
            });

        Assert.Equal(
            HttpStatusCode.Created,
            createResponse.StatusCode);

        var createdRequest =
            await createResponse.Content
                .ReadFromJsonAsync<DispatchRequestResponse>();

        Assert.NotNull(createdRequest);

        DispatchRequestResponse? completedRequest = null;

        for (var attempt = 0; attempt < 30; attempt++)
        {
            await Task.Delay(
                TimeSpan.FromMilliseconds(500));

            completedRequest =
                await _client.GetFromJsonAsync<
                    DispatchRequestResponse>(
                    $"/api/dispatch-requests/{createdRequest.Id}");

            if (completedRequest?.Status == "Completed")
            {
                break;
            }
        }

        Assert.NotNull(completedRequest);

        Assert.Equal(
            "Completed",
            completedRequest.Status);

        var updateResponse = await _client.PutAsJsonAsync(
            $"/api/dispatch-requests/{createdRequest.Id}",
            new
            {
                pickupAddress = "Forbidden pickup",
                deliveryAddress = "Forbidden delivery",
                packageDescription = "Forbidden package"
            });

        Assert.Equal(
            HttpStatusCode.Conflict,
            updateResponse.StatusCode);

        var unchangedRequest =
            await _client.GetFromJsonAsync<
                DispatchRequestResponse>(
                $"/api/dispatch-requests/{createdRequest.Id}");

        Assert.NotNull(unchangedRequest);

        Assert.Equal(
            "Original pickup",
            unchangedRequest.PickupAddress);

        Assert.Equal(
            "Original delivery",
            unchangedRequest.DeliveryAddress);

        Assert.Equal(
            "Original package",
            unchangedRequest.PackageDescription);
    }

    [Fact]
    public async Task GetCustomers_WithoutToken_ReturnsUnauthorized()
    {
        using var unauthenticatedClient = new HttpClient
        {
            BaseAddress =
                new Uri("http://localhost:5247")
        };

        var response = await unauthenticatedClient.GetAsync(
            "/api/customers");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }
    private sealed record CustomerResponse(
       Guid Id,
       string Name,
       string Email,
       string? Phone,
       DateTimeOffset CreatedAtUtc);

    private sealed record DispatchRequestResponse(
        Guid Id,
        Guid CustomerId,
        string CustomerName,
        string PickupAddress,
        string DeliveryAddress,
        string PackageDescription,
        string Status,
        DateTimeOffset CreatedAtUtc,
        IReadOnlyList<StatusHistoryResponse> StatusHistory);

    private sealed record StatusHistoryResponse(
        Guid Id,
        string Status,
        string Description,
        DateTimeOffset ChangedAtUtc);

    private sealed record PagedResponse<T>(
        IReadOnlyList<T> Items,
        int Page,
        int PageSize,
        int TotalCount,
        int TotalPages);

    private sealed record LoginResponse(
    string AccessToken,
    DateTimeOffset ExpiresAtUtc,
    string Email);
}
