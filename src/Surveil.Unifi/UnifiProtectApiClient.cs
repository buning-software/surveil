using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Surveil.Application.Ports;
using Surveil.Domain.Cameras;

namespace Surveil.Unifi;

public sealed class UnifiProtectApiClient : ICameraProvider, IDisposable
{
    private const int BodyPreviewLength = 500;
    private const int ErrorBodyPreviewLength = 300;

    private static readonly string[] RequiredQualities = ["high", "low"];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;
    private readonly string _reachableHost;
    private readonly bool _ownsHttpClient;

    public UnifiProtectApiClient(IOptions<UnifiProtectOptions> options)
    {
        var opts = options.Value;
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };

        _http = new HttpClient(handler)
        {
            BaseAddress = new Uri($"{opts.BaseUrl.TrimEnd('/')}/{UnifiProtectOptions.ApiPath}/")
        };

        _http.DefaultRequestHeaders.Add("Accept", "application/json");
        _http.DefaultRequestHeaders.Add("X-API-KEY", opts.ApiKey);

        _reachableHost = new Uri(opts.BaseUrl).Host;
        _ownsHttpClient = true;
    }

    internal UnifiProtectApiClient(HttpClient httpClient, string? reachableHost = null)
    {
        _http = httpClient;
        _reachableHost = reachableHost ?? httpClient.BaseAddress?.Host ?? string.Empty;
    }

    public void Dispose()
    {
        if (_ownsHttpClient)
            _http.Dispose();
    }

    public async Task<IReadOnlyList<Camera>> GetCamerasAsync(CancellationToken ct = default)
    {
        var dtos = await GetJsonAsync<List<CameraDto>>("v1/cameras", ct) ?? [];
        return dtos.ConvertAll(d => new Camera(d.Id, d.Name, d.State == "CONNECTED"));
    }

    public async Task<IReadOnlyList<RtspsStream>> GetRtspsStreamsAsync(string cameraId, CancellationToken ct = default)
    {
        var dto = await GetJsonAsync<RtspsStreamDto>(RtspsStreamPath(cameraId), ct);
        return ToStreams(dto);
    }

    public async Task<IReadOnlyList<RtspsStream>> CreateRtspsStreamsAsync(string cameraId, CancellationToken ct = default)
    {
        var existing = await GetJsonAsync<RtspsStreamDto>(RtspsStreamPath(cameraId), ct);
        var qualities = (existing?.Qualities() ?? []).Union(RequiredQualities).ToArray();

        var request = JsonContent.Create(new CreateRtspsStreamRequest(qualities), options: JsonOptions);
        var response = await _http.PostAsync(RtspsStreamPath(cameraId), request, ct);
        await EnsureSuccessAsync(response, ct);
        var dto = await DeserializeAsync<RtspsStreamDto>(response, ct);

        var streams = ToStreams(dto);
        return streams.Count > 0
            ? streams
            : throw new InvalidOperationException("No usable RTSPS URL in CreateRtspsStream response.");
    }

    private static string RtspsStreamPath(string cameraId) => $"v1/cameras/{cameraId}/rtsps-stream";

    private List<RtspsStream> ToStreams(RtspsStreamDto? dto) =>
        dto?.Streams().Select(s => new RtspsStream(NormalizeForLibVlc(s.Url), s.Quality)).ToList() ?? [];

    private string NormalizeForLibVlc(string url)
    {
        var rewritten = url
            .Replace("rtsps://", "rtsp://")
            .Replace(":7441/", ":7447/")
            .Replace("?enableSrtp", "")
            .TrimEnd('?');

        var streamUri = new Uri(rewritten);
        if (string.IsNullOrEmpty(_reachableHost) ||
            string.Equals(streamUri.Host, _reachableHost, StringComparison.OrdinalIgnoreCase))
            return rewritten;

        return new UriBuilder(streamUri) { Host = _reachableHost }.Uri.ToString();
    }

    private async Task<T?> GetJsonAsync<T>(string requestUri, CancellationToken ct)
    {
        var response = await _http.GetAsync(requestUri, ct);
        await EnsureSuccessAsync(response, ct);
        return await DeserializeAsync<T>(response, ct);
    }

    private static async Task<T?> DeserializeAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        var body = await response.Content.ReadAsStringAsync(ct);
        var preview = Truncate(body, BodyPreviewLength);
        Debug.WriteLine($"[UnifiProtectApiClient] {response.RequestMessage?.RequestUri} → {preview}");
        try
        {
            return JsonSerializer.Deserialize<T>(body, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Failed to deserialize response from {response.RequestMessage?.RequestUri} as {typeof(T).Name}. " +
                $"Body: {preview}", ex);
        }
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return;

        var body = string.Empty;
        try { body = await response.Content.ReadAsStringAsync(ct); }
        catch
        {
            // ignored
        }

        body = Truncate(body, ErrorBodyPreviewLength);

        Debug.WriteLine($"[UnifiProtectApiClient] HTTP {(int)response.StatusCode} {response.ReasonPhrase}: {body}");
        throw new HttpRequestException(
            $"HTTP {(int)response.StatusCode} {response.ReasonPhrase} — {body}",
            inner: null,
            statusCode: response.StatusCode);
    }

    internal static string Truncate(string text, int maxLength) =>
        text.Length <= maxLength ? text : text[..maxLength] + "…";

    private sealed record CameraDto(string Id, string Name, string State);

    private sealed record CreateRtspsStreamRequest(string[] Qualities);

    private sealed record RtspsStreamDto(
        string? High,
        string? Medium,
        string? Low,
        string? Package)
    {
        public IEnumerable<string> Qualities() => All().Select(s => s.Quality);

        public IReadOnlyList<(string Url, string Quality)> Streams()
        {
            var main = All().Where(s => s.Quality != "package").ToList();
            return main.Count > 0 ? main : All().ToList();
        }

        private IEnumerable<(string Url, string Quality)> All()
        {
            if (High is not null) yield return (High, "high");
            if (Medium is not null) yield return (Medium, "medium");
            if (Low is not null) yield return (Low, "low");
            if (Package is not null) yield return (Package, "package");
        }
    }
}
