using System;

namespace Hagar.Configuration
{
    internal class DefaultTypeConfiguration : IConfigurationProvider<TypeConfiguration>
    {
        public void Configure(TypeConfiguration options)
        {
            var wellKnownTypes = options.WellKnownTypes;
            wellKnownTypes[0] = typeof(void); // Represents the type of null
            wellKnownTypes[1] = typeof(int);
            wellKnownTypes[2] = typeof(string);
            wellKnownTypes[3] = typeof(bool);
            wellKnownTypes[4] = typeof(short);
            wellKnownTypes[5] = typeof(long);
            wellKnownTypes[6] = typeof(sbyte);
            wellKnownTypes[7] = typeof(uint);
            wellKnownTypes[8] = typeof(ushort);
            wellKnownTypes[9] = typeof(ulong);
            wellKnownTypes[10] = typeof(byte);
            wellKnownTypes[11] = typeof(float);
            wellKnownTypes[12] = typeof(double);
            wellKnownTypes[13] = typeof(decimal);
            wellKnownTypes[14] = typeof(char);
            wellKnownTypes[15] = typeof(Guid);
            wellKnownTypes[16] = typeof(DateTime);
            wellKnownTypes[17] = typeof(TimeSpan);
        }
    }
}