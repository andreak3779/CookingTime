// filepath: wwwroot/js/theme.js
//
// Phase 12: dark-mode toggle for the CookingTime theme.
//
// The theme palette is driven by a `data-theme` attribute on <html>:
//   <html data-theme="dark">   -> the [data-theme="dark"] token block in
//                                  wwwroot/css/cookingtime.css applies.
//
// This file is loaded as a BLOCKING script in the <head> of index.html so
// that the persisted preference is applied to <html> before the browser
// paints the first frame. Deferring it to the end of <body> would produce a
// visible flash of the light theme on every load for dark-mode users.
//
// The Blazor <ThemeToggle /> component drives the same API through
// IJSRuntime, so there is exactly one implementation of "what is the current
// theme" and "how do we change it".

window.cookingTimeTheme = (function () {
    const STORAGE_KEY = 'ct-theme';
    const DARK = 'dark';
    const LIGHT = 'light';

    function isDark() {
        return document.documentElement.getAttribute('data-theme') === DARK;
    }

    // Reads the persisted preference. Wrapped because localStorage throws in
    // some privacy modes; a missing preference simply means "light".
    function readStored() {
        try {
            return window.localStorage.getItem(STORAGE_KEY);
        } catch {
            return null;
        }
    }

    function store(theme) {
        try {
            window.localStorage.setItem(STORAGE_KEY, theme);
        } catch {
            // Preference is not persisted (private browsing / storage disabled).
            // The theme still applies for this session.
        }
    }

    // Applies a theme and persists it. Returns the applied theme.
    function apply(theme) {
        const value = theme === DARK ? DARK : LIGHT;
        document.documentElement.setAttribute('data-theme', value);
        store(value);
        return value;
    }

    // Flips between light and dark. Returns the newly applied theme.
    function toggle() {
        return apply(isDark() ? LIGHT : DARK);
    }

    // Apply the persisted preference immediately. This runs while the
    // document is still in <head>, so documentElement exists and the
    // attribute is set before first paint.
    const stored = readStored();
    document.documentElement.setAttribute('data-theme', stored === DARK ? DARK : LIGHT);

    return {
        isDark: isDark,
        apply: apply,
        toggle: toggle
    };
})();
</content>