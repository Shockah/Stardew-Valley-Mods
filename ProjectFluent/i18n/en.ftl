-mod-name = Project Fluent
-content-patcher-mod-name = Content Patcher

config-contentPatcherPatchingMode = { -content-patcher-mod-name } patching
    .tooltip =
        Controls how and if { -content-patcher-mod-name } gets patched to support { -mod-name }.
        > { config-contentPatcherPatchingMode-value.Disabled }:
        {"   "}Content Patcher will not be patched.
        > { config-contentPatcherPatchingMode-value.PatchFluentToken }:
        {"   "}Content Patcher will be patched to directly allow the usage of the {"{{"}Fluent{"}}"} token.
        > { config-contentPatcherPatchingMode-value.PatchAllTokens }:
        {"   "}Content Patcher will be patched to directly allow the usage of any tokens registered for each mod.

        Technical details:
        By default (as of { -content-patcher-mod-name } version 1.24.x), { -content-patcher-mod-name } mods
        have to spell out the whole name of the Fluent localization token, including their ID.
        Enabling the "Patch all tokens" is discouraged, but if you are working on your own C# mod
        adding tokens for { -content-patcher-mod-name } mods, it will allow those to also be used directly.
config-contentPatcherPatchingMode-value = .
    .Disabled = Disabled
    .PatchFluentToken = Only patch {"{{"}Fluent{"}}"}
    .PatchAllTokens = Patch all tokens

config-localeOverride = Locale override
    .tooltip =
        Allows you to override the current locale used by { -mod-name }.
        Enter either a built-in locale (listed below), or a different (mod) locale.
        Leave empty to use the game locale.
    .subtitle =
        Built-in values:
        { $Values }