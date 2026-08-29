// jsdom doesn't implement these browser APIs, but several Vuetify
// components use them internally (menu/dialog positioning, breakpoint
// detection) regardless of whether a given test actually exercises that
// behavior — without these, mounting almost any real Vuetify component
// throws ReferenceErrors that have nothing to do with what's under test.

class ResizeObserverStub {
  observe() {}
  unobserve() {}
  disconnect() {}
}
globalThis.ResizeObserver ??= ResizeObserverStub as unknown as typeof ResizeObserver

class IntersectionObserverStub {
  observe() {}
  unobserve() {}
  disconnect() {}
  takeRecords() {
    return []
  }
}
globalThis.IntersectionObserver ??= IntersectionObserverStub as unknown as typeof IntersectionObserver

if (!window.matchMedia) {
  window.matchMedia = (query: string) =>
    ({
      matches: false,
      media: query,
      onchange: null,
      addListener: () => {},
      removeListener: () => {},
      addEventListener: () => {},
      removeEventListener: () => {},
      dispatchEvent: () => false,
    }) as unknown as MediaQueryList
}

// Vuetify's VOverlay location strategy (used by v-dialog/v-menu/v-select)
// reads this when computing where to position content — jsdom has no
// viewport of its own, so this is undefined without a stub.
if (!window.visualViewport) {
  Object.defineProperty(window, 'visualViewport', {
    writable: true,
    value: {
      width: 1024,
      height: 768,
      offsetLeft: 0,
      offsetTop: 0,
      addEventListener: () => {},
      removeEventListener: () => {},
    },
  })
}
