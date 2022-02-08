## Config

config-daysToRetain-section-text = Days to retain water
config-daysToRetain-section-tooltip =
	0: will never retain water
	-1: will always retain water
config-daysToRetain-basic-name = Basic Retaining Soil
config-daysToRetain-quality-name = Quality Retaining Soil
config-daysToRetain-deluxe-name = Deluxe Retaining Soil

## Tooltip

retainingSoil-tooltip =
	This soil { $Days ->
		[-1] will stay watered overnight
		[0] will not stay watered overnight
		[one] will stay watered overnight once
		*[other] will stay watered overnight for { $Days } nights
	}.
	Mix into tilled soil.