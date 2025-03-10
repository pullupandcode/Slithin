﻿using System.Reflection;

namespace Slithin.Core.FeatureToggle;

public static class Features
{
    private static readonly Dictionary<string, Type> _allFeatures = new();

    public static void Collect()
    {
        var types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes()).Where(_ => _.IsAssignableTo(typeof(IFeature)));

        foreach (var type in types)
        {
            if (!type.IsInterface && !type.IsAbstract && !_allFeatures.ContainsKey(type.Name))
            {
                _allFeatures.Add(type.Name, type);
            }
        }
    }

    public static void Disable<T1, T2, T3>()
        where T1 : IFeature
        where T2 : IFeature
        where T3 : IFeature
    {
        Feature<T1>.Disable();
        Feature<T2>.Disable();
        Feature<T3>.Disable();
    }

    public static void Enable<T1, T2, T3>()
        where T1 : IFeature
        where T2 : IFeature
        where T3 : IFeature
    {
        Feature<T1>.Enable();
        Feature<T2>.Enable();
        Feature<T3>.Enable();
    }

    public static void EnableAll()
    {
        foreach (var feature in _allFeatures)
        {
            var featureType = typeof(Feature<>).MakeGenericType(new Type[] { feature.Value });
            featureType.GetMethod("Enable").Invoke(null, null);
        }
    }

    public static DynamicFeature FromString(string featureName)
    {
        return new(typeof(Feature<>).MakeGenericType(new Type[] { _allFeatures[featureName] }));
    }

    public class DynamicFeature
    {
        public DynamicFeature(Type featureType)
        {
            Property = featureType.GetProperty("IsEnabled");
        }

        public bool IsEnabled
        {
            get
            {
                return (bool)Property.GetValue(null);
            }
            set
            {
                Property.SetValue(null, value);
            }
        }

        public PropertyInfo Property { get; init; }
    }
}
