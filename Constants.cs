using System;
using System.Collections.Generic;
using System.Reflection;

namespace NyahHelper
{
    public class Constants
    {
        public readonly static BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
        public readonly static BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;
        public readonly static BindingFlags PrivateStatic = BindingFlags.NonPublic | BindingFlags.Static;
        public readonly static BindingFlags PublicStatic = BindingFlags.Public | BindingFlags.Static;
    }
}
