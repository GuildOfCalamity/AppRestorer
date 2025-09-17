using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;

namespace AppRestorer;

/// <summary>
/// A reusable class that tracks DependencyPropertyDescriptor subscriptions and safely 
/// unhooks them to prevent memory leaks. Can be useful for debugging, live previews, 
/// or designer surfaces where you want to observe property changes dynamically.
/// </summary>
public class PropertyWatcher : IDisposable
{
    readonly DependencyObject? _target;
    readonly Dictionary<DependencyProperty, DependencyPropertyDescriptor> _descriptors = new();
    readonly Dictionary<DependencyProperty, EventHandler> _handlers = new();

    public PropertyWatcher(DependencyObject target)
    {
        _target = target ?? throw new ArgumentNullException(nameof(target));
    }

    public void Watch(DependencyProperty property, EventHandler handler)
    {
        if (property == null )
            throw new ArgumentNullException(nameof(property));
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        if (_descriptors.ContainsKey(property))
            return; // Already watching

        var descriptor = DependencyPropertyDescriptor.FromProperty(property, _target?.GetType());
        if (descriptor != null)
        {
            descriptor.AddValueChanged(_target, handler);
            _descriptors[property] = descriptor;
            _handlers[property] = handler;
        }
    }

    public void Unwatch(DependencyProperty property)
    {
        if (_descriptors.TryGetValue(property, out var descriptor) &&
            _handlers.TryGetValue(property, out var handler))
        {
            descriptor.RemoveValueChanged(_target, handler);
            _descriptors.Remove(property);
            _handlers.Remove(property);
        }
    }

    public void Dispose()
    {
        foreach (var kvp in _descriptors)
        {
            var property = kvp.Key;
            var descriptor = kvp.Value;
            var handler = _handlers[property];
            descriptor.RemoveValueChanged(_target, handler);
        }
        _descriptors.Clear();
        _handlers.Clear();
    }
}
