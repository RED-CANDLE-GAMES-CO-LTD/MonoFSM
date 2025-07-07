## Overview

MonoFSM is a comprehensive Finite State Machine (FSM) framework for Unity, designed to simplify the implementation of complex game behaviors and logic. The framework provides a visual and modular approach to designing state machines, making it easier to create, debug, and maintain game systems.
[Nine Sols](https://store.steampowered.com/app/1809540/Nine_Sols/) is the first project to use this framework, and it has been developed and tested in the context of that game.
## Pre-requirement Dependencies
### Paid Tools
  * [Odin-Inspector](https://odininspector.com/)
### Free Tools
  * Unity Official Package
    * Unity.Addressable
    * Unity.Timeline
  * ThirdParty Tools
    * [UniTask](https://github.com/Cysharp/UniTask)
    * [ZString](https://github.com/Cysharp/ZString)
    * [PrimeTween](https://github.com/KyryloKuzyk/PrimeTween)
### Included in Project (with modification)
* Auto Attribute
* GuidManager
* System.Runtime.CompilerServices.Unsafe

# Installation
* Just use: Install through Unity Package Manager with git url
* To Contribute: use "git submodule" to include this module into your project, and add as local package through Unity package manager

## Why Use MonoFSM?

## Core Features

### State Machine System

- **States**: `GeneralState` objects that represent discrete behavior states
- **Transitions**: Logic for moving between states with condition-based switching
- **State Actions**: Modular behaviors attached to states (`AbstractStateAction`)

### Object Pooling

- Efficient object instantiation and reuse through `PoolManager`
- Serialization caching for prefab instances (`PrefabSerializeCache`)

### Data Management

- **Game Flags**: Persistent data storage through `GameFlagBase`
- **Generic Mono Variable**: Runtime state with `GenericMonoVariable`
- **Descriptable Data**: Content description system via `DescriptableData`
- **Mono Descriptable**: Runtime representation of descriptable objects (`MonoDescriptable`)
- **Stats System**: Character and object statistics through `StatData`

### Debug and Development Tools
- **DebugProvider**: Runtime debugging utility
- Logs state changes and transitions
- Toggle debug mode with keyboard shortcuts (%#_L)
- Visual indicators in hierarchy
- DebugSetting: Configuration for debug features

### Animation Integration

- Animation control through state machines (`AbstractAnimatorPlayAction`)
- Runtime animation clip generation

### Editor Tools

- **FSM Graph Editor**: Visual editor for state machines
- **Issue Tracking**: In-editor issue management with `Issue` and `Comment`
- **Design Tags**: Scene annotations with `GamePlayTag`
- **Reference Window**: Component reference explorer (`ReferenceWindow`)
- **Inline Favorites**: Quick access to frequently used components (`InlineFavoriteComponent`)

## Getting Started

1. Add the MonoFSM as a submodule or package to your Unity project
2. Create a new GameObject and add the `StateMachineOwner` component
3. Add a `GeneralFSMContext` to the GameObject
4. Create states by clicking "Add State" in the FSM Context inspector
5. Add transitions between states
6. Add actions to states to define behavior

## Example Usage
### todo

## Best Practices

1. Use descriptive names for states and transitions
2. Break complex behaviors into smaller, manageable states
3. Use the hierarchical structure to organize related states
4. Add comments and issues to document your FSM design
5. Use the visual editor to get an overview of your state machines

## Integration with Other Systems

MonoFSM can be integrated with:
- Addressable Asset system for content loading
- Unity Animation system for character animations
- Unity UI system for user interfaces

## Notes

This framework is designed for Unity projects and makes extensive use of MonoBehaviours and ScriptableObjects.
```
MonoFSM Architecture
│
├── Variables System
│   ├── Base Classes
│   │   ├── AbstractMonoVariable - Base for all variables
│   │   ├── GenericMonoVariable<TScriptableData, TField, TType> - Generic implementation
│   │   └── GenericUnityObjectVariable<T> - For Unity Object variables
│   │
│   ├── Variable Types
│   │   ├── VarBool - Boolean variables
│   │   ├── VarFloat - Float variables
│   │   ├── VarInt - Integer variables
│   │   ├── VarString - String variables
│   │   ├── VarMono - MonoDescriptable variables
│   │   └── VarGameData - Game data variables
│   │
│   ├── Variable Management
│   │   ├── RCGVariableFolder - Container for variables
│   │   ├── VariableDictionary - Dictionary of variables
│   │   └── VariableBinder - Connects variables
│   │
│   └── Value Providers
│       ├── FloatValueSource/FloatValueGetter
│       ├── StringProvider
│       ├── AnimatorProvider
│       └── GameDataProvider
│
├── State Machine System
│   ├── States
│   │   ├── AbstractState<T> - Base state
│   │   ├── GeneralState - Main state implementation
│   │   └── RCGFSMAnyState - Special state for any-to-any transitions
│   │
│   ├── Transitions
│   │   ├── StateTransition - Base transition
│   │   ├── StateTransitionSkippable - Transition with skip check
│   │   ├── VarBoolTransition - Transition based on boolean
│   │   └── VarIntTransition - Transition based on int
│   │
│   ├── Conditions
│   │   ├── AbstractConditionComp - Base condition
│   │   ├── VarBoolValueCondition - Bool comparison
│   │   ├── FloatCompareCondition - Float comparison
│   │   ├── IntCompareCondition - Int comparison
│   │   ├── IsStateCondition - Check current state
│   │   ├── StateTimeUpCondition - Time based condition
│   │   └── ConditionFolder - Container for conditions
│   │
│   └── Actions
│       ├── AbstractStateAction - Base action
│       ├── AnimatorActions
│       │   ├── AnimatorPlayAction - Play animation
│       │   ├── AnimatorParameterSetValueAction - Set animator parameter
│       │   └── AnimatorPlayActionModule - Animation modules
│       │
│       ├── Variable Actions
│       │   ├── SetVariableBoolAction - Set bool
│       │   ├── SetVariableFloatAction - Set float
│       │   ├── SetVariableIntAction - Set int
│       │   ├── VariableFloatArithmeticAction - Float arithmetic
│       │   └── VariableIntArithmeticAction - Int arithmetic
│       │
│       └── Debug Actions
│           └── LogAction - Debug logging
│
├── Interaction System
│   ├── Spatial Detection
│   │   ├── AbstractDetector - Base detector
│   │   ├── TriggerSpatialDetector - Collider detection
│   │   ├── TriggerSpatialDetector2D - 2D collider detection
│   │   ├── CollisionSpatialDetector - Collision detection
│   │   ├── MouseDownDetector - Mouse input detection
│   │   ├── SpatialDetectable - Objects that can be detected
│   │   └── ReliableOnTriggerExit - Fixes Unity's OnTriggerExit issues
│   │
│   └── Effect System
│       ├── IEffectType/GeneralEffectType - Effect type definition
│       ├── Dealers
│       │   └── GeneralEffectDealer - Apply effects
│       │
│       ├── Receivers
│       │   └── GeneralEffectReceiver - Receive effects
│       │
│       ├── EffectHitData
│       │   └── GeneralEffectHitData - Data passed between dealer and receiver
│       │
│       ├── Effect Resolvers
│       │   ├── EffectResolver - Base resolver
│       │   ├── EffectHitFloatValueCompareCondition - Effect condition
│       │   └── EffectHitFloatArithmeticAction - Effect action
│       │
│       └── Effect Nodes
│           ├── EffectEnterNode - Handle entry
│           ├── EffectExitNode - Handle exit
│           └── EffectHitFailNode - Handle failure
│
├── Data Provider System
│   ├── Interface Providers
│   │   ├── IFloatProvider - Float value source
│   │   ├── IBoolProvider - Bool value source
│   │   ├── IStringProvider - String value source
│   │   ├── ISpriteProvider - Sprite source
│   │   └── IGameDataProvider - Game data source
│   │
│   ├── Component Wrappers
│   │   ├── VariableProviderRef<T,V> - Reference variable
│   │   ├── VarFloatProviderRef - Float variable ref
│   │   ├── VarBoolProviderRef - Bool variable ref
│   │   ├── VarGameDataRef - Game data ref
│   │   └── VarMonoRef - MonoDescriptable ref
│   │
│   └── Field Value Providers
│       ├── AbstractFieldValueProvider - Base field provider
│       ├── GameDataObjectFieldProvider - Field from game data
│       └── VariableFieldProvider - Field from variable
│
├── Stats System
│   ├── StatData - Stat definition
│   ├── CharacterStat - Stat implementation
│   ├── StatModifier - Stat modifier
│   └── StatModifierEntry - Configurable modifier
│
└── Utility Classes
    ├── Cache<K,V> - Generic caching
    ├── SerializedDictionary<K,V> - Unity serializable dictionary
    ├── SerializableDateTime - Date/time serialization
    ├── MonoDict<T,U> - Dictionary of MonoBehaviours
    ├── SingletonBehaviour<T> - Singleton pattern
    └── ValueInstance<T> - Generic value container
```
