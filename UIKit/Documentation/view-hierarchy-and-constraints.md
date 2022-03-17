# View hierarchy
A UI in UIKit consists of a tree of `UIView`s, with a `UIRootView` at the root of the tree. The `UIRootView` manages all of the views, their state, their positioning and sizing, and any input to the UI.
A simple `UIView` does not provide much functionality, other than providing a grouping for other views.

A `UIView`'s frame is defined by its `X1`, `Y1`, `X2` and `Y2` properties (and alternatively `Width` and `Height`).
The system will not allow a view to have those properties set up in a way where either `Width` or `Height` would end up being negative.
Additionally, a view exposes helper properties `TopLeft`, `TopRight`, `BottomLeft` and `BottomRight`.
All of the coordinates are relative to the view's superview (or in other words parent, or in other words the view containing it).
Converting a point between two different views can be achieved via the `UIViews.ConvertPointBetweenViews` utility method, as long as both views are in the same hierarchy.

# Constraints
In a usual simple UI system, you would position views by setting their coordinates and sizes directly. In UIKit, this should be avoided in most cases, and constraints should be used instead.

A **constraint** defines a relationship between two view **anchors** (or sometimes just one). For example: `B.Top == A.Top + 16`. Such a constraint would make the `B` view's top anchor to be pinned to the `A` view's top anchor, but offset by 16 points (pixels).
It's important to note that those constraints are *not* assignments -- they're actually linear equations. Whenever any layout change happens, this equation will be solved and both `A` and `B` views' properties will be set accordingly. This also means the order of constraints in a valid layout does not matter.

The anchors available on all `UIView`s are: `LeftAnchor`, `RightAnchor`, `TopAnchor`, `BottomAnchor`, `WidthAnchor`, `HeightAnchor`, `CenterXAnchor` and `CenterYAnchor`.

## Anatomy of a constraint
As mentioned above, constraints are actually linear equations, in the form of $y = ax + b$. For the above example, we have $a = 1; b = 16$. We didn't explicitly specify the value of $a$, but it defaults to $1$ ($b$ defaults to $0$).
In UIKit, the $a$ variable is called the **multiplier**, while $b$ is called the **constant**.

Additionally, constraints can be *inequalities*. Imagine you have a view that you want to stay in width between 40 and 200 points. To achieve this, you can use two inequality constraints:
* `MyView.Width >= 40`
* `MyView.Width <= 200`

The above is also an example of a constraint using only one anchor in the equation.
These constraints will make sure the view will never be shorter than 40 points or longer than 200 points, BUT it does not specify what width it will have exactly, so if those are the only constraints constraining the width of the view, the width would be [ambiguous](#ambiguous-layouts).
Under the hood, inequality constraints are also what allows the system to enforce the `Width` and `Height` properties to never be negative.

## Constraint priorities

Up until now, all constraints were strictly required. In reality, a lot of the constraints are not always required or wanted to be satisfied.
This is where priorities come in. Each constraint in UIKit can have a **priority** value specified, between 0 and 1000. A constraint with a priority of 1000 is required; anything below it will be only satisfied if it can be done, while keeping any constraints with higher priorities satisfied.
UIKit defines some default priorities in `UILayoutConstraintPriority`: `Required` (1000), `High` (750), `Medium` (500) and `Low` (250).
Additionally, there is an `OptimalCalculations` (50) priority, but it generally should not be used for declaring constraints between views -- see the [optimal size calculations section](#optimal-size-calculations).

# Intrinsic view size

An **intrinsic size** is a size, that is defined by the view's content, unrelated to any subviews and constraints. An example would be a `UILabel`, which displays some text. The size of the text on screen is the value of the intrinsic size of the `UILabel`. This does not necessarily mean the `UILabel` will be displayed at this size, however. It is just its preferred size.
The `IntrinsicWidth` and `IntrinsicHeight` properties are automatically transformed into (non-required) inequality constraints. Because they are non-required, additional constraints can be added which override those values.

To create those inequality constraints, `IntrinsicWidth` and `IntrinsicHeight` are used together with the `HorizontalContentHuggingPriority`, `VerticalContentHuggingPriority`, `HorizontalCompressionResistancePriority` and `VerticalCompressionResistancePriority`.
**Content hugging** priority defines how much do the view's contents want to stick to the edges of the view (restrict growing).
**Compression resistance** priority defines how much do the view's contents want to keep being at least their preferred size (restrict shrinking).
By default, the content hugging priorities are `Low`, while compression resistance priorities are `High`, but this can vary from `UIView` to `UIView` (`UILabel` overrides content hugging with a value of 251 -- a bit higher than `Low`) and can also be set manually.
`Required` priorities should generally be avoided for those 4 properties to avoid any potential unsatisfiable layout problems.

## Content hugging and compression resistance

For this exercise we will completely ignore vertical positioning.
Imagine a layout like this:
`|[LabelA][LabelB]|`
`UILabel`s declare their `IntrinsicWidth` automatically.

This may seem like a simple layout to recreate with these constraints:
* `LabelA.Left == Root.Left`
* `LabelB.Left == Root.Right`
* `LabelB.Right == Root.Right`

The reality is -- it's often not that simple.

### Growing, content hugging

If the root view is constrained to be larger than the labels' width sum, the labels would also have to be resized in some way. But in what way?
* `LabelA` should be made larger.
	`|[LabelA......][LabelB]|`
* `LabelB` should be made larger.
	`|[LabelA][LabelB......]|`
* Both labels should be made larger.
	`|[LabelA...][LabelB...]|`

All three options are valid here, but [it is not obvious which one should be used](#ambiguous-layouts). This is why content hugging priorities exist.
* Setting `LabelA`'s `HorizontalContentHuggingPriority` lower than `LabelB`'s will make `LabelA` grow.
* Setting `LabelB`'s `HorizontalContentHuggingPriority` lower than `LabelA`'s will make `LabelB` grow.
* If both priorities are equal, it is not clear which label should be grown. To help the constraint solver in this case, you can add an additional constraint `LabelA.Width == LabelB.Width`.

### Shrinking, compression resistance

If the root view is constrained to be smaller than the labels' width sum, the labels would also have to be resized in some way (possibly truncating the text displayed). But in what way?
* `LabelA` should be made smaller.
	`|[La..][LabelB]|`
* `LabelB` should be made smaller.
	`|[LabelA][La..]|`
* Both labels should be made smaller.
	`|[Lab..][Lab..]|`

All three options are valid here, but [it is not obvious which one should be used](#ambiguous-layouts). This is why compression resistance priorities exist.
* Setting `LabelA`'s `HorizontalCompressionResistancePriority` lower than `LabelB`'s will make `LabelA` shrink.
* Setting `LabelB`'s `HorizontalCompressionResistancePriority` lower than `LabelA`'s will make `LabelB` shrink.
* If both priorities are equal, it is not clear which label should be shrinked. To help the constraint solver in this case, you can add an additional constraint `LabelA.Width == LabelB.Width`.

## Unsatisfiable layouts
An unsatisfiable layout is a layout, for which the constraint solver could not find a solution in which all required constraints are satisfied (often due to overconstraining).
In case of such a layout, the unsatisfiable constraints will be broken (ignored), allowing the code to proceed. Additionally, the `UIRootView`'s `UnsatifiableConstraintEvent` will be fired.
It may be tempting to ignore these problems (especially if it seems like the views are layed out okay), but any change to the view hierarchy or UIKit itself could alter the set of broken constraints, suddenly producing an obviously broken layout.

## Ambiguous layouts
An ambiguous layout is a layout, for which there are many possible solutions and it is not clear which one should be used. The two main causes are:
* Underconstraining -- the layout requires some additional constraints.
* Conflicting non-required constraints with the same priority.

UIKit ***does not*** detect layout ambiguity.
Ambiguous layouts may look completely broken, but also completely right, depending on what the constraint solver decides to do.
Even if the layout looks right, if you know you may have an ambiguous layout problem, it is recommended to fix it. Any change to the view hierarchy or UIKit itself could result in a wildly different layout, suddenly producing an obviously broken one.

## Optimal size calculations
Sometimes it may be beneficial to calculate an optimal size for a `UIView`, taking into account all of its current constraints. For example, this is being used for proper word wrapping in `UILabel`s.
This is achieved by the `UIView.GetOptimalSize` method. The method allows specifying a `UIOptimalSideLength` for each axis, a priority to use for that length and whether the view's current intrinsic length in that axis should be ignored.

Allowed `UIOptimalSideLength` values are:
* `Compressed` -- the smallest possible value will be calculated.
* `Expanded` -- the largest possible value will be calculated.
* `OfLength(x)` -- a constant (provided) value will be used.

The default priority (if not provided) is `OptimalCalculations` (50).
