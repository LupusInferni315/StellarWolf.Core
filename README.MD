# StellarWolf.Core
Core Library for the StellarWolf Engine extensions for Unity3D.

## StellarWolf.ChaosEngine
### Seeding
#### ChaosEngine.Reset()
Reset reverts the engine to the initial state of the last seed supplied, this allows users to 'reuse' the same seed from the initial state without the need to create a new instance.
#### ChaosEngine.Reseed()
Reseed generates a new seed to populate the engine with, this allows users to reuse the instance of the engine without being 'stuck' with the same seed and without needing to supply a new seed.
#### ChaosEngine.Reseed(string) & ChaosEngine.Reseed(int)
Reseed provides a new seed to populate the engine with, this allows users to reuse the instance of the engine without being 'stuck' with the same seed..
### Generation Methods
Most method types in the engine have an option to return a singular value, fill a 'buffer' or return an `IEnumerable` of the method type.
The engine also allows the user to shuffle an array, select (potentially weighted) items out of a collection, and select an enum value (also potentially weighted).
The engine also provides methods to return boolean values with equal odds, or a percent chance as well as a in b odds.

## StellarWolf.IWeighted
`IWeighted` is an interface designed for creating types that will be used in collection selection, it exposes only an `Int32 Weight` and is taken into account with `ChaosEngine.Choose<T>(IEnumerable<T> collection)`. Providing an item with a weight of 0 or less, removes the possibility of a value from being selected.

## StellarWolf.IWeightedObject&lt;T&gt;
`IWeightedObject<T>` is an interface which implements `IWeighted` and is designed to allow non-weighted types (namely types you didn't create) to be assigned a weight, The `ChaosEngine.Choose<T>(IEnumerable<IWeightedObject<T>> collection)` methods return the value assigned to it as opposed to the interface object itself. Providing an item with a weight of 0 or less, removes the possibility of a value from being selected.

## StellarWolf.WeightAttribute
`WeightAttribute` is an attribute designed for Enum values to provide otherwise identical functionality as `IWeighted` it exposes only `Int32 Weight` and is taken into account by `NextEnum<T>()` any enum value lacking a `WeightAttribute` is treated as having a weight of 1. Providing an item with a weight of 0 or less, removes the possibility of a value from being selected.

## StellarWolf.TypeExtensions
Type extensions contains methods to return a 'friendly' string representation of a type, as well as determine if a type can either by explicit or implicit casting or by inheritence be converted to another type, it also allows custom defined conversion between types when no other method is available to convert. It also exposes methods to attempt this conversion which works with numerical types

## StellarEditor.SerializedPropertyExtensions
At this time SerializedPropertyExtensions exposes only 1 method, a way to `GetPropertyInstance()` of the SerializedProperty provided.

