# Palette's Journal

## 2026-03-01 - [ASP.NET MVC model label association mismatch]
**Learning:** Custom elements and manually assigned lowercase IDs on model-bound inputs override default ASP.NET Core MVC labels, breaking accessibility relationships (e.g. click-to-focus and screen reader mapping).
**Action:** Always add an explicit `for="..."` attribute matching the input `id` to restore the accessible relationship.
