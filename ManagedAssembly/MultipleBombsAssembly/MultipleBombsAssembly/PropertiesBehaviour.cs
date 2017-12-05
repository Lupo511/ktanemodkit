using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MultipleBombsAssembly
{
    public class PropertiesBehaviour : MonoBehaviour, IDictionary<string, object>
    {
        public class Property
        {
            public delegate object PropertyGetDelegate();
            private PropertyGetDelegate getDelegate;
            public delegate void PropertySetDelegate(object value);
            private PropertySetDelegate setDelegate;

            public Property(PropertyGetDelegate get, PropertySetDelegate set)
            {
                getDelegate = get;
                setDelegate = set;
            }

            public PropertyGetDelegate Get
            {
                get
                {
                    return getDelegate;
                }
            }

            public bool CanSet()
            {
                return setDelegate != null;
            }

            public PropertySetDelegate Set
            {
                get
                {
                    return setDelegate;
                }
            }
        }

        private Dictionary<string, Property> properties;

        public PropertiesBehaviour()
        {
            properties = new Dictionary<string, Property>();
        }

        public void AddProperty(string name, Property property)
        {
            properties.Add(name, property);
        }

        public void AddProperty(string name, Property.PropertyGetDelegate get, Property.PropertySetDelegate set)
        {
            properties.Add(name, new Property(get, set));
        }

        public object this[string key]
        {
            get
            {
                return properties[key].Get();
            }
            set
            {
                Property property = properties[key];
                if (!property.CanSet())
                {
                    throw new Exception("The key \"" + key + "\" cannot be set (it is read-only).");
                }
                property.Set(value);
            }
        }

        public int Count
        {
            get
            {
                return properties.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public ICollection<string> Keys
        {
            get
            {
                return properties.Keys.ToList();
            }
        }

        public ICollection<object> Values
        {
            get
            {
                throw new NotImplementedException("The Values property is not supported in this Dictionary.");
            }
        }

        public void Add(KeyValuePair<string, object> item)
        {
            throw new NotImplementedException("You can't add items to this Dictionary.");
        }

        public void Add(string key, object value)
        {
            throw new NotImplementedException("You can't add items to this Dictionary.");
        }

        public void Clear()
        {
            throw new NotImplementedException("You can't clear this Dictionary.");
        }

        public bool Contains(KeyValuePair<string, object> item)
        {
            throw new NotImplementedException("The Contains method is not supported in this Dictionary.");
        }

        public bool ContainsKey(string key)
        {
            return properties.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            throw new NotImplementedException("The CopyTo method is not supported in this Dictionary.");
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            throw new NotImplementedException("The GetEnumerator method is not supported in this Dictionary.");
        }

        public bool Remove(KeyValuePair<string, object> item)
        {
            throw new NotImplementedException("The Remove method is not supported in this Dictionary.");
        }

        public bool Remove(string key)
        {
            throw new NotImplementedException("The Remove method is not supported in this Dictionary.");
        }

        public bool TryGetValue(string key, out object value)
        {
            try
            {
                value = properties[key].Get();
                return true;
            }
            catch (Exception)
            {
                value = null;
                return false;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException("The GetEnumerator method is not supported in this Dictionary.");
        }
    }
}
