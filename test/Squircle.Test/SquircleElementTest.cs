using Bunit;
using Squircle.Blazor;

namespace Squircle.Test;

public class SquircleElementTest : TestContext {
    [Fact]
    public void Test1() {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupVoid("eval").SetVoidResult();
        JSInterop.SetupVoid("dispose").SetVoidResult();

        var component = RenderComponent<SquircleElement>(el => {
            el.AddChildContent<Div>();
        });

        Assert.Equal(1, component.Instance.CacheCount);

        component.SetParametersAndRender(c => {
            c.Add(p => p.Roundness, 0.2f);
        });

        // Check if cache is increased
        Assert.Equal(2, component.Instance.CacheCount);

        // Check if cache is cleared
        component.Instance.ClearCache();
        Assert.Equal(0, component.Instance.CacheCount);

        component.Dispose();
    }
}