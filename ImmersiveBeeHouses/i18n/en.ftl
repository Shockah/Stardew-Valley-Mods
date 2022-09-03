## Config

-flowers-capitalized = { $Count ->
    [0] No flowers
    [one] 1 flower
    *[other] { $Count } flowers
}

duration = { $Days ->
    [0] { $Hours ->
        [0] { $Minutes }m
        *[other] { $Hours }h { $Minutes }m
    }
    *[other] { $Days }d { $Hours }h { $Minutes }m
}

config-compatibilityMode = Compatibility mode
    .tooltip = Patch the game code in a more mod-compatible way, at the cost of some performance.
config-daysToProduce = Days to produce
    .tooltip =
        How many days should it take to produce Honey with no flowers around a Bee House.
        Vanilla Bee Houses always take 4 days.
        { "" }
        Note: this mod changes this time to be counted from the moment of pick-up.
        Vanilla counts "until 4 days pass".
config-flowerCoefficient = Flower coefficient
    .tooltip =
        Formula coefficient to use for flower count.
        Values below 1 mean more flowers have diminishing returns.
        Values above 1 mean more flowers have additional effects.
config-graph-axis-flowers = Flowers
config-graph-axis-days = Days