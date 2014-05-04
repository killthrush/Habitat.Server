using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Security.Cryptography;
using System.Text;

namespace Habitat.Core.TestingLibrary
{
    public class RandomValueGenerator
    {
        private static readonly RNGCryptoServiceProvider RandomNumberGenerator = new RNGCryptoServiceProvider();

        public static byte GetByte()
        {
            var bytes = new byte[1];
            RandomNumberGenerator.GetBytes(bytes);
            return bytes[0];
        }

        public static short GetInt16()
        {
            var bytes = new byte[2];
            RandomNumberGenerator.GetBytes(bytes);
            return BitConverter.ToInt16(bytes, 0);
        }

        public static int GetInt32()
        {
            var bytes = new byte[4];
            RandomNumberGenerator.GetBytes(bytes);
            return BitConverter.ToInt32(bytes, 0);
        }

        public static long GetInt64()
        {
            var bytes = new byte[8];
            RandomNumberGenerator.GetBytes(bytes);
            return BitConverter.ToInt64(bytes, 0);
        }

        public static float GetFloat()
        {
            var bytes = new byte[4];
            RandomNumberGenerator.GetBytes(bytes);
            return BitConverter.ToSingle(bytes, 0);
        }

        public static double GetDouble()
        {
            var bytes = new byte[8];
            RandomNumberGenerator.GetBytes(bytes);
            return BitConverter.ToDouble(bytes, 0);
        }

        public static string GetAsciiString(int stringLength)
        {
            var bytes = new byte[stringLength];
            RandomNumberGenerator.GetBytes(bytes);
            string randomString = Encoding.ASCII.GetString(bytes);
            return randomString;
        }

        public static decimal GetDecimal()
        {
            return new decimal(GetInt32(), GetInt32(), GetInt32(), GetBool(), (byte) (GetByte()%28));
        }

        public static decimal GetDecimal(int maxDigits, int maxScale)
        {
            // Sometimes when testing values with SQL server, we need to restrict the number of digits (SQL's precision and scale).
            // The simplest way to make sure we generate a number with the right number of digits is to generate each one randomly as opposed to generating a random decimal which is often going to be out of range.
            var formatArray = BuildDecimalDigitArray(maxDigits, maxScale);
            string formatString = new string(formatArray);
            var formattedDecimal = decimal.Parse(formatString);
            return formattedDecimal;
        }

        public static DateTime GetDateTime()
        {
            int year = Math.Abs(GetInt16() % (9999 - 1753));
            int month = Math.Abs(GetByte() % 12);
            int day = Math.Abs(GetByte() % 28); // don't bother with the "complicated" days of the month
            int hour = Math.Abs(GetByte() % 24);
            int minute = Math.Abs(GetByte() % 60);
            int second = Math.Abs(GetByte() % 60);
            int millisecond = Math.Abs(GetByte() % 997);

            // For now, we'll constrain the dates to a range SQL Server can handle with the datetime datatype.  datetime2 should be able to handle the full range, but it's not all that common in practice since it's new.
            // SQL Server is also not precise down to the millisecond, it's precise to something like 3.3ms.  Hence, use the SQlDateTime type as a wrapper to handle the necessary rounding.
            var randomDate = new DateTime(1753 + year, 1 + month, 1 + day, hour, minute, second, millisecond);
            return new SqlDateTime(randomDate).Value;
        }

        public static DateTime GetDate()
        {
            return GetDateTime().Date;
        }

        public static bool GetBool()
        {
            return (GetByte() % 2) == 0;
        }

        public static T GetEnum<T>()
            where T : struct
        {
            T returnValue = default(T);
            Type enumType = typeof(T);
            if (enumType.IsEnum)
            {
                Array enumValues = Enum.GetValues(enumType);
                int randomInteger = Math.Abs(GetInt32());
                int randomIndex = randomInteger % enumValues.Length;
                returnValue = (T)enumValues.GetValue(randomIndex);
            }
            return returnValue;
        }

        private static char[] BuildDecimalDigitArray(int maxPrecision, int maxScale)
        {
            // Build the format spec right-to-left, because scale works right-to-left.  We reverse the string at the end.
            List<char> formatList = new List<char>();
            for (int i = 0; i < maxPrecision; i++)
            {
                if (i == maxScale && maxScale != 0)
                {
                    formatList.Add('.');
                }
                formatList.Add((GetByte()%10).ToString()[0]);
            }
            char[] formatArray = formatList.ToArray();
            Array.Reverse(formatArray);
            return formatArray;
        }
    }
}