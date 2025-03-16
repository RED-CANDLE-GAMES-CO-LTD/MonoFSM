## Overview

RCGMakerFSM is a comprehensive Finite State Machine (FSM) framework for Unity, designed to simplify the implementation of complex game behaviors and logic. The framework provides a visual and modular approach to designing state machines, making it easier to create, debug, and maintain game systems.
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

## Core Features

### State Machine System

- **General FSM Context**: The central manager for state machines (`GeneralFSMContext`) handling state transitions and updates
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

1. Add the RCGMakerFSM as a submodule or package to your Unity project
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

RCGMakerFSM can be integrated with:
- Addressable Asset system for content loading
- Unity Animation system for character animations
- Unity UI system for user interfaces

## Notes

This framework is designed for Unity projects and makes extensive use of MonoBehaviours and ScriptableObjects.