using System.Security.Cryptography;
using System.Text;

namespace VirtualList.Helpers;

public static class PasswordHelper
{
    public static byte[] HashPassword(byte[] salt, string password) =>
        HMACSHA3_384.HashData(salt, Encoding.UTF8.GetBytes(password));
}
