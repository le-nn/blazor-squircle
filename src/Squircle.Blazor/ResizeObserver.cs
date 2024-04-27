using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Diagnostics.CodeAnalysis;

namespace Squircle.Blazor;

class ResizeObserver {
    static bool _isInitialized;

    const string _observeSymbol = "__squircle_observe";
    const string _disposeSymbol = "__squircle_dispose";

    const string _src = $$"""
        function __squircle_removeElementFromArray(array, elem) {
          const index = array.indexOf(elem);
          if (index === -1) {
            return;
          }
          array.splice(array.indexOf(elem), 1);
        }

        const __squircle_resizeListenersMap = new WeakMap();
        const __squircle_observedElements = new Set();
        let observer;

        function __squircle_getObserver() {
          if (observer) {
            return observer;
          }

          const newObserver = new ResizeObserver(entries => {
            for (const entry of entries) {
              const listeners = __squircle_resizeListenersMap.get(entry.target);
              if (!listeners) {
                continue;
              }

              listeners.forEach(callback => {
                callback(entry);
              });
            }
          });

          observer = newObserver;
          return newObserver;
        }

        function {{_observeSymbol}}(element, dotnetAction){
            // This would fail in tests as there is no resize observer in jsdom.
            // We don't 'resize' window in tests, tho.
            if (!ResizeObserver) {
              throw new Error("there is no resize observer in jsdom");
            }

            
          const currentObservers = __squircle_resizeListenersMap.get(element) || [];

          const handleResize = entry => {
              console.log(entry)
              dotnetAction.invokeMethodAsync("Invoke",entry.contentRect.width,entry.contentRect.height)
          }

          currentObservers.push(handleResize);
          __squircle_resizeListenersMap.set(element, currentObservers);
        
          if (!__squircle_observedElements.has(element)) {
              __squircle_getObserver().observe(element);
          }

          return {
            dispose: () => {
                __squircle_removeElementFromArray(currentObservers, handleResize);
                if (currentObservers.length === 0) {
                  __squircle_getObserver().unobserve(element);
                  __squircle_observedElements.delete(element);
                }
            }
          };
        }

        function {{_disposeSymbol}}(subscription) {
            subscription.dispose();
        }
        """;

    public static async Task<IAsyncDisposable> ObserveAsync(IJSRuntime jsRuntime, ElementReference element, Action<float, float> action) {
        if (element.Equals(default) || element.Id is null or "") {
            return new Subscription(() => ValueTask.CompletedTask);
        }


        if (_isInitialized is false) {
            await jsRuntime.InvokeVoidAsync("eval", _src);
            _isInitialized = true;
        }

        var wrapper = new ActionWrapper(action);
        var actionRef = DotNetObjectReference.Create(wrapper);
        var subscription = await jsRuntime.InvokeAsync<IJSObjectReference>(_observeSymbol, element, actionRef);

        return new Subscription(async () => {
            await jsRuntime.InvokeVoidAsync(_disposeSymbol, subscription);
            await subscription.DisposeAsync();
            actionRef.Dispose();
        });
    }

    class Subscription(Func<ValueTask> action) : IAsyncDisposable {
        public async ValueTask DisposeAsync() {
            await action.Invoke();
        }
    }

    class ActionWrapper {
        public Action<float, float> _action;

        [DynamicDependency(nameof(Invoke))]
        public ActionWrapper(Action<float, float> action) {
            _action = action;
        }

        [JSInvokable]
        public void Invoke(float width, float height) => _action.Invoke(width, height);
    }

}

