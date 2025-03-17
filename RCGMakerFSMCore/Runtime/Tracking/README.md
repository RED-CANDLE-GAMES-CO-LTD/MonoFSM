# UserDataTracker System

A lightweight abstraction layer for analytics tracking in Unity games and applications. This system provides a framework for integrating various analytics services (such as Mixpanel) with a consistent API.

## Overview

The UserDataTracker system consists of three main components:

1. **UserDataTracker** - A static facade class that serves as the primary interface for tracking events
2. **ITracker** - An interface for implementing specific analytics service connections
3. **ITrackableValue** - An interface for implementing containers of tracked event properties

## Features

- **Service-agnostic** - Works with any analytics service through the implementation of the interfaces
- **Memory-efficient** - Implements an object pool pattern for tracking value containers
- **Simple API** - Streamlined interface for tracking events with minimal code

## How to Use

### Basic Usage

```csharp
// Get a trackable value container
var properties = UserDataTracker.BorrowTrackableValue;

// Add properties to track
properties.SetProperty("level_id", "dungeon-3");
properties.SetProperty("time_spent", 120.5f);
properties.SetProperty("completed", true);

// Track the event and recycle the properties container
UserDataTracker.Track("level_complete", properties);
```

### Implementing a Tracker for a Specific Service

To connect the UserDataTracker with a specific analytics service (e.g., Mixpanel):

1. Create an implementation of `ITracker`
2. Create an implementation of `ITrackableValue` 
3. Assign your implementation to `UserDataTracker._tracker` during initialization

Example implementation for Mixpanel:

```csharp
public class MixpanelTracker : ITracker
{
    private readonly ObjectPool<MixpanelTrackableValue> _valuePool;
    
    public MixpanelTracker()
    {
        _valuePool = new ObjectPool<MixpanelTrackableValue>(
            createFunc: () => new MixpanelTrackableValue(),
            actionOnGet: value => value.Clear(),
            defaultCapacity: 10
        );
    }
    
    public ITrackableValue BorrowTrackableValue()
    {
        return _valuePool.Get();
    }
    
    public void RecycleTrackableValue(ITrackableValue value)
    {
        if (value is MixpanelTrackableValue mixpanelValue)
        {
            _valuePool.Release(mixpanelValue);
        }
    }
}

public class MixpanelTrackableValue : ITrackableValue
{
    private readonly Dictionary<string, object> _properties = new Dictionary<string, object>();
    
    public void SetProperty(string key, object value)
    {
        _properties[key] = value;
    }
    
    public void Track(string eventName)
    {
        Mixpanel.Track(eventName, _properties);
    }
    
    public void Clear()
    {
        _properties.Clear();
    }
}
```

### Integration in Your Project

Initialize the tracking system in your game's initialization code:

```csharp
// In your game's initialization code
void InitializeAnalytics()
{
    // Initialize your analytics service (e.g., Mixpanel)
    Mixpanel.Init("your-project-token");
    
    // Create and assign your tracker implementation
    UserDataTracker._tracker = new MixpanelTracker();
}
```

## Best Practices

1. **Event Naming** - Use consistent naming conventions for events (e.g., `level_start`, `level_complete`)
2. **Property Naming** - Use snake_case for property names to maintain consistency
3. **Sensitive Data** - Never track personally identifiable information (PII) or sensitive data
4. **Performance** - Track events at appropriate points, avoiding high-frequency tracking during gameplay
5. **Initialization** - Always check that the tracker is initialized before attempting to track events

## Extending the System

You can extend the system by:

1. Implementing batch tracking functionality
2. Adding user opt-in/opt-out controls for GDPR compliance
3. Creating additional utility methods for common tracking patterns
4. Adding automatic property injection (e.g., session ID, app version)

## Troubleshooting

- If events aren't being tracked, ensure `UserDataTracker._tracker` has been properly assigned
- Check that the analytics service is properly initialized and has network connectivity
- Verify that you're calling `Track()` on the UserDataTracker, not directly on the trackable value