using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.ComponentModel
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Event)]
    public class DisplayNameAttribute : Attribute
    {
        public DisplayNameAttribute(string displayName)
        {
            DisplayName = displayName;
        }
        public virtual string DisplayName
        {
            get;
            private set;
        }
    }


}
