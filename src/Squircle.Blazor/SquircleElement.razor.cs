using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Collections.Concurrent;

namespace Squircle.Blazor;

/// <summary>
/// Squircle shape component like iOS corner radius.
/// </summary>
public partial class SquircleElement : ComponentBase, IAsyncDisposable {
    private const float _defaultSmoothness = 0.0586f / 0.332f;
    private ElementReference _divRef;
    private float _width = default;
    private float _height = default;
    private bool _isDisposed = false;
    private IAsyncDisposable? _subscription;
    static readonly ConcurrentDictionary<string, string> _cache = [];

    /// <summary>
    /// Gets or sets the CSS class for the SquircleElement component.
    /// </summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>
    /// Gets or sets the inline style for the SquircleElement component.
    /// </summary>
    [Parameter]
    public string? Style { get; set; }

    /// <summary>
    /// Gets or sets the radius of the SquircleElement.
    /// </summary>
    [Parameter]
    public float? Radius { get; set; }

    /// <summary>
    /// Gets or sets the smoothness of the SquircleElement.
    /// Recommended to set range 0 - 0.4.
    /// The default value is the ratio of iOS default.
    /// </summary>
    [Parameter]
    public float? Smoothness { get; set; } = _defaultSmoothness;

    /// <summary>
    /// Gets or sets the child content of the SquircleElement.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Gets or sets additional attributes for the SquircleElement.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>
    /// Gets the injected IJSRuntime instance.
    /// </summary>
    [Inject]
    public required IJSRuntime JSRuntime { get; set; }

    /// <summary>
    /// Gets the count of cached styles.
    /// </summary>
    public int CacheCount => _cache.Count;

    /// <summary>
    /// Clears the cache of generated styles.
    /// </summary>
    public void ClearCache() {
        _cache.Clear();
    }

    /// <summary>
    /// Disposes the SquircleElement component and clears the cache.
    /// </summary>
    public async ValueTask DisposeAsync() {
        _isDisposed = true;
        _cache.Clear();
        await (_subscription?.DisposeAsync() ?? ValueTask.CompletedTask);
        _subscription = null;
    }

    /// <summary>
    /// Called after each render of the component.
    /// Subscribes to the ResizeObserver if it's the first render and the component is not disposed.
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool firstRender) {
        if (firstRender && _isDisposed is false) {
            _subscription = await ResizeObserver.ObserveAsync(JSRuntime, _divRef, OnResized);
        }
    }

    /// <summary>
    /// Called when the size of the SquircleElement changes.
    /// Updates the width and height values and triggers a re-render of the component.
    /// </summary>
    async void OnResized(float width, float height) {
        if (width != _width || height != _height) {
            _width = width;
            _height = height;
            await InvokeAsync(StateHasChanged);
        }
    }

    /// <summary>
    /// Gets the style string for the SquircleElement based on the current dimensions, radius, and smoothness.
    /// Caches the generated style to improve performance.
    /// </summary>
    string GetStyle() {
        var width = _width;
        var height = _height;
        var radius = Radius;
        var smoothness = Smoothness ?? _defaultSmoothness;
        var key = $"{width}-{height}-{radius}-{smoothness}";

        if (_cache.ContainsKey(key)) {
            return Style + _cache[key];
        }
        else {
            var mask = GetMaskStyle(width, height, radius, smoothness);
            _cache[key] = mask;
            return Style + mask;
        }
    }

    /// <summary>
    /// Generates the mask style string for the SquircleElement based on the provided dimensions, radius, and smoothness.
    /// </summary>
    static string GetMaskStyle(float width, float height, float? radius, float smoothness) {
        var maxBorderRadius = Math.Min(width, height) / 2;
        var finalBorderRadius = Math.Min(radius ?? maxBorderRadius, maxBorderRadius);
        var dataUri = SquirclePathGenerator.GetSquirclePathAsDataUri(
            width,
            height,
            finalBorderRadius * smoothness,
            finalBorderRadius
        );

        return $"""
            mask-image: url("{dataUri}");
            mask-position: center;
            mask-repeat: no-repeat;
            """;
    }
}